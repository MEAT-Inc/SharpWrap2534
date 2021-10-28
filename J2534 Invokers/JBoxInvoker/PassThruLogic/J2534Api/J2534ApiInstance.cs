using System;
using System.Text;
using JBoxInvoker.PassThruLogic.PassThruImport;
using JBoxInvoker.PassThruLogic.PassThruTypes;
using JBoxInvoker.PassThruLogic.SupportingLogic;

namespace JBoxInvoker.PassThruLogic.J2534Api
{
    /// <summary>
    /// Instance object for the API built in the PassThru logic class.
    /// </summary>
    public class J2534ApiInstance
    {
        /// <summary>
        /// Builds a new instance of this J2534 Api
        /// </summary>
        /// <param name="DeviceNumber"></param>
        public J2534ApiInstance(JDeviceNumber DeviceNumber)
        {
            // Store Number and status values.
            this.DeviceNumber = DeviceNumber;
            this.ApiStatus = PTInstanceStatus.NULL;
        } 
        
        /// <summary>
        /// Deconstructor for this type class.
        /// </summary>
        ~J2534ApiInstance()
        {
            // Release the DLL used and make a new delegate set.
            this.JDllImporter = null;
            this.DelegateSet = new PassThruDelegates();
        }

        // ------------------------------------ CLASS VALUES FOR J2534 API ---------------------------------

        // JDevice Number.
        public PTInstanceStatus ApiStatus { get; private set; }
        public JDeviceNumber DeviceNumber { get; set; }

        // Version of the DLL for the J2534 DLL
        public JVersion ApiVersion;
        public string J2534DllPath { get; private set; }
        public PassThruPaths J2534DllType { get; private set; }

        // PassThru method delegates
        public PassThruImporter JDllImporter;
        public PassThruDelegates DelegateSet;

        // ------------------------------ CONSTRUCTOR INIT METHOD FOR INSTANCE -----------------------------

        /// <summary>
        /// Builds a new JInstance setup based
        /// </summary>
        /// <param name="JApiDllType">J2534 DLL object to use</param>
        /// <returns>True if setup. False if not.</returns>
        public bool SetupJApiInstance(PassThruPaths JApiDllType)
        {
            // Check status value.
            if (this.ApiStatus == PTInstanceStatus.INITIALIZED) return false;

            // Set the version and build our delegate/Importer objects
            this.J2534DllType = JApiDllType;
            this.J2534DllPath = this.J2534DllType.ToDescriptionString();
            this.ApiVersion = this.J2534DllPath.Contains("0500") ? JVersion.V0500 : JVersion.V0404;

            // Build instance values for delegates and importer
            this.DelegateSet = new PassThruDelegates();
            this.JDllImporter = new PassThruImporter(this.J2534DllPath);
            this.JDllImporter.MapDelegateMethods(out this.DelegateSet);

            // Set the status value.
            this.ApiStatus = PTInstanceStatus.INITIALIZED;

            // Return passed.
            return true;
        }

        // ---------------------------------- J2534 PUBLIC FACING API CALLS ---------------------------------

