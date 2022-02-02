using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using SharpWrap2534.PassThruTypes;
using SharpWrap2534.SupportingLogic;

namespace SharpWrap2534.J2534Api
{
    /// <summary>
    /// Used to marshall out API methods from an instance of the DLL Api
    /// </summary>
    internal class J2534ApiMarshaller
    {
        // Class values for the marshall configuration
        internal J2534ApiInstance ApiInstance { get; private set; }
        public PTInstanceStatus MarshallStatus { get; private set; }

        // Reflected API Values.
        public JVersion J2534Version => ApiInstance.J2534Version;        // Version of the API
        public PTInstanceStatus ApiStatus => ApiInstance.ApiStatus;     // Status of the API

        // -------------------------------- CONSTRUCTOR FOR A NEW J2534 API MARSHALL -------------------------------

        /// <summary>
        /// Builds a new J2354 API Marshalling object.
        /// </summary>
        /// <param name="ApiInstance">Api to marshall out.</param>
        public J2534ApiMarshaller(J2534ApiInstance ApiInstance)
        {
            // Store API Values.
            this.ApiInstance = ApiInstance;
            MarshallStatus = PTInstanceStatus.INITIALIZED;
        }

        /// <summary>
        /// Break down values on the class instance
        /// </summary>
        ~J2534ApiMarshaller()
        {
            // Breakdown values.
            ApiInstance = null;
            MarshallStatus = PTInstanceStatus.FREED;
        }

        // -------------------------------- PASSTHRU MESSAGE SUPPORTING METHODS ------------------------------------

        /// <summary>
        /// Builds a PTMessage set from a native message set.
        /// </summary>
        /// <param name="NativeMessage"></param>
        /// <param name="ManagedMessage"></param>
        internal static void CopyPassThruMsgToNative(ref PassThruStructsNative.PASSTHRU_MSG NativeMessage, PassThruStructs.PassThruMsg ManagedMessage)
        {
            // Set the values from the native message to the managed one.
            NativeMessage.ProtocolID = (uint)ManagedMessage.ProtocolID;
            NativeMessage.RxStatus = ManagedMessage.RxStatus;
            NativeMessage.TxFlags = ManagedMessage.TxFlags;
            NativeMessage.Timestamp = ManagedMessage.Timestamp;
            NativeMessage.DataSize = ManagedMessage.DataSize;
            NativeMessage.ExtraDataIndex = ManagedMessage.ExtraDataIndex;

            // Copy message data using a buffer.
            Buffer.BlockCopy(ManagedMessage.Data, 0, NativeMessage.Data, 0, (int)ManagedMessage.DataSize);
        }
        /// <summary>
        /// Builds a Managed message from a Native message
        /// </summary>
        /// <param name="ManagedMessage"></param>
        /// <param name="NativeMessage"></param>
        internal static void CopyPassThruMsgFromNative(ref PassThruStructs.PassThruMsg ManagedMessage, PassThruStructsNative.PASSTHRU_MSG NativeMessage)
        {
            // Copy the values from our managed message to the native one.
            ManagedMessage = new PassThruStructs.PassThruMsg(NativeMessage.DataSize)
            {
                ProtocolID = (ProtocolId)NativeMessage.ProtocolID,
                RxStatus = NativeMessage.RxStatus,
                TxFlags = NativeMessage.TxFlags,
                Timestamp = NativeMessage.Timestamp,
                DataSize = NativeMessage.DataSize,
                ExtraDataIndex = NativeMessage.ExtraDataIndex
            };

            // Copy message data using a buffer.
            Buffer.BlockCopy(NativeMessage.Data, 0, ManagedMessage.Data, 0, (int)NativeMessage.DataSize);
        }
        /// <summary>
        /// Takes an SDevice struct and copies it into a managed SDevice struct for use later on.
        /// </summary>
        /// <param name="NativeSDevice">Device to copy from native</param>
        /// <returns>Managed SDevice object</returns>
        internal static PassThruStructs.SDevice CopySDeviceFromNative(PassThruStructsNative.SDEVICE NativeSDevice)
        {
            // Build the new managed device object.
            PassThruStructs.SDevice ManagedSDevice = new PassThruStructs.SDevice();

            // Apply properties here.
            ManagedSDevice.DeviceName = NativeSDevice.DeviceName;
            ManagedSDevice.DeviceAvailable = NativeSDevice.DeviceAvailable;
            ManagedSDevice.DeviceDllFWStatus = NativeSDevice.DeviceAvailable;
            ManagedSDevice.DeviceConnectMedia = NativeSDevice.DeviceAvailable;
            ManagedSDevice.DeviceConnectSpeed = NativeSDevice.DeviceAvailable;
            ManagedSDevice.DeviceSignalQuality = NativeSDevice.DeviceAvailable;
            ManagedSDevice.DeviceSignalStrength = NativeSDevice.DeviceAvailable;

            // Return it here.
            return ManagedSDevice;
        }

