using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;

enum DirectoryState { Pending, Scanning, Complete, Inaccessible, Stopped, Failed, Incomplete }

// Value records crossing the worker/dispatcher boundary. No live Node/List is shared.
sealed class DirectoryUpdate
{
    public string Path, ParentPath, Name, Outcome;
    public long Bytes, Files;
    public bool Denied;
    public DirectoryState State;
    public Dictionary<string, long[]> DirectExt;
}
sealed class FileUpdate
{
    public string Directory, Name;
    public long Bytes, Modified;
}
sealed class ScanBatch
{
    public Dictionary<string, DirectoryUpdate> Directories = new Dictionary<string, DirectoryUpdate>(StringComparer.OrdinalIgnoreCase);
    public Dictionary<string, long[]> Extensions = new Dictionary<string, long[]>();
    public List<FileUpdate> Largest = new List<FileUpdate>();
    public long Revision;
    public bool Terminal;
    public string Outcome, Error, Mode;
}

// One worker, one attempt stream, one coalesced mailbox. UI consumption never
// enumerates the scanner's tree. Pending history is bounded by changed folders.
sealed class ScanSession
{
    readonly object mailboxLock = new object();
    ScanBatch pending = new ScanBatch();
    readonly HashSet<Node> dirty = new HashSet<Node>();
    readonly Dictionary<Node, long[]> directDeltas = new Dictionary<Node, long[]>();
    readonly List<Node> known = new List<Node>();
    readonly Stopwatch clock = Stopwatch.StartNew();
    long lastPublish, revision;
    int entries;
    public readonly string[] Paths;
    public readonly bool VirtualRoot, PreferMft;
    public Node Root;
    public volatile bool CancellationRequested;
    public string Outcome = "scanning", Error = "", Mode = "classic";
    public Dictionary<string, long[]> SeedExtensions = new Dictionary<string, long[]>();
    public List<FileUpdate> SeedLargest = new List<FileUpdate>();
#if DISKWEFT_TEST
    // Deterministic test barrier: real filesystem work pauses only in test builds.
    internal static Action<ScanSession, Node> Published;
    internal static Action<ScanSession, Node> Enumerated;
#endif

