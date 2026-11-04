using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

// Only directory state is retained here. Compact per-record metadata remains
// owned by Mft. A path becomes visible only after its complete ancestry resolves.
sealed class MftProgress
{
    sealed class Slot
    {
        public Node Node = new Node { State = DirectoryState.Pending, Outcome = "scanning" };
        public readonly List<int> Children = new List<int>();
        public int Parent;
        public bool Known, Resolved, Excluded;
    }
    readonly Dictionary<int, Slot> slots = new Dictionary<int, Slot>();
    readonly Dictionary<Node, long[]> deltas = new Dictionary<Node, long[]>();
    readonly ScanSession session;
    readonly Action<int> resolved;
    readonly Stopwatch clock = Stopwatch.StartNew();
    long lastPublish;
    int lastSlotId = -1;
    Slot lastSlot;
    Node deltaNode;
    long[] deltaValue;
    bool usefulMetadata, usefulPublished;
    public readonly Node Root;
    public readonly List<FileEntry> SeedTop;
    public long UnresolvedFiles { get; private set; }

    public MftProgress(Node root, ScanSession session, Action<int> resolved)
    {
        this.session = session; this.resolved = resolved; Root = root;
        SeedTop = new List<FileEntry>(Stats.Top);
        root.State = DirectoryState.Scanning; root.Outcome = "scanning";
        slots[5] = new Slot { Known = true, Resolved = true, Node = root };
        if (session != null) { session.Discover(root); root.State = DirectoryState.Scanning; }
    }
    Slot Get(int id)
    {
        if (lastSlot != null && lastSlotId == id) return lastSlot;
        Slot slot;
        if (!slots.TryGetValue(id, out slot)) slots[id] = slot = new Slot();
        lastSlotId = id; lastSlot = slot;
        return slot;
    }
    Node NodeOf(int id, Slot slot) { return id == 5 ? Root : slot.Node; }
    public Node ResolvedNode(int id)
    {
        Slot slot;
        if (lastSlot != null && lastSlotId == id) slot = lastSlot;
        else if (!slots.TryGetValue(id, out slot)) return null;
        return slot.Resolved && !slot.Excluded ? NodeOf(id, slot) : null;
    }
    public bool IsExcluded(int id) { return (id != 5 && id < 16) || Get(id).Excluded; }
    public bool FileAllowed(int parent, string candidateName)
    {
        if (parent != 5 && parent < 16) return false;
        var slot = Get(parent);
        if (slot.Excluded) return false;
        return candidateName == null || (slot.Resolved && !Exclude.MatchPath(Scanner.Join(NodeOf(parent, slot).FullPath, candidateName)));
    }
    static void AddDirect(Node node, string ext, long bytes, long count)
    {
        long[] value;
        // The last pair is the amount already included in global statistics.
        // Only scanner-owned MFT arrays have four cells; publication copies the
        // public [bytes,count] pair at terminal handoff.
        if (!node.DirectExt.TryGetValue(ext, out value)) node.DirectExt[ext] = value = new long[4];
        value[0] += bytes; value[1] += count;
        if (value[1] == 0 && value[3] == 0) node.DirectExt.Remove(ext);
    }
    static void AddGlobal(string ext, long bytes, long count)
    {
        Stats.AddExt(ext, bytes, count);
        if (Stats.Ext[ext][1] == 0) Stats.Ext.Remove(ext);
    }
    void Delta(Node node, long bytes, long files)
    {
        if (deltaNode != node)
        {
            deltaNode = node;
            if (!deltas.TryGetValue(node, out deltaValue)) deltas[node] = deltaValue = new long[2];
            if (session != null) session.Changed(node);
        }
        deltaValue[0] += bytes; deltaValue[1] += files;
    }
    public void File(int parent, string ext, long bytes, long count)
    {
        var slot = Get(parent); var node = NodeOf(parent, slot);
        node.Size += bytes; node.Files += count; AddDirect(node, ext, bytes, count);
        if (slot.Resolved && !slot.Excluded) { usefulMetadata |= count > 0; Delta(node, bytes, count); }
    }
    public void Directory(int id, int parent, string name, bool excluded)
    {
        if (id < 16) return;
        var slot = Get(id);
        if (slot.Resolved && (excluded || slot.Parent != parent || slot.Node.Name != name)) Prune(id);
        if (!slot.Known || slot.Parent != parent) Get(parent).Children.Add(id);
        slot.Known = true; slot.Parent = parent; slot.Node.Name = name;
        slot.Excluded = slot.Excluded || excluded;
        Resolve(id);
    }
    void Resolve(int id)
    {
        var slot = Get(id);
        if (!slot.Known || slot.Resolved || slot.Excluded) return;
        var parent = ResolvedNode(slot.Parent);
        if (parent == null) { if (Get(slot.Parent).Excluded) ExcludeBranch(id); return; }
        slot.Node.Parent = parent;
        if (Exclude.Any && Exclude.Match(slot.Node.Name, slot.Node.FullPath)) { slot.Node.Parent = null; ExcludeBranch(id); return; }
        slot.Resolved = true; slot.Node.State = DirectoryState.Scanning;
        parent.Dirs.Add(slot.Node);
        if (session != null) { session.Discover(slot.Node); slot.Node.State = DirectoryState.Scanning; session.Changed(parent); }
        PublishDirect(slot.Node);
        usefulMetadata |= slot.Node.Files > 0;
        Delta(slot.Node, slot.Node.Size, slot.Node.Files);
        if (resolved != null) resolved(id);
        foreach (int child in slot.Children) Resolve(child);
    }
    void ExcludeBranch(int id)
    {
        var slot = Get(id); slot.Excluded = true;
        foreach (int child in slot.Children) ExcludeBranch(child);
    }
    void Prune(int id)
    {
        Flush();
        var slot = Get(id); var node = slot.Node;
        for (var p = node.Parent; p != null; p = p.Parent) { p.Size -= node.Size; p.Files -= node.Files; if (session != null) session.Changed(p); }
        if (node.Parent != null) node.Parent.Dirs.Remove(node);
        var removed = new List<Node>();
        PruneBranch(id, removed);
        Stats.Top.RemoveAll(f => removed.Contains(f.Dir));
        if (session != null) session.Forget(removed);
        node.Parent = null;
    }
    void PruneBranch(int id, List<Node> removed)
    {
        var slot = Get(id);
        if (slot.Resolved)
        {
            foreach (var kv in slot.Node.DirectExt) AddGlobal(kv.Key, -kv.Value[0], -kv.Value[1]);
            removed.Add(slot.Node); slot.Resolved = false;
        }
        slot.Excluded = true;
        foreach (int child in slot.Children) PruneBranch(child, removed);
    }
    void Flush()
    {
#if DISKWEFT_TEST
        long started = Stopwatch.GetTimestamp();
#endif
        foreach (var kv in deltas)
        {
            PublishDirect(kv.Key);
            for (var p = kv.Key.Parent; p != null; p = p.Parent)
            { p.Size += kv.Value[0]; p.Files += kv.Value[1]; if (session != null) session.Changed(p); }
        }
        deltas.Clear();
        deltaNode = null; deltaValue = null;
#if DISKWEFT_TEST
        if (session != null) session.MftFlushTicks += Stopwatch.GetTimestamp() - started;
#endif
    }
    static void PublishDirect(Node node)
    {
        List<string> empty = null;
        foreach (var kv in node.DirectExt)
        {
            var value = kv.Value;
            long bytes = value[0] - value[2], files = value[1] - value[3];
            if (bytes != 0 || files != 0) AddGlobal(kv.Key, bytes, files);
            value[2] = value[0]; value[3] = value[1];
            if (value[1] == 0) { if (empty == null) empty = new List<string>(); empty.Add(kv.Key); }
        }
        if (empty != null) foreach (string ext in empty) node.DirectExt.Remove(ext);
    }
    public void Checkpoint(bool force)
    {
        if (!force && (usefulPublished || !usefulMetadata) && clock.ElapsedMilliseconds - lastPublish < 500) return;
        Flush();
        if (session != null) session.Publish(false, Root, force || (!usefulPublished && usefulMetadata));
        usefulPublished |= usefulMetadata;
        lastPublish = clock.ElapsedMilliseconds;
    }
    public void Finish(long pendingCandidates = 0)
    {
        Flush(); UnresolvedFiles = pendingCandidates + slots.Where(kv => !kv.Value.Resolved && !kv.Value.Excluded).Sum(kv => NodeOf(kv.Key, kv.Value).Files);
        foreach (var kv in slots.Where(kv => kv.Value.Resolved && !kv.Value.Excluded))
        {
            var node = NodeOf(kv.Key, kv.Value);
            node.State = Scanner.Cancel ? DirectoryState.Stopped : UnresolvedFiles > 0 ? DirectoryState.Incomplete : DirectoryState.Complete;
            node.Outcome = Scanner.Cancel ? "stopped" : UnresolvedFiles > 0 ? "incomplete" : "complete";
            if (session != null) session.Changed(node);
        }
        // ScanSession publishes the single terminal reconciliation immediately.
        // Avoid a full nonterminal copy of these same final directory values.
    }
}
