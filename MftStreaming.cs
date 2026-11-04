using System;
using System.Collections.Generic;
using System.IO;
using System.Diagnostics;
using System.Collections.Concurrent;
using System.Threading;
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
    readonly object rawReadSync = new object();
    readonly Dictionary<int, List<int>> pendingPathCandidates = new Dictionary<int, List<int>>();
    struct MftFile { public int Parent, Ext; public long Size; public bool Eligible; }
    sealed class ReadChunk { public byte[] Buffer; public int Length; }
    IEnumerable<ReadChunk> ReadChunks(SafeFileHandle handle, List<KeyValuePair<long,long>> runs, int cluster, int chunk)
    {
        // One serialized raw reader, two buffers, one pending chunk. The parser
        // remains the sole owner of records/tree/statistics. Its iterator owns
        // cancellation and joins the reader before the raw handle can close.
        using (var cancel = new CancellationTokenSource())
        using (var available = new BlockingCollection<byte[]>(2))
        using (var ready = new BlockingCollection<ReadChunk>(1))
        {
            available.Add(new byte[chunk]); available.Add(new byte[chunk]);
            Exception failure = null;
            var reader = new Thread(() => {
                try
                {
                    long records = 0;
                    foreach (var run in runs)
                    {
                        long offset = run.Key * cluster, left = run.Value * cluster;
                        while (left > 0 && records < count && !Scanner.Cancel)
                        {
                            byte[] buffer = available.Take(cancel.Token);
                            int length = (int)Math.Min(left, chunk);
#if DISKWEFT_TEST
                            long started = Stopwatch.GetTimestamp();
#endif
                            lock (rawReadSync) Read(handle, offset, buffer, length);
#if DISKWEFT_TEST
                            if (publication != null) publication.MftReadTicks += Stopwatch.GetTimestamp() - started;
#endif
                            ready.Add(new ReadChunk { Buffer = buffer, Length = length }, cancel.Token);
                            records += length / recSize; offset += length; left -= length;
                        }
                        if (records >= count || Scanner.Cancel) break;
                    }
                }
                catch (OperationCanceledException) { }
                catch (Exception ex) { failure = ex; }
                finally { ready.CompleteAdding(); }
            }) { IsBackground = true, Name = "Diskweft NTFS reader" };
            reader.Start();
            try
            {
                foreach (var part in ready.GetConsumingEnumerable())
                {
                    yield return part;
                    available.Add(part.Buffer);
                    if (Scanner.Cancel) break;
                }
                if (failure != null) throw new IOException("NTFS reader failed: " + failure.Message, failure);
            }
            finally { cancel.Cancel(); reader.Join(); }
        }
    }
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
#if DISKWEFT_TEST
        bool profile = publication != null && ScanSession.DetailedMftProfile;
        long started = profile ? Stopwatch.GetTimestamp() : 0;
#endif
        lastRecord = Math.Max(lastRecord, record);
        if (previous.Eligible)
        {
            progress.File(previous.Parent, extNames[previous.Ext], -previous.Size, -1);
            var previousDirectory = progress.ResolvedNode(previous.Parent);
            // A capturing predicate here makes the Framework compiler allocate
            // a display-class object at entry for every MFT record, including
            // the ordinary records that never enter this correction branch.
            for (int i = 0; i < Stats.Top.Count; i++)
            {
                var priorTop = Stats.Top[i];
                if (priorTop.Record != record || priorTop.Dir != previousDirectory) continue;
                if (name == null) name = priorTop.Name;
                if (modified == 0) modified = priorTop.Modified;
                Stats.Top.RemoveAt(i); break;
            }
        }
        if ((flags[record] & (InUse | IsDir | GoodName)) == (InUse | IsDir | GoodName))
        {
#if DISKWEFT_TEST
            long directoryStarted = profile ? Stopwatch.GetTimestamp() : 0;
#endif
            progress.Directory(record, parent[record], names[record], (flags[record] & Excluded) != 0);
#if DISKWEFT_TEST
            if (profile) publication.MftDirectoryTicks += Stopwatch.GetTimestamp() - directoryStarted;
#endif
        }
        var next = FileMeta(record);
        if (next.Eligible)
        {
#if DISKWEFT_TEST
            long fileStarted = profile ? Stopwatch.GetTimestamp() : 0;
#endif
            progress.File(next.Parent, extNames[next.Ext], next.Size, 1);
#if DISKWEFT_TEST
            if (profile) publication.MftFileTicks += Stopwatch.GetTimestamp() - fileStarted;
#endif
            var dir = progress.ResolvedNode(next.Parent);
            if (dir != null && name != null && Stats.WantsTop(next.Size))
                Stats.AddTop(new FileEntry { Dir = dir, Record = record, ParentRec = next.Parent, Name = name, Size = next.Size, Modified = modified });
        }
#if DISKWEFT_TEST
        if (profile) { publication.MftUpdateTicks += Stopwatch.GetTimestamp() - started; publication.MftUpdateCount++; }
#endif
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
#if DISKWEFT_TEST
        if (publication != null) publication.MftNameRereads++;
#endif
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
#if DISKWEFT_TEST
        long started = Stopwatch.GetTimestamp();
#endif
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
        var cached = Stats.Top.Where(f => f.Record >= FirstUserRecord && f.Record < count
            && progress.ResolvedNode(parent[f.Record]) == f.Dir).ToDictionary(f => f.Record);
        Stats.Top = new List<FileEntry>(progress.SeedTop);
        foreach (int record in candidates.OrderByDescending(r => size[r]).Take(Stats.TopN))
        {
            FileEntry entry;
            if (!cached.TryGetValue(record, out entry) || entry.Size != size[record]) entry = FileDetails(record, read);
            Stats.AddTop(entry);
        }
        Stats.Trim();
#if DISKWEFT_TEST
        if (publication != null) publication.MftFinalTopTicks += Stopwatch.GetTimestamp() - started;
#endif
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
        m.progress.Finish(m.PendingCandidateCount());
        session.Outcome = m.progress.Root.Outcome; session.Publish(true, m.progress.Root, true);
        return m.progress.Root;
    }
#endif
}
