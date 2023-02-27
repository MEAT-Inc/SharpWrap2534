using System;
using System.Runtime.InteropServices;
using SharpWrapper.PassThruSupport;

namespace SharpWrapper.PassThruImport
{
    /// <summary>
    /// Imports a provided DLL file and maps functions out for the PassThru calls for it. 
    /// This can take any standard V0404 J2534 DLL input and provides basic interfacing for all the 
    /// DLLs native calls.
    /// </summary>
    internal class PassThruApiImporter
    {
        // Class values for the DLL to import.
        public string JDllPath;
        public IntPtr ModulePointer;

        // ------------------------------------------------------------------------------------------------------------------------------------------
        
        // Loads a DLL into the memory.
        [DllImport("kernel32.dll", EntryPoint = "LoadLibrary", SetLastError = true)]
        public static extern IntPtr LoadLibrary([MarshalAs(UnmanagedType.LPStr)] string lpLibFileName);

        // Gets function address in the memory.
        [DllImport("kernel32.dll", EntryPoint = "GetProcAddress")]
        public static extern IntPtr GetProcAddress(IntPtr hModule, [MarshalAs(UnmanagedType.LPStr)] string lpProcName);

        // Unloads the lib object.
        [DllImport("kernel32.dll", EntryPoint = "FreeLibrary")]
        public static extern bool FreeLibrary(IntPtr hModule);

        // Get the error from the import call
        [DllImport("kernel32.dll")]
        public static extern uint GetLastError();

        // ------------------------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Imports a new JDLL into the project and stores all of its outputs.
        /// </summary>
        /// <param name="DllPath"></param>
        public PassThruApiImporter(string DllPath)
        {
            // Store the DLL path ehre and import the path as an assy.
            JDllPath = DllPath;
            ModulePointer = LoadLibrary(JDllPath);

            // Check pointer value.
            if (ModulePointer == IntPtr.Zero)
                throw new COMException($"Failed to load new Win32 DLL! Exception: {Marshal.GetLastWin32Error()}");
        }
        /// <summary>
        /// DCTOR For this instance object.
        /// Removes the loaded lib objects
        /// </summary>
        ~PassThruApiImporter() { FreeLibrary(ModulePointer); }

        // ------------------------------------------------------------------------------------------------------------------------------------------

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
                // ---------------------------------- DELEGATES FOR IMPORTING DEVICES ----------------------------------------------

                // USED FOR INIT NEXT DEVICE SETUP!
                IntPtr pAddressOfFunctionToCall = GetProcAddress(ModulePointer, "PassThruGetNextCarDAQ");
                if (pAddressOfFunctionToCall != IntPtr.Zero)
                    DelegateSet.PTInitNextPassThruDevice = (PassThruDelegates.DelegateInitGetNextCarDAQ)Marshal.GetDelegateForFunctionPointer(
                        pAddressOfFunctionToCall, typeof(PassThruDelegates.DelegateInitGetNextCarDAQ));

                // USED FOR INIT NEXT DEVICE SETUP!
                pAddressOfFunctionToCall = GetProcAddress(ModulePointer, "PassThruGetNextCarDAQ");
                if (pAddressOfFunctionToCall != IntPtr.Zero)
                    DelegateSet.PTGetNextPassThruDevice = (PassThruDelegates.DelegateGetNextCarDAQ)Marshal.GetDelegateForFunctionPointer(
                        pAddressOfFunctionToCall, typeof(PassThruDelegates.DelegateGetNextCarDAQ));

                // USED FOR SCAN NEXT DEVICES (V0500 ONLY!)
                pAddressOfFunctionToCall = GetProcAddress(ModulePointer, "PassThruScanForDevices");
                if (pAddressOfFunctionToCall != IntPtr.Zero)
                    DelegateSet.PTScanForDevices = (PassThruDelegates.DelegatePassThruScanForDevices)Marshal.GetDelegateForFunctionPointer(
                        pAddressOfFunctionToCall, typeof(PassThruDelegates.DelegatePassThruScanForDevices));

