using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Windows.Automation;
using System.Web.Script.Serialization;

class InstalledLaunchProbe
{
    [STAThread]
    static int Main(string[] args)
    {
        Process process=null;AutomationElement status=null,root=null;AutomationPropertyChangedEventHandler changed=null;
        AutomationEventHandler opened=null;
        using(var finished=new ManualResetEvent(false))
        using(var windowReady=new ManualResetEvent(false))
        {
            string terminal=null;
            try
            {
                if(!args[0].Equals(@"C:\Program Files (x86)\Darkaxt\Diskweft\Diskweft.exe",StringComparison.OrdinalIgnoreCase)
                    || !(args[1].Equals(@"E:\Temp\Diskweft-release-verification-20261104\installed-fixture",StringComparison.OrdinalIgnoreCase)
                         || args[1].Equals(@"E:\Temp\Diskweft-file-actions-resume-20261104\installed-fixture",StringComparison.OrdinalIgnoreCase)))throw new Exception("Use the exact installed application and owned fixture.");
                var start=new ProcessStartInfo(args[0],"\""+args[1]+"\""){UseShellExecute=false};
                opened=(sender,e)=>{
                    try {
                        var candidate=(AutomationElement)sender;
                        if(process!=null && candidate.Current.ProcessId==process.Id && candidate.Current.Name=="Diskweft")
                        {root=candidate;windowReady.Set();}
                    }catch(ElementNotAvailableException) { }
                };
                Automation.AddAutomationEventHandler(WindowPattern.WindowOpenedEvent,AutomationElement.RootElement,TreeScope.Subtree,opened);
                process=Process.Start(start);process.EnableRaisingEvents=true;
                process.Exited+=(s,e)=>{windowReady.Set();finished.Set();};
                if(!process.WaitForInputIdle())throw new Exception("Actual application exited before input-idle.");
                process.Refresh();
                // A first-run notes window can become Process.MainWindowHandle.
                // Bind the actual owned application window by PID and title.
                root=AutomationElement.RootElement.FindFirst(TreeScope.Children,new AndCondition(
                    new PropertyCondition(AutomationElement.ProcessIdProperty,process.Id),
                    new PropertyCondition(AutomationElement.NameProperty,"Diskweft")));
                if(root!=null)windowReady.Set();
                // Input-idle may precede the first WPF window. Observe its real
                // WindowOpened transition, with exit as the other terminal event.
                windowReady.WaitOne();
                if(root==null)throw new Exception("Application exited before its owned main window opened.");
                Automation.RemoveAutomationEventHandler(WindowPattern.WindowOpenedEvent,AutomationElement.RootElement,opened);opened=null;
                status=root.FindFirst(TreeScope.Descendants,new PropertyCondition(AutomationElement.AutomationIdProperty,"status"));
                if(status==null)throw new Exception("Actual installed status automation element is absent.");
                Action observe=()=>{
                    string text=status.Current.Name;
                    if(text.StartsWith("COMPLETE",StringComparison.Ordinal) || text.StartsWith("FAILED",StringComparison.Ordinal) || text.StartsWith("STOPPED",StringComparison.Ordinal))
                    {terminal=text;finished.Set();}
                };
                changed=(sender,e)=>observe();
                Automation.AddAutomationPropertyChangedEventHandler(status,TreeScope.Element,changed,AutomationElement.NameProperty);
                observe();finished.WaitOne();
                if(terminal==null || !terminal.StartsWith("COMPLETE",StringComparison.Ordinal) || !terminal.Contains("classic") || !terminal.Contains(args[1]))throw new Exception("Actual bare-path launch did not finish the requested scan: "+terminal);
                int pid=process.Id;
                Automation.RemoveAutomationPropertyChangedEventHandler(status,changed);changed=null;
                ((WindowPattern)root.GetCurrentPattern(WindowPattern.Pattern)).Close();
                process.WaitForExit();
                if(process.ExitCode!=0)throw new Exception("Actual installed application exited with "+process.ExitCode);
                File.WriteAllText(args[2],new JavaScriptSerializer().Serialize(new{passed=true,pid=pid,actualExecutable=args[0],barePath=args[1],status=terminal,normalClose=true,exitCode=process.ExitCode,boundary="Actual installed EXE process; read-only UI Automation status; event-driven completion; normal owned-window close"}));
                return 0;
            }
            catch(Exception ex){Console.Error.WriteLine(ex);return 1;}
            finally
            {
                if(changed!=null && status!=null)Automation.RemoveAutomationPropertyChangedEventHandler(status,changed);
                if(process!=null && !process.HasExited)
                {
                    process.Refresh();
                    if(process.MainWindowHandle==IntPtr.Zero){windowReady.WaitOne();process.Refresh();}
                    if(root!=null)((WindowPattern)root.GetCurrentPattern(WindowPattern.Pattern)).Close();
                    else process.CloseMainWindow();
                    process.WaitForExit();
                }
                if(opened!=null)Automation.RemoveAutomationEventHandler(WindowPattern.WindowOpenedEvent,AutomationElement.RootElement,opened);
            }
        }
    }
}
