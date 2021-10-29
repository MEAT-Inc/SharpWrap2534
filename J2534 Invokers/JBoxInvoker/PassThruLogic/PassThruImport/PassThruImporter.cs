using System;
using System.Runtime.InteropServices;
using JBoxInvoker.PassThruLogic.SupportingLogic;

namespace JBoxInvoker.PassThruLogic.PassThruImport
{
    /// <summary>
    /// Impors a provided DLL file and maps functions out for the PassThru calls for it. 
    /// This can take any standard V0404 J2534 DLL input and provides basic interfacing for all the 
    /// DLLs native calls.
    /// </summary>
    public class PassThruImporter
    {
        // Class values for the DLL to import.
        public string JDllPath;
        public IntPtr ModulePointer;

        // ---------------------------------CONSTRUCTOR LOGIC FOR CLASS -------------------------------------------

        /// <summary>
        /// Imports a new JDLL into the project and stores all of its outputs.
        /// </summary>
        /// <param name="DllPath"></param>
        public PassThruImporter(string DllPath)
        {
            // Store the DLL path ehre and import the path as an assy.
            this.JDllPath = DllPath;
            this.ModulePointer = Win32Invokers.LoadLibrary(this.JDllPath);

            // Check pointer value.
            if (this.ModulePointer == IntPtr.Zero) 
                throw new COMException($"Failed to load new Win32 DLL! Exception: {Marshal.GetLastWin32Error()}");
        }

        /// <summary>
        /// DCTOR For this instance object.
        /// Removes the loaded lib objects
        /// </summary>
        ~PassThruImporter() { Win32Invokers.FreeLibrary(this.ModulePointer); }

