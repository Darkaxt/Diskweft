using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;

partial class MainUI
{
    ScanSession scanSession;
    long generation, lastRenderedRevision;
    int renderedAttempt;
    readonly DispatcherTimer liveResults = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(500) };
    bool rescanActive, closing;
    string priorOutcome = "complete";
    int ownedWorkers;
    readonly Dictionary<string, Node> displayNodes = new Dictionary<string, Node>(StringComparer.OrdinalIgnoreCase);
    readonly Dictionary<Node, Row> directoryRows = new Dictionary<Node, Row>();
    readonly Dictionary<Node, ListingRequest> listings = new Dictionary<Node, ListingRequest>();
    Dictionary<string, long[]> displayExtensions = new Dictionary<string, long[]>();
    List<FileEntry> displayLargest = new List<FileEntry>();
    HashSet<string> rescanPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
    HashSet<string> rescanDiscovered = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
    sealed class ListingRequest { public volatile bool Cancelled; }
#if DISKWEFT_TEST
    internal event Action<MainUI, ScanBatch> ScanApplied;
    internal event Action<MainUI> BecameIdle;
    internal event Action<MainUI, Row> ListingApplied;
    internal static Action<MainUI, Node> ListingObserved;
    internal static string[] DiagnosticDrivePaths;
#endif

    void RunOwned(Action work, Action completed, bool deepStack = false, bool ownsBusy = false)
    {
        ownedWorkers++;
        Exception failure = null;
        Thread worker = null;
        worker = new Thread(() =>
        {
            try { work(); }
            catch (Exception ex) { failure = ex; }
            finally
            {
                Window.Dispatcher.BeginInvoke(new Action(() =>
                {
                    worker.Join(); // work has returned; observe actual native-thread exit
                    ownedWorkers--;
                    if (completed != null) completed();
                    if (failure != null) { if (ownsBusy) SetBusy(false); status.Text = "Failed: " + failure.Message; }
#if DISKWEFT_TEST
                    if (ownedWorkers == 0 && !busy && BecameIdle != null) BecameIdle(this);
#endif
                    if (closing && ownedWorkers == 0) Window.Close();
                }));
            }
        }, deepStack ? 256 * 1024 * 1024 : 1024 * 1024) { IsBackground = true };
        worker.Start();
    }

    void ConfigureLifetime()
    {
        liveResults.Tick += delegate { ApplyScanUpdates(); };
        Window.Closing += (s, e) =>
        {
            if (ownedWorkers == 0) return;
            closing = true; e.Cancel = true;
            if (scanSession != null) scanSession.Cancel(); else Scanner.Cancel = true;
            foreach (var request in listings.Values) request.Cancelled = true;
            status.Text = "Stopping - waiting for owned filesystem work to finish";
        };
    }

    void StartOrStop()
    {
        if (busy) { if (scanSession != null) scanSession.Cancel(); else Scanner.Cancel = true; return; }
        string path = pathBox.Text.Trim();
        if (path.Length == 2 && path[1] == ':') path += "\\";
        if (path.Length == 0) { Error("Choose a folder or drive."); return; }
        StartScan(new[] { path }, false, true, false);
    }
    void ScanAllDrives()
    {
        if (busy) return;
        tabTree.IsChecked = true;
        // Drive readiness/metadata is queried by the owned worker, not the dispatcher.
#if DISKWEFT_TEST
        if (DiagnosticDrivePaths != null) { StartScan(DiagnosticDrivePaths, true, true, false); return; }
#endif
        StartScan(null, true, true, false);
    }
    void Rescan(List<Node> nodes)
    {
        if (busy || nodes.Count == 0) return;
        if (nodes.Any(n => n == root && n.Name == "This PC")) { ScanAllDrives(); return; }
        var unique = nodes.Where(n => !nodes.Any(other => other != n && IsUnder(n, other))).Distinct().ToList();
        StartScan(unique.Select(n => n.FullPath).ToArray(), unique.Count > 1, false, true);
    }

    void StartScan(string[] paths, bool virtualRoot, bool preferMft, bool partial)
    {
        foreach (var request in listings.Values) request.Cancelled = true;
        listings.Clear();
        dirCache.Clear(); // query results must resolve against this scan generation
        generation++; lastRenderedRevision = 0; renderedAttempt = 0;
        long owner = generation;
        rescanActive = partial;
        priorOutcome = partial && root != null ? root.Outcome : "complete";
        rescanDiscovered.Clear();
        rescanPaths = new HashSet<string>(paths ?? new string[0], StringComparer.OrdinalIgnoreCase);
        var seedExt = partial ? ScanSession.CopyExtensions(Stats.Ext) : new Dictionary<string, long[]>();
        var seedTop = partial ? Stats.Top.Where(f => !InsideRescan(f.Dir)).Select(f => new FileUpdate {
            Directory = f.Dir.FullPath, Name = f.Name, Bytes = f.Size, Modified = f.Modified
        }).ToList() : new List<FileUpdate>();
        if (partial)
        {
            foreach (var n in displayNodes.Values.Where(InsideRescan))
                foreach (var kv in n.DirectExt)
                {
                    long[] total;
                    if (seedExt.TryGetValue(kv.Key, out total)) { total[0] -= kv.Value[0]; total[1] -= kv.Value[1]; }
                }
        }
        else
        {
            root = null; rows.Clear(); displayNodes.Clear(); directoryRows.Clear();
            displayExtensions.Clear(); displayLargest.Clear();
            findSort.Clear(); dups.Clear(); cmpSort.Clear(); dirCache.Clear(); lastDiff = null;
            ownHistoryFile = null; dupSummary.Text = "";
        }
        emptyState.Visibility = Visibility.Collapsed;
        phase = 0; SetBusy(true);
        var session = new ScanSession(paths, virtualRoot, preferMft) { SeedExtensions = seedExt, SeedLargest = seedTop };
        scanSession = session;
        liveResults.Start();
        RunOwned(session.Run, () =>
        {
            if (owner != generation) return;
            if (scanSession == null) { SetBusy(false); return; }
            ApplyScanUpdates();
            CompleteScan(owner);
        }, true, true);
    }
    bool InsideRescan(Node node)
    {
        for (var n = node; n != null; n = n.Parent) if (rescanPaths.Contains(n.FullPath)) return true;
        return false;
    }
    void ApplyScanUpdates()
    {
        if (scanSession == null) return;
        var batch = scanSession.Drain();
        if (batch == null || batch.Revision <= lastRenderedRevision) return;
        lastRenderedRevision = batch.Revision;
        if (renderedAttempt != batch.Attempt || batch.RemovedPaths.Count > 0)
        {
            foreach (var request in listings.Values) request.Cancelled = true;
            listings.Clear(); renderedAttempt = batch.Attempt;
            var removed = new HashSet<Node>(displayNodes.Where(kv => batch.RemovedPaths.Contains(kv.Key)).Select(kv => kv.Value));
            foreach (var row in rows.Where(r => r.Node != null ? removed.Contains(r.Node) : removed.Contains(r.File.Dir)).ToList()) rows.Remove(row);
            foreach (var node in removed) { if (node.Parent != null) node.Parent.Dirs.Remove(node); directoryRows.Remove(node); }
            foreach (string path in batch.RemovedPaths) displayNodes.Remove(path);
            if (treeList.SelectedItem == null && rows.Count > 0) treeList.SelectedItem = rows[0];
        }
        var touchedParents = new HashSet<Node>();
        foreach (var update in batch.Directories.Values)
        {
            if (rescanActive && update.Path == "This PC") continue;
            Node node;
            if (!displayNodes.TryGetValue(update.Path, out node))
                displayNodes[update.Path] = node = new Node { Name = update.Name };
            if (rescanActive) rescanDiscovered.Add(update.Path);
        }
        foreach (var update in batch.Directories.Values)
        {
            if (rescanActive && update.Path == "This PC") continue;
            var node = displayNodes[update.Path];
            if (!rescanActive || !rescanPaths.Contains(update.Path))
            {
                Node parent = null;
                if (update.ParentPath != null && (!rescanActive || update.ParentPath != "This PC")) parent = displayNodes[update.ParentPath];
                if (node.Parent != parent)
                {
                    if (node.Parent != null) node.Parent.Dirs.Remove(node);
                    node.Parent = parent;
                    if (parent != null && !parent.Dirs.Contains(node)) parent.Dirs.Add(node);
                }
            }
            long deltaBytes = update.Bytes - node.Size, deltaFiles = update.Files - node.Files;
            if (!rescanActive || !rescanPaths.Contains(update.Path)) node.Name = update.Name;
            node.Size = update.Bytes; node.Files = update.Files;
            node.State = update.State; node.Denied = update.Denied; node.Outcome = update.Outcome;
            if (update.DirectExt != null) node.DirectExt = update.DirectExt;
            if (rescanActive && rescanPaths.Contains(update.Path))
                for (var p = node.Parent; p != null; p = p.Parent) { p.Size += deltaBytes; p.Files += deltaFiles; }
            if (node.Parent != null) touchedParents.Add(node.Parent);
            if (!rescanActive && node.Parent == null) root = node;
        }
        if (root != null && rows.Count == 0)
        {
            var top = DirectoryRow(root, 0); rows.Add(top); top.IsExpanded = true;
            treeList.SelectedItem = top;
        }
        foreach (var parent in touchedParents) InsertDiscoveredChildren(parent);
        if (root != null) InsertDiscoveredChildren(root);
        displayExtensions = batch.Extensions;
        displayLargest = batch.Largest.Where(f => displayNodes.ContainsKey(f.Directory)).Select(f => new FileEntry {
            Dir = displayNodes[f.Directory], Name = f.Name, Size = f.Bytes, Modified = f.Modified
        }).ToList();
        foreach (var row in rows) row.Changed();
        RefreshLists();
#if DISKWEFT_TEST
        if (ScanApplied != null) ScanApplied(this, batch);
#endif
    }
    Row DirectoryRow(Node node, int level)
    {
        Row row;
        if (!directoryRows.TryGetValue(node, out row)) directoryRows[node] = row = new Row { Node = node };
        row.Level = level;
        return row;
    }
    void InsertDiscoveredChildren(Node parent)
    {
        Row row;
        if (!directoryRows.TryGetValue(parent, out row) || !row.IsExpanded || !rows.Contains(row)) return;
        int at = rows.IndexOf(row) + 1;
        while (at < rows.Count && rows[at].Level > row.Level) at++;
        foreach (var child in parent.Dirs)
        {
            var childRow = DirectoryRow(child, row.Level + 1);
            if (!rows.Contains(childRow)) rows.Insert(at++, childRow);
        }
    }
    void CompleteScan(long owner)
    {
        var session = scanSession;
        SetBusy(false); scanSession = null;
        if (root == null) { status.Text = session.Outcome == "stopped" ? "STOPPED - no folders read" : "Scan failed: " + session.Error; return; }
        if (rescanActive)
        {
            foreach (var node in displayNodes.Values.Where(n => InsideRescan(n) && !rescanDiscovered.Contains(n.FullPath)).ToList())
            {
                // On a stopped scan, unvisited prior nodes are no longer asserted as current results.
                if (node.Parent != null) node.Parent.Dirs.Remove(node);
                Row row;
                if (directoryRows.TryGetValue(node, out row)) { if (row.IsExpanded) Collapse(row); rows.Remove(row); directoryRows.Remove(node); }
                displayNodes.Remove(node.FullPath);
            }
        }
        root.Outcome = rescanActive && !rescanPaths.Contains(root.FullPath) && priorOutcome != "complete" && session.Outcome == "complete"
            ? priorOutcome : session.Outcome;
        if (root.Outcome != "complete" && root.State == DirectoryState.Complete)
            root.State = root.Outcome == "incomplete" ? DirectoryState.Incomplete : root.Outcome == "failed" ? DirectoryState.Failed : DirectoryState.Stopped;
        // Only the dispatcher reads this transferred snapshot from now on.
        Stats.Ext = ScanSession.CopyExtensions(displayExtensions);
        Stats.Top = new List<FileEntry>(displayLargest); Stats.Trim();
        status.Text = string.Format("{0} - {1} in {2:N0} files - {3:0.0}s - {4}{5}",
            root.Outcome.ToUpperInvariant(), Brand.Fmt(root.Size), root.Files, sw.Elapsed.TotalSeconds,
            session.Mode, session.Error.Length == 0 ? (root.Outcome != session.Outcome ? " - scope completed; full scan remains partial" : "") : " - " + session.Error);
        if (!closing) foreach (var row in rows.ToList()) if (row.Node != null && row.IsExpanded) LoadFiles(row);
        foreach (var row in rows) row.Changed();
        ownHistoryFile = null;
        if (root.Outcome == "complete")
        {
            var frozen = CloneTree(root, null);
            string saved = null;
            RunOwned(() => saved = History.Save(frozen), () => { if (owner == generation) ownHistoryFile = saved; });
        }
    }
    static Node CloneTree(Node source, Node parent)
    {
        var node = new Node { Name = source.Name, Parent = parent, Size = source.Size, Files = source.Files,
            Denied = source.Denied, State = source.State, Outcome = source.Outcome,
            DirectExt = ScanSession.CopyExtensions(source.DirectExt) };
        foreach (var child in source.Dirs) node.Dirs.Add(CloneTree(child, node));
        return node;
    }

    void Expand(Row row)
    {
        if (!row.CanExpand || row.IsExpanded) return;
        row.IsExpanded = true; InsertDiscoveredChildren(row.Node); LoadFiles(row);
    }
    void LoadFiles(Row row)
    {
        ListingRequest previous;
        if (listings.TryGetValue(row.Node, out previous)) previous.Cancelled = true;
        var request = new ListingRequest(); listings[row.Node] = request;
        long owner = generation;
        var node = row.Node;
        string path = node.FullPath;
        row.ListingText = "listing files"; row.Changed();
        var files = new List<FileEntry>();
        long bytes = 0, count = 0;
        bool accessible = false;
        RunOwned(() =>
        {
            accessible = Scanner.EnumWithCancellation(path, (name, attr, size, modified) =>
            {
                if ((attr & Scanner.DIR) != 0 || Exclude.Match(name, Scanner.Join(path, name))) return;
                bytes += size; count++;
                files.Add(new FileEntry { Name = name, Size = size, Modified = modified });
                if (files.Count >= MaxFilesShown * 2) files = files.OrderByDescending(f => f.Size).Take(MaxFilesShown).ToList();
            }, () => request.Cancelled);
            files = files.OrderByDescending(f => f.Size).Take(MaxFilesShown).ToList();
            if (count > files.Count) files.Add(new FileEntry { Summary = true, Size = bytes - files.Sum(f => f.Size), Name = "... " + (count - files.Count).ToString("N0") + " smaller files" });
#if DISKWEFT_TEST
            if (ListingObserved != null) ListingObserved(this, node);
#endif
        }, () =>
        {
            ListingRequest current;
            if (request.Cancelled || owner != generation || !row.IsExpanded || !listings.TryGetValue(node, out current) || current != request) return;
            listings.Remove(node);
            foreach (var f in files) f.Dir = node;
            row.ListingText = !accessible ? "files inaccessible" : busy ? "files listed; scan still running" : "";
            var existing = rows.Where(r => r.File != null && r.File.Dir == node)
                .ToDictionary(r => r.File.Summary ? "\0summary" : r.File.Name, StringComparer.OrdinalIgnoreCase);
            var incoming = new HashSet<string>(files.Select(f => f.Summary ? "\0summary" : f.Name), StringComparer.OrdinalIgnoreCase);
            foreach (var old in existing.Where(kv => !incoming.Contains(kv.Key)).Select(kv => kv.Value).ToList()) rows.Remove(old);
            int at = rows.IndexOf(row) + 1;
            if (at == 0) return;
            while (at < rows.Count && rows[at].Level > row.Level) at++;
            foreach (var f in files)
            {
                Row fileRow;
                if (existing.TryGetValue(f.Summary ? "\0summary" : f.Name, out fileRow)) { fileRow.File = f; fileRow.Changed(); }
                else rows.Insert(at++, new Row { File = f, Level = row.Level + 1 });
            }
            row.Changed();
#if DISKWEFT_TEST
            if (ListingApplied != null) ListingApplied(this, row);
#endif
        });
    }
    void Collapse(Row row)
    {
        int i = rows.IndexOf(row);
        while (i + 1 < rows.Count && rows[i + 1].Level > row.Level)
        {
            var child = rows[i + 1];
            if (child.Node != null) { CancelListing(child.Node); child.IsExpanded = false; }
            rows.RemoveAt(i + 1);
        }
        CancelListing(row.Node); row.IsExpanded = false; row.ListingText = "";
    }
    void CancelListing(Node node)
    {
        ListingRequest request;
        if (listings.TryGetValue(node, out request)) request.Cancelled = true;
        listings.Remove(node);
    }
}
