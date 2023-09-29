using System;
using System.Linq;
using System.Runtime.Remoting.Metadata.W3cXsd2001;
using SharpWrapper.J2534Api;
using SharpWrapper.PassThruSupport;
using SharpWrapper.PassThruTypes;

namespace SharpWrapper.J2534Objects
{
    /// <summary>
    /// J2534 Device object used to control the API, the Marshall, and other methods of it.
    /// </summary>
    public sealed class J2534Device
    {
        // -------------------------- SINGLETON CONFIGURATION ----------------------------

        // TODO: REMOVE THIS SINGLETON INSTANCE!
        // Singleton schema for this class object. Two total instances can exist. Device 1/2
        // private static J2534Device[] _jDeviceInstances;

        /// <summary>
        /// PRIVATE CTOR FOR SINGLETON USE ONLY!
        /// </summary>
        /// <param name="NameFilter"></param>
        /// <param name="Dll"></param>
        private J2534Device(J2534Dll Dll, string NameFilter = "")
        {
            // Store DLL Value and build marshall.
            JDll = Dll;

            // Build API and marshall.
            ApiInstance = new J2534ApiInstance(Dll.FunctionLibrary);
            ApiMarshall = new J2534ApiMarshaller(ApiInstance);

            // Build API Instance.
            ApiInstance.SetupJApiInstance();

            // Build channels, set status output.
            DeviceStatus = SharpSessionStatus.INITIALIZED;
            J2534Version = ApiInstance.J2534Version;
            DeviceChannels = J2534Channel.BuildDeviceChannels(this);

            // Open and close the device. Read version while open.
            PTOpen(NameFilter);
            ApiMarshall.PassThruReadVersion(DeviceId, out string FwVer, out string DllVer, out string JApiVer);
            DeviceFwVersion = FwVer; DeviceDllVersion = DllVer; DeviceApiVersion = JApiVer;
            PTClose();
        }
        /// <summary>
        /// Closes out a J2534 Device session instance
        /// </summary>
        /// <returns>True if closed ok. False if not.</returns>
        internal bool DestroyDevice()
        {
            // Set this to a new instance with FREED as the status. Pull based on device status value.
            if (this.IsOpen) { this.PTClose(); }
            try
            {
                // Close our instance value here and then destroy channels for it.
                J2534Channel.DestroyDeviceChannels(this);
                return true;
            }
            catch { return false; }
        }
        /// <summary>
        /// DCTOR Method routine attempt for when this object is closed out by garbage collection
        /// </summary>
        ~J2534Device() { this?.DestroyDevice(); }


        // ---------------------- INSTANCE VALUES AND SETUP FOR DEVICE HERE ---------------

        // Device information.
        public JVersion J2534Version { get; }
        public SharpSessionStatus DeviceStatus { get; }

        // Device Members.
        internal J2534Dll JDll;
        internal J2534ApiInstance ApiInstance;
        internal J2534ApiMarshaller ApiMarshall;
        public J2534Channel[] DeviceChannels;

        // Device Properties
        public uint DeviceId;
        public string DeviceName;
        public bool IsConnected;

        // Not null name and ID is not 0. If our name contains in use then we need to set to open too
        public bool IsOpen => !string.IsNullOrWhiteSpace(this.DeviceName) && this.DeviceId != 0;

        // Version information
        public string DeviceFwVersion { get; }
        public string DeviceDllVersion { get; }
        public string DeviceApiVersion { get; }

        // Connection Information
        public uint ConnectFlags { get; set; }                  // Used by ConnectStrategy
        public uint ConnectBaud { get; set; }                   // Used by ConnectStrategy
        public ProtocolId ConnectProtocol { get; set; }         // Used by ConnectStrategy


        // --------------------- J2534 DEVICE TO STRING OVERRIDES --------------------------

