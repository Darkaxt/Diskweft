using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Diagnostics;
using System.Security.AccessControl;
using System.Security.Principal;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
using System.Web.Script.Serialization;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;
using System.Net;
using System.Net.Sockets;
using System.Text;

class Stage1Tests
{
    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    static extern bool CreateDirectoryW(string path, IntPtr security);
    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    static extern SafeFileHandle CreateFileW(string path, uint access, uint share, IntPtr security, uint creation, uint flags, IntPtr template);
    static readonly List<string> evidence = new List<string>();
    static string fixture, output;
    static double partialDisplayMilliseconds;
    static Updater.Release ReadUpdate(string name, string url)
    {
        string json = new JavaScriptSerializer().Serialize(new { tag_name = "v9.0.0", assets = new[] { new {
            name, browser_download_url = url, size = 1, digest = "sha256:" + new string('0', 64)
        } } });
        var listener = new TcpListener(IPAddress.Loopback, 0); listener.Start();
        var endpoint = (IPEndPoint)listener.LocalEndpoint;
        Exception serverFailure = null;
        var server = new Thread(() => {
            try
            {
                using (var client = listener.AcceptTcpClient())
                using (var stream = client.GetStream())
                using (var reader = new StreamReader(stream, Encoding.ASCII, false, 1024, true))
                {
                    while (!string.IsNullOrEmpty(reader.ReadLine())) { }
                    byte[] bytes = Encoding.UTF8.GetBytes(json);
                    byte[] header = Encoding.ASCII.GetBytes("HTTP/1.1 200 OK\r\nContent-Type: application/json\r\nContent-Length: " + bytes.Length + "\r\nConnection: close\r\n\r\n");
                    stream.Write(header, 0, header.Length); stream.Write(bytes, 0, bytes.Length); stream.Flush();
                }
            }
            catch (Exception ex) { serverFailure = ex; }
        });
        string original = Updater.Feed;
        server.Start();
        try
        {
            Updater.Feed = "http://127.0.0.1:" + endpoint.Port + "/release";
            return Updater.Newer();
        }
        finally
        {
            Updater.Feed = original; listener.Stop(); server.Join();
            if (serverFailure != null) throw new Exception("Local updater fixture failed.", serverFailure);
        }
    }
    static void Check(bool condition, string message) { if (!condition) throw new Exception(message); }
    static string Prepare(string parent)
    {
        string path = Path.Combine(parent, "fixture");
        Directory.CreateDirectory(Path.Combine(path, "child\\nested"));
        Directory.CreateDirectory(Path.Combine(path, "empty"));
        File.WriteAllBytes(Path.Combine(path, "root.bin"), new byte[1000]);
        File.WriteAllBytes(Path.Combine(path, "child\\child.bin"), new byte[2000]);
        File.WriteAllBytes(Path.Combine(path, "child\\nested\\deep.bin"), new byte[4000]);
        File.WriteAllBytes(Path.Combine(path, "skip.tmp"), new byte[500]);
        File.WriteAllBytes(Path.Combine(path, "child\\nested\\zero.dat"), new byte[0]);
        return path;
    }
    static void Core()
    {
        Check(ReadUpdate("SpaceScan-9.0.0-setup.exe", "https://github.com/limburatorul/spacescan/releases/download/v9.0.0/SpaceScan-9.0.0-setup.exe") == null, "Updater accepted upstream identity.");
        Check(ReadUpdate("Diskweft-9.0.0-setup.exe", "https://github.com/limburatorul/spacescan/releases/download/v9.0.0/Diskweft-9.0.0-setup.exe") == null, "Updater accepted an asset outside the fork.");
        Check(ReadUpdate("Diskweft-9.0.0-setup.exe", "https://github.com/Darkaxt/Diskweft/releases/download/v9.0.0/Diskweft-9.0.0-setup.exe") != null, "Updater refused its own valid asset identity.");
        evidence.Add("Updater exercised against local HTTP responses: upstream/foreign assets refused and fork-owned identity accepted; no installer downloaded or run.");
        Exclude.Set(new string[0]);
        var session = new ScanSession(new[] { fixture }, false, false);
        var reached = new ManualResetEvent(false); var release = new ManualResetEvent(false);
        bool paused = false;
        ScanSession.Enumerated = (s, n) =>
        {
            if (n.Name != "child" || paused) return;
            paused = true; s.Publish(false, n, true); reached.Set(); release.WaitOne();
        };
        var worker = new Thread(() => { try { session.Run(); } finally { reached.Set(); } });
        worker.Start(); reached.WaitOne();
        try
        {
            Check(paused, "No real child enumeration checkpoint reached.");
            var partial = session.Drain();
            Check(partial != null && !partial.Terminal, "No partial batch while worker is still active.");
            Check(partial.Directories[fixture].Bytes == 3500, "Ancestor total omitted unfinished child's bytes.");
            Check(partial.Directories[Path.Combine(fixture, "child")].State == DirectoryState.Scanning, "Incomplete child marked complete.");
            Check(partial.Extensions[".bin"][0] == 3000, "Partial extension statistics incorrect.");
            Check(partial.Largest.Any(f => f.Bytes == 2000), "Partial largest files missing.");
            Check(session.Drain() == null, "Drained revisions replayed.");
            session.Cancel();
        }
        finally { release.Set(); worker.Join(); ScanSession.Enumerated = null; reached.Dispose(); release.Dispose(); }
        var terminal = session.Drain();
        Check(terminal.Terminal && terminal.Outcome == "stopped" && session.Root.Size == 3500, "Stop/final reconciliation double-counted or completed a cancelled run.");
        Check(History.Save(session.Root) == null, "Cancelled scan entered automatic history.");
        string stoppedPath = Path.Combine(output, "stopped.scan");
        Snapshot.Save(session.Root, stoppedPath);
        DateTime taken; string rootPath;
        var loaded = Snapshot.Load(stoppedPath, out taken, out rootPath);
        Check(loaded.Outcome == "stopped", "Snapshot lost partial provenance.");
        bool rejected = false;
        try { Comparer.Run(loaded, new Node(), 100); } catch (InvalidOperationException) { rejected = true; }
        Check(rejected, "Partial history accepted as complete comparison baseline.");
        evidence.Add("Real filesystem partial parent totals, statistics, cancellation, mailbox draining and partial-history provenance verified with event barriers.");

        var complete = new ScanSession(new[] { fixture }, false, false); complete.Run();
        Check(complete.Outcome == "complete" && complete.Root.Size == 7500 && complete.Root.Files == 5, "Restart/final exact totals failed.");
        Check(Stats.Ext[".bin"][0] == 7000 && Stats.Ext[".bin"][1] == 3, "Final extension totals failed.");
        Check(Stats.Top.Count == 5 && Stats.Top[0].Size == 4000, "Final largest files failed.");
        Exclude.Set(new[] { "*.tmp" });
        var excluded = new ScanSession(new[] { fixture }, false, false); excluded.Run();
        Check(excluded.Root.Size == 7000 && excluded.Root.Files == 4, "Name exclusion failed.");
        Exclude.Set(new[] { Path.Combine(fixture, "child") });
        var subtree = new ScanSession(new[] { fixture }, false, false); subtree.Run();
        Check(subtree.Root.Size == 1500 && subtree.Root.Files == 2, "Path exclusion failed.");
        Exclude.Set(new string[0]);
        string otherRoot = Path.Combine(output, "other-root"); Directory.CreateDirectory(otherRoot);
        var multiple = new ScanSession(new[] { fixture, otherRoot }, true, false); multiple.Run();
        Check(multiple.Root.Size == 7500 && multiple.Root.Files == 5 && multiple.Root.Dirs.Count == 2, "All-drives classic aggregation failed.");
        evidence.Add("Exact controlled totals, name/path exclusions and virtual all-drives classic aggregation verified.");
        string longPath = Path.Combine(output, "long");
        Check(CreateDirectoryW(Scanner.LongPath(longPath), IntPtr.Zero), "Could not create native long-path fixture root.");
        for (int i = 0; i < 6; i++)
        {
            longPath = Path.Combine(longPath, new string('d', 50));
            Check(CreateDirectoryW(Scanner.LongPath(longPath), IntPtr.Zero), "Could not create native long-path fixture component.");
        }
        using (var handle = CreateFileW(Scanner.LongPath(Path.Combine(longPath, "long.bin")), 0x40000000, 0, IntPtr.Zero, 1, 0, IntPtr.Zero))
        {
            Check(!handle.IsInvalid, "Could not create native long-path fixture file.");
            using (var stream = new FileStream(handle, FileAccess.Write)) stream.Write(new byte[321], 0, 321);
        }
        var longScan = new ScanSession(new[] { Path.Combine(output, "long") }, false, false); longScan.Run();
        Check(longScan.Outcome == "complete" && longScan.Root.Size == 321 && longScan.Root.Files == 1, "Long-path scan failed.");

        string deniedPath = Path.Combine(output, "restricted"); Directory.CreateDirectory(deniedPath);
        var originalAcl = Directory.GetAccessControl(deniedPath);
        var deniedAcl = new DirectorySecurity(); deniedAcl.SetSecurityDescriptorBinaryForm(originalAcl.GetSecurityDescriptorBinaryForm(), AccessControlSections.Access);
        using (var id = WindowsIdentity.GetCurrent()) deniedAcl.AddAccessRule(new FileSystemAccessRule(id.User, FileSystemRights.ListDirectory, AccessControlType.Deny));
        try
        {
            Directory.SetAccessControl(deniedPath, deniedAcl);
            var denied = new ScanSession(new[] { deniedPath }, false, false); denied.Run();
            Check(denied.Outcome == "incomplete" && denied.Root.Denied && denied.Root.State == DirectoryState.Inaccessible, "Denied directory was presented as a completed zero.");
        }
        finally
        {
            var restored = new DirectorySecurity();
            restored.SetSecurityDescriptorBinaryForm(originalAcl.GetSecurityDescriptorBinaryForm(), AccessControlSections.Access);
            Directory.SetAccessControl(deniedPath, restored);
        }
        string manyPath = Path.Combine(output, "many"); Directory.CreateDirectory(manyPath);
        for (int i = 0; i < 2100; i++) File.WriteAllBytes(Path.Combine(manyPath, i + ".bin"), new byte[0]);
        var bounded = new ScanSession(new[] { manyPath }, false, false); bounded.Run();
        var boundedBatch = bounded.Drain();
        Check(boundedBatch.Largest.Count == Stats.TopN && boundedBatch.Directories.Count == 1 && bounded.Root.Files == 2100, "Publication retained per-file queue history or exceeded largest-file cap.");
        var broken = new ScanSession(new[] { fixture }, false, false);
        ScanSession.Enumerated = (s, n) => { throw new IOException("diagnostic enumeration failure"); };
        try { broken.Run(); } finally { ScanSession.Enumerated = null; }
        Check(broken.Drain().Outcome == "failed" && broken.Root.Outcome == "failed", "Worker failure was treated as success.");
        evidence.Add("Long paths, actual fixture ACL denial (restored in finally), bounded top-file/mailbox state and failed terminal publication verified.");
        string network = Environment.GetEnvironmentVariable("DISKWEFT_NETWORK_FIXTURE");
        if (string.IsNullOrEmpty(network)) throw new Exception("An explicit read-only UNC test path is required.");
        var unc = new ScanSession(new[] { network }, false, false); unc.Run();
        Check(unc.Outcome == "complete" && unc.Root.Files > 0, "Real UNC scan did not complete with files.");
        evidence.Add("Read-only real UNC filesystem scan completed; private endpoint omitted from public evidence.");

        bool admin;
        using (var id = System.Security.Principal.WindowsIdentity.GetCurrent())
            admin = new System.Security.Principal.WindowsPrincipal(id).IsInRole(System.Security.Principal.WindowsBuiltInRole.Administrator);
        if (!admin)
        {
            var fallback = new ScanSession(new[] { Path.GetPathRoot(Environment.SystemDirectory) }, false, true);
            var reparseNames = new List<string>();
            Scanner.EnumWithCancellation(Path.GetPathRoot(Environment.SystemDirectory), (name, attr, size, modified) => {
                if ((attr & Scanner.REPARSE) != 0 && (attr & Scanner.DIR) != 0) reparseNames.Add(name);
            }, () => false);
            ScanSession.Enumerated = (s, n) => { if (n.Parent == null) s.Cancel(); };
            try { fallback.Run(); } finally { ScanSession.Enumerated = null; }
            Check(fallback.Mode.Contains("MFT unavailable") && fallback.Outcome == "stopped", "Real unprivileged MFT fallback/stop was not exercised.");
            Check(reparseNames.Count > 0 && !fallback.Root.Dirs.Any(n => reparseNames.Contains(n.Name)), "Existing system-root reparse directories were not skipped.");
            evidence.Add("Real denied raw-volume access fell back to a cancellable classic root enumeration without elevation.");
        }
        else throw new Exception("Run S1 acceptance unprivileged so real MFT-denial fallback is qualified safely.");
        Scanner.Cancel = false;
        string compatible = Path.Combine(output, "compatibility"); Directory.CreateDirectory(compatible);
        File.WriteAllBytes(Path.Combine(compatible, "copy-a.bin"), new byte[5000]);
        File.WriteAllBytes(Path.Combine(compatible, "copy-b.bin"), new byte[5000]);
        File.WriteAllBytes(Path.Combine(compatible, "different.bin"), Enumerable.Repeat((byte)17, 5000).ToArray());
        var compatibility = new ScanSession(new[] { compatible }, false, false); compatibility.Run();
        var hits = Finder.Run(compatibility.Root, new Query { Name = "copy", MinSize = 5000 });
        Check(hits.Count == 2, "Classic search regression.");
        var duplicates = Dupes.Find(hits, (done, total) => { });
        Check(duplicates.Count == 1 && duplicates[0].Count == 2, "Duplicate hashing regression.");
        string legacyPath = Path.Combine(output, "legacy.scan");
        using (var file = File.Create(legacyPath))
        using (var zip = new System.IO.Compression.GZipStream(file, System.IO.Compression.CompressionMode.Compress))
        using (var writer = new StreamWriter(zip))
        {
            writer.WriteLine("spacescan\u00011\u0001" + compatible + "\u0001" + DateTime.UtcNow.ToString("o"));
            writer.WriteLine("0\u00011000\u00011\u0001" + compatible);
        }
        var legacy = Snapshot.Load(legacyPath, out taken, out rootPath);
        Check(legacy.Outcome == "legacy-unknown" && History.Save(legacy) == null, "Legacy snapshot provenance was silently promoted.");
        Check(Comparer.Run(legacy, compatibility.Root, 100).Count == 1, "Explicit legacy snapshot comparison failed.");
        evidence.Add("Classic name/size search, real duplicate hashing and explicit legacy-snapshot comparison/provenance verified.");
    }

