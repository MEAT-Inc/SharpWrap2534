using System;
using System.Runtime.InteropServices;
using System.Text;
using JBoxInvoker.PassThruLogic.PassThruTypes;

namespace JBoxInvoker.PassThruLogic.PassThruImport
{
    /// <summary>
    /// Delegates used for controlling API Logic.
    /// </summary>
    public class PassThruDelegates
    {
        // DELEGATE: PASSTHRU OPEN
        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate int DelegatePassThruOpen(IntPtr DllPointer, out uint DeviceId);
        public DelegatePassThruOpen PTOpen;

        // DELEGATE: PASSTHRU CLOSE
        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate int DelegatePassThruClose(uint DeviceId);
        public DelegatePassThruClose PTClose;

        // DELEGATE: PASSTHRU CONNECT
        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate int DelegatePassThruConnect(uint DeviceId, uint ProtocolId, uint MsgFlags, uint BaudRate, out uint ChannelId);
        public DelegatePassThruConnect PTConnect;

        // DELEGATE: PASSTHRU DISCONNECT
        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate int DelegatePassThruDisconnect(uint ChannelId);
        public DelegatePassThruDisconnect PTDisconnect;

        // DELEGATE: PASSTHRU READ MESSAGE
        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate int DelegatePassThruReadMsgs(uint ChannelId, [In, Out] PassThruStructsNative.PASSTHRU_MSG[] PassThruMsg, out uint MsgCount, uint MsgTimeout);       
        public DelegatePassThruReadMsgs PTReadMsgs;

        // DELEGATE: PASSTHRU WRITE MESSAGE
        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate int DelegatePassThruWriteMsgs(uint ChannelId, [In] PassThruStructsNative.PASSTHRU_MSG[] PassThruMsg, ref uint MsgCount, uint MsgTimeout);              
        public DelegatePassThruWriteMsgs PTWriteMsgs;

        // DELEGATE: PASSTHRU START PERIODIC
        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate int DelegatePassThruStartPeriodicMsg(uint ChannelId, [In] ref PassThruStructsNative.PASSTHRU_MSG PassThruMsg, out uint PassThruMsgId, uint TimeInterval);  
        public DelegatePassThruStartPeriodicMsg PTStartPeriodicMsg;

        // DELEGATE: PASSTHRU STOP PERIODIC
        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate int DelegatePassThruStopPeriodicMsg(uint ChannelId, uint MsgId);
        public DelegatePassThruStopPeriodicMsg PTStopPeriodicMsg;

        // DELEGATE: PASSTHRU START FILTER (With message flow Ctl)
        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate int DelegatePassThruStartMsgFilter(uint ChannelId, uint FilterType, [In] ref PassThruStructsNative.PASSTHRU_MSG PassThruMask, [In] ref PassThruStructsNative.PASSTHRU_MSG PassThruPattern, [In] ref PassThruStructsNative.PASSTHRU_MSG PassThruFlowCtl, out uint FilterId);
        public DelegatePassThruStartMsgFilter PTStartMsgFilter;

        // DELEGATE: PASSTHRU START FILTER
        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate int DelegatePassThruStartMsgFilterFlowPtr(uint ChannelId, uint FilterType, [In] ref PassThruStructsNative.PASSTHRU_MSG PassThruMask, [In] ref PassThruStructsNative.PASSTHRU_MSG PassThruPattern, [In] IntPtr PassThruFlowCtl, out uint FilterId);
        public DelegatePassThruStartMsgFilterFlowPtr PTStartMsgFilterFlowPtr;

        // DELEGATE: PASSTHRU STOP FILTER
        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate int DelegatePassThruStopMsgFilter(uint ChannelId, uint FilterId);
        public DelegatePassThruStopMsgFilter PTStopMsgFilter;

        // DELEGATE: PASSTHRU SET PROGRAMMING VOLTAGE
        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate int DelegatePassThruSetProgrammingVoltage(uint DeviceId, uint DevicePin, uint SetVoltage);
        public DelegatePassThruSetProgrammingVoltage PTSetProgrammingVoltage;

        // DELEGATE: PASSTHRU READ VERSIONS
        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate int DelegatePassThruReadVersion(uint DeviceId, StringBuilder FirmwareVersion, StringBuilder DllVersion, StringBuilder ApiVersion);
        public DelegatePassThruReadVersion PTReadVersion;

        // DELEGATE: PASSTHRU GET LAST ERROR
        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate int DelegatePassThruGetLastError(StringBuilder ErrorDescription);
        public DelegatePassThruGetLastError PTGetLastError;

        // DELEGATE: PASSTHRU IOCTL
        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate int DelegatePassThruIoctl(uint ChannelId, uint IoctlId, IntPtr InputPtr, IntPtr OutputPtr);
        public DelegatePassThruIoctl PTIoctl;
    }
}