        /// <summary>
        /// Returns the device object as a string output.
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            // Build return string and store it.
            return $"J2534 Device: {DeviceName} ({J2534Version.ToDescriptionString()})";
        }
        /// <summary>
        /// builds a detailed output information string about the J2534 device in question
        /// </summary>
        /// <returns></returns>
        public string ToDetailedString()
        {
            // Build string information here.
            var Constants = new PassThruConstants(J2534Version);
            string OutputDetailsString =
                $"Device: {DeviceName} ({J2534Version.ToDescriptionString()})" +
                $"\n--> Instance Information: " +
                $"\n    \\__ Max Devices:    {Constants.MaxDeviceCount} Device instances" +
                $"\n    \\__ Device Id:      {DeviceId}" +
                $"\n    \\__ Device Name:    {DeviceName}" +
                $"\n    \\__ Device Version: {J2534Version.ToDescriptionString()}" +
                $"\n    \\__ Device Status:  {(IsOpen ? "OPEN" : "NOT OPEN")} AND {(IsConnected ? "CONNECTED" : "NOT CONNECTED")}" +
                $"\n--> Device Setup Information:" +
                $"\n    \\__ DLL Version:    {DeviceDllVersion}" +
                $"\n    \\__ FW Version:     {DeviceFwVersion}" +
                $"\n    \\__ API Version:    {DeviceApiVersion}" +
                $"\n--> Device Channel Information:" +
                $"\n    \\__ Channel Count:  {DeviceChannels.Length} Channels" +
                $"\n    \\__ Logical Chan:   {(J2534Version == JVersion.V0404 ? "NOT SUPPORTED!" : "SUPPORTED!")}" + 
                $"\n    \\__ Logical Count:  {(Constants.MaxChannelsLogical)} Logical Channels on each physical channel" +
                $"\n    \\__ Filter Count:   {DeviceChannels.Length * Constants.MaxFilters} Filters Max across (Evenly Split On All Channels)" +
                $"\n    \\__ Periodic Count: {DeviceChannels.Length * Constants.MaxPeriodicMsgs} Periodic Msgs Max (Evenly Split On All Channels)";

            // Return the output string here.
            return OutputDetailsString;
        }

        // ------------------- J2534 DEVICE OBJECT CTOR WITH SINGLETON ----------------------

        /// <summary>
        /// Builds a new Device instance using the DLL Given
        /// </summary>
        /// <param name="Dll">DLL To build from</param>
        internal static J2534Device BuildJ2534Device(J2534Dll Dll, string DeviceNameFilter = "")
        {
            // Build new instance and return it.
            return new J2534Device(Dll, DeviceNameFilter);
        }

        // ---------------------------- STATIC DEVICE MESSAGE HELPER METHODS -----------------------

