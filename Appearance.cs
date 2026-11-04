using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using System.Web.Script.Serialization;
using Microsoft.Win32;

sealed class AppearancePreferences
{
    public string Mode = "System", AccentMode = "System", CustomAccent = "#0078D4";
}
sealed class SystemAppearance
{
    public bool Dark, HighContrast;
    public Color Accent;
}

// Application-owned palette and window lifetime only. No OS settings are written.
static class Appearance
{
    [DllImport("dwmapi.dll")] static extern int DwmGetColorizationColor(out uint color, out bool opaque);
    [DllImport("dwmapi.dll")] static extern int DwmIsCompositionEnabled(out bool enabled);
    [DllImport("dwmapi.dll")] static extern int DwmSetWindowAttribute(IntPtr window, int attribute, ref int value, int size);
    static readonly Dictionary<string, SolidColorBrush> brushes = new Dictionary<string, SolidColorBrush>();
    static readonly List<Window> windows = new List<Window>();
    static int queued;
    static Dispatcher dispatcher;
    public static AppearancePreferences Preferences = new AppearancePreferences();
    public static bool Dark { get; private set; }
    public static bool HighContrast { get; private set; }
    public static string ReadError = "";
    public static long Revision { get; private set; }
    public static long NotificationRevision { get; private set; }
    public static int NativeDarkResult { get; private set; }
    public static int NativeCornerResult { get; private set; }
    public static bool Composition { get; private set; }
    public static event Action Changed;
#if DISKWEFT_TEST
    internal static Func<SystemAppearance> SystemOverride;
#endif
    public static Brush Brush(string key)
    {
        SolidColorBrush brush;
        if (!brushes.TryGetValue(key,out brush)) brushes[key]=brush=new SolidColorBrush(Colors.Transparent);
        return brush;
    }
    static string SettingsPath { get { return Path.Combine(Ecosystem.Data,"appearance.json"); } }
    public static void Load()
    {
        Preferences=new AppearancePreferences(); ReadError="";
        try
        {
            if (!File.Exists(SettingsPath)) return;
            var saved=new JavaScriptSerializer().Deserialize<AppearancePreferences>(File.ReadAllText(SettingsPath));
            Color color;
            if (saved==null || !ValidMode(saved.Mode) || (saved.AccentMode!="System" && saved.AccentMode!="Custom") || !TryAccent(saved.CustomAccent,out color))
                throw new FormatException("Invalid appearance settings");
            Preferences=saved;
        }
        catch (IOException) { InvalidSettings(); }
        catch (UnauthorizedAccessException) { InvalidSettings(); }
        catch (ArgumentException) { InvalidSettings(); }
        catch (FormatException) { InvalidSettings(); }
        catch (InvalidOperationException) { InvalidSettings(); }
    }
    static void InvalidSettings() { ReadError="Appearance settings could not be read. Windows defaults are in use."; }
    static bool ValidMode(string value) { return value=="System" || value=="Light" || value=="Dark"; }
    public static bool TryAccent(string value, out Color color)
    {
        color=Colors.Transparent;
        if (value==null || value.Length!=7 || value[0]!='#') return false;
        uint rgb;
        if (!uint.TryParse(value.Substring(1),System.Globalization.NumberStyles.HexNumber,System.Globalization.CultureInfo.InvariantCulture,out rgb)) return false;
        color=Color.FromRgb((byte)(rgb>>16),(byte)(rgb>>8),(byte)rgb); return true;
    }
    public static bool Save(string mode, string accentMode, string accent, out string error)
    {
        Color color; error="";
        if (!ValidMode(mode) || (accentMode!="System" && accentMode!="Custom") || !TryAccent(accent,out color))
        { error="Enter an accent as #RRGGBB, for example #0078D4."; return false; }
        var saved=new AppearancePreferences {Mode=mode,AccentMode=accentMode,CustomAccent=accent.ToUpperInvariant()};
        try
        {
            Directory.CreateDirectory(Ecosystem.Data);
            string temporary=SettingsPath+".tmp";
            File.WriteAllText(temporary,new JavaScriptSerializer().Serialize(saved));
            if (File.Exists(SettingsPath)) File.Replace(temporary,SettingsPath,null); else File.Move(temporary,SettingsPath);
        }
        catch (IOException ex)
        { error="Appearance could not be saved: "+ex.Message; return false; }
        catch (UnauthorizedAccessException ex)
        { error="Appearance could not be saved: "+ex.Message; return false; }
        Preferences=saved; Apply(); return true;
    }
    static SystemAppearance ReadSystem()
    {
#if DISKWEFT_TEST
        if (SystemOverride!=null) return SystemOverride();
#endif
        bool dark=false;
        using (var key=Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize"))
            if (key!=null) dark=Convert.ToInt32(key.GetValue("AppsUseLightTheme",1))==0;
        Color accent=SystemColors.HighlightColor;
        try
        {
            uint argb; bool opaque;
            if (DwmGetColorizationColor(out argb,out opaque)==0) accent=Color.FromRgb((byte)(argb>>16),(byte)(argb>>8),(byte)argb);
        }
        catch (DllNotFoundException) { }
        catch (EntryPointNotFoundException) { }
        return new SystemAppearance {Dark=dark,HighContrast=SystemParameters.HighContrast,Accent=accent};
    }
    static Color Hex(string value) { return (Color)ColorConverter.ConvertFromString(value); }
    static double Channel(byte value) { double v=value/255.0; return v<=0.04045?v/12.92:Math.Pow((v+0.055)/1.055,2.4); }
    static double Luminance(Color c) { return 0.2126*Channel(c.R)+0.7152*Channel(c.G)+0.0722*Channel(c.B); }
    internal static double Contrast(Color a, Color b)
    { double x=Luminance(a),y=Luminance(b); return (Math.Max(x,y)+0.05)/(Math.Min(x,y)+0.05); }
    static Color Mix(Color a, Color b, double amount)
    { return Color.FromRgb((byte)Math.Round(a.R+(b.R-a.R)*amount),(byte)Math.Round(a.G+(b.G-a.G)*amount),(byte)Math.Round(a.B+(b.B-a.B)*amount)); }
    static Color ReadableAccent(Color accent, Color surface)
    {
        if (Contrast(accent,surface)>=4.5) return accent;
        Color target=Luminance(surface)>0.5?Colors.Black:Colors.White;
        double low=0,high=1;
        for (int i=0;i<24;i++) { double mid=(low+high)/2; if (Contrast(Mix(accent,target,mid),surface)>=4.5) high=mid; else low=mid; }
        return Mix(accent,target,high);
    }
    public static void Apply()
    {
        var system=ReadSystem(); HighContrast=system.HighContrast;
        Dark=Preferences.Mode=="Dark" || (Preferences.Mode=="System" && system.Dark);
        Color custom; TryAccent(Preferences.CustomAccent,out custom);
        Color ground=Dark?Hex("#202020"):Hex("#F3F3F3"), panel=Dark?Hex("#191919"):Colors.White;
        Color text=Dark?Hex("#F2F2F2"):Hex("#1B1B1B"), dim=Dark?Hex("#B8B8B8"):Hex("#595959");
        Color accent=ReadableAccent(Preferences.AccentMode=="Custom"?custom:system.Accent,panel);
        var colors=new Dictionary<string,Color> {
            {"Ground",ground},{"Text",text},{"Dim",dim},{"Dimmer",dim},{"Panel",panel},
            {"Panel2",Dark?Hex("#303030"):Hex("#EAEAEA")},{"FieldBackground",Dark?Hex("#2C2C2C"):Colors.White},
            {"Edge",Dark?Hex("#414141"):Hex("#D1D1D1")},{"EdgeBright",Dark?Hex("#808080"):Hex("#808080")},
            {"Accent",accent},{"Focus",accent},{"AccentSoft",Mix(panel,accent,0.14)},
            {"AccentText",Contrast(Colors.White,accent)>=4.5?Colors.White:Colors.Black},
            {"DisabledText",dim},
            {"Danger",ReadableAccent(Dark?Hex("#FF99A4"):Hex("#B00020"),panel)}
        };
        if (HighContrast)
        {
            colors["Ground"]=colors["Panel"]=colors["FieldBackground"]=colors["AccentSoft"]=SystemColors.WindowColor;
            colors["Panel2"]=SystemColors.ControlColor;
            colors["Text"]=colors["Dim"]=colors["Dimmer"]=colors["Edge"]=colors["EdgeBright"]=SystemColors.WindowTextColor;
            colors["Accent"]=colors["Focus"]=colors["Danger"]=SystemColors.HighlightColor;
            colors["AccentText"]=SystemColors.HighlightTextColor;
            colors["DisabledText"]=SystemColors.GrayTextColor;
        }
        foreach (var kv in colors)
        {
            var brush=(SolidColorBrush)Brush(kv.Key);
            if (brush.IsFrozen) brushes[kv.Key]=new SolidColorBrush(kv.Value); else brush.Color=kv.Value;
        }
        foreach (var window in windows.ToArray())
        {
            foreach (var kv in brushes) window.Resources[kv.Key]=kv.Value;
            window.Resources["DisabledOpacity"]=HighContrast?1.0:0.5;
            window.Background=Brush("Ground"); window.Foreground=Brush("Text"); ApplyNative(window);
        }
        Revision++; if (Changed!=null) Changed();
    }
    static void QueueNotification()
    {
        var owner=dispatcher;
        if (owner==null || owner.HasShutdownStarted || System.Threading.Interlocked.CompareExchange(ref queued,1,0)!=0) return;
        owner.BeginInvoke(new Action(() => { System.Threading.Interlocked.Exchange(ref queued,0); NotificationRevision++; Apply(); }));
    }
    static void UserPreference(object sender, UserPreferenceChangedEventArgs e) { QueueNotification(); }
    static void SystemProperty(object sender, PropertyChangedEventArgs e) { QueueNotification(); }
    static IntPtr NativeMessage(IntPtr hwnd,int message,IntPtr wParam,IntPtr lParam,ref bool handled)
    { if (message==0x1A || message==0x31A || message==0x320) QueueNotification(); return IntPtr.Zero; }
    public static void Attach(Window window, bool load=false)
    {
        if (load) Load();
        if (windows.Count==0) { dispatcher=window.Dispatcher; SystemEvents.UserPreferenceChanged+=UserPreference; SystemParameters.StaticPropertyChanged+=SystemProperty; }
        windows.Add(window);
        window.SourceInitialized+=delegate {
            var source=HwndSource.FromHwnd(new WindowInteropHelper(window).Handle);
            source.AddHook(NativeMessage); ApplyNative(window);
        };
        window.Closed+=delegate {
            var source=PresentationSource.FromVisual(window) as HwndSource;
            if (source!=null) source.RemoveHook(NativeMessage);
            windows.Remove(window);
            if (windows.Count==0) { dispatcher=null; SystemEvents.UserPreferenceChanged-=UserPreference; SystemParameters.StaticPropertyChanged-=SystemProperty; }
        };
        using (var icon=typeof(Appearance).Assembly.GetManifestResourceStream("Diskweft.ico"))
            if (icon!=null) { var frame=BitmapFrame.Create(icon,BitmapCreateOptions.None,BitmapCacheOption.OnLoad); frame.Freeze(); window.Icon=frame; }
        Apply();
    }
    static void ApplyNative(Window window)
    {
        var handle=new WindowInteropHelper(window).Handle; if (handle==IntPtr.Zero) return;
        NativeDarkResult=NativeCornerResult=-1; Composition=false;
        try
        {
            bool enabled;
            if (DwmIsCompositionEnabled(out enabled)!=0 || !enabled) return;
            Composition=true;
            int dark=Dark && !HighContrast?1:0, corners=HighContrast?1:2;
            NativeDarkResult=DwmSetWindowAttribute(handle,20,ref dark,4);
            NativeCornerResult=DwmSetWindowAttribute(handle,33,ref corners,4);
        }
        catch (DllNotFoundException) { }
        catch (EntryPointNotFoundException) { }
    }
}

partial class MainUI
{
    void ConfigureAppearance()
    {
        var mode=Find<ComboBox>("appearanceMode"); var accentMode=Find<ComboBox>("appearanceAccentMode");
        var accent=Find<TextBox>("appearanceAccent"); var feedback=Find<TextBlock>("appearanceStatus");
        Appearance.Attach(Window,true);
        mode.SelectedIndex=Appearance.Preferences.Mode=="Light"?1:Appearance.Preferences.Mode=="Dark"?2:0;
        accentMode.SelectedIndex=Appearance.Preferences.AccentMode=="Custom"?1:0;
        accent.Text=Appearance.Preferences.CustomAccent;
        feedback.Text=Appearance.ReadError;
        Find<Button>("appearanceApply").Click+=delegate {
            string error;
            bool saved=Appearance.Save(new[]{"System","Light","Dark"}[mode.SelectedIndex],accentMode.SelectedIndex==0?"System":"Custom",accent.Text.Trim(),out error);
            feedback.Text=saved?"Appearance saved. Accent contrast is adjusted for readability.":error;
        };
        accentMode.SelectionChanged+=delegate { accent.IsEnabled=accentMode.SelectedIndex==1 && !Appearance.HighContrast; };
        Action changed=() => {
            mode.IsEnabled=accentMode.IsEnabled=!Appearance.HighContrast;
            accent.IsEnabled=accentMode.SelectedIndex==1 && !Appearance.HighContrast;
            if (Appearance.HighContrast) feedback.Text="Windows contrast theme controls these colors.";
            else if (feedback.Text=="Windows contrast theme controls these colors.") feedback.Text="";
            RefreshViews();
        };
        Appearance.Changed+=changed; changed();
        Window.Closed+=delegate { Appearance.Changed-=changed; };
        excludePopup.Opened+=delegate { Find<ScrollViewer>("settingsScroll").MaxHeight=Math.Max(220,Math.Min(Window.ActualHeight-160,SystemParameters.WorkArea.Height-120)); };
    }
}
