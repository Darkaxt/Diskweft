using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
using System.Web.Script.Serialization;

class Stage4Bench
{
    static readonly BindingFlags Static = BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;
    static readonly BindingFlags Instance = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
    static object Field(object owner, string name) { return owner.GetType().GetField(name, Instance).GetValue(owner); }
    static int Directories(object node)
    {
        int count=1;
        foreach (object child in (System.Collections.IEnumerable)Field(node,"Dirs")) count+=Directories(child);
        return count;
    }
    static void Save(string file, object value) { File.WriteAllText(file,new JavaScriptSerializer().Serialize(value)); }
    [STAThread]
    static int Main(string[] args)
    {
        try
        {
            if (args[0]=="native")return Native(args[1],args[2]);
            if (args[0]=="prepare")
            {
                string fixture=Path.GetFullPath(args[1]);
                if (!fixture.StartsWith(@"D:\Temp\Diskweft-s4-",StringComparison.OrdinalIgnoreCase) || Directory.Exists(fixture))
                    throw new InvalidOperationException("Prepare requires a new exact task-owned D: fixture.");
                Directory.CreateDirectory(fixture);
                byte[] bytes=new byte[65536];
                for(int i=0;i<bytes.Length;i++)bytes[i]=(byte)(i%251);
                for(int directory=0;directory<1000;directory++)
                {
                    string path=Path.Combine(fixture,"group"+(directory/100).ToString("D2"),"folder"+directory.ToString("D4"));
                    Directory.CreateDirectory(path);
                    for(int file=0;file<1000;file++)
                        using(var stream=new FileStream(Path.Combine(path,"item"+file.ToString("D4")+(file%2==0?".bin":".txt")),FileMode.CreateNew,FileAccess.Write,FileShare.None))
                        { int length=file%100==0?65536:file%10==0?1024:0; stream.Write(bytes,0,length); }
                    if(directory%100==99)Console.WriteLine("Prepared "+((directory+1)*1000)+" files");
                }
                Save(args[2],new {passed=true,files=1000000,directories=1011,bytes=747520000L,distribution="90% empty, 9% 1KiB, 1% 64KiB; equal bin/txt names"});
                return 0;
            }
            string kind=args[0], engine=args[1], root=args[2], output=args[3];
            var process=Process.GetCurrentProcess();
            var cpu=process.TotalProcessorTime;
            var clock=new Stopwatch(); object result;
            var beforeAssemblies=AppDomain.CurrentDomain.GetAssemblies().Select(a=>a.GetName().Name).ToArray();
            double publicationMs=0; int mailboxDirectories=0,mailboxLargest=0; long revisions=0;
            string outcome="baseline";
            if(kind=="baseline")
            {
                var assembly=Assembly.LoadFrom(args[4]);
                var scanner=assembly.GetType("Scanner");
                assembly.GetType("Stats").GetMethod("Reset",Static).Invoke(null,null);
                scanner.GetField("Cancel",Static).SetValue(null,false);
                scanner.GetField("CollectExt",Static).SetValue(null,true);
                var node=assembly.GetType("Node");
                result=Activator.CreateInstance(node,true);node.GetField("Name").SetValue(result,root);
                clock.Start();
                if(engine=="mft")result=assembly.GetType("Mft").GetMethod("Scan",Static).Invoke(null,new object[]{root});
                else scanner.GetMethod("Scan",Static).Invoke(null,new object[]{result,root});
                clock.Stop();
            }
            else
            {
                Exclude.Set(new string[0]);
                var session=new ScanSession(new[]{root},false,engine=="mft");
                clock.Start(); session.Run(); clock.Stop();
                result=session.Root;outcome=session.Outcome;
                if(engine=="mft" && !session.Mode.Contains("MFT (progressive records)"))throw new Exception("MFT benchmark fell back: "+session.Mode);
                publicationMs=1000.0*session.PublicationTicks/Stopwatch.Frequency;
                mailboxDirectories=session.MaxMailboxDirectories;mailboxLargest=session.MaxMailboxLargest;
                var batch=session.Drain();revisions=batch.Revision;
            }
            process.Refresh();
            long files=(long)Field(result,"Files"), bytesTotal=(long)Field(result,"Size");
            int directories=Directories(result);
            if(engine=="classic" && (files!=1000000 || bytesTotal!=747520000L || directories!=1011))throw new Exception("Million-file fixture totals do not match: "+files+"/"+bytesTotal+"/"+directories);
            if(kind!="baseline" && (mailboxDirectories>directories || mailboxLargest>Stats.TopN))throw new Exception("Unconsumed mailbox exceeded directory/bounded-top limits.");
            Save(output,new {passed=true,kind=kind,engine=engine,milliseconds=clock.Elapsed.TotalMilliseconds,cpuMilliseconds=(process.TotalProcessorTime-cpu).TotalMilliseconds,
                peakWorkingSet=process.PeakWorkingSet64,peakPagedBytes=process.PeakPagedMemorySize64,privateBytesAtCompletion=process.PrivateMemorySize64,files=files,bytes=bytesTotal,directories=directories,outcome=outcome,
                publicationMilliseconds=publicationMs,maxMailboxDirectories=mailboxDirectories,maxMailboxLargest=mailboxLargest,revisions=revisions,
                loadedAssemblies=AppDomain.CurrentDomain.GetAssemblies().Select(a=>a.GetName().Name).Except(beforeAssemblies).ToArray()});
            Console.WriteLine(File.ReadAllText(output));return 0;
        }
        catch(Exception ex){Console.Error.WriteLine(ex);return 1;}
    }
    static int Native(string fixture,string output)
    {
        if(Environment.GetEnvironmentVariable("DISKWEFT_SANDBOX")!="1")throw new Exception("Native performance must run in the owned guest.");
        Directory.CreateDirectory(output);
        Environment.SetEnvironmentVariable("DISKWEFT_TEST_ROOT",Path.Combine(output,"profile"));
        var app=new Application();var ui=new MainUI();
        var samples=new List<double>();Exception failure=null;
        long started=0, usableMetadata=0;double firstUseful=-1;int waiting=0;bool selected=false;
        ScanSession.Published=(session,node)=>{if(usableMetadata==0 && session.Root!=null && session.Root.Files>0)Interlocked.CompareExchange(ref usableMetadata,Stopwatch.GetTimestamp(),0);};
        var timer=new Timer(state=>{
            if(Interlocked.CompareExchange(ref waiting,1,0)!=0)return;
            long sent=Stopwatch.GetTimestamp();
            ui.Window.Dispatcher.BeginInvoke(DispatcherPriority.Normal,new Action(()=>{
                if(ui.DiagnosticBusy)samples.Add(1000.0*(Stopwatch.GetTimestamp()-sent)/Stopwatch.Frequency);
                Interlocked.Exchange(ref waiting,0);
            }));
        },null,Timeout.Infinite,Timeout.Infinite);
        ui.ScanApplied+=(owner,batch)=>{
            if(!batch.Terminal && owner.DiagnosticRoot!=null && owner.DiagnosticRoot.Files>0)
            {
                if(firstUseful<0)firstUseful=1000.0*(Stopwatch.GetTimestamp()-Interlocked.Read(ref usableMetadata))/Stopwatch.Frequency;
                if(!selected && owner.DiagnosticRoot.Dirs.Count>0)
                {
                    selected=true;owner.DiagnosticSelect(owner.DiagnosticRoot.Dirs[0].FullPath);owner.DiagnosticKey(Key.Right);
                }
            }
        };
        ui.BecameIdle+=owner=>{
            try
            {
                timer.Change(Timeout.Infinite,Timeout.Infinite);
                samples.Sort();
                if(owner.DiagnosticRoot==null || owner.DiagnosticRoot.Files!=1000000 || owner.DiagnosticRoot.Size!=747520000L)throw new Exception("Native million-file totals mismatch.");
                double p95=samples.Count==0?double.PositiveInfinity:samples[(int)Math.Ceiling(samples.Count*.95)-1];
                if(samples.Count<20 || p95>100 || firstUseful<0 || firstUseful>1000 || !selected)throw new Exception("Native performance acceptance failed: p95="+p95+", samples="+samples.Count+", firstUseful="+firstUseful);
                owner.DiagnosticCapture(output,"million-terminal");
                Save(Path.Combine(output,"result.json"),new {passed=true,files=owner.DiagnosticRoot.Files,bytes=owner.DiagnosticRoot.Size,
                    firstUsefulFromProducerPublicationMs=firstUseful,startToUsefulProducerMetadataMs=1000.0*(usableMetadata-started)/Stopwatch.Frequency,
                    startToFirstUsefulWpfMs=1000.0*(usableMetadata-started)/Stopwatch.Frequency+firstUseful,
                    dispatcherP95Ms=p95,dispatcherMaxMs=samples.Last(),samples=samples.Count,navigationDuringScan=selected,ownedWorkers=owner.DiagnosticWorkers,
                    boundary="Owned headless guest WPF dispatcher actions; real million-file readonly host mapping; metadata origin is first nonzero resolved producer publication, not first raw OS record; 50ms diagnostic cadence, one outstanding sample"});
            }
            catch(Exception ex){failure=ex;}
            owner.Window.Close();
        };
        app.DispatcherUnhandledException+=(s,e)=>{failure=e.Exception;e.Handled=true;ui.Window.Close();};
        ui.Window.Loaded+=delegate{started=Stopwatch.GetTimestamp();timer.Change(0,50);ui.DiagnosticScan(fixture);};
        try{app.Run(ui.Window);}finally{timer.Dispose();ScanSession.Published=null;}
        if(failure!=null)throw failure;
        if(!File.Exists(Path.Combine(output,"result.json")))throw new Exception("Native performance ended without acceptance result.");
        return 0;
    }
}
