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
using System.Web.Script.Serialization;

partial class MainUI
{
    internal bool DiagnosticBusy { get { return busy; } }
    internal Node DiagnosticRoot { get { return root; } }
    internal long DiagnosticRevision { get { return lastRenderedRevision; } }
    internal long DiagnosticGeneration { get { return generation; } }
    internal int DiagnosticWorkers { get { return ownedWorkers; } }
    internal int DiagnosticDirectoryCacheCount { get { return dirCache.Count; } }
    internal Dictionary<string, long[]> DiagnosticExtensions { get { return displayExtensions; } }
    internal Row DiagnosticSelect(string path)
    {
        tabTree.IsChecked = true;
        var row = directoryRows[displayNodes[path]];
        treeList.SelectedItem = row;
        treeList.ScrollIntoView(row);
        treeList.UpdateLayout();
        return row;
    }
    internal object DiagnosticSelection { get { return treeList.SelectedItem; } }
    static ScrollViewer FindScroll(DependencyObject node)
    {
        var viewer = node as ScrollViewer; if (viewer != null) return viewer;
        for (int i = 0; i < VisualTreeHelper.GetChildrenCount(node); i++) { viewer = FindScroll(VisualTreeHelper.GetChild(node, i)); if (viewer != null) return viewer; }
        return null;
    }
    internal double DiagnosticScrollOffset { get { return FindScroll(treeList).VerticalOffset; } }
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
    {
        Window.UpdateLayout();
        var bitmap = new RenderTargetBitmap((int)Window.ActualWidth, (int)Window.ActualHeight, 96, 96, PixelFormats.Pbgra32);
        bitmap.Render(Window);
        var encoder = new PngBitmapEncoder(); encoder.Frames.Add(BitmapFrame.Create(bitmap));
        using (var file = File.Create(Path.Combine(directory, name + ".png"))) encoder.Save(file);
        var visuals = new List<object>();
        CaptureVisual(Window, visuals);
        var report = new {
            build = typeof(MainUI).Assembly.GetName().Version.ToString(), renderer = "WPF instantiated visual tree / RenderTargetBitmap",
            units = "DIP", generation, revision = lastRenderedRevision, busy, ownedWorkers,
            rootOutcome = root == null ? null : root.Outcome,
            projectionRows = rows.Count, knownDirectories = displayNodes.Count,
            mountedRows = Enumerable.Range(0, rows.Count).Count(i => treeList.ItemContainerGenerator.ContainerFromIndex(i) != null),
            scrollOffset = DiagnosticScrollOffset,
            visualNodes = visuals.Count, truncated = visuals.Count >= 3000,
            presentationLimit = "Hidden native window; offscreen WPF render, not proof of physical focus, DWM composition or pixels presented.",
            rows = rows.Select(r => new { path = r.Node != null ? r.Node.FullPath : r.File.Summary ? null : r.File.FullPath,
                role = r.Node != null ? "directory" : r.File.Summary ? "summary" : "file", text = r.DisplayName,
                expanded = r.IsExpanded, bytes = r.Node != null ? r.Node.Size : r.File.Size,
                state = r.Node == null ? null : r.Node.State.ToString(), selected = treeList.SelectedItems.Contains(r) }).ToArray(),
            visuals
        };
        File.WriteAllText(Path.Combine(directory, name + ".json"), new JavaScriptSerializer { MaxJsonLength = int.MaxValue }.Serialize(report));
    }
    static void CaptureVisual(DependencyObject node, List<object> result)
    {
        if (result.Count >= 3000) return;
        var element = node as FrameworkElement;
        var text = node as TextBlock;
        if (element != null && element.IsVisible)
        {
            Point origin;
            try { origin = element.TransformToAncestor(System.Windows.Window.GetWindow(element)).Transform(new Point()); }
            catch (InvalidOperationException) { origin = new Point(); }
            result.Add(new { id = element.Name, role = element.GetType().Name, text = text == null ? null : text.Text,
                x = origin.X, y = origin.Y, width = element.ActualWidth, height = element.ActualHeight,
                enabled = element.IsEnabled, focused = element.IsKeyboardFocused });
        }
        for (int i = 0; i < VisualTreeHelper.GetChildrenCount(node); i++) CaptureVisual(VisualTreeHelper.GetChild(node, i), result);
    }
}
#endif