        /// <summary>
        /// Runs a new PTOpen command for the provided Device ID
        /// </summary>
        /// <param name="DeviceId">Device ID returned for this instance.</param>
        public void PassThruOpen(out uint DeviceId)
        {
            // Make our call to run the PTOpen command here.
            J2534Err PTCommandError = (J2534Err)this.DelegateSet.PTOpen(IntPtr.Zero, out DeviceId);

            // If the error is not a NOERROR Response then throw it.
            if (PTCommandError == J2534Err.STATUS_NOERROR) { return; }
            var ErrorBuilder = new StringBuilder(100);
            PassThruGetLastError(ErrorBuilder);

            // Throw exception here.
            throw new PassThruException(PTCommandError, ErrorBuilder);
        }
        /// <summary>
        /// Runs a new PTOpen command for the provided Device ID
        /// </summary>
        /// <param name="DeviceId">Device ID returned for this instance.</param>
        /// <param name="DevicePtr">Pointer for device name.</param>
        public void PassThruOpen(IntPtr DevicePtr, out uint DeviceId)
        {
            // Make our call to run the PTOpen command here.
            J2534Err PTCommandError = (J2534Err)this.DelegateSet.PTOpen(DevicePtr, out DeviceId);

            // If the error is not a NOERROR Response then throw it.
            if (PTCommandError == J2534Err.STATUS_NOERROR) { return; }
            var ErrorBuilder = new StringBuilder(100);
            PassThruGetLastError(ErrorBuilder);

            // Throw exception here.
            throw new PassThruException(PTCommandError, ErrorBuilder);
        }
        /// <summary>
        /// Runs a PTClose for the provided Device ID
        /// </summary>
        /// <param name="DeviceId">ID of device to close down</param>
        public void PassThruClose(uint DeviceId)
        {
            // Run our PT Close method.
            J2534Err PTCommandError = (J2534Err)this.DelegateSet.PTClose(DeviceId);

            // If the error is not a NOERROR Response then throw it.
            if (PTCommandError == J2534Err.STATUS_NOERROR) { return; }
            var ErrorBuilder = new StringBuilder(100);
            PassThruGetLastError(ErrorBuilder);

            // Throw exception here.
            throw new PassThruException(PTCommandError, ErrorBuilder);
        }
        /// <summary>
        /// Issues a PTConnect method to the device specified.
        /// </summary>
        /// <param name="DeviceId">ID of device to open</param>
        /// <param name="Protocol">Protocol of the channel</param>
        /// <param name="ConnectFlags">Flag args for the channel</param>
        /// <param name="ConnectBaud">Baudrate of the channel</param>
        /// <param name="ChannelId">ID Of the oppened channel>/param>
        public void PassThruConnect(uint DeviceId, ProtocolId Protocol, uint ConnectFlags, BaudRate ConnectBaud, out uint ChannelId)
        {
            // Run our PassThru connect method here.
            J2534Err PTCommandError = (J2534Err)this.DelegateSet.PTConnect(DeviceId, (uint)Protocol, ConnectFlags, (uint)ConnectBaud, out ChannelId);

            // If the error is not a NOERROR Response then throw it.
            if (PTCommandError == J2534Err.STATUS_NOERROR) { return; }
            var ErrorBuilder = new StringBuilder(100);
            PassThruGetLastError(ErrorBuilder);

            // Throw exception here.
            throw new PassThruException(PTCommandError, ErrorBuilder);
        }
        /// <summary>
        /// Issues a PTConnect method to the device specified.
        /// </summary>
        /// <param name="DeviceId">ID of device to open</param>
        /// <param name="Protocol">Protocol of the channel</param>
        /// <param name="ConnectFlags">Flag args for the channel</param>
        /// <param name="ConnectBaud">Baudrate of the channel</param>
        /// <param name="ChannelId">ID Of the oppened channel>/param>
        public void PassThruConnect(uint DeviceId, ProtocolId Protocol, uint ConnectFlags, uint ConnectBaud, out uint ChannelId)
        {
            // Run our PassThru connect method here.
            J2534Err PTCommandError = (J2534Err)this.DelegateSet.PTConnect(DeviceId, (uint)Protocol, ConnectFlags, ConnectBaud, out ChannelId);

            // If the error is not a NOERROR Response then throw it.
            if (PTCommandError == J2534Err.STATUS_NOERROR) { return; }
            var ErrorBuilder = new StringBuilder(100);
            PassThruGetLastError(ErrorBuilder);

            // Throw exception here.
            throw new PassThruException(PTCommandError, ErrorBuilder);
        }
        /// <summary>
        /// Runs a PassThru disconnect method on the device ID given
        /// </summary>
        /// <param name="ChannelId">ID Of the channel to drop out.</param>
        public void PassThruDisconnect(uint ChannelId)
        {
            // Run the disconnect command
            J2534Err PTCommandError = (J2534Err)this.DelegateSet.PTDisconnect(ChannelId);
            
            // If the error is not a NOERROR Response then throw it.
            if (PTCommandError == J2534Err.STATUS_NOERROR) { return; }
            var ErrorBuilder = new StringBuilder(100);
            PassThruGetLastError(ErrorBuilder);

            // Throw exception here.
            throw new PassThruException(PTCommandError, ErrorBuilder);
        }
        /// <summary>
        /// Reads messages from the given ChannelID
        /// </summary>
        /// <param name="ChannelId">Channel ID to read from.</param>
        /// <param name="Messages">Messages to send</param>
        /// <param name="MsgCount">Number of messages to read</param>
        /// <param name="ReadTimeout">Read timeout value.</param>
        public void PassThruReadMsgs(uint ChannelId, PassThruStructsNative.PASSTHRU_MSG[] Messages, out uint MsgCount, uint ReadTimeout)
        {
            // Run our PTRead command.
            J2534Err PTCommandError = (J2534Err)this.DelegateSet.PTReadMsgs(ChannelId, Messages, out MsgCount, ReadTimeout);

            // If the error is not a NOERROR Response then throw it.
            if (PTCommandError == J2534Err.STATUS_NOERROR) { return; }
            var ErrorBuilder = new StringBuilder(100);
            PassThruGetLastError(ErrorBuilder);

            // Throw exception here.
            throw new PassThruException(PTCommandError, ErrorBuilder);
        }
        /// <summary>
        /// Sends a message to the given channel ID
        /// </summary>
        /// <param name="ChannelId">Channel to send to</param>
        /// <param name="MsgToSend">Message to send</param>
        /// <param name="MsgCount">Messages sent</param>
        /// <param name="SendTimeout">Timeout value for send</param>
        public void PassThruWriteMsgs(uint ChannelId, PassThruStructsNative.PASSTHRU_MSG MsgToSend, out uint MsgCount, uint SendTimeout)
        {
            // Wrap the messages into a native array.
            MsgCount = 1;
            PassThruStructsNative.PASSTHRU_MSG[] NativeWrappedMsgs = new PassThruStructsNative.PASSTHRU_MSG[1] { MsgToSend };

            // Send the PTWrite command here.
            J2534Err PTCommandError = (J2534Err)this.DelegateSet.PTWriteMsgs(ChannelId, NativeWrappedMsgs, ref MsgCount, SendTimeout);

            // If the error is not a NOERROR Response then throw it.
            if (PTCommandError == J2534Err.STATUS_NOERROR) { return; }
            var ErrorBuilder = new StringBuilder(100);
            PassThruGetLastError(ErrorBuilder);

            // Throw exception here.
            throw new PassThruException(PTCommandError, ErrorBuilder);
        }
        /// <summary>
        /// Writes a given set of PTMessages out to our JDevice.
        /// </summary>
        /// <param name="ChannelId">Channel to send on</param>
        /// <param name="Msgs">Messages to send out</param>
        /// <param name="MsgCount">Number of messages to send</param>
        /// <param name="SendTimeout">Send timeout for operation</param>
        public void PassThruWriteMsgs(uint ChannelId, PassThruStructsNative.PASSTHRU_MSG[] Msgs, ref uint MsgCount, uint SendTimeout)
        {
            // Run the PTWrite command and store the error output.
            J2534Err PTCommandError = (J2534Err)this.DelegateSet.PTWriteMsgs(ChannelId, Msgs, ref MsgCount, SendTimeout);

            // If the error is not a NOERROR Response then throw it.
            if (PTCommandError == J2534Err.STATUS_NOERROR) { return; }
            var ErrorBuilder = new StringBuilder(100);
            PassThruGetLastError(ErrorBuilder);

            // Throw exception here.
            throw new PassThruException(PTCommandError, ErrorBuilder);
        }
        /// <summary>
        /// Starts a periodic message filter on the given channel with the provided mesasage.
        /// </summary>
        /// <param name="ChannelId">Channel to send on.</param>
        /// <param name="Msg">Message ot send </param>
        /// <param name="MsgId">ID of the newly made message</param>
        /// <param name="MessageInterval">Timeout for the send operation</param>
        public void PassThruStartPeriodicMsg(uint ChannelId, PassThruStructsNative.PASSTHRU_MSG Msg, out uint MsgId, uint MessageInterval)
        {
            // Runs the PT Start periodic command and stores error code.
            J2534Err PTCommandError = (J2534Err)this.DelegateSet.PTStartPeriodicMsg(ChannelId, ref Msg, out MsgId, MessageInterval);

            // If the error is not a NOERROR Response then throw it.
            if (PTCommandError == J2534Err.STATUS_NOERROR) { return; }
            var ErrorBuilder = new StringBuilder(100);
            PassThruGetLastError(ErrorBuilder);

            // Throw exception here.
            throw new PassThruException(PTCommandError, ErrorBuilder);
        }
        /// <summary>
        /// Stops a periodic message.
        /// </summary>
        /// <param name="ChannelId">Channel to stop on</param>
        /// <param name="MsgId">Message Id to stop</param>
        public void PassThruStopPeriodicMsg(uint ChannelId, uint MsgId)
        {
            // Run the PTStop Periodic command.
            J2534Err PTCommandError = (J2534Err)this.DelegateSet.PTStopPeriodicMsg(ChannelId, MsgId);

            // If the error is not a NOERROR Response then throw it.
            if (PTCommandError == J2534Err.STATUS_NOERROR) { return; }
            var ErrorBuilder = new StringBuilder(100);
            PassThruGetLastError(ErrorBuilder);

            // Throw exception here.
            throw new PassThruException(PTCommandError, ErrorBuilder);
        }
        /// <summary>
        /// Stars a non flow control filter.
        /// </summary>
        /// <param name="ChannelId">Channel to filter</param>
        /// <param name="FilterType">Type of filter</param>
        /// <param name="Mask">Mask message</param>
        /// <param name="Pattern">Pattern Message</param>
        /// <param name="FilterId">ID of the newly made filter.</param>
        public void PassThruStartMsgFilter(uint ChannelId, FilterDef FilterType, PassThruStructsNative.PASSTHRU_MSG Mask, PassThruStructsNative.PASSTHRU_MSG Pattern, out uint FilterId)
        {
            // Runs a flow ctl filter without flow control value.
            PassThruStructsNative.PASSTHRU_MSG FlowMsg = new PassThruStructsNative.PASSTHRU_MSG(-1);
            PassThruStartMsgFilter(ChannelId, FilterType, Mask, Pattern, FlowMsg, out FilterId);
        }
        /// <summary>
        /// Starts a message filter with the given type on the specified channel
        /// </summary>
        /// <param name="ChannelId">Channel to filter</param>
        /// <param name="FilterType">Type of filter</param>
        /// <param name="Mask">Mask message</param>
        /// <param name="Pattern">Pattern Message</param>
        /// <param name="FlowCtl">Flow control filter message</param>
        /// <param name="FilterId">ID of the newly made filter.</param>
        public void PassThruStartMsgFilter(uint ChannelId, FilterDef FilterType, PassThruStructsNative.PASSTHRU_MSG Mask, PassThruStructsNative.PASSTHRU_MSG Pattern, PassThruStructsNative.PASSTHRU_MSG? FlowCtl, out uint FilterId)
        {
            // Universal Error object.
            J2534Err PTCommandError;

            // Check for the flow control filter being null or not.
            if (FlowCtl == null) PTCommandError = (J2534Err)this.DelegateSet.PTStartMsgFilterFlowPtr(ChannelId, (uint)FilterType, ref Mask, ref Pattern, IntPtr.Zero, out FilterId);
            else
            {
                // For a non null flow ctl send message filter command.
                PassThruStructsNative.PASSTHRU_MSG FlowCtlNoNull = (PassThruStructsNative.PASSTHRU_MSG)FlowCtl;
                PTCommandError = (J2534Err)this.DelegateSet.PTStartMsgFilter(ChannelId, (uint)FilterType, ref Mask, ref Pattern, ref FlowCtlNoNull, out FilterId);
            }

            // If the error is not a NOERROR Response then throw it.
            if (PTCommandError == J2534Err.STATUS_NOERROR) { return; }
            var ErrorBuilder = new StringBuilder(100);
            PassThruGetLastError(ErrorBuilder);

            // Throw exception here.
            throw new PassThruException(PTCommandError, ErrorBuilder);
        }
        /// <summary>
        /// Runs a PTStopMSg Filter command and returns.
        /// </summary>
        /// <param name="ChannelId">Channel To stop filter on</param>
        /// <param name="FilterId">Filter ID to stop</param>
        public void PassThruStopMsgFilter(uint ChannelId, uint FilterId)
        {
            // Run the stop filter command.
            J2534Err PTCommandError = (J2534Err)this.DelegateSet.PTStopMsgFilter(ChannelId, FilterId);

            // If the error is not a NOERROR Response then throw it.
            if (PTCommandError == J2534Err.STATUS_NOERROR) { return; }
            var ErrorBuilder = new StringBuilder(100);
            PassThruGetLastError(ErrorBuilder);

            // Throw exception here.
            throw new PassThruException(PTCommandError, ErrorBuilder);
        }
        /// <summary>
        /// Sets programming voltage on a given pin and device.
        /// </summary>
        /// <param name="DeviceId">Device to apply to</param>
        /// <param name="PinNumber">Pin to set</param>
        /// <param name="Voltage">Voltage to set on pin</param>
        public void PassThruSetProgrammingVoltage(uint DeviceId, uint PinNumber, uint Voltage)
        {
            // Run the set voltage command.
            J2534Err PTCommandError = (J2534Err)this.DelegateSet.PTSetProgrammingVoltage(DeviceId, PinNumber, Voltage);

            // If the error is not a NOERROR Response then throw it.
            if (PTCommandError == J2534Err.STATUS_NOERROR) { return; }
            var ErrorBuilder = new StringBuilder(100);
            PassThruGetLastError(ErrorBuilder);

            // Throw exception here.
            throw new PassThruException(PTCommandError, ErrorBuilder);
        }
        /// <summary>
        /// Runs a PTRead version command and stores the output.
        /// </summary>
        /// <param name="DeviceId">Device to read</param>
        /// <param name="FirmwareVersion">FW of the Device</param>
        /// <param name="JDllVersion">DLL Version</param>
        /// <param name="JApiVersion">API Version</param>
        public void PassThruReadVersion(uint DeviceId, StringBuilder FirmwareVersion, StringBuilder JDllVersion, StringBuilder JApiVersion)
        {
            // Runs the read version command and stores output of it.
            J2534Err PTCommandError = (J2534Err)this.DelegateSet.PTReadVersion(DeviceId, FirmwareVersion, JDllVersion, JApiVersion);

            // If the error is not a NOERROR Response then throw it.
            if (PTCommandError == J2534Err.STATUS_NOERROR) { return; }
            var ErrorBuilder = new StringBuilder(100);
            PassThruGetLastError(ErrorBuilder);

            // Throw exception here.
            throw new PassThruException(PTCommandError, ErrorBuilder);
        }
        /// <summary>
        /// Builds the last error from our device/DLL Call
        /// </summary>
        /// <param name="LastJ2534Error">Error to build.</param>
        public void PassThruGetLastError(StringBuilder LastJ2534Error)
        {
            // Gets the error from building if one exists.
            J2534Err PTCommandError = (J2534Err)this.DelegateSet.PTGetLastError(LastJ2534Error);

            // If the error is not a NOERROR Response then throw it.
            if (PTCommandError == J2534Err.STATUS_NOERROR) { return; }
            var ErrorBuilder = new StringBuilder(100);
            PassThruGetLastError(ErrorBuilder);

            // Throw exception here.
            throw new PassThruException(PTCommandError, ErrorBuilder);
        }
        /// <summary>
        /// Runs a IOCTL command on the device to setup a new command. 
        /// </summary>
        /// <param name="ChannelId">Channel in use</param>
        /// <param name="IoctlId">IOCTL to use</param>
        /// <param name="InputPtr">Input Struct pointer</param>
        /// <param name="OutputPtr">Output struct pointer.</param>
        public void PassThruIoctl(uint ChannelId, IoctlId IoctlId, IntPtr InputPtr, IntPtr OutputPtr)
        {
            // Runs the PTIoctl command to issue a new IOCTL to the device.
            J2534Err PTCommandError = (J2534Err)this.DelegateSet.PTIoctl(ChannelId, (uint)IoctlId, InputPtr, OutputPtr);

            // If the error is not a NOERROR Response then throw it.
            if (PTCommandError == J2534Err.STATUS_NOERROR) { return; }
            var ErrorBuilder = new StringBuilder(100);
            PassThruGetLastError(ErrorBuilder);

            // Throw exception here.
            throw new PassThruException(PTCommandError, ErrorBuilder);
        }
    }
}
