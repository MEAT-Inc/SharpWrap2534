﻿using System;
using System.Runtime.InteropServices;
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
        /// <param name="ApiDllPath"></param>
        public J2534ApiInstance(string ApiDllPath)
        {
            // Store Number and status values.
            this.J2534DllPath = ApiDllPath;
            this.ApiStatus = PTInstanceStatus.NULL;

            // Set the version and build our delegate/Importer objects
            this.J2534Version = this.J2534DllPath.Contains("0500") ? JVersion.V0500 : JVersion.V0404;
        } 
        
        /// <summary>
        /// Deconstructor for this type class.
        /// </summary>
        ~J2534ApiInstance()
        {
            // Release the DLL used and make a new delegate set.
            this._jDllImporter = null;
            this._delegateSet = new PassThruDelegates();
        }

        // ------------------------------------ CLASS VALUES FOR J2534 API ---------------------------------

        // JDevice Number.
        public PTInstanceStatus ApiStatus { get; private set; }

        // Version of the DLL for the J2534 DLL
        public JVersion J2534Version { get; private set; }
        public string J2534DllPath { get; private set; }

        // PassThru method delegates
        private PassThruApiImporter _jDllImporter;
        private PassThruDelegates _delegateSet;

        // ------------------------------ CONSTRUCTOR INIT METHOD FOR INSTANCE -----------------------------

        /// <summary>
        /// Builds a new JInstance setup based
        /// </summary>
        /// <returns>True if setup. False if not.</returns>
        public bool SetupJApiInstance()
        {
            // Check status value.
            if (this.ApiStatus == PTInstanceStatus.INITIALIZED) return false;

            // Build instance values for delegates and importer
            this._jDllImporter = new PassThruApiImporter(this.J2534DllPath);
            if (!this._jDllImporter.MapDelegateMethods(out this._delegateSet)) return false;

            // Set the status value.
            this.ApiStatus = PTInstanceStatus.INITIALIZED;
            return true;
        }

        // --------------------------------- J2534 DEVICE INIT METHOD CALLS ---------------------------------

        /// <summary>
        /// This wrapper is used to initiate the GetNextPassThruDevice sequence, this will cause the DLL to "discover" currently connected devices
        /// (This must be called before repeatedly calling GetNextPassThruDevice to get the list list of devices one by one)
        /// </summary>
        public void InitNexTPassThruDevice()
        {
            // Passing in NULLs for any one of the parameters will initiate a re-enumeration procedure and return immediately
            J2534Err PTCommandError = (J2534Err)this._delegateSet.InitNextPassThruDevice(IntPtr.Zero, IntPtr.Zero, IntPtr.Zero);

            // If the error is not a NOERROR Response then throw it.
            if (PTCommandError == J2534Err.STATUS_NOERROR) { return; }
            var ErrorBuilder = new StringBuilder(100);
            PassThruGetLastError(ErrorBuilder);

            // Throw exception here.
            throw new PassThruException(PTCommandError, ErrorBuilder);
        }
        /// <summary>
        /// function wrapper to get the list of connected devices, one be one, until parameters come back as empty strings("")
        /// (DTInitGetNextCarDAQ must be called to initialize the procedure)
        /// </summary>
        public void GetNextPassThruDevice(out string DeviceName, out uint DeviceVersion, out string DeviceAddress)
        {
            // Sizes of the name and the address. (Aka the pointers)
            int NamePointerSize = 0; int AddressPointerSize = 0;

            // Build pointers for name and the address.
            IntPtr MarshallPointerName = Marshal.AllocHGlobal(Marshal.SizeOf(NamePointerSize));
            IntPtr MarshallAddressValue = Marshal.AllocHGlobal(Marshal.SizeOf(AddressPointerSize));

            // Marshall it out.
            Marshal.StructureToPtr(NamePointerSize, MarshallPointerName, true);
            Marshal.StructureToPtr(AddressPointerSize, MarshallAddressValue, true);

            // We need clones since the GetNextPassthru call changes our pointers.
            IntPtr CopiedNameMarshall = MarshallPointerName;
            IntPtr CopiedAddressMarshall = MarshallAddressValue;

            // If the error is not a NOERROR Response then throw it.
            J2534Err PTCommandError = (J2534Err)this._delegateSet.GetNextPassThruDevice(ref MarshallPointerName, out DeviceVersion, ref MarshallAddressValue);
            if (PTCommandError != J2534Err.STATUS_NOERROR)
            {
                var ErrorBuilder = new StringBuilder(100);
                PassThruGetLastError(ErrorBuilder);
            }

            //Marshal.FreeHGlobal(ppName);
            DeviceName = Marshal.PtrToStringAnsi(MarshallPointerName);
            DeviceAddress = Marshal.PtrToStringAnsi(MarshallAddressValue);

            // Release the marshall structures.
            Marshal.FreeHGlobal(CopiedNameMarshall);
            Marshal.FreeHGlobal(CopiedAddressMarshall);
        }

        // ---------------------------------- J2534 PUBLIC FACING API CALLS ---------------------------------

        /// <summary>
        /// Runs a new PTOpen command for the provided Device ID
        /// </summary>
        /// <param name="DeviceId">Device ID returned for this instance.</param>
        public void PassThruOpen(out uint DeviceId)
        {
            // Make our call to run the PTOpen command here.
            J2534Err PTCommandError = (J2534Err)this._delegateSet.PTOpen(IntPtr.Zero, out DeviceId);

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
            J2534Err PTCommandError = (J2534Err)this._delegateSet.PTOpen(DevicePtr, out DeviceId);

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
            J2534Err PTCommandError = (J2534Err)this._delegateSet.PTClose(DeviceId);

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
            J2534Err PTCommandError = (J2534Err)this._delegateSet.PTConnect(DeviceId, (uint)Protocol, ConnectFlags, (uint)ConnectBaud, out ChannelId);

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
            J2534Err PTCommandError = (J2534Err)this._delegateSet.PTConnect(DeviceId, (uint)Protocol, ConnectFlags, ConnectBaud, out ChannelId);

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
            J2534Err PTCommandError = (J2534Err)this._delegateSet.PTDisconnect(ChannelId);
            
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
            J2534Err PTCommandError = (J2534Err)this._delegateSet.PTReadMsgs(ChannelId, Messages, out MsgCount, ReadTimeout);

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
            J2534Err PTCommandError = (J2534Err)this._delegateSet.PTWriteMsgs(ChannelId, NativeWrappedMsgs, ref MsgCount, SendTimeout);

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
            J2534Err PTCommandError = (J2534Err)this._delegateSet.PTWriteMsgs(ChannelId, Msgs, ref MsgCount, SendTimeout);

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
            J2534Err PTCommandError = (J2534Err)this._delegateSet.PTStartPeriodicMsg(ChannelId, ref Msg, out MsgId, MessageInterval);

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
            J2534Err PTCommandError = (J2534Err)this._delegateSet.PTStopPeriodicMsg(ChannelId, MsgId);

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
            if (FlowCtl == null) PTCommandError = (J2534Err)this._delegateSet.PTStartMsgFilterFlowPtr(ChannelId, (uint)FilterType, ref Mask, ref Pattern, IntPtr.Zero, out FilterId);
            else
            {
                // For a non null flow ctl send message filter command.
                PassThruStructsNative.PASSTHRU_MSG FlowCtlNoNull = (PassThruStructsNative.PASSTHRU_MSG)FlowCtl;
                PTCommandError = (J2534Err)this._delegateSet.PTStartMsgFilter(ChannelId, (uint)FilterType, ref Mask, ref Pattern, ref FlowCtlNoNull, out FilterId);
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
            J2534Err PTCommandError = (J2534Err)this._delegateSet.PTStopMsgFilter(ChannelId, FilterId);

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
            J2534Err PTCommandError = (J2534Err)this._delegateSet.PTSetProgrammingVoltage(DeviceId, PinNumber, Voltage);

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
            J2534Err PTCommandError = (J2534Err)this._delegateSet.PTReadVersion(DeviceId, FirmwareVersion, JDllVersion, JApiVersion);

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
            J2534Err PTCommandError = (J2534Err)this._delegateSet.PTGetLastError(LastJ2534Error);

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
            J2534Err PTCommandError = (J2534Err)this._delegateSet.PTIoctl(ChannelId, (uint)IoctlId, InputPtr, OutputPtr);

            // If the error is not a NOERROR Response then throw it.
            if (PTCommandError == J2534Err.STATUS_NOERROR) { return; }
            var ErrorBuilder = new StringBuilder(100);
            PassThruGetLastError(ErrorBuilder);

            // Throw exception here.
            throw new PassThruException(PTCommandError, ErrorBuilder);
        }
    }
}