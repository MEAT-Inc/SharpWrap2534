using System;
using System.Linq;
using System.Runtime.Remoting.Metadata.W3cXsd2001;
using SharpWrap2534.J2534Api;
using SharpWrap2534.PassThruTypes;
using SharpWrap2534.SupportingLogic;

namespace SharpWrap2534.J2534Objects
{
    /// <summary>
    /// J2534 Device object used to control the API, the Marshall, and other methods of it.
    /// </summary>
    public sealed class J2534Device
    {
        // -------------------------- SINGLETON CONFIGURATION ----------------------------

        // Singleton schema for this class object. Two total instances can exist. Device 1/2
        private static J2534Device[] _jDeviceInstance;

        /// <summary>
        /// PRIVATE CTOR FOR SINGLETON USE ONLY!
        /// </summary>
        /// <param name="DeviceNumber"></param>
        /// <param name="Dll"></param>
        private J2534Device(int DeviceNumber, J2534Dll Dll, string NameFilter = "")
        {
            // Store DLL Value and build marshall.
            JDll = Dll;
            this.DeviceNumber = DeviceNumber;

            // Build API and marshall.
            ApiInstance = new J2534ApiInstance(Dll.FunctionLibrary);
            ApiMarshall = new J2534ApiMarshaller(ApiInstance);

            // Build API Instance.
            ApiInstance.SetupJApiInstance();

            // Build channels, set status output.
            DeviceStatus = PTInstanceStatus.INITIALIZED;
            J2534Version = ApiInstance.J2534Version;
            DeviceChannels = J2534Channel.BuildDeviceChannels(this);

            // Open and close the device. Read version while open.
            PTOpen(NameFilter);
            ApiMarshall.PassThruReadVersion(DeviceId, out string FwVer, out string DllVer, out string JApiVer);
            DeviceFwVersion = FwVer; DeviceDLLVersion = DllVer; DeviceApiVersion = JApiVer;
            PTClose();
        }

        /// <summary>
        /// Deconstructs the device object and members
        /// </summary>
        ~J2534Device()
        {
            // Set this to a new instance with FREED as the status. Pull based on device status value.
            if (this.IsOpen) { this.PTClose(); }
            _jDeviceInstance[DeviceNumber - 1] = null;
        }

        // ---------------------- INSTANCE VALUES AND SETUP FOR DEVICE HERE ---------------

        // Device information.
        internal int DeviceNumber { get; private set; }
        public PTInstanceStatus DeviceStatus { get; private set; }
        public JVersion J2534Version { get; private set; }

        // Device Members.
        internal J2534Dll JDll;
        internal J2534ApiInstance ApiInstance;
        internal J2534ApiMarshaller ApiMarshall;
        public J2534Channel[] DeviceChannels;

        // Device Properties
        public uint DeviceId;
        public string DeviceName;
        public bool IsOpen => this.DeviceName != null && this.DeviceName.ToUpper().Contains("IN USE");
        public bool IsConnected = false;

        // Version information
        public string DeviceFwVersion { get; private set; }
        public string DeviceDLLVersion { get; private set; }
        public string DeviceApiVersion { get; private set; }

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
            string OutputDetailsString =
                $"Device: {DeviceName} ({J2534Version.ToDescriptionString()})" +
                $"\n--> Instance Information: " +
                $"\n    \\__ Max Devices:    {_jDeviceInstance.Length} Device instances" +
                $"\n    \\__ Device Id:      {DeviceId}" +
                $"\n    \\__ Device Name:    {DeviceName}" +
                $"\n    \\__ Device Version: {J2534Version.ToDescriptionString()}" +
                $"\n    \\__ Device Status:  {(IsOpen ? "OPEN - " : "NOT OPEN -")} AND {(IsConnected ? "CONNECTED" : "NOT CONNECTED")}" +
                $"\n--> Device Setup Information:" +
                $"\n    \\__ DLL Version:    {DeviceDLLVersion}" +
                $"\n    \\__ FW Version:     {DeviceFwVersion}" +
                $"\n    \\__ API Version:    {DeviceApiVersion}" +
                $"\n--> Device Channel Information:" +
                $"\n    \\__ Channel Count:  {DeviceChannels.Length} Channels" +
                $"\n    \\__ Logical Chan:   {(J2534Version == JVersion.V0404 ? "NOT SUPPORTED!" : "SUPPORTED!")}" + 
                $"\n    \\__ Logical Count:  {(new PassThruConstants(J2534Version).MaxChannelsLogical)} Logical Channels on each physical channel" +
                $"\n    \\__ Filter Count:   {DeviceChannels.Length * new PassThruConstants(J2534Version).MaxFilters} Filters Max across (Evenly Split On All Channels)" +
                $"\n    \\__ Periodic Count: {DeviceChannels.Length * new PassThruConstants(J2534Version).MaxPeriodicMsgs} Periodic Msgs Max (Evenly Split On All Channels)";

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
            // Check if an array of devices exists
            if (_jDeviceInstance == null) _jDeviceInstance = new J2534Device[new PassThruConstants(Dll.DllVersion).MaxDeviceCount];

            // Check for an empty spot.
            var FreeDevice = _jDeviceInstance.FirstOrDefault(DeviceObj => DeviceObj?.DeviceStatus == PTInstanceStatus.FREED);
            int NextFreeDeviceIndex = _jDeviceInstance.ToList().IndexOf(FreeDevice ?? null);