    [STAThread]
    public static int Main(string[] args)
    {
        output = Path.GetFullPath(args[0]);
        Directory.CreateDirectory(output);
        Environment.SetEnvironmentVariable("DISKWEFT_TEST_ROOT", Path.Combine(output, "profile"));
        Exception failure = null;
        try
        {
            fixture = Prepare(output);
            Core();
            var isolatedSentinels = new Dictionary<string, byte[]>();
            foreach (var relative in new[] { "ProtagonistLabs\\apps\\spacescan.json", "ProtagonistLabs\\SpaceScan\\exclude.txt", "SpaceScan\\exclude.txt", "Darkaxt\\Pathweft\\settings.json" })
            {
                string path = Path.Combine(Environment.GetEnvironmentVariable("DISKWEFT_TEST_ROOT"), relative);
                Directory.CreateDirectory(Path.GetDirectoryName(path));
                byte[] bytes = System.Text.Encoding.UTF8.GetBytes("preserve original " + relative);
                File.WriteAllBytes(path, bytes); isolatedSentinels[path] = bytes;
            }
            NativeUi();
            foreach (var kv in isolatedSentinels) Check(File.ReadAllBytes(kv.Key).SequenceEqual(kv.Value), "An upstream/Pathweft identity sentinel changed.");
            string card = Path.Combine(Ecosystem.Folder, "apps\\diskweft.json");
            Check(File.Exists(card) && File.ReadAllText(card).Contains("\"id\": \"diskweft\""), "Independent ecosystem card was not written.");
            Check(Ecosystem.Data.EndsWith("\\Darkaxt\\Diskweft", StringComparison.OrdinalIgnoreCase), "Profile is not independently owned.");
            evidence.Add("Isolated SpaceScan legacy/shared profiles, ecosystem card and Pathweft sentinels preserved; Diskweft card/profile independently owned.");
        }
        catch (Exception ex) { failure = ex; }
        File.WriteAllText(Path.Combine(output, "result.json"), new JavaScriptSerializer().Serialize(new {
            passed = failure == null, error = failure == null ? null : failure.ToString(), evidence, partialDisplayMilliseconds,
            coverage = "S1 classic workflows only. No progressive MFT, Windows theme/accent, performance or release acceptance."
        }));
        if (failure != null) { Console.Error.WriteLine(failure); return 1; }
        foreach (var item in evidence) Console.WriteLine(item);
        return 0;
    }

