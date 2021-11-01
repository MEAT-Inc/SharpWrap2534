using System;
using System.Runtime.InteropServices;
using SharpWrap2534.SupportingLogic;

namespace SharpWrap2534.PassThruImport
{
    /// <summary>
    /// Impors a provided DLL file and maps functions out for the PassThru calls for it. 
    /// This can take any standard V0404 J2534 DLL input and provides basic interfacing for all the 
    /// DLLs native calls.
    /// </summary>
    internal class PassThruApiImporter
    {
        // Class values for the DLL to import.
        public string JDllPath;
        public IntPtr ModulePointer;

        // ---------------------------------CONSTRUCTOR LOGIC FOR CLASS -------------------------------------------

        /// <summary>
        /// Imports a new JDLL into the project and stores all of its outputs.
        /// </summary>
        /// <param name="DllPath"></param>
        public PassThruApiImporter(string DllPath)
        {
            // Store the DLL path ehre and import the path as an assy.
            JDllPath = DllPath;
            ModulePointer = Win32Invokers.LoadLibrary(JDllPath);

            // Check pointer value.
            if (ModulePointer == IntPtr.Zero)
                throw new COMException($"Failed to load new Win32 DLL! Exception: {Marshal.GetLastWin32Error()}");
        }

        /// <summary>
        /// DCTOR For this instance object.
        /// Removes the loaded lib objects
        /// </summary>
        ~PassThruApiImporter() { Win32Invokers.FreeLibrary(ModulePointer); }

