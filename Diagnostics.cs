#if DISKWEFT_TEST
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using System.Runtime.InteropServices;
using System.Web.Script.Serialization;

partial class MainUI
{
    internal static Func<bool> DeleteDecision;
    internal static Action BeforeShellDelete;
    internal static Action<string> FileActionErrorObserved;
    internal void DiagnosticMenuAction(string action)
    {
        var item=menu.Items.OfType<MenuItem>().First(i => (string)i.Tag==action);
        item.RaiseEvent(new RoutedEventArgs(MenuItem.ClickEvent,item));
    }
    internal string DiagnosticStatus { get { return status.Text; } }
    internal void DiagnosticAddSelection(string path)
    { treeList.SelectedItems.Add(DiagnosticRow(path)); }
    Row DiagnosticRow(string path)
    {
        Node node;Row row;
        if(!displayNodes.TryGetValue(path,out node))throw new InvalidOperationException("No displayed node for "+path+"; known="+string.Join("|",displayNodes.Keys.Take(20)));
        if(!directoryRows.TryGetValue(node,out row))throw new InvalidOperationException("No instantiated directory row for "+path);
        return row;
    }
    [StructLayout(LayoutKind.Sequential)] struct DiagnosticRect { public int Left,Top,Right,Bottom; }
    [DllImport("user32.dll")] static extern bool GetWindowRect(IntPtr window,out DiagnosticRect rect);
    [DllImport("user32.dll")] static extern IntPtr SendMessageW(IntPtr window,int message,IntPtr wParam,IntPtr lParam);
    internal void DiagnosticDpi(int dpi)
    {
        var handle=new System.Windows.Interop.WindowInteropHelper(Window).Handle;
        DiagnosticRect rect; if(!GetWindowRect(handle,out rect))throw new InvalidOperationException("Owned window bounds unavailable.");
        double current=PresentationSource.FromVisual(Window).CompositionTarget.TransformToDevice.M11;
        double ratio=(dpi/96.0)/current;
        rect.Right=rect.Left+(int)Math.Round((rect.Right-rect.Left)*ratio);rect.Bottom=rect.Top+(int)Math.Round((rect.Bottom-rect.Top)*ratio);
        var memory=Marshal.AllocHGlobal(Marshal.SizeOf(typeof(DiagnosticRect)));
        try{Marshal.StructureToPtr(rect,memory,false);SendMessageW(handle,0x02E0,new IntPtr(dpi|(dpi<<16)),memory);}
        finally{Marshal.FreeHGlobal(memory);}
        Window.Dispatcher.Invoke(DispatcherPriority.Render,new Action(()=>Window.UpdateLayout()));
    }
    internal bool DiagnosticBusy { get { return busy; } }
    internal Node DiagnosticRoot { get { return root; } }
    internal long DiagnosticRevision { get { return lastRenderedRevision; } }
    internal long DiagnosticGeneration { get { return generation; } }
    internal int DiagnosticWorkers { get { return ownedWorkers; } }
    internal int DiagnosticDirectoryCacheCount { get { return dirCache.Count; } }
    internal void DiagnosticSettings(bool open)
    {
        if (open) { var button=Find<Button>("excludeBtn"); button.RaiseEvent(new RoutedEventArgs(Button.ClickEvent,button)); }
        else excludePopup.IsOpen=false;
    }
    internal void DiagnosticAppearance(string mode, string source, string color)
    {
        Find<ComboBox>("appearanceMode").SelectedIndex=mode=="Light"?1:mode=="Dark"?2:0;
        Find<ComboBox>("appearanceAccentMode").SelectedIndex=source=="Custom"?1:0;
        Find<TextBox>("appearanceAccent").Text=color;
        var button=Find<Button>("appearanceApply"); button.RaiseEvent(new RoutedEventArgs(Button.ClickEvent,button));
    }
    internal string DiagnosticAppearanceFeedback { get { return Find<TextBlock>("appearanceStatus").Text; } }
    internal bool DiagnosticAppearanceEnabled { get { return Find<ComboBox>("appearanceMode").IsEnabled; } }
    internal void DiagnosticViewport(double width,double height)
    {
        Window.UpdateLayout();
        var content=(FrameworkElement)Window.Content;
        // A headless guest has a 1024x768 native tracking maximum. The test
        // window's minimum provides a real larger HWND/client for offscreen
        // rendering without changing the desktop or the product's XAML minima.
        double frameWidth=Window.ActualWidth-content.ActualWidth,frameHeight=Window.ActualHeight-content.ActualHeight;
        Window.MinWidth=Math.Max(980,width+frameWidth); Window.MinHeight=Math.Max(600,height+frameHeight);
        Window.Width=Window.MinWidth; Window.Height=Window.MinHeight;
        Window.UpdateLayout();
    }
    internal void DiagnosticCheckLayout()
    {
        Window.UpdateLayout(); var content=(FrameworkElement)Window.Content;
        foreach (string id in new[]{"scanBtn","browseBtn","excludeBtn","drivesBtn","pathBox","tabTree","status"})
        {
            var control=Find<FrameworkElement>(id); var origin=control.TransformToAncestor(content).Transform(new Point());
            if (!control.IsVisible || control.ActualWidth<=0 || origin.X<0 || origin.X+control.ActualWidth>content.ActualWidth+1 || origin.Y<0 || origin.Y+control.ActualHeight>content.ActualHeight+1)
                throw new InvalidOperationException("Required control clipped: "+id);
        }
        if (scanBtn.FocusVisualStyle==null) throw new InvalidOperationException("Keyboard focus visual missing.");
    }
    internal void DiagnosticSettingsCapture(string directory,string name)
    {
        var child=(FrameworkElement)excludePopup.Child; child.UpdateLayout();
        Render(child,Path.Combine(directory,name+".png"),1);
        var scroll=Find<ScrollViewer>("settingsScroll");
        var visuals=new List<object>(); CaptureVisual(child,visuals);
        File.WriteAllText(Path.Combine(directory,name+".json"),new JavaScriptSerializer().Serialize(new {visible=child.IsVisible,width=child.ActualWidth,height=child.ActualHeight,viewport=scroll.ViewportHeight,extent=scroll.ExtentHeight,visuals}));
    }
    internal void DiagnosticMenuCapture(string directory,string name)
    {
        menu.PlacementTarget=treeList; menu.IsOpen=true; menu.UpdateLayout();
        Render(menu,Path.Combine(directory,name+".png"),1); menu.IsOpen=false;
    }
    internal void DiagnosticView(string id) { Find<RadioButton>(id).IsChecked=true; }
    internal void DiagnosticKeyboardTab()
    {
        Window.Activate();Keyboard.Focus(scanBtn);
        var key=new KeyEventArgs(Keyboard.PrimaryDevice,PresentationSource.FromVisual(Window),Environment.TickCount,Key.Tab){RoutedEvent=Keyboard.KeyDownEvent};
        InputManager.Current.ProcessInput(key);Window.UpdateLayout();
        if(Keyboard.FocusedElement==null || Keyboard.FocusedElement==scanBtn)throw new InvalidOperationException("Native WPF keyboard navigation did not advance focus.");
        var button=Keyboard.FocusedElement as Button;
        if(button!=null && (button.BorderBrush!=Appearance.Brush("Focus") || button.BorderThickness.Left<2))throw new InvalidOperationException("Focused command lacks its visible focus outline.");
    }
    internal void DiagnosticReleaseNotes(string directory,string name)
    {
        var button=Find<Button>("whatsNewBtn");button.RaiseEvent(new RoutedEventArgs(Button.ClickEvent,button));
        var dialog=Application.Current.Windows.Cast<Window>().Single(w=>w.Owner==Window);
        dialog.Dispatcher.Invoke(DispatcherPriority.Render,new Action(()=>{
            dialog.UpdateLayout(); var client=(FrameworkElement)dialog.Content;
            if(dialog.Background!=Appearance.Brush("Ground") || dialog.Foreground!=Appearance.Brush("Text"))throw new InvalidOperationException("Owned dialog did not receive the resolved theme.");
            Render(client,Path.Combine(directory,name+".png"),1);
            dialog.Close();
        }));
    }
    static void Render(FrameworkElement element,string path,double scale)
    {
        var window=element as Window;var client=window==null?element:(FrameworkElement)window.Content;
        var bitmap=new RenderTargetBitmap((int)Math.Ceiling(client.ActualWidth*scale),(int)Math.Ceiling(client.ActualHeight*scale),96*scale,96*scale,PixelFormats.Pbgra32);
        bitmap.Render(element);
        var encoder=new PngBitmapEncoder(); encoder.Frames.Add(BitmapFrame.Create(bitmap));
        using (var file=File.Create(path)) encoder.Save(file);
    }
    internal Dictionary<string, long[]> DiagnosticExtensions { get { return displayExtensions; } }
    internal Row DiagnosticSelect(string path)
    {
        tabTree.IsChecked = true;
        var row = DiagnosticRow(path);
        treeList.SelectedItem = row;
        treeList.ScrollIntoView(row);
        treeList.UpdateLayout();
        return row;
    }
    internal object DiagnosticSelection { get { return treeList.SelectedItem; } }
    internal double DiagnosticScrollOffset { get { return FindScroll(treeList).VerticalOffset; } }
    internal Row DiagnosticTopVisibleRow { get { treeList.UpdateLayout();return rows.Count==0?null:rows[Math.Min(rows.Count-1,(int)Math.Floor(DiagnosticScrollOffset))]; } }
    internal Row DiagnosticScrollFiles()
    {
        var row = rows.Where(r => r.File != null && !r.File.Summary).Skip(30).First();
        treeList.SelectedItem = row; treeList.ScrollIntoView(row); treeList.UpdateLayout();
        FindScroll(treeList).PageDown(); treeList.UpdateLayout();
        return row;
    }
    internal void DiagnosticScan(string path)
    {
        pathBox.Text = path;
        scanBtn.RaiseEvent(new RoutedEventArgs(Button.ClickEvent, scanBtn));
    }
    internal void DiagnosticKey(Key key)
    {
        var source = PresentationSource.FromVisual(treeList);
        treeList.RaiseEvent(new KeyEventArgs(Keyboard.PrimaryDevice, source, Environment.TickCount, key)
            { RoutedEvent = Keyboard.PreviewKeyDownEvent });
    }
    internal void DiagnosticRescan()
    {
        var item = menu.Items.OfType<MenuItem>().First(i => (string)i.Tag == "rescan");
        item.RaiseEvent(new RoutedEventArgs(MenuItem.ClickEvent, item));
    }
    internal void DiagnosticSearch(string query)
    {
        tabSearch.IsChecked = true; findName.Text = query; findSize.Text = "0"; findAge.Text = "";
        var button = Find<Button>("findBtn");
        button.RaiseEvent(new RoutedEventArgs(Button.ClickEvent, button));
    }
    internal void DiagnosticScanAll(string[] paths)
    {
        DiagnosticDrivePaths = paths;
        tabDrives.IsChecked = true;
        var button = Find<Button>("scanAllBtn");
        button.RaiseEvent(new RoutedEventArgs(Button.ClickEvent, button));
    }
    internal void DiagnosticCapture(string directory, string name)
    { DiagnosticCapture(directory,name,1); }
    internal void DiagnosticCapture(string directory, string name, double scale)
    {
        Window.UpdateLayout();
        var content=(FrameworkElement)Window.Content;
        Render(Window,Path.Combine(directory,name+".png"),scale);
        var visuals = new List<object>();
        CaptureVisual(Window, visuals);
        var report = new {
            build = typeof(MainUI).Assembly.GetName().Version.ToString(), renderer = "WPF instantiated visual tree / RenderTargetBitmap",
            appearanceRevision=Appearance.Revision, appearanceMode=Appearance.Preferences.Mode, dark=Appearance.Dark, highContrast=Appearance.HighContrast,
            clientWidth=content.ActualWidth,clientHeight=content.ActualHeight,rasterDpi=96*scale,
            testMinimumWidth=Window.MinWidth,testMinimumHeight=Window.MinHeight,
            nativeDpiScale=PresentationSource.FromVisual(Window).CompositionTarget.TransformToDevice.M11,
            nativeCaption=System.Windows.Shell.WindowChrome.GetWindowChrome(Window)==null,
            nativeComposition=Appearance.Composition, nativeDarkResult=Appearance.NativeDarkResult,nativeCornerResult=Appearance.NativeCornerResult,
            nativeCaptionResult=Appearance.NativeCaptionResult,nativeCaptionTextResult=Appearance.NativeCaptionTextResult,
            systemAccentSource=Appearance.SystemAccentSource,
            units = "DIP", generation, revision = lastRenderedRevision, busy, ownedWorkers,
            rootOutcome = root == null ? null : root.Outcome,
            projectionRows = rows.Count, knownDirectories = displayNodes.Count,
            mountedRows = Enumerable.Range(0, rows.Count).Count(i => treeList.ItemContainerGenerator.ContainerFromIndex(i) != null),
            scrollOffset = DiagnosticScrollOffset,
            visualNodes = visuals.Count, truncated = visuals.Count >= 3000,
            presentationLimit = "Native WPF client render; viewport fixture may exceed headless desktop tracking bounds. Raster DPI is declared separately from actual window DPI. Not OS-caption capture, physical focus, DWM composition or pixels presented.",
            rows = rows.Select(r => new { path = r.Node != null ? r.Node.FullPath : r.File.Summary ? null : r.File.FullPath,
                role = r.Node != null ? "directory" : r.File.Summary ? "summary" : "file", text = r.DisplayName,
                expanded = r.IsExpanded, bytes = r.Node != null ? r.Node.Size : r.File.Size,
                state = r.Node == null ? null : r.Node.State.ToString(), selected = treeList.SelectedItems.Contains(r) }).ToArray(),
            visuals
        };
        File.WriteAllText(Path.Combine(directory, name + ".json"), new JavaScriptSerializer { MaxJsonLength = int.MaxValue }.Serialize(report));
    }
    static void CaptureVisual(DependencyObject node, List<object> result, Visual root=null)
    {
        if (result.Count >= 3000) return;
        if (root==null) root=(Visual)node;
        var element = node as FrameworkElement;
        var text = node as TextBlock;
        if (element != null && element.IsVisible)
        {
            Point origin;
            try { origin = element==root?new Point():element.TransformToAncestor(root).Transform(new Point()); }
            catch (InvalidOperationException) { origin = new Point(); }
            result.Add(new { id = element.Name, role = element.GetType().Name, text = text == null ? null : text.Text,
                x = origin.X, y = origin.Y, width = element.ActualWidth, height = element.ActualHeight,
                enabled = element.IsEnabled, focused = element.IsKeyboardFocused });
        }
        for (int i = 0; i < VisualTreeHelper.GetChildrenCount(node); i++) CaptureVisual(VisualTreeHelper.GetChild(node, i), result,root);
    }
}
#endif
