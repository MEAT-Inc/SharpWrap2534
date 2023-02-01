using System;
using System.Runtime.InteropServices;
using SharpSupport;
using SharpWrapper.PassThruSupport;
using static SharpWrapper.PassThruImport.PassThruDelegates;

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
                // ---------------------------------- DELEGATES FOR IMPORTING DEVICES ----------------------------------------------

                // USED FOR INIT NEXT DEVICE SETUP!
                IntPtr pAddressOfFunctionToCall = Win32Invokers.GetProcAddress(ModulePointer, "PassThruGetNextCarDAQ");
                if (pAddressOfFunctionToCall != IntPtr.Zero)
                    DelegateSet.PTInitNextPassThruDevice = (DelegateInitGetNextCarDAQ)Marshal.GetDelegateForFunctionPointer(
                        pAddressOfFunctionToCall, typeof(DelegateInitGetNextCarDAQ));

                // USED FOR INIT NEXT DEVICE SETUP!
                pAddressOfFunctionToCall = Win32Invokers.GetProcAddress(ModulePointer, "PassThruGetNextCarDAQ");
                if (pAddressOfFunctionToCall != IntPtr.Zero)
                    DelegateSet.PTGetNextPassThruDevice = (DelegateGetNextCarDAQ)Marshal.GetDelegateForFunctionPointer(
                        pAddressOfFunctionToCall, typeof(DelegateGetNextCarDAQ));

                // USED FOR SCAN NEXT DEVICES (V0500 ONLY!)
                pAddressOfFunctionToCall = Win32Invokers.GetProcAddress(ModulePointer, "PassThruScanForDevices");
                if (pAddressOfFunctionToCall != IntPtr.Zero)
                    DelegateSet.PTScanForDevices = (DelegatePassThruScanForDevices)Marshal.GetDelegateForFunctionPointer(
                        pAddressOfFunctionToCall, typeof(DelegatePassThruScanForDevices));

                // USED FOR GET NEXT DEVICES (V0500 ONLY!)
                pAddressOfFunctionToCall = Win32Invokers.GetProcAddress(ModulePointer, "PassThruGetNextDevice");
                if (pAddressOfFunctionToCall != IntPtr.Zero)
                    DelegateSet.PTGetNextDevice = (DelegatePassThruGetNextDevice)Marshal.GetDelegateForFunctionPointer(
                        pAddressOfFunctionToCall, typeof(DelegatePassThruGetNextDevice));

                // -----------------------------------------------------------------------------------------------------------------

                // PASSTHRU OPEN
                pAddressOfFunctionToCall = Win32Invokers.GetProcAddress(ModulePointer, "PassThruOpen");
                if (pAddressOfFunctionToCall != IntPtr.Zero)
                    DelegateSet.PTOpen = (DelegatePassThruOpen)Marshal.GetDelegateForFunctionPointer(
                        pAddressOfFunctionToCall, typeof(DelegatePassThruOpen));

                // PASSTHRU CLOSE
                pAddressOfFunctionToCall = Win32Invokers.GetProcAddress(ModulePointer, "PassThruClose");
                if (pAddressOfFunctionToCall != IntPtr.Zero)
                    DelegateSet.PTClose = (DelegatePassThruClose)Marshal.GetDelegateForFunctionPointer(
                        pAddressOfFunctionToCall, typeof(DelegatePassThruClose));

                // PASSTHRU CONNECT
                pAddressOfFunctionToCall = Win32Invokers.GetProcAddress(ModulePointer, "PassThruConnect");
                if (pAddressOfFunctionToCall != IntPtr.Zero)
                    DelegateSet.PTConnect = (DelegatePassThruConnect)Marshal.GetDelegateForFunctionPointer(
                        pAddressOfFunctionToCall, typeof(DelegatePassThruConnect));

                // PASSTHRU DISCONNECT
                pAddressOfFunctionToCall = Win32Invokers.GetProcAddress(ModulePointer, "PassThruDisconnect");
                if (pAddressOfFunctionToCall != IntPtr.Zero)
                    DelegateSet.PTDisconnect = (DelegatePassThruDisconnect)Marshal.GetDelegateForFunctionPointer(
                        pAddressOfFunctionToCall, typeof(DelegatePassThruDisconnect));

                // PASSTHRU READ MSGS
                pAddressOfFunctionToCall = Win32Invokers.GetProcAddress(ModulePointer, "PassThruReadMsgs");
                if (pAddressOfFunctionToCall != IntPtr.Zero)
                    DelegateSet.PTReadMsgs = (DelegatePassThruReadMsgs)Marshal.GetDelegateForFunctionPointer(
                        pAddressOfFunctionToCall, typeof(DelegatePassThruReadMsgs));

                // PASSTHRU WRITE MSGS
                pAddressOfFunctionToCall = Win32Invokers.GetProcAddress(ModulePointer, "PassThruWriteMsgs");
                if (pAddressOfFunctionToCall != IntPtr.Zero)
                    DelegateSet.PTWriteMsgs = (DelegatePassThruWriteMsgs)Marshal.GetDelegateForFunctionPointer(
                        pAddressOfFunctionToCall, typeof(DelegatePassThruWriteMsgs));

                // PASSTHRU START PERIODIC
                pAddressOfFunctionToCall = Win32Invokers.GetProcAddress(ModulePointer, "PassThruStartPeriodicMsg");
                if (pAddressOfFunctionToCall != IntPtr.Zero)
                    DelegateSet.PTStartPeriodicMsg = (DelegatePassThruStartPeriodicMsg)Marshal.GetDelegateForFunctionPointer(
                        pAddressOfFunctionToCall, typeof(DelegatePassThruStartPeriodicMsg));

                // PASSTHRU STOP PERIODIC
                pAddressOfFunctionToCall = Win32Invokers.GetProcAddress(ModulePointer, "PassThruStopPeriodicMsg");
                if (pAddressOfFunctionToCall != IntPtr.Zero)
                    DelegateSet.PTStopPeriodicMsg = (DelegatePassThruStopPeriodicMsg)Marshal.GetDelegateForFunctionPointer(
                        pAddressOfFunctionToCall, typeof(DelegatePassThruStopPeriodicMsg));

                // PASSTHRU START FILTER
                pAddressOfFunctionToCall = Win32Invokers.GetProcAddress(ModulePointer, "PassThruStartMsgFilter");
                if (pAddressOfFunctionToCall != IntPtr.Zero)
                    DelegateSet.PTStartMsgFilter = (DelegatePassThruStartMsgFilter)Marshal.GetDelegateForFunctionPointer(
                        pAddressOfFunctionToCall, typeof(DelegatePassThruStartMsgFilter));

                // PASSTHRU START FILTER
                pAddressOfFunctionToCall = Win32Invokers.GetProcAddress(ModulePointer, "PassThruStartMsgFilter");
                if (pAddressOfFunctionToCall != IntPtr.Zero)
                    DelegateSet.PTStartMsgFilterFlowPtr = (DelegatePassThruStartMsgFilterFlowPtr)Marshal.GetDelegateForFunctionPointer(
                        pAddressOfFunctionToCall, typeof(DelegatePassThruStartMsgFilterFlowPtr));

                // PASSTHRU STOP FILTER
                pAddressOfFunctionToCall = Win32Invokers.GetProcAddress(ModulePointer, "PassThruStopMsgFilter");
                if (pAddressOfFunctionToCall != IntPtr.Zero)
                    DelegateSet.PTStopMsgFilter = (DelegatePassThruStopMsgFilter)Marshal.GetDelegateForFunctionPointer(
                        pAddressOfFunctionToCall, typeof(DelegatePassThruStopMsgFilter));

                // PASSTHRU SET VOLTAGE
                pAddressOfFunctionToCall = Win32Invokers.GetProcAddress(ModulePointer, "PassThruSetProgrammingVoltage");
                if (pAddressOfFunctionToCall != IntPtr.Zero)
                    DelegateSet.PTSetProgrammingVoltage = (DelegatePassThruSetProgrammingVoltage)Marshal.GetDelegateForFunctionPointer(
                        pAddressOfFunctionToCall, typeof(DelegatePassThruSetProgrammingVoltage));

                // PASSTHRU READ VERSION
                pAddressOfFunctionToCall = Win32Invokers.GetProcAddress(ModulePointer, "PassThruReadVersion");
                if (pAddressOfFunctionToCall != IntPtr.Zero)
                    DelegateSet.PTReadVersion = (DelegatePassThruReadVersion)Marshal.GetDelegateForFunctionPointer(
                        pAddressOfFunctionToCall, typeof(DelegatePassThruReadVersion));

                // PASSTHRU GET ERROR
                pAddressOfFunctionToCall = Win32Invokers.GetProcAddress(ModulePointer, "PassThruGetLastError");
                if (pAddressOfFunctionToCall != IntPtr.Zero)
                    DelegateSet.PTGetLastError = (DelegatePassThruGetLastError)Marshal.GetDelegateForFunctionPointer(
                        pAddressOfFunctionToCall, typeof(DelegatePassThruGetLastError));

                // PASSTHRU IOCTL
                pAddressOfFunctionToCall = Win32Invokers.GetProcAddress(ModulePointer, "PassThruIoctl");
                if (pAddressOfFunctionToCall != IntPtr.Zero)
                    DelegateSet.PTIoctl = (DelegatePassThruIoctl)Marshal.GetDelegateForFunctionPointer(
                        pAddressOfFunctionToCall, typeof(DelegatePassThruIoctl));

                // ------------------------------------------ METHODS FOR THE V0500 DLLS ONLY! ------------------------------------------

                // PASSTHRU LOGICAL CONNECT
                pAddressOfFunctionToCall = Win32Invokers.GetProcAddress(ModulePointer, "PassThruLogicalConnect");
                if (pAddressOfFunctionToCall != IntPtr.Zero)
                    DelegateSet.PTLogicalConnect = (DelegatePassThruLogicalConnect)Marshal.GetDelegateForFunctionPointer(
                        pAddressOfFunctionToCall, typeof(DelegatePassThruLogicalConnect));

                // PASSTHRU LOGICAL DISCONNECT
                pAddressOfFunctionToCall = Win32Invokers.GetProcAddress(ModulePointer, "PassThruLogicalDisconnect");
                if (pAddressOfFunctionToCall != IntPtr.Zero)
                    DelegateSet.PTLogicalDisconnect = (DelegatePassThruLogicalDisconnect)Marshal.GetDelegateForFunctionPointer(
                        pAddressOfFunctionToCall, typeof(DelegatePassThruLogicalDisconnect));

                // PASSTHRU SELECT
                pAddressOfFunctionToCall = Win32Invokers.GetProcAddress(ModulePointer, "PassThruSelect");
                if (pAddressOfFunctionToCall != IntPtr.Zero)
                    DelegateSet.PTSelect = (DelegatePassThruSelect)Marshal.GetDelegateForFunctionPointer(
                        pAddressOfFunctionToCall, typeof(DelegatePassThruSelect));
                
                // PASSTHRU QUEUE MESSAGES
                pAddressOfFunctionToCall = Win32Invokers.GetProcAddress(ModulePointer, "PassThruQueueMsgs");
                if (pAddressOfFunctionToCall != IntPtr.Zero) 
                    DelegateSet.PTQueueMsgs = (DelegatePassThruQueueMsgs)Marshal.GetDelegateForFunctionPointer(
                        pAddressOfFunctionToCall, typeof(DelegatePassThruQueueMsgs));

                // -------------------------------------- METHODS FOR THE FULCRUM SHIM DLL ONLY! --------------------------------------

                // PASSTHRU WRITE TO LOG A
                pAddressOfFunctionToCall = Win32Invokers.GetProcAddress(ModulePointer, "PassThruWriteToLogA");
                if (pAddressOfFunctionToCall != IntPtr.Zero)
                    DelegateSet.PTWriteLogA = (DelegatePTWriteLogA)Marshal.GetDelegateForFunctionPointer(
                        pAddressOfFunctionToCall, typeof(DelegatePTWriteLogA));

                // PASSTHRU WRITE TO LOG W
                pAddressOfFunctionToCall = Win32Invokers.GetProcAddress(ModulePointer, "PassThruWriteToLogW");
                if (pAddressOfFunctionToCall != IntPtr.Zero)
                    DelegateSet.PTWriteLogW = (DelegatePTWriteLogW)Marshal.GetDelegateForFunctionPointer(
                        pAddressOfFunctionToCall, typeof(DelegatePTWriteLogW));

                // PASSTHRU SAVE LOG FILE
                pAddressOfFunctionToCall = Win32Invokers.GetProcAddress(ModulePointer, "PassThruSaveLog");
                if (pAddressOfFunctionToCall != IntPtr.Zero)
                    DelegateSet.PTSaveLog = (DelegatePTSaveLog)Marshal.GetDelegateForFunctionPointer(
                        pAddressOfFunctionToCall, typeof(DelegatePTSaveLog));

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
