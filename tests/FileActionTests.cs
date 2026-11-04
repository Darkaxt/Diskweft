using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Threading;
using System.Runtime.InteropServices;
using System.Web.Script.Serialization;

static class FileActionTests
{
    [DllImport("user32.dll",SetLastError=true)] static extern bool OpenClipboard(IntPtr owner);
    [DllImport("user32.dll")] static extern bool CloseClipboard();
    [StructLayout(LayoutKind.Sequential)] struct RecycleInfo { public int Size; public long Bytes,Items; }
    [DllImport("shell32.dll",CharSet=CharSet.Unicode)] static extern int SHQueryRecycleBinW(string root,ref RecycleInfo info);
    static void Check(bool value,string message){if(!value)throw new Exception(message);}
    public static void Run(string output,string[] args)
    {
        Check(Environment.GetEnvironmentVariable("DISKWEFT_SANDBOX")=="1","File-action tests are disposable-guest only.");
        string fixture=Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"Tools","Diskweft-file-actions-"+Guid.NewGuid().ToString("N"));
        string victim=Path.Combine(fixture,"recycle-ș測試"),sibling=Path.Combine(fixture,"keep");
        Directory.CreateDirectory(victim);Directory.CreateDirectory(sibling);
        File.WriteAllBytes(Path.Combine(victim,"one.bin"),new byte[1234]);
        File.WriteAllBytes(Path.Combine(sibling,"two.bin"),new byte[42]);
        var app=new Application();var ui=new MainUI();Exception failure=null;
        bool ran=false,dispatcherInShell=false,sta=false,clipboardHandled=false;int phase=0;
        bool close=args.Contains("--close"),permanent=args.Contains("--permanent"),baseline=args.Contains("--baseline"),closeWaited=false,busyGuard=false;
        bool error=args.Contains("--error"),errorOnDispatcher=false;string nativeError=null;
        MainUI.FileActionErrorObserved=message=>{nativeError=message;errorOnDispatcher=ui.Window.Dispatcher.CheckAccess();};
        var binBefore=new RecycleInfo{Size=Marshal.SizeOf(typeof(RecycleInfo))};
        Check(SHQueryRecycleBinW(Path.GetPathRoot(fixture),ref binBefore)==0,"Guest native Recycle Bin query failed.");
        var entered=new ManualResetEvent(false);var release=new ManualResetEvent(false);
        var held=new ManualResetEvent(false);var unlocked=new ManualResetEvent(false);
        Thread clipboardHolder=null;
        app.DispatcherUnhandledException+=(s,e)=>{failure=e.Exception;e.Handled=true;release.Set();unlocked.Set();ui.Window.Close();};
        MainUI.DeleteDecision=()=>true;
        MainUI.BeforeShellDelete=()=>{
            dispatcherInShell=ui.Window.Dispatcher.CheckAccess();sta=Thread.CurrentThread.GetApartmentState()==ApartmentState.STA;
            entered.Set();
            if(!dispatcherInShell){ui.Window.Dispatcher.BeginInvoke(new Action(()=>{
                Check(ui.DiagnosticBusy,"Destructive operation did not guard busy state.");
                ui.DiagnosticKey(System.Windows.Input.Key.Delete);busyGuard=Directory.Exists(victim);
                ui.DiagnosticSelect(sibling);
                if(close){ui.Window.Close();closeWaited=ui.Window.IsVisible && ui.DiagnosticWorkers>0;}
                release.Set();
            }));release.WaitOne();}
            if(error)Directory.Move(victim,victim+"-moved"); // native path lookup must fail; preserve generated contents
        };
        ui.BecameIdle+=owner=>{
            if(phase==0)
            {
                // Startup drive enumeration can become idle before Loaded starts
                // this scan. Require the actual fixture's terminal scan first.
                if(owner.DiagnosticRoot==null || !owner.DiagnosticRoot.FullPath.Equals(fixture,StringComparison.OrdinalIgnoreCase))return;
                Check(owner.DiagnosticRoot.Outcome=="complete","Initial fixture scan did not complete.");
                phase=1;owner.DiagnosticSelect(victim);
                owner.Window.Dispatcher.BeginInvoke(new Action(()=>{
                    try {
                        if(args.Contains("--clipboard"))
                        {
                            clipboardHolder=new Thread(()=>{Check(OpenClipboard(IntPtr.Zero),"Guest clipboard could not be reserved.");held.Set();unlocked.WaitOne();CloseClipboard();});
                            clipboardHolder.Start();held.WaitOne();
                            try{owner.DiagnosticMenuAction("copypath");clipboardHandled=owner.DiagnosticStatus.Contains("clipboard");}
                            finally{unlocked.Set();clipboardHolder.Join();}
                            Check(clipboardHandled,"Clipboard contention escaped the action instead of reporting locally.");
                            owner.DiagnosticAddSelection(sibling);owner.DiagnosticMenuAction("copypath");
                            string expected=victim+"\r\n"+sibling;
                            Check(Clipboard.GetText()==expected,"Multiline Unicode Copy path did not round-trip after contention.");
                            File.WriteAllText(Path.Combine(output,"clipboard-expected.txt"),expected);
                            ran=true;owner.Window.Close();return;
                        }
                        if(!baseline){MainUI.DeleteDecision=()=>false;owner.DiagnosticMenuAction("recycle");
                        Check(Directory.Exists(victim) && !owner.DiagnosticBusy,"Cancelled confirmation mutated data or left busy state.");}
                        MainUI.DeleteDecision=()=>true;
                        owner.DiagnosticMenuAction(permanent?"delete":"recycle");
                    }catch(Exception ex){failure=ex;release.Set();unlocked.Set();owner.Window.Close();}
                }));
            }
            else if(phase==1 && !args.Contains("--clipboard"))
            {
                if(close)return;
                try {
                    Check(entered.WaitOne(0) && !dispatcherInShell,"Recycle shell work blocked the WPF dispatcher.");
                    Check(sta,"Shell delete did not use an STA worker.");
                    Check(!Directory.Exists(victim) && File.Exists(Path.Combine(sibling,"two.bin")),"Actual recycle result mismatch.");
                    Check(owner.DiagnosticRoot.Size==(error?1276:42) && owner.DiagnosticRoot.Files==(error?2:1),"Post-operation subtree totals mismatch.");
                    if(error)Check(nativeError!=null && errorOnDispatcher && File.Exists(Path.Combine(victim+"-moved","one.bin")),"Native failure was not reported on the dispatcher with data preserved.");
                    Check(owner.DiagnosticWorkers==0,"File operation/rescan worker did not drain.");
                    ran=true;owner.Window.Close();
                }catch(Exception ex){failure=ex;owner.Window.Close();}
            }
        };
        ui.Window.Loaded+=delegate{ui.DiagnosticScan(fixture);};
        try{app.Run(ui.Window);}finally{release.Set();unlocked.Set();MainUI.DeleteDecision=null;MainUI.BeforeShellDelete=null;MainUI.FileActionErrorObserved=null;}
        if(failure!=null)System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(failure).Throw();
        if(close){Check(closeWaited && !dispatcherInShell && sta && busyGuard && !Directory.Exists(victim),"Close did not wait for the actual owned shell operation.");ran=true;}
        Check(ran && ui.DiagnosticWorkers==0,"File action did not reach verified terminal idle.");
        var binAfter=new RecycleInfo{Size=Marshal.SizeOf(typeof(RecycleInfo))};
        Check(SHQueryRecycleBinW(Path.GetPathRoot(fixture),ref binAfter)==0,"Guest final Recycle Bin query failed.");
        if(!args.Contains("--clipboard"))Check(permanent||error?binAfter.Items==binBefore.Items:binAfter.Items>binBefore.Items,"Shell disposition did not match Recycle Bin/permanent action.");
        File.WriteAllText(Path.Combine(output,"result.json"),new JavaScriptSerializer().Serialize(new{passed=true,clipboard=args.Contains("--clipboard"),clipboardHandled,dispatcherInShell,sta,busyGuard,closeWaited,permanent,nativeError,errorOnDispatcher,recycleBinAdded=binAfter.Items-binBefore.Items,ownedWorkers=ui.DiagnosticWorkers,fixture,confirmationBoundary="Test-only Yes/No decision and error-dialog observer; actual menu handler and real Windows shell filesystem operation"}));
    }
}