        /// <summary>
        /// Builds a new message from a given string value
        /// </summary>
        /// <param name="ProtocolId">Message protocol</param>
        /// <param name="MsgFlags">Flags</param>
        /// <param name="MessageString">String of message bytes</param>
        /// <returns>Converted PTMessage</returns>
        public static PassThruStructs.PassThruMsg CreatePTMsgFromString(ProtocolId ProtocolId, uint MsgFlags, string MessageString)
        {
            try
            {
                // Build a soaphex of the message contents assuming it has content
                if (string.IsNullOrWhiteSpace(MessageString)) return default;
                SoapHexBinary SoapHexBin = SoapHexBinary.Parse(MessageString);

                // Built new PTMessage.
                uint MsgDataSize = (uint)SoapHexBin.Value.Length;
                PassThruStructs.PassThruMsg BuiltPtMsg = new PassThruStructs.PassThruMsg(MsgDataSize)
                {
                    ProtocolId = ProtocolId,
                    TxFlags = (TxFlags)MsgFlags,
                    DataSize = MsgDataSize
                };

                // Apply message values into here.
                for (int ByteIndex = 0; ByteIndex < SoapHexBin.Value.Length; ByteIndex++)
                    BuiltPtMsg.Data[ByteIndex] = SoapHexBin.Value[ByteIndex];

                // Return built message.
                return BuiltPtMsg;
            }
            catch
            {
                // On failures, return an empty message object 
                return default;
            }
        }
        /// <summary>
        /// Builds a new message from a given string value
        /// </summary>
        /// <param name="ProtocolId">Message protocol</param>
        /// <param name="MsgFlags">Flags</param>
        /// <param name="RxStatus">The RX Status</param>
        /// <param name="MessageString">String of message bytes</param>
        /// <returns>Converted PTMessage</returns>
        public static PassThruStructs.PassThruMsg CreatePTMsgFromString(ProtocolId ProtocolId, uint MsgFlags, uint RxStatus, string MessageString)
        {
            try
            {
                // Build a soaphex of the message contents.
                if (string.IsNullOrWhiteSpace(MessageString)) return default;
                SoapHexBinary SoapHexBin = SoapHexBinary.Parse(MessageString);

                // Built new PTMessage.
                uint MsgDataSize = (uint)SoapHexBin.Value.Length;
                PassThruStructs.PassThruMsg BuiltPtMsg = new PassThruStructs.PassThruMsg(MsgDataSize)
                {
                    ProtocolId = ProtocolId,
                    DataSize = MsgDataSize,
                    TxFlags = (TxFlags)MsgFlags,
                    RxStatus = (RxStatus)RxStatus
                };

                // Apply message values into here.
                for (int ByteIndex = 0; ByteIndex < SoapHexBin.Value.Length; ByteIndex++)
                    BuiltPtMsg.Data[ByteIndex] = SoapHexBin.Value[ByteIndex];

                // Return built message.
                return BuiltPtMsg;
            }
            catch
            {
                // On failures, return an empty message object 
                return default;
            }
        }
        /// <summary>
        /// Converts a set of bytes into a PTMessage
        /// </summary>
        /// <param name="ProtocolId">Protocol to send</param>
        /// <param name="MsgFlags">Flags for the message</param>
        /// <param name="MessageBytes">Bytes of the message data</param>
        /// <returns>Built PTMessage</returns>
        public static PassThruStructs.PassThruMsg CreatePTMsgFromDataBytes(ProtocolId ProtocolId, uint MsgFlags, byte[] MessageBytes)
        {
            try
            {
                // Make sure we've got input byte content first 
                if (MessageBytes == null || MessageBytes.Length == 0) return default;

                // Build new PTMessage and store the properties of it
                PassThruStructs.PassThruMsg BuiltMessage = new PassThruStructs.PassThruMsg((uint)MessageBytes.Length)
                {
                    // Configure protocol, flags, and data size
                    ProtocolId = ProtocolId,
                    TxFlags = (TxFlags)MsgFlags,
                    DataSize = (uint)MessageBytes.Length
                };

                // Apply message bytes.
                for (int ByteIndex = 0; ByteIndex < (uint)MessageBytes.Length; ByteIndex++)
                    BuiltMessage.Data[ByteIndex] = MessageBytes[ByteIndex];

                // Return built message.
                return BuiltMessage;
            }
            catch
            {
                // On failures, return an empty message object 
                return default;
            }
        }
        /// <summary>
        /// Converts a set of bytes into a PTMessage
        /// </summary>
        /// <param name="ProtocolId">Protocol to send</param>
        /// <param name="MsgFlags">Flags for the message</param>
        /// <param name="RxStatus">The RX Status</param>
        /// <param name="MessageBytes">Bytes of the message data</param>
        /// <returns>Built PTMessage</returns>
        public static PassThruStructs.PassThruMsg CreatePTMsgFromDataBytes(ProtocolId ProtocolId, uint MsgFlags, uint RxStatus, byte[] MessageBytes)
        {
            try
            {
                // Make sure we've got input byte content first 
                if (MessageBytes == null || MessageBytes.Length ==0) return default;

                // Build new PTMessage and store the properties of it
                PassThruStructs.PassThruMsg BuiltMessage = new PassThruStructs.PassThruMsg((uint)MessageBytes.Length)
                {
                    // Configure protocol, flags, data size, and RxStatus
                    ProtocolId = ProtocolId,
                    TxFlags = (TxFlags)MsgFlags,
                    RxStatus = (RxStatus)RxStatus,
                    DataSize = (uint)MessageBytes.Length
                };

                // Apply message bytes.
                for (int ByteIndex = 0; ByteIndex < (uint)MessageBytes.Length; ByteIndex++)
                    BuiltMessage.Data[ByteIndex] = MessageBytes[ByteIndex];

                // Return built message.
                return BuiltMessage;
            }
            catch
            {
                // On failures, return an empty message object 
                return default;
            }
        }
        /// <summary>
        /// Converts an SByte array into an array
        /// </summary>
        /// <param name="SArray">Input SByte array values</param>
        /// <returns>Byte array of output values.</returns>
        public static byte[] CreateByteArrayFromSByteArray(PassThruStructs.SByteArray SArray)
        {
            // Build array and append input values.
            byte[] ResultBytes = new byte[SArray.NumberOfBytes];
            for (int ByteIndex = 0; ByteIndex < SArray.NumberOfBytes; ByteIndex++)
                ResultBytes[ByteIndex] = SArray.Data[ByteIndex];

            // Return new output.
            return ResultBytes;
        }