    public ScanSession(string[] paths, bool virtualRoot, bool preferMft)
    { Paths = paths; VirtualRoot = virtualRoot; PreferMft = preferMft; }
    public void Cancel() { CancellationRequested = true; Scanner.Cancel = true; }
    public ScanBatch Drain()
    {
        lock (mailboxLock)
        {
            if (pending.Revision == 0) return null;
            var batch = pending;
            pending = new ScanBatch();
            return batch;
        }
    }
    public static Dictionary<string, long[]> CopyExtensions(Dictionary<string, long[]> source)
    { return source.ToDictionary(kv => kv.Key, kv => new[] { kv.Value[0], kv.Value[1] }); }
    public void Discover(Node node)
    { node.State = DirectoryState.Pending; known.Add(node); dirty.Add(node); }
    public void Changed(Node node) { dirty.Add(node); }
    public void FileFound(Node node, long bytes)
    {
        long[] delta;
        if (!directDeltas.TryGetValue(node, out delta)) directDeltas[node] = delta = new long[2];
        delta[0] += bytes; delta[1]++;
        dirty.Add(node);
        Checkpoint(node);
    }
    public void Checkpoint(Node node)
    {
        if ((++entries & 63) == 0 && clock.ElapsedMilliseconds - lastPublish >= 100) Publish(false, node);
    }
    public void Publish(bool terminal, Node current, bool force = false)
    {
        if (!terminal && !force && revision > 0 && clock.ElapsedMilliseconds - lastPublish < 100) return;
        foreach (var kv in directDeltas)
            for (var p = kv.Key.Parent; p != null; p = p.Parent)
            { p.Size += kv.Value[0]; p.Files += kv.Value[1]; dirty.Add(p); }
        directDeltas.Clear();
        var updates = dirty.Select(n => new DirectoryUpdate {
            Path = n.FullPath, Name = n.Name, ParentPath = n.Parent == null ? null : n.Parent.FullPath,
            Bytes = n.Size, Files = n.Files, Denied = n.Denied, State = n.State,
            Outcome = n.Outcome, DirectExt = CopyExtensions(n.DirectExt)
        }).ToList();
        dirty.Clear();
        var extensions = CopyExtensions(Stats.Ext);
        var largest = Stats.Top.OrderByDescending(f => f.Size).Take(Stats.TopN).Select(f => new FileUpdate {
            Directory = f.Dir.FullPath, Name = f.Name, Bytes = f.Size, Modified = f.Modified
        }).ToList();
        lock (mailboxLock)
        {
            foreach (var update in updates) pending.Directories[update.Path] = update;
            pending.Extensions = extensions; pending.Largest = largest;
            pending.Revision = ++revision; pending.Terminal = terminal;
            pending.Outcome = Outcome; pending.Error = Error; pending.Mode = Mode;
        }
        lastPublish = clock.ElapsedMilliseconds;
#if DISKWEFT_TEST
        var observer = Published;
        if (observer != null) observer(this, current);
#endif
    }
    void Import(Node node)
    {
        node.State = DirectoryState.Complete; known.Add(node); dirty.Add(node);
        foreach (var child in node.Dirs) Import(child);
    }
    public void Run()
    {
        Stats.Reset();
        foreach (var kv in SeedExtensions) Stats.AddExt(kv.Key, kv.Value[0], kv.Value[1]);
        foreach (var f in SeedLargest) Stats.AddTop(new FileEntry { Dir = new Node { Name = f.Directory }, Name = f.Name, Size = f.Bytes, Modified = f.Modified });
        Scanner.Cancel = CancellationRequested; Scanner.FilesSeen = Scanner.Total = 0; Scanner.DeniedCount = 0;
        Scanner.CollectExt = true;
        Root = VirtualRoot ? new Node { Name = "This PC", State = DirectoryState.Scanning } : null;
        if (Root != null) { known.Add(Root); dirty.Add(Root); }
        var modes = new List<string>();
        try
        {
            var paths = Paths ?? DriveInfo.GetDrives().Where(d => d.IsReady && d.DriveType == DriveType.Fixed).Select(d => d.Name).ToArray();
            if (paths.Length == 0) throw new IOException("No local drives found.");
            foreach (string path in paths)
            {
                if (CancellationRequested || Scanner.Cancel) break;
                var node = new Node { Name = path, Parent = Root };
                if (VirtualRoot) Root.Dirs.Add(node); else Root = node;
                Node fast = null;
                string mode = "classic";
                RetainLargest();
                if (PreferMft && Mft.IsNtfsRoot(path))
                {
                    try
                    {
                        fast = Mft.Scan(path);
                        if (!Scanner.Cancel && MainUI.NothingFound(fast) != "") throw new IOException("MFT found no files on a drive in use");
                        mode = "MFT (fast; final folder results)";
                    }
                    catch (Exception ex)
                    {
                        mode = "classic - MFT unavailable: " + ex.Message;
                        // MFT has not published folders in S1. Restore seed/previous
                        // roots' statistics, not abandoned attempt contributions.
                        RestorePublishedStatistics();
                        Scanner.FilesSeen = Scanner.Total = 0;
                    }
                }
                modes.Add(path + " " + mode); Mode = string.Join("; ", modes);
                if (fast != null)
                {
                    fast.Parent = VirtualRoot ? Root : null;
                    if (VirtualRoot) { Root.Dirs[Root.Dirs.Count - 1] = fast; Root.Size += fast.Size; Root.Files += fast.Files; }
                    else Root = fast;
                    Import(fast);
                }
                else
                {
                    Discover(node); node.State = DirectoryState.Scanning; Changed(node); Publish(false, node);
                    Scanner.ScanLive(node, path, this);
                }
                Publish(false, node);
            }
            Outcome = CancellationRequested || Scanner.Cancel ? "stopped" : Scanner.DeniedCount > 0 ? "incomplete" : "complete";
        }
        catch (Exception ex) { Outcome = "failed"; Error = ex.Message; }
        foreach (var node in known)
        {
            if (node.State == DirectoryState.Pending || node.State == DirectoryState.Scanning)
                node.State = Outcome == "complete" ? DirectoryState.Complete : Outcome == "failed" ? DirectoryState.Failed : Outcome == "incomplete" ? DirectoryState.Incomplete : DirectoryState.Stopped;
            node.Outcome = Outcome; dirty.Add(node);
        }
        if (Root != null) Root.Outcome = Outcome;
        Stats.Trim(); Publish(true, Root);
    }
    void RestorePublishedStatistics()
    {
        Stats.Reset();
        // Capture the last owned publication, even if the UI has drained it.
        foreach (var kv in SeedExtensions) Stats.AddExt(kv.Key, kv.Value[0], kv.Value[1]);
        foreach (var n in known)
            foreach (var kv in n.DirectExt) Stats.AddExt(kv.Key, kv.Value[0], kv.Value[1]);
        // Failed MFT candidates must not leak into the classic replacement.
        foreach (var f in retainedLargest) Stats.AddTop(new FileEntry { Dir = new Node { Name = f.Directory }, Name = f.Name, Size = f.Bytes, Modified = f.Modified });
    }
    readonly List<FileUpdate> retainedLargest = new List<FileUpdate>();
    public void RetainLargest()
    {
        retainedLargest.Clear();
        retainedLargest.AddRange(Stats.Top.Where(f => f.Dir != null).Select(f => new FileUpdate { Directory = f.Dir.FullPath, Name = f.Name, Bytes = f.Size, Modified = f.Modified }));
    }
}