                // USED FOR GET NEXT DEVICES (V0500 ONLY!)
                pAddressOfFunctionToCall = GetProcAddress(ModulePointer, "PassThruGetNextDevice");
                if (pAddressOfFunctionToCall != IntPtr.Zero)
                    DelegateSet.PTGetNextDevice = (PassThruDelegates.DelegatePassThruGetNextDevice)Marshal.GetDelegateForFunctionPointer(
                        pAddressOfFunctionToCall, typeof(PassThruDelegates.DelegatePassThruGetNextDevice));

                // -----------------------------------------------------------------------------------------------------------------

                // PASSTHRU OPEN
                pAddressOfFunctionToCall = GetProcAddress(ModulePointer, "PassThruOpen");
                if (pAddressOfFunctionToCall != IntPtr.Zero)
                    DelegateSet.PTOpen = (PassThruDelegates.DelegatePassThruOpen)Marshal.GetDelegateForFunctionPointer(
                        pAddressOfFunctionToCall, typeof(PassThruDelegates.DelegatePassThruOpen));

                // PASSTHRU CLOSE
                pAddressOfFunctionToCall = GetProcAddress(ModulePointer, "PassThruClose");
                if (pAddressOfFunctionToCall != IntPtr.Zero)
                    DelegateSet.PTClose = (PassThruDelegates.DelegatePassThruClose)Marshal.GetDelegateForFunctionPointer(
                        pAddressOfFunctionToCall, typeof(PassThruDelegates.DelegatePassThruClose));

                // PASSTHRU CONNECT
                pAddressOfFunctionToCall = GetProcAddress(ModulePointer, "PassThruConnect");
                if (pAddressOfFunctionToCall != IntPtr.Zero)
                    DelegateSet.PTConnect = (PassThruDelegates.DelegatePassThruConnect)Marshal.GetDelegateForFunctionPointer(
                        pAddressOfFunctionToCall, typeof(PassThruDelegates.DelegatePassThruConnect));

                // PASSTHRU DISCONNECT
                pAddressOfFunctionToCall = GetProcAddress(ModulePointer, "PassThruDisconnect");
                if (pAddressOfFunctionToCall != IntPtr.Zero)
                    DelegateSet.PTDisconnect = (PassThruDelegates.DelegatePassThruDisconnect)Marshal.GetDelegateForFunctionPointer(
                        pAddressOfFunctionToCall, typeof(PassThruDelegates.DelegatePassThruDisconnect));

                // PASSTHRU READ MSGS
                pAddressOfFunctionToCall = GetProcAddress(ModulePointer, "PassThruReadMsgs");
                if (pAddressOfFunctionToCall != IntPtr.Zero)
                    DelegateSet.PTReadMsgs = (PassThruDelegates.DelegatePassThruReadMsgs)Marshal.GetDelegateForFunctionPointer(
                        pAddressOfFunctionToCall, typeof(PassThruDelegates.DelegatePassThruReadMsgs));

                // PASSTHRU WRITE MSGS
                pAddressOfFunctionToCall = GetProcAddress(ModulePointer, "PassThruWriteMsgs");
                if (pAddressOfFunctionToCall != IntPtr.Zero)
                    DelegateSet.PTWriteMsgs = (PassThruDelegates.DelegatePassThruWriteMsgs)Marshal.GetDelegateForFunctionPointer(
                        pAddressOfFunctionToCall, typeof(PassThruDelegates.DelegatePassThruWriteMsgs));

                // PASSTHRU START PERIODIC
                pAddressOfFunctionToCall = GetProcAddress(ModulePointer, "PassThruStartPeriodicMsg");
                if (pAddressOfFunctionToCall != IntPtr.Zero)
                    DelegateSet.PTStartPeriodicMsg = (PassThruDelegates.DelegatePassThruStartPeriodicMsg)Marshal.GetDelegateForFunctionPointer(
                        pAddressOfFunctionToCall, typeof(PassThruDelegates.DelegatePassThruStartPeriodicMsg));