        // ----------------------------- MARSHALL API CALLS GENERATED FROM THE API --------------------------------

        /// <summary>
        /// Runs a new PTOpen command for the provided Device ID
        /// </summary>
        /// <param name="DeviceId">Device ID returned for this instance.</param>
        public void PassThruOpen(out uint DeviceId) { ApiInstance.PassThruOpen(out DeviceId); }
        /// <summary>
        /// Runs a new PTOpen command for the provided Device ID
        /// </summary>
        /// <param name="DeviceId">Device ID returned for this instance.</param>
        /// <param name="DeviceName">Name of the device to open</param>
        public void PassThruOpen(string DeviceName, out uint DeviceId)
        {
            IntPtr NameAsPtr = Marshal.StringToHGlobalAnsi(DeviceName);
            try { ApiInstance.PassThruOpen(NameAsPtr, out DeviceId); }
            finally { Marshal.FreeHGlobal(NameAsPtr); }
        }
        /// <summary>
        /// Runs a PTClose for the provided Device ID
        /// </summary>
        /// <param name="DeviceId">ID of device to close down</param>
        public void PassThruClose(uint DeviceId) { ApiInstance.PassThruClose(DeviceId); }
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
            ApiInstance.PassThruConnect(DeviceId, Protocol, ConnectFlags, ConnectBaud, out ChannelId);
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
            ApiInstance.PassThruConnect(DeviceId, Protocol, ConnectFlags, ConnectBaud, out ChannelId);
        }
        /// <summary>
        /// Runs a PassThru disconnect method on the device ID given
        /// </summary>
        /// <param name="ChannelId">ID Of the channel to drop out.</param>
        public void PassThruDisconnect(uint ChannelId) { ApiInstance.PassThruDisconnect(ChannelId); }
        /// <summary>
        /// Reads messages from the given ChannelID
        /// </summary>
        /// <param name="ChannelId">Channel ID to read from.</param>
        /// <param name="Messages">Messages to send</param>
        /// <param name="MsgCount">Number of messages to read</param>
        /// <param name="ReadTimeout">Read timeout value.</param>
        public void PassThruReadMsgs(uint ChannelId, out PassThruStructs.PassThruMsg[] Messages, ref uint MsgCount, uint ReadTimeout)
        {
            // Set messages value to null for not.
            Messages = null;
            PassThruStructsNative.PASSTHRU_MSG[] MessagesNative = new PassThruStructsNative.PASSTHRU_MSG[MsgCount];
            for (var MsgIndex = 0; MsgIndex < MsgCount; MsgIndex++) { MessagesNative[MsgIndex] = new PassThruStructsNative.PASSTHRU_MSG(-1); }

            try
            {
                // Pull messages from device now.
                ApiInstance.PassThruReadMsgs(ChannelId, MessagesNative, out MsgCount, ReadTimeout);
                if (MsgCount == 0) { return; }

                // Loop the messages and build new values.
                Messages = new PassThruStructs.PassThruMsg[MsgCount];
                for (int MsgIndex = 0; MsgIndex < MsgCount; MsgIndex++)
                    CopyPassThruMsgFromNative(ref Messages[MsgIndex], MessagesNative[MsgIndex]);
            }
            catch (PassThruException PtEx)
            {
                // If timed out and some values were read out then build new output.
                if (PtEx.J2534ErrorCode != J2534Err.ERR_TIMEOUT && MsgCount > 0) throw PtEx;

                // If we have some values from the read apply them here.
                Messages = new PassThruStructs.PassThruMsg[MsgCount];
                for (int MsgIndex = 0; MsgIndex < MsgCount; MsgIndex++)
                    CopyPassThruMsgFromNative(ref Messages[MsgIndex], MessagesNative[MsgIndex]);
            }
        }
        /// <summary>
        /// Reads messages from the given ChannelID
        /// </summary>
        /// <param name="ChannelId">Channel ID to read from.</param>
        /// <param name="Message">Messages to send</param>
        /// <param name="MsgCount">Number of messages to read</param>
        /// <param name="ReadTimeout">Read timeout value.</param>
        public void PassThruReadMsgs(uint ChannelId, ref PassThruStructs.PassThruMsg Message, ref uint MsgCount, uint ReadTimeout)
        {
            // Check for more than one message.
            if (MsgCount > 1) throw new Exception("PassThruReadMsgs, for this function overload, message count to read must be 1");

            // Build native and copy to device.
            PassThruStructsNative.PASSTHRU_MSG[] NativeMessage = new PassThruStructsNative.PASSTHRU_MSG[1];
            NativeMessage[0] = new PassThruStructsNative.PASSTHRU_MSG(-1);
            ApiInstance.PassThruReadMsgs(ChannelId, NativeMessage, out MsgCount, ReadTimeout);
            CopyPassThruMsgFromNative(ref Message, NativeMessage[0]);
        }
        /// <summary>
        /// Sends a message to the given channel ID
        /// </summary>
        /// <param name="ChannelId">Channel to send to</param>
        /// <param name="Messages">Message to send</param>
        /// <param name="MsgCount">Messages sent</param>
        /// <param name="SendTimeout">Timeout value for send</param>
        public void PassThruWriteMsgs(uint ChannelId, PassThruStructs.PassThruMsg[] Messages, ref uint MsgCount, uint SendTimeout)
        {
            // Check for message mismatch
            if (Messages.Length < MsgCount) throw new Exception("PassThruWriteMsgs, PassThruMsg array size smaller than message count to write");

            // Copy to native and send.
            PassThruStructsNative.PASSTHRU_MSG[] MessagesNative = new PassThruStructsNative.PASSTHRU_MSG[MsgCount];
            for (var MsgIndex = 0; MsgIndex < Messages.Length; MsgIndex++)
            {
                MessagesNative[MsgIndex] = new PassThruStructsNative.PASSTHRU_MSG(-1);
                CopyPassThruMsgToNative(ref MessagesNative[MsgIndex], Messages[MsgIndex]);
            }

            // Write values.
            ApiInstance.PassThruWriteMsgs(ChannelId, MessagesNative, ref MsgCount, SendTimeout);
        }
        /// <summary>
        /// Sends a message to the given channel ID
        /// </summary>
        /// <param name="ChannelId">Channel to send to</param>
        /// <param name="Message">Message to send</param>
        /// <param name="MsgCount">Messages sent</param>
        /// <param name="SendTimeout">Timeout value for send</param>
        public void PassThruWriteMsgs(uint ChannelId, PassThruStructs.PassThruMsg Message, ref uint MsgCount, uint SendTimeout)
        {
            // Check for message mismatch
            if (MsgCount > 1) throw new Exception("PassThruWriteMsgs, for this function overload, numMsgs to read must be 1");

            // Copy to native and send to device
            PassThruStructsNative.PASSTHRU_MSG[] MessagesNative = new PassThruStructsNative.PASSTHRU_MSG[1];
            MessagesNative[0] = new PassThruStructsNative.PASSTHRU_MSG(-1);
            CopyPassThruMsgToNative(ref MessagesNative[0], Message);

            // Write to device.
            ApiInstance.PassThruWriteMsgs(ChannelId, MessagesNative, ref MsgCount, SendTimeout);
        }
        /// <summary>
        /// Starts a periodic message filter on the given channel with the provided mesasage.
        /// </summary>
        /// <param name="ChannelId">Channel to send on.</param>
        /// <param name="Message">Message ot send </param>
        /// <param name="MessageId">ID of the newly made message</param>
        /// <param name="MessageInterval">Timeout for the send operation</param>
        public void PassThruStartPeriodicMsg(uint ChannelId, PassThruStructs.PassThruMsg Message, out uint MessageId, uint MessageInterval)
        {
            // Copy to native and send out.
            PassThruStructsNative.PASSTHRU_MSG NativeMessage = new PassThruStructsNative.PASSTHRU_MSG(-1);
            CopyPassThruMsgToNative(ref NativeMessage, Message);

            // Send to device.
            ApiInstance.PassThruStartPeriodicMsg(ChannelId, NativeMessage, out MessageId, MessageInterval);
        }
        /// <summary>
        /// Stops a periodic message.
        /// </summary>
        /// <param name="ChannelId">Channel to stop on</param>
        /// <param name="MessageId">Message Id to stop</param>
        public void PassThruStopPeriodicMsg(uint ChannelId, uint MessageId) { ApiInstance.PassThruStopPeriodicMsg(ChannelId, MessageId); }
        /// <summary>
        /// Stars a non flow control filter.
        /// </summary>
        /// <param name="ChannelId">Channel to filter</param>
        /// <param name="FilterType">Type of filter</param>
        /// <param name="Mask">Mask message</param>
        /// <param name="Pattern">Pattern Message</param>
        /// <param name="FilterId">ID of the newly made filter.</param>
        public void PassThruStartMsgFilter(uint ChannelId, FilterDef FilterType, PassThruStructs.PassThruMsg Mask, PassThruStructs.PassThruMsg Pattern, PassThruStructs.PassThruMsg FlowCtl, out uint FilterId)
        {
            // Build native messages for the Pattern/Mask
            PassThruStructsNative.PASSTHRU_MSG maskMsgNative = new PassThruStructsNative.PASSTHRU_MSG(-1);
            PassThruStructsNative.PASSTHRU_MSG patternMsgNative = new PassThruStructsNative.PASSTHRU_MSG(-1);

            // Copy messages over.
            CopyPassThruMsgToNative(ref patternMsgNative, Pattern);
            CopyPassThruMsgToNative(ref maskMsgNative, Mask);

            // Check for flow control filter in use.
            if (FilterType == FilterDef.FLOW_CONTROL_FILTER)
            {
                // Send out a flow ctl filter command here.
                PassThruStructsNative.PASSTHRU_MSG flowControlMsgNative = new PassThruStructsNative.PASSTHRU_MSG(-1);
                CopyPassThruMsgToNative(ref flowControlMsgNative, FlowCtl);
                ApiInstance.PassThruStartMsgFilter(ChannelId, FilterType, maskMsgNative, patternMsgNative, flowControlMsgNative, out FilterId);
                return;
            }

            // Issue non flow ctl filter.
            ApiInstance.PassThruStartMsgFilter(ChannelId, FilterType, maskMsgNative, patternMsgNative, null, out FilterId);
        }
        /// <summary>
        /// Runs a PTStopMSg Filter command and returns.
        /// </summary>
        /// <param name="ChannelId">Channel To stop filter on</param>
        /// <param name="FilterId">Filter ID to stop</param>
        public void PassThruStopMsgFilter(uint ChannelId, uint FilterId) { ApiInstance.PassThruStopMsgFilter(ChannelId, FilterId); }
        /// <summary>
        /// Sets programming voltage on a given pin and device.
        /// </summary>
        /// <param name="DeviceId">Device to apply to</param>
        /// <param name="PinNumber">Pin to set</param>
        /// <param name="Voltage">Voltage to set on pin</param>
        public void PassThruSetProgrammingVoltage(uint DeviceId, uint PinNumber, uint Voltage) { ApiInstance.PassThruSetProgrammingVoltage(DeviceId, PinNumber, Voltage); }
        /// <summary>
        /// Runs a PTRead version command and stores the output.
        /// </summary>
        /// <param name="DeviceId">Device to read</param>
        /// <param name="FirmwareVersion">FW of the Device</param>
        /// <param name="JDllVersion">DLL Version</param>
        /// <param name="JApiVersion">API Version</param>
        public void PassThruReadVersion(uint DeviceID, out string FirmwareVersion, out string JDllVersion, out string JApiVersion)
        {
            // Build SBs for the various out values.
            StringBuilder FwVersionBuilder = new StringBuilder(100);
            StringBuilder DllVersionBuilder = new StringBuilder(100);
            StringBuilder ApiVersionBuilder = new StringBuilder(100);

            // Issue the command to the device
            ApiInstance.PassThruReadVersion(DeviceID, FwVersionBuilder, DllVersionBuilder, ApiVersionBuilder);

            // Store results.
            FirmwareVersion = FwVersionBuilder.ToString();
            JDllVersion = DllVersionBuilder.ToString();
            JApiVersion = ApiVersionBuilder.ToString();
        }

