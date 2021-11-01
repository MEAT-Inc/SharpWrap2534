using System;
using System.Runtime.InteropServices;

namespace JBoxInvoker.PassThruLogic.SupportingLogic
{
    /// <summary>
    /// Static methods for running Win32 importing calls.
    /// </summary>
    internal static class Win32Invokers
    {
        // Loads a DLL into the memory.
        [DllImport("kernel32.dll", EntryPoint = "LoadLibrary", SetLastError = true)]
        public static extern IntPtr LoadLibrary([MarshalAs(UnmanagedType.LPStr)] string lpLibFileName);

        // Gets function address in the memory.
        [DllImport("kernel32.dll", EntryPoint = "GetProcAddress")]
        public static extern IntPtr GetProcAddress(IntPtr hModule, [MarshalAs(UnmanagedType.LPStr)] string lpProcName);
        
        // Unloads the lib object.
        [DllImport("kernel32.dll", EntryPoint = "FreeLibrary")]
        public static extern bool FreeLibrary(IntPtr hModule);
    }
}