        // --------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Runs the setup logic for this class 
        /// </summary>
        /// <returns></returns>
        public bool MapDelegateMethods(out PassThruDelegates DelegateSet)
        {
            // Build Delegate Set.
            DelegateSet = new PassThruDelegates();

            // Map methods here.
            try
            {
                // PASSTHRU OPEN
                IntPtr pAddressOfFunctionToCall = Win32Invokers.GetProcAddress(ModulePointer, "PassThruOpen");
                if (pAddressOfFunctionToCall != IntPtr.Zero)
                    DelegateSet.PTOpen = (PassThruDelegates.DelegatePassThruOpen)Marshal.GetDelegateForFunctionPointer(
                        pAddressOfFunctionToCall, typeof(PassThruDelegates.DelegatePassThruOpen));

                // PASSTHRU CLOSE
                pAddressOfFunctionToCall = Win32Invokers.GetProcAddress(ModulePointer, "PassThruClose");
                if (pAddressOfFunctionToCall != IntPtr.Zero)
                    DelegateSet.PTClose = (PassThruDelegates.DelegatePassThruClose)Marshal.GetDelegateForFunctionPointer(
                        pAddressOfFunctionToCall, typeof(PassThruDelegates.DelegatePassThruClose));

                // PASSTHRU CONNECT
                pAddressOfFunctionToCall = Win32Invokers.GetProcAddress(ModulePointer, "PassThruConnect");
                if (pAddressOfFunctionToCall != IntPtr.Zero)
                    DelegateSet.PTConnect = (PassThruDelegates.DelegatePassThruConnect)Marshal.GetDelegateForFunctionPointer(
                        pAddressOfFunctionToCall, typeof(PassThruDelegates.DelegatePassThruConnect));

                // PASSTHRU DISCONNECT
                pAddressOfFunctionToCall = Win32Invokers.GetProcAddress(ModulePointer, "PassThruDisconnect");
                if (pAddressOfFunctionToCall != IntPtr.Zero)
                    DelegateSet.PTDisconnect = (PassThruDelegates.DelegatePassThruDisconnect)Marshal.GetDelegateForFunctionPointer(
                        pAddressOfFunctionToCall, typeof(PassThruDelegates.DelegatePassThruDisconnect));

                // PASSTHRU READ MSGS
                pAddressOfFunctionToCall = Win32Invokers.GetProcAddress(ModulePointer, "PassThruReadMsgs");
                if (pAddressOfFunctionToCall != IntPtr.Zero)
                    DelegateSet.PTReadMsgs = (PassThruDelegates.DelegatePassThruReadMsgs)Marshal.GetDelegateForFunctionPointer(
                        pAddressOfFunctionToCall, typeof(PassThruDelegates.DelegatePassThruReadMsgs));

                // PASSTHRU WRITE MSGS
                pAddressOfFunctionToCall = Win32Invokers.GetProcAddress(ModulePointer, "PassThruWriteMsgs");
                if (pAddressOfFunctionToCall != IntPtr.Zero)
                    DelegateSet.PTWriteMsgs = (PassThruDelegates.DelegatePassThruWriteMsgs)Marshal.GetDelegateForFunctionPointer(
                        pAddressOfFunctionToCall, typeof(PassThruDelegates.DelegatePassThruWriteMsgs));

                // PASSTHRU START PERIODIC
                pAddressOfFunctionToCall = Win32Invokers.GetProcAddress(ModulePointer, "PassThruStartPeriodicMsg");
                if (pAddressOfFunctionToCall != IntPtr.Zero)
                    DelegateSet.PTStartPeriodicMsg = (PassThruDelegates.DelegatePassThruStartPeriodicMsg)Marshal.GetDelegateForFunctionPointer(
                        pAddressOfFunctionToCall, typeof(PassThruDelegates.DelegatePassThruStartPeriodicMsg));

                // PASSTHRU STOP PERIODIC
                pAddressOfFunctionToCall = Win32Invokers.GetProcAddress(ModulePointer, "PassThruStopPeriodicMsg");
                if (pAddressOfFunctionToCall != IntPtr.Zero)
                    DelegateSet.PTStopPeriodicMsg = (PassThruDelegates.DelegatePassThruStopPeriodicMsg)Marshal.GetDelegateForFunctionPointer(
                        pAddressOfFunctionToCall, typeof(PassThruDelegates.DelegatePassThruStopPeriodicMsg));

                // PASSTHRU START FILTER
                pAddressOfFunctionToCall = Win32Invokers.GetProcAddress(ModulePointer, "PassThruStartMsgFilter");
                if (pAddressOfFunctionToCall != IntPtr.Zero)
                    DelegateSet.PTStartMsgFilter = (PassThruDelegates.DelegatePassThruStartMsgFilter)Marshal.GetDelegateForFunctionPointer(
                        pAddressOfFunctionToCall, typeof(PassThruDelegates.DelegatePassThruStartMsgFilter));

                // PASSTHRU START FILTER
                pAddressOfFunctionToCall = Win32Invokers.GetProcAddress(ModulePointer, "PassThruStartMsgFilter");
                if (pAddressOfFunctionToCall != IntPtr.Zero)
                    DelegateSet.PTStartMsgFilterFlowPtr = (PassThruDelegates.DelegatePassThruStartMsgFilterFlowPtr)Marshal.GetDelegateForFunctionPointer(
                        pAddressOfFunctionToCall, typeof(PassThruDelegates.DelegatePassThruStartMsgFilterFlowPtr));

                // PASSTHRU STOP FILTER
                pAddressOfFunctionToCall = Win32Invokers.GetProcAddress(ModulePointer, "PassThruStopMsgFilter");
                if (pAddressOfFunctionToCall != IntPtr.Zero)
                    DelegateSet.PTStopMsgFilter = (PassThruDelegates.DelegatePassThruStopMsgFilter)Marshal.GetDelegateForFunctionPointer(
                        pAddressOfFunctionToCall, typeof(PassThruDelegates.DelegatePassThruStopMsgFilter));

                // PASSTHRU SET VOLTAGE
                pAddressOfFunctionToCall = Win32Invokers.GetProcAddress(ModulePointer, "PassThruSetProgrammingVoltage");
                if (pAddressOfFunctionToCall != IntPtr.Zero)
                    DelegateSet.PTSetProgrammingVoltage = (PassThruDelegates.DelegatePassThruSetProgrammingVoltage)Marshal.GetDelegateForFunctionPointer(
                        pAddressOfFunctionToCall, typeof(PassThruDelegates.DelegatePassThruSetProgrammingVoltage));

                // PASSTHRU READ VERSION
                pAddressOfFunctionToCall = Win32Invokers.GetProcAddress(ModulePointer, "PassThruReadVersion");
                if (pAddressOfFunctionToCall != IntPtr.Zero)
                    DelegateSet.PTReadVersion = (PassThruDelegates.DelegatePassThruReadVersion)Marshal.GetDelegateForFunctionPointer(
                        pAddressOfFunctionToCall, typeof(PassThruDelegates.DelegatePassThruReadVersion));

                // PASSTHRU GET ERROR
                pAddressOfFunctionToCall = Win32Invokers.GetProcAddress(ModulePointer, "PassThruGetLastError");
                if (pAddressOfFunctionToCall != IntPtr.Zero)
                    DelegateSet.PTGetLastError = (PassThruDelegates.DelegatePassThruGetLastError)Marshal.GetDelegateForFunctionPointer(
                        pAddressOfFunctionToCall, typeof(PassThruDelegates.DelegatePassThruGetLastError));

                // PASSTHRU IOCTL
                pAddressOfFunctionToCall = Win32Invokers.GetProcAddress(ModulePointer, "PassThruIoctl");
                if (pAddressOfFunctionToCall != IntPtr.Zero)
                    DelegateSet.PTIoctl = (PassThruDelegates.DelegatePassThruIoctl)Marshal.GetDelegateForFunctionPointer(
                        pAddressOfFunctionToCall, typeof(PassThruDelegates.DelegatePassThruIoctl));

                // ---------------------------------- DELEGATES FOR IMPORTING DEVICES ----------------------------------------------

                // USED FOR INIT NEXT DEVICE SETUP!
                pAddressOfFunctionToCall = Win32Invokers.GetProcAddress(ModulePointer, "PassThruGetNextCarDAQ");
                if (pAddressOfFunctionToCall != IntPtr.Zero)
                    DelegateSet.InitNextPassThruDevice = (PassThruDelegates.DelegateInitGetNextCarDAQ)Marshal.GetDelegateForFunctionPointer(
                        pAddressOfFunctionToCall, typeof(PassThruDelegates.DelegateInitGetNextCarDAQ));

                // USED FOR INIT NEXT DEVICE SETUP!
                pAddressOfFunctionToCall = Win32Invokers.GetProcAddress(ModulePointer, "PassThruGetNextCarDAQ");
                if (pAddressOfFunctionToCall != IntPtr.Zero)
                    DelegateSet.GetNextPassThruDevice = (PassThruDelegates.DelegateGetNextCarDAQ)Marshal.GetDelegateForFunctionPointer(
                        pAddressOfFunctionToCall, typeof(PassThruDelegates.DelegateGetNextCarDAQ));

                // USED FOR SCAN NEXT DEVICES (V0500 ONLY!)
                pAddressOfFunctionToCall = Win32Invokers.GetProcAddress(ModulePointer, "PassThruScanForDevices");
                if (pAddressOfFunctionToCall != IntPtr.Zero)
                    DelegateSet.PTScanForDevices = (PassThruDelegates.DelegatePassThruScanForDevices)Marshal.GetDelegateForFunctionPointer(
                        pAddressOfFunctionToCall, typeof(PassThruDelegates.DelegatePassThruScanForDevices));

                pAddressOfFunctionToCall = Win32Invokers.GetProcAddress(ModulePointer, "PassThruScanForDevices");
                if (pAddressOfFunctionToCall != IntPtr.Zero) 
                    DelegateSet.PTScanForDevicesPtr = (PassThruDelegates.DelegatePassThruScanForDevicesPtr)Marshal.GetDelegateForFunctionPointer(
                        pAddressOfFunctionToCall, typeof(PassThruDelegates.DelegatePassThruScanForDevicesPtr));

                // -----------------------------------------------------------------------------------------------------------------

                // Store ex value to nothing and return.
                // Win32Invokers.FreeLibrary(this.ModulePointer);
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