        /// <summary>
        /// Pulls the values of a Message object and converts them into a string table.
        /// </summary>
        /// <param name="InputMessage"></param>
        /// <returns></returns>
        public static string PTMessageToTableString(PassThruStructs.PassThruMsg InputMessage)
        {
            // Build an output tuple array for the message object here.
            Tuple<string, string>[] FieldsAndValues = InputMessage.GetType().GetFields()
                .Select(FieldObj => new Tuple<string, string>(FieldObj.Name, FieldObj.GetValue(InputMessage).ToString()))
                .ToArray();

            // Now make our table string.
            return FieldsAndValues.ToStringTable(new[] { "Property", "Message Value" });
        }
        /// <summary>
        /// Pulls the values of a Message object and converts them into a string table.
        /// </summary>
        /// <param name="InputMessages">Input messages to convert over</param>
        /// <returns>String set of messages built.</returns>
        public static string PTMessageToTableString(PassThruStructs.PassThruMsg[] InputMessages)
        {
            // Build our set of message values and return them
            var MessageStringSet = InputMessages.Select(PTMessageToTableString);
            string OutputString = string.Join("\n", MessageStringSet);
            return OutputString;
        }

        // --------------------------------- J2534 DEVICE OBJECT METHODS ----------------------------

        /// <summary>
        /// Opens this instance of a passthru device.
        /// </summary>
        /// <param name="DeviceNameFilter">Name of the device to be opened.</param>
        public void PTOpen(string DeviceNameFilter = "")
        {
            // If the device is open at this point, then just return out. We don't want to do anything more.
            if (this.IsOpen) { return; }

            // Pull all device names out and find next open one.
            if (DeviceNameFilter == "") DeviceNameFilter = this.DeviceName;
            if (string.IsNullOrWhiteSpace(DeviceNameFilter)) {
                var FreeDevice = JDll.FindConnectedDeviceNames().FirstOrDefault(Name => !Name.ToUpper().Contains("IN USE"));
                DeviceNameFilter = FreeDevice ?? throw new AccessViolationException("No free J2534 devices could be located!");
            }

            // Set name and open the device here.
            this.DeviceName = DeviceNameFilter;
            ApiMarshall.PassThruOpen(DeviceNameFilter, out DeviceId);
        }
        /// <summary>
        /// Closes the currently open device object.
        /// </summary>
        public void PTClose()
        {
            // Check if currently open and close. If it's not open, then just exit out.
            if (!this.IsOpen) { return; }

            // Close device, clear out ID values
            ApiMarshall.PassThruClose(DeviceId);
            DeviceId = 0;
        }

        /// <summary>
        /// Builds a new PTChannel for this device object.
        /// </summary>
        /// <param name="ChannelIndex">Index of channel</param>
        /// <param name="Protocol">Channel protocol</param>
        /// <param name="ChannelFlags">Connect flags</param>
        /// <param name="ChannelBaud">Channel baud rate</param>
        public void PTConnect(int ChannelIndex, ProtocolId Protocol, PassThroughConnect ChannelFlags, BaudRate ChannelBaud)
        {
            // Issue the connect command and store our channel
            ApiMarshall.PassThruConnect(DeviceId, Protocol, (uint)ChannelFlags, ChannelBaud, out uint ChannelId);
            DeviceChannels[ChannelIndex].ConnectChannel(ChannelId, Protocol, (uint)ChannelFlags, (uint)ChannelBaud);
            this.IsConnected = DeviceChannels.Any(ChObj => ChObj?.ChannelId != 0);
        }
        /// <summary>
        /// Disconnects the channel values.
        /// </summary>
        /// <param name="ChannelIndex">Channel to remove</param>
        public void PTDisconnect(int ChannelIndex)
        {
            // Disconnect from marshall and remove from channel set.
            ApiMarshall.PassThruDisconnect(DeviceChannels[ChannelIndex].ChannelId);
            DeviceChannels[ChannelIndex].DisconnectChannel();
            this.IsConnected = DeviceChannels.Any(ChObj => ChObj?.ChannelId != 0);
        }

        /// <summary>
        /// Reads the voltage of the pin number given and returns it as a uint value natively.
        /// </summary>
        /// <returns>The Uint value of the voltage on the given pin number in milivolts</returns>
        public int PTReadVBattery()
        {
            // Read the voltage off of our ApiMarshall. On Failure, return -1 so we know it's failed.
            this.ApiMarshall.PassThruIoctl(this.DeviceId, IoctlId.READ_PIN_VOLTAGE, out uint VoltageRead);
            return (int)VoltageRead;
        }
    }
}