        // --------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Runs the setup logic for this class 
        /// </summary>
        /// <returns></returns>
        public bool MapDelegateMethods(out PassThruDelegates DelegateSet)
        {
            // Build Delegate Set.
            DelegateSet = new PassThruDelegates();

            // Mape methods here.
            try
            {
                // PASSTHRU OPEN
                IntPtr pAddressOfFunctionToCall = Win32Invokers.GetProcAddress(this.ModulePointer, "PassThruOpen");
                if (pAddressOfFunctionToCall != IntPtr.Zero)
                    DelegateSet.PTOpen = (PassThruDelegates.DelegatePassThruOpen)Marshal.GetDelegateForFunctionPointer(
                        pAddressOfFunctionToCall, typeof(PassThruDelegates.DelegatePassThruOpen));

                // PASSTHRU CLOSE
                pAddressOfFunctionToCall = Win32Invokers.GetProcAddress(this.ModulePointer, "PassThruClose");
                if (pAddressOfFunctionToCall != IntPtr.Zero)
                    DelegateSet.PTClose = (PassThruDelegates.DelegatePassThruClose)Marshal.GetDelegateForFunctionPointer(
                        pAddressOfFunctionToCall, typeof(PassThruDelegates.DelegatePassThruClose));

                // PASSTHRU CONNECT
                pAddressOfFunctionToCall = Win32Invokers.GetProcAddress(this.ModulePointer, "PassThruConnect");
                if (pAddressOfFunctionToCall != IntPtr.Zero)
                    DelegateSet.PTConnect = (PassThruDelegates.DelegatePassThruConnect)Marshal.GetDelegateForFunctionPointer(
                        pAddressOfFunctionToCall, typeof(PassThruDelegates.DelegatePassThruConnect));

                // PASSTHRU DISCONNECT
                pAddressOfFunctionToCall = Win32Invokers.GetProcAddress(this.ModulePointer, "PassThruDisconnect");
                if (pAddressOfFunctionToCall != IntPtr.Zero)
                    DelegateSet.PTDisconnect = (PassThruDelegates.DelegatePassThruDisconnect)Marshal.GetDelegateForFunctionPointer(
                        pAddressOfFunctionToCall, typeof(PassThruDelegates.DelegatePassThruDisconnect));

                // PASSTHRU READ MSGS
                pAddressOfFunctionToCall = Win32Invokers.GetProcAddress(this.ModulePointer, "PassThruReadMsgs");
                if (pAddressOfFunctionToCall != IntPtr.Zero)
                    DelegateSet.PTReadMsgs = (PassThruDelegates.DelegatePassThruReadMsgs)Marshal.GetDelegateForFunctionPointer(
                        pAddressOfFunctionToCall, typeof(PassThruDelegates.DelegatePassThruReadMsgs));

                // PASSTHRU WRITE MSGS
                pAddressOfFunctionToCall = Win32Invokers.GetProcAddress(this.ModulePointer, "PassThruWriteMsgs");
                if (pAddressOfFunctionToCall != IntPtr.Zero)
                    DelegateSet.PTWriteMsgs = (PassThruDelegates.DelegatePassThruWriteMsgs)Marshal.GetDelegateForFunctionPointer(
                        pAddressOfFunctionToCall, typeof(PassThruDelegates.DelegatePassThruWriteMsgs));

                // PASSTHRU START PERIODIC
                pAddressOfFunctionToCall = Win32Invokers.GetProcAddress(this.ModulePointer, "PassThruStartPeriodicMsg");
                if (pAddressOfFunctionToCall != IntPtr.Zero)
                    DelegateSet.PTStartPeriodicMsg = (PassThruDelegates.DelegatePassThruStartPeriodicMsg)Marshal.GetDelegateForFunctionPointer(
                        pAddressOfFunctionToCall, typeof(PassThruDelegates.DelegatePassThruStartPeriodicMsg));

                // PASSTHRU STOP PERIODIC
                pAddressOfFunctionToCall = Win32Invokers.GetProcAddress(this.ModulePointer, "PassThruStopPeriodicMsg");
                if (pAddressOfFunctionToCall != IntPtr.Zero)
                    DelegateSet.PTStopPeriodicMsg = (PassThruDelegates.DelegatePassThruStopPeriodicMsg)Marshal.GetDelegateForFunctionPointer(
                        pAddressOfFunctionToCall, typeof(PassThruDelegates.DelegatePassThruStopPeriodicMsg));

                // PASSTHRU START FILTER
                pAddressOfFunctionToCall = Win32Invokers.GetProcAddress(this.ModulePointer, "PassThruStartMsgFilter");
                if (pAddressOfFunctionToCall != IntPtr.Zero)
                    DelegateSet.PTStartMsgFilter = (PassThruDelegates.DelegatePassThruStartMsgFilter)Marshal.GetDelegateForFunctionPointer(
                        pAddressOfFunctionToCall, typeof(PassThruDelegates.DelegatePassThruStartMsgFilter));

                // PASSTHRU START FILTER
                pAddressOfFunctionToCall = Win32Invokers.GetProcAddress(this.ModulePointer, "PassThruStartMsgFilter");
                if (pAddressOfFunctionToCall != IntPtr.Zero)
                    DelegateSet.PTStartMsgFilterFlowPtr = (PassThruDelegates.DelegatePassThruStartMsgFilterFlowPtr)Marshal.GetDelegateForFunctionPointer(
                        pAddressOfFunctionToCall, typeof(PassThruDelegates.DelegatePassThruStartMsgFilterFlowPtr));

                // PASSTHRU STOP FILTER
                pAddressOfFunctionToCall = Win32Invokers.GetProcAddress(this.ModulePointer, "PassThruStopMsgFilter");
                if (pAddressOfFunctionToCall != IntPtr.Zero)
                    DelegateSet.PTStopMsgFilter = (PassThruDelegates.DelegatePassThruStopMsgFilter)Marshal.GetDelegateForFunctionPointer(
                        pAddressOfFunctionToCall, typeof(PassThruDelegates.DelegatePassThruStopMsgFilter));

                // PASSTHRU SET VOLTAGE
                pAddressOfFunctionToCall = Win32Invokers.GetProcAddress(this.ModulePointer, "PassThruSetProgrammingVoltage");
                if (pAddressOfFunctionToCall != IntPtr.Zero)
                    DelegateSet.PTSetProgrammingVoltage = (PassThruDelegates.DelegatePassThruSetProgrammingVoltage)Marshal.GetDelegateForFunctionPointer(
                        pAddressOfFunctionToCall, typeof(PassThruDelegates.DelegatePassThruSetProgrammingVoltage));

                // PASSTHRU READ VERSION
                pAddressOfFunctionToCall = Win32Invokers.GetProcAddress(this.ModulePointer, "PassThruReadVersion");
                if (pAddressOfFunctionToCall != IntPtr.Zero)
                    DelegateSet.PTReadVersion = (PassThruDelegates.DelegatePassThruReadVersion)Marshal.GetDelegateForFunctionPointer(
                        pAddressOfFunctionToCall, typeof(PassThruDelegates.DelegatePassThruReadVersion));

                // PASSTHRU GET ERROR
                pAddressOfFunctionToCall = Win32Invokers.GetProcAddress(this.ModulePointer, "PassThruGetLastError");
                if (pAddressOfFunctionToCall != IntPtr.Zero)
                    DelegateSet.PTGetLastError = (PassThruDelegates.DelegatePassThruGetLastError)Marshal.GetDelegateForFunctionPointer(
                        pAddressOfFunctionToCall, typeof(PassThruDelegates.DelegatePassThruGetLastError));

                // PASSTHRU IOCTL
                pAddressOfFunctionToCall = Win32Invokers.GetProcAddress(this.ModulePointer, "PassThruIoctl");
                if (pAddressOfFunctionToCall != IntPtr.Zero)
                    DelegateSet.PTIoctl = (PassThruDelegates.DelegatePassThruIoctl)Marshal.GetDelegateForFunctionPointer(
                        pAddressOfFunctionToCall, typeof(PassThruDelegates.DelegatePassThruIoctl));

                // Store ex value to nothing and return.
                Win32Invokers.FreeLibrary(this.ModulePointer);
                return true;
            }
            catch (Exception Ex)
            {
                // Throw new ex and set values.
                return false;
            }
        }
    }
}