                // PASSTHRU STOP PERIODIC
                pAddressOfFunctionToCall = GetProcAddress(ModulePointer, "PassThruStopPeriodicMsg");
                if (pAddressOfFunctionToCall != IntPtr.Zero)
                    DelegateSet.PTStopPeriodicMsg = (PassThruDelegates.DelegatePassThruStopPeriodicMsg)Marshal.GetDelegateForFunctionPointer(
                        pAddressOfFunctionToCall, typeof(PassThruDelegates.DelegatePassThruStopPeriodicMsg));

                // PASSTHRU START FILTER
                pAddressOfFunctionToCall = GetProcAddress(ModulePointer, "PassThruStartMsgFilter");
                if (pAddressOfFunctionToCall != IntPtr.Zero)
                    DelegateSet.PTStartMsgFilter = (PassThruDelegates.DelegatePassThruStartMsgFilter)Marshal.GetDelegateForFunctionPointer(
                        pAddressOfFunctionToCall, typeof(PassThruDelegates.DelegatePassThruStartMsgFilter));

                // PASSTHRU START FILTER
                pAddressOfFunctionToCall = GetProcAddress(ModulePointer, "PassThruStartMsgFilter");
                if (pAddressOfFunctionToCall != IntPtr.Zero)
                    DelegateSet.PTStartMsgFilterFlowPtr = (PassThruDelegates.DelegatePassThruStartMsgFilterFlowPtr)Marshal.GetDelegateForFunctionPointer(
                        pAddressOfFunctionToCall, typeof(PassThruDelegates.DelegatePassThruStartMsgFilterFlowPtr));

                // PASSTHRU STOP FILTER
                pAddressOfFunctionToCall = GetProcAddress(ModulePointer, "PassThruStopMsgFilter");
                if (pAddressOfFunctionToCall != IntPtr.Zero)
                    DelegateSet.PTStopMsgFilter = (PassThruDelegates.DelegatePassThruStopMsgFilter)Marshal.GetDelegateForFunctionPointer(
                        pAddressOfFunctionToCall, typeof(PassThruDelegates.DelegatePassThruStopMsgFilter));

                // PASSTHRU SET VOLTAGE
                pAddressOfFunctionToCall = GetProcAddress(ModulePointer, "PassThruSetProgrammingVoltage");
                if (pAddressOfFunctionToCall != IntPtr.Zero)
                    DelegateSet.PTSetProgrammingVoltage = (PassThruDelegates.DelegatePassThruSetProgrammingVoltage)Marshal.GetDelegateForFunctionPointer(
                        pAddressOfFunctionToCall, typeof(PassThruDelegates.DelegatePassThruSetProgrammingVoltage));

                // PASSTHRU READ VERSION
                pAddressOfFunctionToCall = GetProcAddress(ModulePointer, "PassThruReadVersion");
                if (pAddressOfFunctionToCall != IntPtr.Zero)
                    DelegateSet.PTReadVersion = (PassThruDelegates.DelegatePassThruReadVersion)Marshal.GetDelegateForFunctionPointer(
                        pAddressOfFunctionToCall, typeof(PassThruDelegates.DelegatePassThruReadVersion));

                // PASSTHRU GET ERROR
                pAddressOfFunctionToCall = GetProcAddress(ModulePointer, "PassThruGetLastError");
                if (pAddressOfFunctionToCall != IntPtr.Zero)
                    DelegateSet.PTGetLastError = (PassThruDelegates.DelegatePassThruGetLastError)Marshal.GetDelegateForFunctionPointer(
                        pAddressOfFunctionToCall, typeof(PassThruDelegates.DelegatePassThruGetLastError));

                // PASSTHRU IOCTL
                pAddressOfFunctionToCall = GetProcAddress(ModulePointer, "PassThruIoctl");
                if (pAddressOfFunctionToCall != IntPtr.Zero)
                    DelegateSet.PTIoctl = (PassThruDelegates.DelegatePassThruIoctl)Marshal.GetDelegateForFunctionPointer(
                        pAddressOfFunctionToCall, typeof(PassThruDelegates.DelegatePassThruIoctl));

