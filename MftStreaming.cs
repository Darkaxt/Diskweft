using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Win32.SafeHandles;

partial class Mft
{
    ScanSession publication;
    Node suppliedRoot;
    MftProgress progress;
    int[] nameSource;
    int lastRecord;
    Func<int, byte[]> recordReader;
    readonly Dictionary<int, List<int>> pendingPathCandidates = new Dictionary<int, List<int>>();
    struct MftFile { public int Parent, Ext; public long Size; public bool Eligible; }
    void StartProgress(string root)
    {
        nameSource = new int[count];
        progress = new MftProgress(suppliedRoot ?? new Node { Name = root }, publication, ResolvePathCandidates);
    }
    MftFile FileMeta(int record)
    {
        return new MftFile { Parent = parent[record], Ext = extId[record], Size = size[record],
            Eligible = record >= FirstUserRecord && (flags[record] & (InUse | IsDir | GoodName | Excluded | HasData | PendingPath)) == (InUse | GoodName | HasData)
                && progress.FileAllowed(parent[record], null) };
    }
    void AddPathCandidate(int record, string name)
    {
        var directory = progress.ResolvedNode(parent[record]);
        if (progress.IsExcluded(parent[record]) || (directory != null && Exclude.MatchPath(Scanner.Join(directory.FullPath, name)))) { flags[record] |= Excluded; return; }
        if (directory != null) return;
        flags[record] |= PendingPath;
        List<int> pending;
        if (!pendingPathCandidates.TryGetValue(parent[record], out pending)) pendingPathCandidates[parent[record]] = pending = new List<int>();
        pending.Add(record);
    }
    void ResolvePathCandidates(int directory)
    {
        List<int> pending;
        if (!pendingPathCandidates.TryGetValue(directory, out pending)) return;
        pendingPathCandidates.Remove(directory);
        foreach (int record in pending)
        {
            string name = FileDetails(record, recordReader).Name;
            flags[record] &= unchecked((byte)~PendingPath);
            if (Exclude.MatchPath(Scanner.Join(progress.ResolvedNode(directory).FullPath, name))) flags[record] |= Excluded;
            var file = FileMeta(record);
            if (file.Eligible) progress.File(file.Parent, extNames[file.Ext], file.Size, 1);
        }
    }
    long PendingCandidateCount() { return pendingPathCandidates.Where(kv => !progress.IsExcluded(kv.Key)).Sum(kv => (long)kv.Value.Count); }
    void UpdateProgress(int record, MftFile previous, string name, long modified)
    {
        lastRecord = Math.Max(lastRecord, record);
        if (previous.Eligible)
        {
            progress.File(previous.Parent, extNames[previous.Ext], -previous.Size, -1);
            var previousDirectory = progress.ResolvedNode(previous.Parent);
            var priorTop = Stats.Top.FirstOrDefault(f => f.Record == record && f.Dir == previousDirectory);
            if (priorTop != null) { if (name == null) name = priorTop.Name; if (modified == 0) modified = priorTop.Modified; Stats.Top.Remove(priorTop); }
        }
        if ((flags[record] & (InUse | IsDir | GoodName)) == (InUse | IsDir | GoodName))
            progress.Directory(record, parent[record], names[record], (flags[record] & Excluded) != 0);
        var next = FileMeta(record);
        if (next.Eligible)
        {
            progress.File(next.Parent, extNames[next.Ext], next.Size, 1);
            var dir = progress.ResolvedNode(next.Parent);
            if (dir != null && name != null && Stats.WantsTop(next.Size))
                Stats.AddTop(new FileEntry { Dir = dir, Record = record, ParentRec = next.Parent, Name = name, Size = next.Size, Modified = modified });
        }
    }
    byte[] ReadLogicalRecord(SafeFileHandle handle, List<KeyValuePair<long,long>> runs, int cluster, int record)
    {
        long logical = (long)record * recSize;
        var result = new byte[recSize]; int copied = 0;
        foreach (var run in runs)
        {
            long length = run.Value * cluster;
            if (logical >= length) { logical -= length; continue; }
            int bytes = (int)Math.Min(recSize - copied, length - logical);
            var part = new byte[bytes]; Read(handle, run.Key * cluster + logical, part, bytes);
            Array.Copy(part, 0, result, copied, bytes); copied += bytes; logical = 0;
            if (copied == recSize) return result;
        }
        throw new IOException("MFT record outside the validated run map");
    }
    FileEntry FileDetails(int record, Func<int,byte[]> read)
    {
        byte[] b = read(nameSource[record]);
        if (!Fixup(b, 0, recSize)) throw new IOException("MFT largest-file name record changed while reading");
        string name = null; long modified = 0;
        for (int a = BitConverter.ToUInt16(b, 20); a + 24 <= recSize; )
        {
            uint type = BitConverter.ToUInt32(b, a); int length = BitConverter.ToInt32(b, a + 4);
            if (type == 0xffffffff || length < 24 || a + length > recSize) break;
            int v = a + BitConverter.ToUInt16(b, a + 20);
            if (b[a + 8] == 0 && type == 0x30 && b[v + 65] != 2) name = NameOf(b, v);
            if (b[a + 8] == 0 && type == 0x10) modified = BitConverter.ToInt64(b, v + 8);
            a += length;
        }
        if (name == null) throw new IOException("MFT largest-file name was unavailable after record resolution");
        return new FileEntry { Dir = progress.ResolvedNode(parent[record]), Record = record, ParentRec = parent[record], Name = name, Size = size[record], Modified = modified };
    }
    void FinalTop(Func<int,byte[]> read)
    {
        // Re-select from compact metadata after exclusions/ancestry settle. Only
        // bounded winning names are re-read; no retained filename per scanned file.
        var candidates = new List<int>(); long minimum = -1;
        for (int record = FirstUserRecord; record <= lastRecord; record++)
        {
            if (size[record] < minimum || !FileMeta(record).Eligible || progress.ResolvedNode(parent[record]) == null) continue;
            candidates.Add(record);
            if (candidates.Count >= 2 * Stats.TopN)
            {
                candidates = candidates.OrderByDescending(r => size[r]).Take(Stats.TopN).ToList();
                minimum = size[candidates[candidates.Count - 1]];
            }
        }
        Stats.Top = new List<FileEntry>(progress.SeedTop);
        foreach (int record in candidates.OrderByDescending(r => size[r]).Take(Stats.TopN)) Stats.AddTop(FileDetails(record, read));
        Stats.Trim();
    }
#if DISKWEFT_TEST
    internal static Action<long, ScanSession> RecordsObserved;
    internal static Node ReplayRecords(string root, byte[][] records, int[] ids, ScanSession session, Action<int> checkpoint)
    {
        var m = new Mft { count = ids.Max() + 32, recSize = 1024, publication = session };
        m.parent = new int[m.count]; m.extId = new int[m.count]; m.size = new long[m.count]; m.flags = new byte[m.count]; m.names = new string[m.count];
        m.recordReader = id => (byte[])records[Array.IndexOf(ids,id)].Clone();
        m.StartProgress(root); session.Root = m.progress.Root; session.Mode = "MFT - reading records; unresolved paths withheld";
        for (int i = 0; i < records.Length && !Scanner.Cancel; i++)
        {
            m.ParseRecord((byte[])records[i].Clone(), 0, ids[i]); m.progress.Checkpoint(true);
            if (checkpoint != null) checkpoint(i);
        }
        if (!Scanner.Cancel) m.FinalTop(m.recordReader);
        m.progress.Finish(m.PendingCandidateCount()); return m.progress.Root;
    }
#endif
}
