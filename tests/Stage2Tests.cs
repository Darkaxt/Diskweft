using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Web.Script.Serialization;
using System.Threading;
using System.Diagnostics;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
using System.Security.Principal;

// Breaks caught: final-only MFT UI, unresolved/late-excluded ancestry leaking
// totals, duplicate extent accounting, and abandoned attempt results surviving fallback.
class Stage2Tests
{
    static string output;
    static double partialMilliseconds;
    static volatile int phase;
    static void SelectionTrace(MainUI ui, Row expected, string point)
    {
        var actual = ui.DiagnosticSelection as Row;
        File.AppendAllText(Path.Combine(output,"selection-events.jsonl"), new JavaScriptSerializer().Serialize(new {
            point, phase, same=object.ReferenceEquals(expected,actual), expected=expected==null?null:expected.Node.FullPath,
            actual=actual==null?null:actual.Node==null?actual.File.FullPath:actual.Node.FullPath,
            generation=ui.DiagnosticGeneration, revision=ui.DiagnosticRevision, workers=ui.DiagnosticWorkers
        })+Environment.NewLine);
    }
    static void Check(bool value, string message) { if (!value) throw new Exception(message); }
    static void Put(byte[] b, int at, ushort value) { Array.Copy(BitConverter.GetBytes(value), 0, b, at, 2); }
    static void Put(byte[] b, int at, uint value) { Array.Copy(BitConverter.GetBytes(value), 0, b, at, 4); }
    static void Put(byte[] b, int at, long value) { Array.Copy(BitConverter.GetBytes(value), 0, b, at, 8); }
    static byte[] Record(int id, int parent, string name, long size, bool directory, bool reparse = false, int baseRecord = 0)
    {
        var b = new byte[1024]; Put(b, 0, 0x454c4946u); Put(b, 4, (ushort)48); Put(b, 6, (ushort)3);
        Put(b, 20, (ushort)64); Put(b, 22, (ushort)(directory ? 3 : 1)); Put(b, 32, (long)baseRecord);
        int a = 64;
        if (name != null)
        {
            int length = (24 + 66 + name.Length * 2 + 7) / 8 * 8;
            Put(b, a, 0x30u); Put(b, a + 4, (uint)length); Put(b, a + 20, (ushort)24);
            int v = a + 24; Put(b, v, (long)parent); Put(b, v + 56, (directory ? 0x10000000u : 0u) | (reparse ? 0x400u : 0u));
            b[v + 64] = (byte)name.Length; b[v + 65] = 1; Array.Copy(Encoding.Unicode.GetBytes(name), 0, b, v + 66, name.Length * 2);
            a += length;
        }
        if (!directory)
        {
            Put(b, a, 0x80u); Put(b, a + 4, 72u); b[a + 8] = 1; Put(b, a + 48, size); a += 72;
        }
        Put(b, a, 0xffffffffu); Put(b, 48, (ushort)0xa55a); Put(b, 50, (ushort)0); Put(b, 52, (ushort)0);
        Put(b, 510, (ushort)0xa55a); Put(b, 1022, (ushort)0xa55a);
        return b;
    }
    static void Synthetic()
    {
        var replay = typeof(Mft).GetMethod("ReplayRecords", BindingFlags.Static | BindingFlags.NonPublic);
        Check(replay != null, "MFT record-reading path cannot yet publish live folder results.");
        var records = new[] {
            Record(5,5,"root",0,true), Record(18,17,"early.bin",400,false),
            Record(17,16,"child",0,true), Record(19,17,"ignore.tmp",900,false),
            Record(16,5,"keep",0,true), Record(20,17,"second.bin",600,false),
            Record(21,22,"secret.bin",8000,false), Record(22,5,"excluded",0,true),
            Record(23,24,"orphan.bin",10000,false), Record(25,5,"late.bin",100,false),
            Record(26,5,null,300,false,false,25)
        };
        int[] ids = {5,18,17,19,16,20,21,22,23,25,26};
        Exclude.Set(new[] { @"C:\excluded", @"C:\keep\child\ignore.tmp" });
        Stats.Reset(); Scanner.Cancel = false;
        Stats.AddExt(".prior",7777,1);
        Stats.AddTop(new FileEntry { Record=25,Dir=new Node{Name=@"D:\prior"},Name="previous.prior",Size=7777 });
        var session = new ScanSession(new[] { @"C:\" }, false, true);
        bool partial = false;
        Action<int> checkpoint = i => {
            var batch = session.Drain();
            if (i == 2) Check(batch == null || !batch.Directories.Values.Any(n => n.Name == "child"), "Unresolved folder path was invented.");
            if (i == 4)
            {
                Check(batch != null && batch.Directories[@"C:\"].Bytes == 400, "Out-of-order parent did not publish its already-read file before EOF.");
                Check(batch.Extensions[".bin"][0] == 400 && !batch.Extensions.ContainsKey(".tmp"), "Partial statistics leaked an excluded file.");
                Check(batch.Directories[@"C:\keep\child"].State != DirectoryState.Complete, "MFT folder claimed complete before record reading ended.");
                partial = true;
            }
            if (i == 10) Check(batch.Largest.Any(f => f.Name=="previous.prior" && f.Bytes==7777),"An extent on this volume removed another volume's same-numbered largest-file record.");
        };
        var root = (Node)replay.Invoke(null, new object[] { @"C:\", records, ids, session, checkpoint });
        Check(partial && root.Size == 1300 && root.Files == 3, "Final totals double-counted an extent or retained excluded/unresolved bytes.");
        Check(Stats.Ext[".bin"][0] == 1300 && Stats.Ext[".bin"][1] == 3 && !Stats.Ext.ContainsKey(".tmp"), "Final MFT extensions disagree with tree totals.");
        Check(Stats.Top.Count == 4 && Stats.Top.Sum(f => f.Size) == 9077 && Stats.Top.All(f => f.Dir != null), "Final bounded largest files include excluded/unresolved entries or stale sizes.");
        Check(root.State == DirectoryState.Incomplete, "Unresolved live records were silently treated as complete.");
        Exclude.Set(new string[0]);
    }
    [STAThread]
    static int Main(string[] args)
    {
        output = Path.GetFullPath(args[0]); Directory.CreateDirectory(output);
        try {
            Synthetic();
            if (args.Length > 1)
            {
                Check(new WindowsPrincipal(WindowsIdentity.GetCurrent()).IsInRole(WindowsBuiltInRole.Administrator), "Real NTFS checks require the approved elevated token.");
                Environment.SetEnvironmentVariable("DISKWEFT_TEST_ROOT", Path.Combine(output,"profile"));
                Native(args[1]);
            }
            File.WriteAllText(Path.Combine(output,"result.json"), new JavaScriptSerializer().Serialize(new {passed=true,realNtfs=args.Length>1,partialMilliseconds,scope="S2 reordered raw records / live publication / native NTFS workflow"})); Console.WriteLine("S2 record and requested native workflows passed."); return 0;
        }
        catch (Exception ex) { Console.Error.WriteLine(ex); File.WriteAllText(Path.Combine(output,"result.json"), new JavaScriptSerializer().Serialize(new {passed=false,error=ex.ToString()})); return 1; }
    }
    static void Native(string volume)
    {
        string classic = Path.Combine(Path.GetDirectoryName(typeof(Stage2Tests).Assembly.Location),"mixed-root"); Directory.CreateDirectory(classic);
        Check(!classic.StartsWith(volume,StringComparison.OrdinalIgnoreCase),"Mixed-root fixture must be on a separate volume, like real all-drives sources.");
        File.WriteAllBytes(Path.Combine(classic,"mixed.bin"), new byte[731]);
        var app = new Application { ShutdownMode = ShutdownMode.OnMainWindowClose };
        var ui = new MainUI();
        ui.Window.ShowActivated = false; ui.Window.ShowInTaskbar = false; ui.Window.Left = -10000;
        ui.Window.Width = 1280; ui.Window.Height = 820;
        var release = new ManualResetEvent(false);
        var fallbackRelease = new ManualResetEvent(false);
        Exception failure = null; bool paused = false, navigated = false, fallbackRemoved = false;
        Row selected = null; long observed = 0; long firstVolumeBytes = 0;
        string lastMode = "";
        Action fail = () => { phase = -1; release.Set(); fallbackRelease.Set(); ui.Window.Close(); };
        app.DispatcherUnhandledException += (s,e) => { failure=e.Exception; e.Handled=true; fail(); };
        Mft.RecordsObserved = (records, session) => {
            if (session == null || paused || phase == 4 || session.Root == null || session.Root.Files == 0) return;
            paused = true; observed = Stopwatch.GetTimestamp(); session.Publish(false,session.Root,true);
            release.WaitOne();
            if (phase == 2) throw new IOException("Controlled failure after actual NTFS publication");
        };
        ScanSession.Published = (session,node) => {
            if (phase == 2 && session.Mode.Contains("classic - MFT unavailable")) fallbackRelease.WaitOne();
        };
        ui.ScanApplied += (owner,batch) => {
            lastMode = batch.Mode;
            if (phase == 2 && !fallbackRemoved && batch.Attempt > 0)
            {
                Check(batch.RemovedPaths.Count > 0 && owner.DiagnosticRoot.Size == 0, "Classic fallback retained the abandoned MFT projection: bytes="+owner.DiagnosticRoot.Size+", removed="+batch.RemovedPaths.Count);
                fallbackRemoved = true; owner.DiagnosticScan(volume); ScanSession.Published=null; fallbackRelease.Set(); return;
            }
            if (batch.Terminal || !paused || navigated || owner.DiagnosticRoot == null || owner.DiagnosticRoot.Files == 0) return;
            if (phase == 0)
            {
                partialMilliseconds = 1000.0*(Stopwatch.GetTimestamp()-observed)/Stopwatch.Frequency;
                Check(partialMilliseconds <= 1000, "Real MFT partial publication exceeded one second after usable metadata.");
                var child = owner.DiagnosticRoot.Dirs.FirstOrDefault(n => !n.Denied);
                Check(child != null, "Real MFT publication contained no navigable directory.");
                navigated = true; selected=owner.DiagnosticSelect(child.FullPath); owner.DiagnosticKey(Key.Right);
                SelectionTrace(owner,selected,"after-expand");
                Check(selected.IsExpanded, "Native MFT tree did not expand while reading records.");
            }
            else if (phase == 1)
            {
                navigated = true; phase = 2; release.Set();
            }
            else if (phase == 3)
            {
                navigated = true; owner.DiagnosticScan(volume); release.Set();
            }
        };
        ui.ListingApplied += (owner,row) => {
            if (phase != 0 || row != selected) return;
            owner.Window.Dispatcher.BeginInvoke(DispatcherPriority.Render,new Action(() => {
                if (phase != 0) return; // terminal relisting may queue behind the next scan transition
                SelectionTrace(owner,selected,"after-listing");
                owner.DiagnosticCapture(output,"mft-live");
                Check(object.ReferenceEquals(selected,owner.DiagnosticSelection), "MFT async listing replaced the selected row.");
                release.Set();
            }));
        };
        ui.BecameIdle += owner => {
            if (phase == 0)
            {
                Check(paused && navigated && lastMode.Contains("MFT (progressive records)") && owner.DiagnosticRoot.Files > 0 && owner.DiagnosticRoot.Outcome != "failed", "Real NTFS scan failed before terminal reconciliation.");
                Check(object.ReferenceEquals(selected,owner.DiagnosticSelection), "MFT final reconciliation replaced directory selection.");
                firstVolumeBytes=owner.DiagnosticRoot.Size; owner.DiagnosticCapture(output,"mft-terminal");
                phase=1; paused=navigated=false; release.Reset(); owner.DiagnosticScan(volume);
            }
            else if (phase == 2)
            {
                Check(fallbackRemoved && owner.DiagnosticRoot.Outcome == "stopped", "Native fallback did not terminate as a stopped replacement attempt.");
                owner.DiagnosticCapture(output,"mft-fallback-stopped");
                phase=3; paused=navigated=false; release.Reset(); owner.DiagnosticScan(volume);
            }
            else if (phase == 3)
            {
                Check(owner.DiagnosticRoot.Outcome=="stopped", "Cancelled real MFT scan was reported complete.");
                phase=4; Mft.RecordsObserved=null; owner.DiagnosticScanAll(new[]{volume,classic});
            }
            else if (phase == 4)
            {
                var root=owner.DiagnosticRoot;
                owner.DiagnosticCapture(output,"mft-mixed-terminal");
                Check(root.Name=="This PC" && root.Dirs.Count==2 && root.Size==root.Dirs.Sum(n=>n.Size) && root.Files==root.Dirs.Sum(n=>n.Files),"Mixed MFT/classic all-drives totals double-counted or omitted a source: roots="+root.Dirs.Count+", bytes="+root.Size+", sum="+root.Dirs.Sum(n=>n.Size));
                Check(root.Dirs.Single(n=>n.FullPath==classic).Size==731 && firstVolumeBytes>0,"Mixed classic child did not reach the native terminal projection.");
                phase=5; owner.Window.Close();
            }
        };
        ui.Window.Loaded += delegate { ui.DiagnosticScan(volume); };
        try { app.Run(ui.Window); }
        finally { release.Set(); fallbackRelease.Set(); Mft.RecordsObserved=null; ScanSession.Published=null; MainUI.DiagnosticDrivePaths=null; release.Dispose(); fallbackRelease.Dispose(); }
        if (failure!=null) throw failure;
        Check(phase==5 && ui.DiagnosticWorkers==0,"Native NTFS workflow ended before its acceptance predicate or worker exit.");
    }
}