            // If still -1, we're just out of spots
            if (NextFreeDeviceIndex == -1) throw new InvalidOperationException($"No free device slots exist at this time! A max of {_jDeviceInstance.Length} devices can exist at once!");

            // Build new instance and return it.
            _jDeviceInstance[NextFreeDeviceIndex] = new J2534Device(NextFreeDeviceIndex + 1, Dll, DeviceNameFilter);
            return _jDeviceInstance[NextFreeDeviceIndex];
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
            // Build a soaphex of the message contents.
            SoapHexBinary SoapHexBin = SoapHexBinary.Parse(MessageString);

            // Built new PTMessage.
            uint MsgDataSize = (uint)SoapHexBin.Value.Length;
            PassThruStructs.PassThruMsg BuiltPtMsg = new PassThruStructs.PassThruMsg(MsgDataSize);
            BuiltPtMsg.ProtocolID = ProtocolId;
            BuiltPtMsg.TxFlags = MsgFlags;
            BuiltPtMsg.DataSize = MsgDataSize;

            // Apply message values into here.
            for (int ByteIndex = 0; ByteIndex < SoapHexBin.Value.Length; ByteIndex++)
                BuiltPtMsg.Data[ByteIndex] = SoapHexBin.Value[ByteIndex];

            // Return built message.
            return BuiltPtMsg;
        }
        /// <summary>
        /// Converts a set of bytes into a PTMessage
        /// </summary>
        /// <param name="ProtocolId">Protocol to send</param>
        /// <param name="MessageFlags">Flags for the message</param>
        /// <param name="MessageBytes">Bytes of the message data</param>
        /// <returns>Built PTMessage</returns>
        public static PassThruStructs.PassThruMsg CreatePTMsgFromDataBytes(ProtocolId ProtocolId, uint MessageFlags, byte[] MessageBytes)
        {
            // Build new PTMessage
            PassThruStructs.PassThruMsg BuiltMessage = new PassThruStructs.PassThruMsg((uint)MessageBytes.Length);

            // Store properties onto the message.
            BuiltMessage.ProtocolID = ProtocolId;
            BuiltMessage.TxFlags = MessageFlags;
            BuiltMessage.DataSize = (uint)MessageBytes.Length;

            // Apply message bytes.
            for (int ByteIndex = 0; ByteIndex < (uint)MessageBytes.Length; ByteIndex++)
                BuiltMessage.Data[ByteIndex] = MessageBytes[ByteIndex];

            // Return built message.
            return BuiltMessage;
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

        // --------------------------------- J2534 DEVICE OBJECT METHODS ----------------------------

        /// <summary>
        /// Opens this instance of a passthru device.
        /// </summary>
        /// <param name="DeviceName">Name of the device to be opened.</param>
        internal void PTOpen(string DeviceName = "")
        {
            // Make sure we exist in here.
            if (_jDeviceInstance[this.DeviceNumber - 1]?.DeviceName != this.DeviceName)
            {
                // Check if it can
                if (_jDeviceInstance[this.DeviceNumber - 1] == null) _jDeviceInstance[this.DeviceNumber - 1] = this;
                throw new ObjectDisposedException("Can not use a sharp device which has been previously closed out!");
            }

            // Check for no name given.
            if (DeviceName == "")
            {
                // Pull all device names out and find next open one.
                var FreeDevice = JDll.FindConnectedDeviceNames().FirstOrDefault(Name => !Name.ToUpper().Contains("IN USE"));
                DeviceName = FreeDevice ?? throw new AccessViolationException("No free J2534 devices could be located!");
            }

            // Set name and open the device here.
            this.DeviceName = DeviceName;
            ApiMarshall.PassThruOpen(this.DeviceName, out DeviceId);
        }
        /// <summary>
        /// Closes the currently open device object.
        /// </summary>
        internal void PTClose()
        {
            // Check if currently open and close.
            ApiMarshall.PassThruClose(DeviceId);
            _jDeviceInstance[this.DeviceNumber - 1] = null;
        }

        /// <summary>
        /// Builds a new PTChannel for this device object.
        /// </summary>
        /// <param name="ChannelIndex">Index of channel</param>
        /// <param name="Protocol">Channel protocol</param>
        /// <param name="ChannelFlags">Connect flags</param>
        /// <param name="ChannelBaud">Channel baud rate</param>
        internal void PTConnect(int ChannelIndex, ProtocolId Protocol, uint ChannelFlags, uint ChannelBaud)
        {
            // Issue the connect command and store our channel
            ApiMarshall.PassThruConnect(DeviceId, Protocol, ChannelFlags, ChannelBaud, out uint ChannelId);
            DeviceChannels[ChannelIndex].ConnectChannel(ChannelId, ConnectProtocol, ChannelFlags, ChannelBaud);
        }
        /// <summary>
        /// Disconnects the channel values.
        /// </summary>
        /// <param name="ChannelIndex">Channel to remove</param>
        internal void PTDisconnect(int ChannelIndex)
        {
            // Disconnect from marshall and remove from channel set.
            ApiMarshall.PassThruDisconnect(DeviceChannels[ChannelIndex].ChannelId);
            DeviceChannels[ChannelIndex].DisconnectChannel();
        }
    }
}