        // ---------------------------------------------------- VERSION 0500 API MARSHALLS -----------------------------------------------------

        /// <summary>
        /// VERSION 0500 ONLY!
        /// Issues a PTLogical Connect
        /// </summary>
        /// <param name="PhysicalChannelId">Physical ID</param>
        /// <param name="ProtocolId">Protocol of channel</param>
        /// <param name="Flags">Flags of message</param>
        /// <param name="Descriptor">Channel Descriptor</param>
        /// <param name="ChannelId">ChannelId connected to</param>
        public void PassThruLogicalConnect(uint PhysicalChannelId, ProtocolId ProtocolId, uint Flags, PassThruStructs.ISO15765ChannelDescriptor ChannelDescriptor, out uint ChannelId)
        {
            // Build descriptor on native side first.
            var DescriptorBuilt = new PassThruStructsNative.ISO15765_CHANNEL_DESCRIPTOR(
                ChannelDescriptor.LocalTxFlags,
                ChannelDescriptor.RemoteTxFlags, 
                ChannelDescriptor.LocalAddress,
                ChannelDescriptor.RemoteAddress
            );

            // Issue command to API
            ApiInstance.PassThruLogicalConnect(PhysicalChannelId, (uint)ProtocolId, Flags, DescriptorBuilt, out ChannelId);
        }
        /// <summary>
        /// Disconnects from a logical channel based on the ID of it.
        /// </summary>
        /// <param name="ChannelId"></param>
        public void PassThruLogicalDisconnect(uint ChannelId) { ApiInstance.PassThruLogicalDisconnect(ChannelId); }
        /// <summary>
        /// Runs a PassThruSelect command on the channel provided 
        /// </summary>
        /// <param name="ChannelSet">Channel set method</param>
        /// <param name="Timeout">Timeout for the command</param>
        public void PassThruSelect(ref PassThruStructs.SChannelSet ChannelSet, uint Timeout)
        {
            // Make sure list of channels matches up with the count of channels
            if (ChannelSet.ChannelList.Count != ChannelSet.ChannelCount)
                throw new Exception("PassThruSelect, SChannelSet.channelList count different than SChannelSet.channelCount");
            
            // Built native SchannelSet
            PassThruStructsNative.SCHANNELSET ChannelSetNative = new PassThruStructsNative.SCHANNELSET
            {
                ChannelCount = ChannelSet.ChannelCount,
                ChannelThreshold = ChannelSet.ChannelThreshold
            };

            // Allocate unmanaged memory and create pointer for the ChannelList
            IntPtr ChannelListPointer = Marshal.AllocHGlobal((int)(sizeof(uint) * ChannelSet.ChannelCount));
            int[] ArrayIntChannels = ChannelSet.ChannelList.ToArray();
            Marshal.Copy(ArrayIntChannels, 0, ChannelListPointer, (int)ChannelSet.ChannelCount);
            
            // Create a pointer to the channelset
            ChannelSetNative.ChannelListPointer = ChannelListPointer;
            IntPtr ptrChannelSet = Marshal.AllocHGlobal(Marshal.SizeOf(ChannelSetNative));
            Marshal.StructureToPtr(ChannelSetNative, ptrChannelSet, true);

            // Issue the command to the API here and copy the channel set back in
            ApiInstance.PassThruSelect(ptrChannelSet, SelectType.READABLE_TYPE, Timeout);
            ChannelSetNative = (PassThruStructsNative.SCHANNELSET)Marshal.PtrToStructure(ptrChannelSet, typeof(PassThruStructsNative.SCHANNELSET));
            if (ChannelSetNative.ChannelCount == 0) { ArrayIntChannels = Array.Empty<int>(); }
            else 
            {
                // Populate values here.
                ArrayIntChannels = new int[ChannelSetNative.ChannelCount];
                Marshal.Copy(ChannelListPointer, ArrayIntChannels, 0, (int)ChannelSetNative.ChannelCount);
            }

            // Copy SCHANNELSET NATIVE to SChannelSet
            ChannelSet.ChannelCount = ChannelSetNative.ChannelCount;
            ChannelSet.ChannelThreshold = ChannelSetNative.ChannelThreshold;
            ChannelSet.ChannelList = ArrayIntChannels.ToList();

            // Free unmanaged memory from the instance now.
            Marshal.DestroyStructure(ptrChannelSet, typeof(PassThruStructsNative.SCHANNELSET));
            Marshal.FreeHGlobal(ChannelListPointer);
        }
        /// <summary>
        /// Queues messages to be sent out.
        /// </summary>
        /// <param name="ChannelId">ID Of channel to apply to</param>
        /// <param name="Messages">Messages to queue</param>
        /// <param name="MessageCount">Number of messages to send</param>
        public void PassThruQueueMsgs(uint ChannelId, PassThruStructs.PassThruMsg[] Messages, ref uint MessageCount)
        {
            // Make sure the message count lines up
            if (Messages.Length < MessageCount) throw new Exception("PassThruQueueMsgs, PassThruMsg array size smaller than MessageCount to write");

            // Build native message structure now.
            PassThruStructsNative.PASSTHRU_MSG[] SendMessagesNative = new PassThruStructsNative.PASSTHRU_MSG[MessageCount];
            for (var MessageIndex = 0; MessageIndex < Messages.Length; MessageIndex++)
            {
                SendMessagesNative[MessageIndex] = new PassThruStructsNative.PASSTHRU_MSG();
                CopyPassThruMsgToNative(ref SendMessagesNative[MessageIndex], Messages[MessageIndex]);
            }

            // Issue command to the API
            ApiInstance.PassThruQueueMsgs(ChannelId, SendMessagesNative, ref MessageCount);
        }
        /// <summary>
        /// Sends a single PassThrumessage using the Queue command
        /// </summary>
        /// <param name="ChannelId">Channel to send on</param>
        /// <param name="Message">Message to send</param>
        /// <param name="MessageCount">Number of messages to send.</param>
        public void PassThruQueueMsgs(uint ChannelId, PassThruStructs.PassThruMsg Message, ref uint MessageCount)
        {
            // Check the message count vs messages being sent
            if (MessageCount > 1) throw new Exception("PassThruQueueMsgs, for this function overload, MessageCount to read must be 1");
            
            // Build native messages.
            PassThruStructs.PassThruMsg[] MessageSetNative = new PassThruStructs.PassThruMsg[1];
            MessageSetNative[0] = Message;

            // Send to the API 
            PassThruQueueMsgs(ChannelId, MessageSetNative, ref MessageCount);
        }

        // -------------------------------------------------------------------------------------------------------------------------------------

    }
}