    static void NativeUi()
    {
        Exclude.Set(new string[0]);
        var app = new Application { ShutdownMode = ShutdownMode.OnMainWindowClose };
        var ui = new MainUI();
        ui.Window.ShowActivated = false; ui.Window.ShowInTaskbar = false; ui.Window.Left = -10000;
        ui.Window.Width = 1280; ui.Window.Height = 820;
        var release = new ManualResetEvent(false);
        var listingRelease = new ManualResetEvent(false);
        Exception failure = null;
        int phase = 0; bool paused = false, navigated = false;
        Row selected = null;
        long firstGeneration = 0;
        long publishedAt = 0;
        bool staleListing = false, closeRequested = false;
        bool scrollNavigated = false;
        Row scrollingSelection = null;
        double preservedOffset = 0;
        app.DispatcherUnhandledException += (s, e) => { failure = e.Exception; phase = -1; e.Handled = true; release.Set(); listingRelease.Set(); ui.Window.Close(); };
        ScanSession.Enumerated = (session, node) =>
        {
            if (node.Name != "child" || paused) return;
            paused = true; publishedAt = Stopwatch.GetTimestamp(); session.Publish(false, node, true); release.WaitOne();
        };
        ui.ScanApplied += (owner, batch) =>
        {
            if (phase == 8 && !scrollNavigated && !batch.Terminal && owner.DiagnosticRoot != null && owner.DiagnosticRoot.Files == 2100)
            {
                scrollNavigated = true;
                owner.DiagnosticSelect(Path.Combine(output, "many")); owner.DiagnosticKey(Key.Left); owner.DiagnosticKey(Key.Right);
                return;
            }
            if (phase == 7 && !batch.Terminal && owner.DiagnosticRoot != null && owner.DiagnosticRoot.Files > 0)
            {
                closeRequested = true; owner.Window.Close();
                Check(owner.DiagnosticWorkers > 0, "Close did not retain the original owned worker.");
                release.Set(); return;
            }
            if (phase != 0 || navigated || batch.Terminal || owner.DiagnosticRoot == null || owner.DiagnosticRoot.Size != 3500) return;
            navigated = true; firstGeneration = owner.DiagnosticGeneration;
            partialDisplayMilliseconds = 1000.0 * (Stopwatch.GetTimestamp() - publishedAt) / Stopwatch.Frequency;
            Check(partialDisplayMilliseconds <= 1000, "Useful partial results missed the one-second metadata-to-display criterion.");
            Check(owner.DiagnosticBusy, "UI was not busy during partial results.");
            selected = owner.DiagnosticSelect(Path.Combine(fixture, "child"));
            owner.DiagnosticKey(Key.Right);
            Check(selected.IsExpanded, "Real tree-key handler did not expand while scanning.");
            owner.DiagnosticKey(Key.Delete); // generated fixture only; busy guard must refuse
            Check(Directory.Exists(Path.Combine(fixture, "child")), "Destructive key was not refused during scan.");
        };
        ui.ListingApplied += (owner, row) =>
        {
            if (phase == 8 && row.Node.FullPath == Path.Combine(output, "many"))
            {
                owner.Window.Dispatcher.BeginInvoke(DispatcherPriority.Render, new Action(() => {
                    scrollingSelection = owner.DiagnosticScrollFiles(); preservedOffset = owner.DiagnosticScrollOffset;
                    Check(preservedOffset > 0, "Actual virtualized list did not scroll.");
                    phase = 9; ScanSession.Enumerated = null; release.Set();
                }));
                return;
            }
            if (phase != 0 || row != selected) return;
            owner.Window.Dispatcher.BeginInvoke(DispatcherPriority.Render, new Action(() =>
            {
                Check(object.ReferenceEquals(selected, owner.DiagnosticSelection), "Async live listing lost selection.");
                owner.DiagnosticCapture(output, "classic-live");
                phase = 1; ScanSession.Enumerated = null;
                owner.DiagnosticScan(fixture); // same Scan/Stop button event while busy
                release.Set();
            }));
        };
        ui.BecameIdle += owner =>
        {
            if (phase == 1)
            {
                Check(owner.DiagnosticRoot.Outcome == "stopped", "Stopped UI was reported completed.");
                Check(History.List(fixture).Count == 0, "Stopped native scan entered history.");
                owner.DiagnosticCapture(output, "classic-stopped");
                phase = 10; owner.DiagnosticSelect(Path.Combine(fixture, "child")); owner.DiagnosticRescan();
            }
            else if (phase == 10)
            {
                Check(owner.DiagnosticRoot.Outcome == "stopped" && History.List(fixture).Count == 0, "Completing one subtree falsely promoted a stopped full tree to complete history.");
                evidence.Add("Completing a subtree of a stopped tree retained partial provenance and did not enter complete automatic history.");
                phase = 2; owner.DiagnosticScan(fixture);
            }
            else if (phase == 2)
            {
                Check(owner.DiagnosticGeneration > firstGeneration && owner.DiagnosticRoot.Size == 7500 && owner.DiagnosticRoot.Files == 5, "Native restart totals/generation failed.");
                Check(History.List(fixture).Count == 1, "Completed native scan was not saved once.");
                phase = 11; owner.DiagnosticSearch("root");
            }
            else if (phase == 11)
            {
                Check(owner.DiagnosticDirectoryCacheCount > 0, "Native search did not resolve its directory ownership.");
                File.WriteAllBytes(Path.Combine(fixture, "child\\added.bin"), new byte[3000]);
                owner.DiagnosticSelect(Path.Combine(fixture, "child"));
                phase = 3; owner.DiagnosticRescan();
            }
            else if (phase == 3)
            {
                Check(owner.DiagnosticRoot.Size == 10500 && owner.DiagnosticRoot.Files == 6, "Partial rescan did not reconcile ancestors.");
                Check(owner.DiagnosticExtensions[".bin"][0] == 10000 && owner.DiagnosticExtensions[".bin"][1] == 4, "Rescan statistics duplicated or omitted old/new data.");
                Check(owner.DiagnosticDirectoryCacheCount == 0, "A new scan generation retained the preceding query directory cache.");
                owner.DiagnosticCapture(output, "classic-rescanned");
                evidence.Add("Native WPF Scan/Stop/restart, tree keyboard expansion, async listing, selection, busy deletion guard and subtree rescan/statistics/history verified.");
                phase = 4;
                MainUI.ListingObserved = (window, node) =>
                {
                    if (node.Name != "child" || staleListing) return;
                    staleListing = true;
                    window.Window.Dispatcher.BeginInvoke(new Action(() => {
                        phase = 5; window.DiagnosticScan(fixture); listingRelease.Set();
                    }));
                    listingRelease.WaitOne();
                };
                var reopen = owner.DiagnosticSelect(Path.Combine(fixture, "child"));
                if (reopen.IsExpanded) owner.DiagnosticKey(Key.Left);
                owner.DiagnosticKey(Key.Right);
            }
            else if (phase == 5)
            {
                Check(staleListing && owner.DiagnosticRoot.Size == 10500 && owner.DiagnosticRoot.Files == 6, "Obsolete listing contaminated the new scan.");
                MainUI.ListingObserved = null;
                evidence.Add("A deliberately delayed old-generation file listing was discarded after a real Scan-button restart.");
                phase = 6; owner.DiagnosticScanAll(new[] { fixture, Path.Combine(output, "other-root") });
            }
            else if (phase == 6)
            {
                owner.DiagnosticCapture(output, "classic-all-roots");
                Check(owner.DiagnosticRoot.Name == "This PC" && owner.DiagnosticRoot.Size == 10500 && owner.DiagnosticRoot.Dirs.Count == 2, "Native all-drives classic projection failed: name="+owner.DiagnosticRoot.Name+", bytes="+owner.DiagnosticRoot.Size+", roots="+owner.DiagnosticRoot.Dirs.Count);
                evidence.Add("The real Scan-all button projected controlled classic filesystem roots into the native This PC tree; host-wide scanning was not performed.");
                MainUI.DiagnosticDrivePaths = null;
                phase = 8; paused = false; release.Reset();
                ScanSession.Enumerated = (session, node) => {
                    if (node.Parent != null || paused) return;
                    paused = true; session.Publish(false, node, true); release.WaitOne();
                };
                owner.DiagnosticScan(Path.Combine(output, "many"));
            }
            else if (phase == 9)
            {
                Check(object.ReferenceEquals(scrollingSelection, owner.DiagnosticSelection), "Terminal listing replaced the selected file row.");
                Check(Math.Abs(owner.DiagnosticScrollOffset - preservedOffset) <= 1, "Terminal updates reset the user's scroll position.");
                owner.DiagnosticCapture(output, "classic-scrolled");
                evidence.Add("A real virtualized 2100-file listing preserved file-row selection and scroll across terminal reconciliation/relisting; display remained bounded.");
                phase = 7; paused = false; release.Reset();
                ScanSession.Enumerated = (session, node) => {
                    if (node.Name != "child" || paused) return;
                    paused = true; session.Publish(false, node, true); release.WaitOne();
                };
                owner.DiagnosticScan(fixture);
            }
        };
        ui.Window.ContentRendered += (s, e) => ui.DiagnosticScan(fixture);
        try { app.Run(ui.Window); }
        finally { release.Set(); listingRelease.Set(); ScanSession.Enumerated = null; ScanSession.Published = null; MainUI.ListingObserved = null; release.Dispose(); listingRelease.Dispose(); }
        if (failure != null) throw new Exception("Native UI acceptance failed.", failure);
        Check(phase == 7 && navigated && closeRequested && ui.DiagnosticWorkers == 0, "Native close did not await owned work/acceptance state.");
        evidence.Add("Native close requested cancellation and waited for owned scan/listing/history threads to exit.");
    }
}
