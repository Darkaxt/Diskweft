using System;
using System.IO;
using System.Linq;
using System.Diagnostics;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Security.AccessControl;
using System.Security.Principal;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Threading;
using System.Web.Script.Serialization;

class Stage3Tests
{
    [DllImport("user32.dll")] static extern IntPtr SendMessageW(IntPtr window,int message,IntPtr wParam,IntPtr lParam);
    [DllImport("user32.dll",EntryPoint="GetWindowLongW")] static extern int WindowStyle(IntPtr window,int index);
    [StructLayout(LayoutKind.Sequential,CharSet=CharSet.Unicode)]
    struct DisplayMode
    {
        [MarshalAs(UnmanagedType.ByValTStr,SizeConst=32)] public string Device;
        public ushort Spec,Driver,Size,Extra; public uint Fields;
        public int X,Y; public uint Orientation,FixedOutput;
        public short Color,Duplex,YResolution,Option,Collate;
        [MarshalAs(UnmanagedType.ByValTStr,SizeConst=32)] public string Form;
        public ushort Dpi; public uint Bits,Width,Height,Flags,Frequency,Method,Intent,Media,Dither,Reserved1,Reserved2,PanningWidth,PanningHeight;
    }
    [DllImport("user32.dll",CharSet=CharSet.Unicode)] static extern bool EnumDisplaySettingsW(string name,int mode,ref DisplayMode settings);
    [DllImport("user32.dll",CharSet=CharSet.Unicode)] static extern int ChangeDisplaySettingsExW(string device,ref DisplayMode settings,IntPtr hwnd,uint flags,IntPtr parameter);
    [DllImport("user32.dll")] static extern int GetSystemMetrics(int index);
    static void DisplayProbe(bool configure)
    {
        var modes=new List<DisplayMode>();
        for(int i=0;;i++) { var mode=new DisplayMode{Size=(ushort)Marshal.SizeOf(typeof(DisplayMode))}; if(!EnumDisplaySettingsW(null,i,ref mode))break; modes.Add(mode); }
        int? result=null;
        if(configure)
        {
            Check(Environment.GetEnvironmentVariable("DISKWEFT_SANDBOX")=="1","Display configuration is guest-only.");
            var candidates=modes.Where(m=>m.Width>=1920 && m.Height>=1080 && m.Bits==32).OrderByDescending(m=>(long)m.Width*m.Height).ToList();
            if(candidates.Count>0) { var mode=candidates[0]; result=ChangeDisplaySettingsExW(null,ref mode,IntPtr.Zero,0,IntPtr.Zero); }
        }
        File.WriteAllText(Path.Combine(output,"display-probe.json"),new JavaScriptSerializer().Serialize(new {width=GetSystemMetrics(0),height=GetSystemMetrics(1),configure,result,modes=modes.Select(m=>new {width=m.Width,height=m.Height,bits=m.Bits,frequency=m.Frequency}).Distinct().ToArray()}));
    }
    static string output,fixture;
    static int phase;
    static Color systemAccent=Colors.OrangeRed;
    static bool systemDark=true,systemContrast;
    static readonly List<string> evidence=new List<string>();
    static void Check(bool value,string message) { if (!value) throw new Exception(message); }
    static Color ColorOf(string key) { return ((SolidColorBrush)Appearance.Brush(key)).Color; }
    static void CheckContrast()
    {
        Check(Appearance.Contrast(ColorOf("Text"),ColorOf("Panel"))>=4.5,"Body contrast failed.");
        Check(Appearance.Contrast(ColorOf("Dim"),ColorOf("Panel"))>=4.5,"Secondary text contrast failed.");
        Check(Appearance.Contrast(ColorOf("Accent"),ColorOf("Panel"))>=4.5,"Accent contrast failed.");
        Check(Appearance.Contrast(ColorOf("AccentText"),ColorOf("Accent"))>=4.5,"Primary button text contrast failed.");
        Check(Appearance.Contrast(ColorOf("Text"),ColorOf("AccentSoft"))>=4.5,"Selected row text contrast failed.");
        Check(Appearance.Contrast(ColorOf("Focus"),ColorOf("Ground"))>=3,"Focus contrast failed.");
    }
    static void Palette()
    {
        Appearance.SystemOverride=()=>new SystemAppearance{Dark=systemDark,Accent=systemAccent,HighContrast=systemContrast};
        foreach (string mode in new[]{"Light","Dark"})
            foreach (string accent in new[]{"#000000","#FFFFFF","#FFFF00","#00FF00","#00FFFF","#FF00FF","#FF0000","#D06010","#0078D4"})
            {
                Appearance.Preferences=new AppearancePreferences{Mode=mode,AccentMode="Custom",CustomAccent=accent}; Appearance.Apply(); CheckContrast();
            }
        Appearance.Preferences=new AppearancePreferences(); Appearance.Apply();
        Check(Appearance.Dark,"Default System mode did not resolve the Windows preference.");
        systemDark=false; Appearance.Apply(); Check(!Appearance.Dark,"System light preference was ignored."); systemDark=true;
        systemContrast=true; Appearance.Apply();
        Check(ColorOf("Panel")==SystemColors.WindowColor && ColorOf("Text")==SystemColors.WindowTextColor,"High contrast did not use Windows colors.");
        systemContrast=false; Appearance.Preferences=new AppearancePreferences(); Appearance.Apply();
        evidence.Add("Palette contrast checked in both modes with very light/dark and saturated custom accents; System and high-contrast resolution use the real palette path.");
    }
    static void ActualSystemSource()
    {
        Appearance.SystemOverride=null;Appearance.Preferences=new AppearancePreferences();Appearance.Apply();
        bool expected=false;
        using(var key=Microsoft.Win32.Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize"))
            if(key!=null)expected=Convert.ToInt32(key.GetValue("AppsUseLightTheme",1))==0;
        Check(Appearance.Dark==expected && Appearance.HighContrast==SystemParameters.HighContrast,"Actual Windows preference source did not match the current account.");
        if(!Appearance.HighContrast)CheckContrast();
        File.WriteAllText(Path.Combine(output,"result.json"),new JavaScriptSerializer().Serialize(new {passed=true,scope="read-only actual Windows preference source",dark=Appearance.Dark,highContrast=Appearance.HighContrast,accent=ColorOf("Accent").ToString()}));
    }
    static void DpiProbe()
    {
        Check(Environment.GetEnvironmentVariable("DISKWEFT_SANDBOX")=="1","DPI notification probe is guest-only.");
        var app=new Application{ShutdownMode=ShutdownMode.OnMainWindowClose};var ui=new MainUI();
        int notifications=0;Exception failure=null;var scales=new List<double>();
        ui.Window.DpiChanged+=(s,e)=>notifications++;
        app.DispatcherUnhandledException+=(s,e)=>{failure=e.Exception;e.Handled=true;ui.Window.Close();};
        ui.Window.Loaded+=delegate{
            foreach(int dpi in new[]{144,192,96})
            {
                ui.DiagnosticDpi(dpi);
                double scale=PresentationSource.FromVisual(ui.Window).CompositionTarget.TransformToDevice.M11;
                scales.Add(scale);Check(Math.Abs(scale-dpi/96.0)<0.01,"Framework ignored the actual WM_DPICHANGED notification: "+dpi+" -> "+scale);
            }
            ui.Window.Close();
        };
        app.Run(ui.Window);if(failure!=null)throw failure;
        File.WriteAllText(Path.Combine(output,"result.json"),new JavaScriptSerializer().Serialize(new {passed=true,scope="owned HWND controlled DPI notifications",notifications,scales}));
    }
    static void Matrix(MainUI ui)
    {
        ui.DiagnosticSettings(true);
        long before=Appearance.Revision;
        ui.DiagnosticAppearance("Light","Custom","not a color");
        Check(Appearance.Revision==before && ui.DiagnosticAppearanceFeedback.Contains("#RRGGBB"),"Invalid custom color changed or silently saved appearance.");
        foreach (string mode in new[]{"Light","Dark"})
        {
            ui.DiagnosticAppearance(mode,"Custom",mode=="Light"?"#0078D4":"#D06010"); CheckContrast();
            Check(ui.Window.Background==Appearance.Brush("Ground"),"Window palette did not update.");
            ui.DiagnosticSettingsCapture(output,mode.ToLowerInvariant()+"-settings");
            ui.DiagnosticSettings(false);
            foreach (var size in new[]{new[]{960.0,560.0},new[]{1280.0,820.0},new[]{1600.0,1000.0}})
            {
                foreach (double scale in new[]{1.0,1.5,2.0})
                {
                    ui.DiagnosticDpi((int)(96*scale));ui.DiagnosticViewport(size[0],size[1]);ui.DiagnosticCheckLayout();
                    var client=(FrameworkElement)ui.Window.Content;
                    double actualScale=PresentationSource.FromVisual(ui.Window).CompositionTarget.TransformToDevice.M11;
                    Check(Math.Abs(actualScale-scale)<0.01,"Actual WPF DPI transform did not follow the owned window notification.");
                    Check(client.ActualWidth>=size[0]-1 && client.ActualWidth<=size[0]+24 && client.ActualHeight>=size[1]-1 && client.ActualHeight<=size[1]+24,
                        "Native client viewport was clamped: requested "+size[0]+"x"+size[1]+", actual "+client.ActualWidth+"x"+client.ActualHeight);
                    ui.DiagnosticCapture(output,mode.ToLowerInvariant()+"-"+size[0]+"-"+(int)(scale*100),scale);
                }
            }
            ui.DiagnosticDpi(96);
            ui.DiagnosticMenuCapture(output,mode.ToLowerInvariant()+"-menu");
            ui.DiagnosticViewport(1280,820);
            foreach(string view in new[]{"tabExt","tabTop","tabSearch","tabDup","tabCmp","tabDrives"})
            {ui.DiagnosticView(view);ui.DiagnosticCheckLayout();ui.DiagnosticCapture(output,mode.ToLowerInvariant()+"-"+view);}
            ui.DiagnosticView("tabTree");
            ui.DiagnosticSettings(true);
            ui.DiagnosticReleaseNotes(output,mode.ToLowerInvariant()+"-release-notes");
            ui.DiagnosticSettings(true);
        }
        ui.DiagnosticViewport(960,560);
        var scroll=(ScrollViewer)ui.Window.FindName("settingsScroll"); scroll.UpdateLayout();
        Check(scroll.ExtentHeight>scroll.ViewportHeight,"Settings cannot scroll at the minimum window size.");
        scroll.ScrollToEnd(); scroll.UpdateLayout();
        ui.DiagnosticSettingsCapture(output,"settings-minimum-bottom"); scroll.ScrollToTop();
        ui.DiagnosticAppearance("System","System","#D06010");
        ui.DiagnosticSettings(false);ui.DiagnosticViewport(1280,820);ui.DiagnosticKeyboardTab();ui.DiagnosticCapture(output,"keyboard-focus");ui.DiagnosticSettings(true);
        evidence.Add("Real Settings Apply handler, inline invalid-color feedback, scrollable settings, light/dark native menus and measured client layouts at 960/1280/1600 DIP; actual WPF raster exports at 96/144/192 DPI.");
    }
    static void Native()
    {
        Check(Environment.GetEnvironmentVariable("DISKWEFT_SANDBOX")=="1" && output.StartsWith(@"C:\Coordinator\Work\",StringComparison.OrdinalIgnoreCase),"Native S3 workflow must use its owned headless guest.");
        fixture=Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),"Diskweft-S3-"+Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(Path.Combine(fixture,"child")); Directory.CreateDirectory(Path.Combine(fixture,"empty"));
        File.WriteAllBytes(Path.Combine(fixture,"root.bin"),new byte[1000]); File.WriteAllBytes(Path.Combine(fixture,"child\\live.bin"),new byte[2000]);
        string restricted=Path.Combine(fixture,"restricted"); Directory.CreateDirectory(restricted);
        var original=Directory.GetAccessControl(restricted); bool aclChanged=false;
        var app=new Application{ShutdownMode=ShutdownMode.OnMainWindowClose};
        var ui=new MainUI(); ui.Window.ShowActivated=true;
        ui.Window.Width=1296; ui.Window.Height=859;
        var release=new ManualResetEvent(false); Exception failure=null;
        bool paused=false,listed=false; Row selected=null; long scanGeneration=0,notification=0;
        app.DispatcherUnhandledException+=(s,e)=>{failure=e.Exception;e.Handled=true;phase=-1;release.Set();ui.Window.Close();};
        ScanSession.Enumerated=(session,node)=>{if(paused || node.Name!="child")return;paused=true;session.Publish(false,node,true);release.WaitOne();};
        ui.ScanApplied+=(owner,batch)=>{
            if(phase!=0 || selected!=null || batch.Terminal || !paused || owner.DiagnosticRoot==null || owner.DiagnosticRoot.Size!=3000)return;
            scanGeneration=owner.DiagnosticGeneration;selected=owner.DiagnosticSelect(Path.Combine(fixture,"child"));owner.DiagnosticKey(Key.Right);
        };
        ui.ListingApplied+=(owner,row)=>{
            if(phase!=0 || listed || row!=selected)return; listed=true;
            owner.Window.Dispatcher.BeginInvoke(DispatcherPriority.Render,new Action(()=>{
                Matrix(owner);
                Check(owner.DiagnosticBusy && object.ReferenceEquals(selected,owner.DiagnosticSelection),"Applying appearance restarted the scan or replaced selection.");
                Check((WindowStyle(new WindowInteropHelper(owner.Window).Handle,-16)&0x00C00000)==0x00C00000,"Native Windows caption is absent.");
                phase=1; notification=Appearance.NotificationRevision; systemDark=false;systemAccent=Colors.MediumVioletRed;
                SendMessageW(new WindowInteropHelper(owner.Window).Handle,0x1A,IntPtr.Zero,IntPtr.Zero);
            }));
        };
        Action changed=()=>{
            if(phase==1 && Appearance.NotificationRevision>notification)
            {
                Check(!Appearance.Dark && ui.DiagnosticBusy && ui.DiagnosticGeneration==scanGeneration && object.ReferenceEquals(selected,ui.DiagnosticSelection),"Windows preference message failed or disturbed the live scan.");
                ui.DiagnosticCapture(output,"system-change-live");
                phase=2; notification=Appearance.NotificationRevision; systemContrast=true;
                SendMessageW(new WindowInteropHelper(ui.Window).Handle,0x31A,IntPtr.Zero,IntPtr.Zero);
            }
            else if(phase==2 && Appearance.NotificationRevision>notification)
            {
                Check(Appearance.HighContrast && !ui.DiagnosticAppearanceEnabled,"High contrast did not override app appearance controls.");
                Check(((ComboBox)ui.Window.FindName("appearanceMode")).Opacity==1,"High contrast was weakened by disabled-control opacity.");
                ui.DiagnosticCapture(output,"high-contrast-live"); ui.DiagnosticSettingsCapture(output,"high-contrast-settings");
                phase=3; notification=Appearance.NotificationRevision; systemContrast=false;
                SendMessageW(new WindowInteropHelper(ui.Window).Handle,0x31A,IntPtr.Zero,IntPtr.Zero);
            }
            else if(phase==3 && Appearance.NotificationRevision>notification)
            {
                Check(!Appearance.HighContrast && ui.DiagnosticAppearanceEnabled,"App appearance did not recover after contrast mode ended.");
                phase=4;
                ui.Window.Dispatcher.BeginInvoke(new Action(()=>{
                    ui.DiagnosticAppearance("Dark","Custom","#D06010");ui.DiagnosticSettings(false);ui.DiagnosticCapture(output,"dark-live");
                    ui.DiagnosticScan(fixture); release.Set();
                }));
            }
        };
        Appearance.Changed+=changed;
        ui.BecameIdle+=owner=>{
            if(phase==4)
            {
                Check(owner.DiagnosticRoot.Outcome=="stopped","Stopped themed scan lost provenance.");
                owner.DiagnosticCapture(output,"dark-stopped");ScanSession.Enumerated=null;
                phase=5;owner.DiagnosticScan(fixture);
            }
            else if(phase==5)
            {
                Check(owner.DiagnosticRoot.Size==3000 && owner.DiagnosticRoot.Files==2,"Themed terminal totals changed.");
                owner.DiagnosticCapture(output,"dark-complete");phase=6;owner.DiagnosticScan(Path.Combine(fixture,"empty"));
            }
            else if(phase==6)
            {
                Check(owner.DiagnosticRoot.Outcome=="complete" && owner.DiagnosticRoot.Size==0,"Actual empty scan was not complete zero.");
                owner.DiagnosticCapture(output,"empty-complete");
                var denied=new DirectorySecurity();denied.SetSecurityDescriptorBinaryForm(original.GetSecurityDescriptorBinaryForm(),AccessControlSections.Access);
                denied.AddAccessRule(new FileSystemAccessRule(WindowsIdentity.GetCurrent().User,FileSystemRights.ListDirectory,AccessControlType.Deny));
                Directory.SetAccessControl(restricted,denied);aclChanged=true;phase=7;owner.DiagnosticScan(restricted);
            }
            else if(phase==7)
            {
                Check(owner.DiagnosticRoot.Denied && owner.DiagnosticRoot.Outcome=="incomplete","Actual inaccessible folder was presented as empty complete.");
                owner.DiagnosticCapture(output,"denied-incomplete");
                Directory.SetAccessControl(restricted,original);aclChanged=false;phase=8;owner.Window.Close();
            }
        };
        ui.Window.Loaded+=delegate{ui.DiagnosticScan(fixture);};
        try{app.Run(ui.Window);}
        finally{release.Set();Appearance.Changed-=changed;ScanSession.Enumerated=null;release.Dispose();if(aclChanged)Directory.SetAccessControl(restricted,original);}
        if(failure!=null)throw failure;
        Check(phase==8 && ui.DiagnosticWorkers==0,"Native theme workflow ended before its acceptance predicate or owned worker exit.");
        evidence.Add("Real Windows setting/theme messages reached the owned HWND and updated the live WPF UI through the production notification path without restarting the scan; high-contrast colors use SystemColors with an isolated preference source override.");
        evidence.Add("Native caption style, stopped/complete/empty/actual ACL-denied states, exact terminal totals, selection and owned-thread exit passed in the disposable guest.");
    }
    [STAThread]
    static int Main(string[] args)
    {
        output=Path.GetFullPath(args[0]);Directory.CreateDirectory(output);
        Environment.SetEnvironmentVariable("DISKWEFT_TEST_ROOT",Path.Combine(output,"profile"));
        try
        {
            if(args.Contains("--dpi-probe")){DpiProbe();return 0;}
            if(args.Contains("--system-source")){ActualSystemSource();return 0;}
            if(args.Contains("--classic-ui"))
            {
                Check(Environment.GetEnvironmentVariable("DISKWEFT_SANDBOX")=="1","Native classic regression is guest-only here.");
                Stage1Tests.NativeRegression(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),"Diskweft-S3-Classic-"+Guid.NewGuid().ToString("N")),output);
                return 0;
            }
            if(args.Contains("--display-probe") || args.Contains("--configure-display")) { DisplayProbe(args.Contains("--configure-display")); return 0; }
            if(args.Length>1 && args[1]=="--persisted")
            {
                Appearance.Load();Check(Appearance.Preferences.Mode=="Dark" && Appearance.Preferences.AccentMode=="Custom" && Appearance.Preferences.CustomAccent=="#D06010","Appearance did not persist into a fresh process.");return 0;
            }
            Palette();
            if(args.Length>1 && args[1]=="--guest")
            {
                Native();
                var child=Process.Start(new ProcessStartInfo(typeof(Stage3Tests).Assembly.Location,"\""+output+"\" --persisted"){UseShellExecute=false,CreateNoWindow=true});
                child.WaitForExit();Check(child.ExitCode==0,"Fresh process appearance persistence failed.");
                evidence.Add("Persisted Dark/custom accent overrides were read and validated by a fresh process.");
            }
            File.WriteAllText(Path.Combine(output,"result.json"),new JavaScriptSerializer().Serialize(new{passed=true,native=args.Contains("--guest"),evidence}));
            Console.WriteLine("S3 requested palette/native workflows passed.");return 0;
        }
        catch(Exception ex){File.WriteAllText(Path.Combine(output,"result.json"),new JavaScriptSerializer().Serialize(new{passed=false,error=ex.ToString(),evidence}));Console.Error.WriteLine(ex);return 1;}
    }
}
