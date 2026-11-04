using System;
using System.Runtime.InteropServices;
using System.Threading;

// A delete-only binding to the Windows Shell. Unlike Visual Basic FileSystem,
// this does not switch to permanent deletion in a noninteractive process.
static class ShellDelete
{
    [ComImport, Guid("43826D1E-E718-42EE-BC55-A1E261C37BFE"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    interface IShellItem
    {
        void BindToHandler(IntPtr context,ref Guid handler,ref Guid iid,out IntPtr result);
        void GetParent(out IShellItem parent);
        void GetDisplayName(uint kind,out IntPtr name);
        void GetAttributes(uint mask,out uint attributes);
        void Compare(IShellItem other,uint hint,out int order);
    }
    // Full native vtable order is required even though only delete methods are used.
    [ComImport, Guid("947AAB5F-0A5C-4C13-B4D6-4BF7836FC9F8"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    interface IFileOperation
    {
        void Advise(IntPtr sink,out uint cookie);
        void Unadvise(uint cookie);
        void SetOperationFlags(uint flags);
        void SetProgressMessage([MarshalAs(UnmanagedType.LPWStr)]string message);
        void SetProgressDialog(IntPtr dialog);
        void SetProperties(IntPtr changes);
        void SetOwnerWindow(IntPtr owner);
        void ApplyPropertiesToItem(IShellItem item);
        void ApplyPropertiesToItems([MarshalAs(UnmanagedType.IUnknown)]object items);
        void RenameItem(IShellItem item,[MarshalAs(UnmanagedType.LPWStr)]string name,IntPtr sink);
        void RenameItems([MarshalAs(UnmanagedType.IUnknown)]object items,[MarshalAs(UnmanagedType.LPWStr)]string name);
        void MoveItem(IShellItem item,IShellItem destination,[MarshalAs(UnmanagedType.LPWStr)]string name,IntPtr sink);
        void MoveItems([MarshalAs(UnmanagedType.IUnknown)]object items,IShellItem destination);
        void CopyItem(IShellItem item,IShellItem destination,[MarshalAs(UnmanagedType.LPWStr)]string name,IntPtr sink);
        void CopyItems([MarshalAs(UnmanagedType.IUnknown)]object items,IShellItem destination);
        void DeleteItem(IShellItem item,IntPtr sink);
        void DeleteItems([MarshalAs(UnmanagedType.IUnknown)]object items);
        void NewItem(IShellItem destination,uint attributes,[MarshalAs(UnmanagedType.LPWStr)]string name,[MarshalAs(UnmanagedType.LPWStr)]string template,IntPtr sink);
        void PerformOperations();
        void GetAnyOperationsAborted([MarshalAs(UnmanagedType.Bool)]out bool aborted);
    }
    [DllImport("shell32.dll",CharSet=CharSet.Unicode,PreserveSig=false)]
    static extern void SHCreateItemFromParsingName(string path,IntPtr context,ref Guid iid,out IShellItem item);

    internal static void Run(string path,bool permanent,IntPtr owner)
    {
        if(Thread.CurrentThread.GetApartmentState()!=ApartmentState.STA)
            throw new InvalidOperationException("Shell deletion requires its owned STA worker.");
        IFileOperation operation=null;IShellItem item=null;
        try
        {
            operation=(IFileOperation)Activator.CreateInstance(Type.GetTypeFromCLSID(new Guid("3AD05575-8857-4850-9277-11B85BDB8E09"),true));
            // App confirmation is already complete. Fail on errors and report on
            // the dispatcher; no native UI, silent skip or automatic elevation.
            uint flags=0x0004|0x0010|0x0400|0x00100000; // SILENT, NOCONFIRMATION, NOERRORUI, EARLYFAILURE
            if(!permanent)flags|=0x0040|0x00080000; // ALLOWUNDO, RECYCLEONDELETE
            operation.SetOperationFlags(flags);operation.SetOwnerWindow(owner);
            Guid iid=typeof(IShellItem).GUID;
            SHCreateItemFromParsingName(path,IntPtr.Zero,ref iid,out item);
            operation.DeleteItem(item,IntPtr.Zero);operation.PerformOperations();
            bool aborted;operation.GetAnyOperationsAborted(out aborted);
            if(aborted)throw new OperationCanceledException("The Windows shell did not complete the file operation.");
        }
        finally
        {
            if(item!=null)Marshal.FinalReleaseComObject(item);
            if(operation!=null)Marshal.FinalReleaseComObject(operation);
        }
    }
}