                // ------------------------------------------ METHODS FOR THE V0500 DLLS ONLY! ------------------------------------------

                // PASSTHRU LOGICAL CONNECT
                pAddressOfFunctionToCall = GetProcAddress(ModulePointer, "PassThruLogicalConnect");
                if (pAddressOfFunctionToCall != IntPtr.Zero)
                    DelegateSet.PTLogicalConnect = (PassThruDelegates.DelegatePassThruLogicalConnect)Marshal.GetDelegateForFunctionPointer(
                        pAddressOfFunctionToCall, typeof(PassThruDelegates.DelegatePassThruLogicalConnect));

                // PASSTHRU LOGICAL DISCONNECT
                pAddressOfFunctionToCall = GetProcAddress(ModulePointer, "PassThruLogicalDisconnect");
                if (pAddressOfFunctionToCall != IntPtr.Zero)
                    DelegateSet.PTLogicalDisconnect = (PassThruDelegates.DelegatePassThruLogicalDisconnect)Marshal.GetDelegateForFunctionPointer(
                        pAddressOfFunctionToCall, typeof(PassThruDelegates.DelegatePassThruLogicalDisconnect));

                // PASSTHRU SELECT
                pAddressOfFunctionToCall = GetProcAddress(ModulePointer, "PassThruSelect");
                if (pAddressOfFunctionToCall != IntPtr.Zero)
                    DelegateSet.PTSelect = (PassThruDelegates.DelegatePassThruSelect)Marshal.GetDelegateForFunctionPointer(
                        pAddressOfFunctionToCall, typeof(PassThruDelegates.DelegatePassThruSelect));
                
                // PASSTHRU QUEUE MESSAGES
                pAddressOfFunctionToCall = GetProcAddress(ModulePointer, "PassThruQueueMsgs");
                if (pAddressOfFunctionToCall != IntPtr.Zero) 
                    DelegateSet.PTQueueMsgs = (PassThruDelegates.DelegatePassThruQueueMsgs)Marshal.GetDelegateForFunctionPointer(
                        pAddressOfFunctionToCall, typeof(PassThruDelegates.DelegatePassThruQueueMsgs));

                // -------------------------------------- METHODS FOR THE FULCRUM SHIM DLL ONLY! --------------------------------------

                // PASSTHRU WRITE TO LOG A
                pAddressOfFunctionToCall = GetProcAddress(ModulePointer, "PassThruWriteToLogA");
                if (pAddressOfFunctionToCall != IntPtr.Zero)
                    DelegateSet.PTWriteLogA = (PassThruDelegates.DelegatePTWriteLogA)Marshal.GetDelegateForFunctionPointer(
                        pAddressOfFunctionToCall, typeof(PassThruDelegates.DelegatePTWriteLogA));

                // PASSTHRU WRITE TO LOG W
                pAddressOfFunctionToCall = GetProcAddress(ModulePointer, "PassThruWriteToLogW");
                if (pAddressOfFunctionToCall != IntPtr.Zero)
                    DelegateSet.PTWriteLogW = (PassThruDelegates.DelegatePTWriteLogW)Marshal.GetDelegateForFunctionPointer(
                        pAddressOfFunctionToCall, typeof(PassThruDelegates.DelegatePTWriteLogW));

                // PASSTHRU SAVE LOG FILE
                pAddressOfFunctionToCall = GetProcAddress(ModulePointer, "PassThruSaveLog");
                if (pAddressOfFunctionToCall != IntPtr.Zero)
                    DelegateSet.PTSaveLog = (PassThruDelegates.DelegatePTSaveLog)Marshal.GetDelegateForFunctionPointer(
                        pAddressOfFunctionToCall, typeof(PassThruDelegates.DelegatePTSaveLog));

                // Return true if we've passed loading all these methods now
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
