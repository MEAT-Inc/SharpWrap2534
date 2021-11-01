using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.Remoting.Metadata.W3cXsd2001;
using System.Text;
using System.Threading.Tasks;
using JBoxInvoker.PassThruLogic.J2534Api;
using JBoxInvoker.PassThruLogic.PassThruTypes;
using JBoxInvoker.PassThruLogic.SupportingLogic;

[assembly: InternalsVisibleTo("JBoxInvokerTests")]
namespace JBoxInvoker.PassThruLogic.J2534Objects
{
    /// <summary>
    /// J2534 Device object used to control the API, the Marshall, and other methods of it.
    /// </summary>
    public sealed class J2534Device
    {
        // -------------------------- SINGLETON CONFIGURATION ----------------------------

        // Singleton schema for this class object. Two total instances can exist. Device 1/2
        private static J2534Device _jDeviceInstance1;
        private static J2534Device _jDeviceInstance2;

        /// <summary>
        /// PRIVATE CTOR FOR SINGLETON USE ONLY!
        /// </summary>
        /// <param name="DeviceNumber"></param>
        /// <param name="Dll"></param>
        private J2534Device(JDeviceNumber DeviceNumber, J2534Dll Dll, string NameFilter = "")
        {
            // Store DLL Value and build marshall.
            this.JDll = Dll;
            this.DeviceNumber = DeviceNumber;

            // Build API and marshall.
            this.ApiInstance = new J2534ApiInstance(Dll.FunctionLibrary);
            this.ApiMarshall = new J2534ApiMarshaller(this.ApiInstance);

            // Build API Instance.
            this.ApiInstance.SetupJApiInstance();

            // Build channels, set status output.
            this.DeviceStatus = PTInstanceStatus.INITIALIZED;
            this.J2534Version = this.ApiInstance.J2534Version;
            this.DeviceChannels = J2534Channel.BuildDeviceChannels(this);

            // Open and close the device. Read version while open.
            this.PTOpen(NameFilter);
            this.ApiMarshall.PassThruReadVersion(this.DeviceId, out string FwVer, out string DllVer, out string JApiVer);
            this.DeviceFwVersion = FwVer; this.DeviceDLLVersion = DllVer; this.DeviceApiVersion = JApiVer;
            this.PTClose();
        }
        /// <summary>
        /// Builds a new SAFE Device instance using a predefined DLL path
        /// </summary>
        /// <param name="DeviceNumber"></param>
        /// <param name="DllPath"></param>
        private J2534Device(JDeviceNumber DeviceNumber, PassThruPaths InputPath, string NameFilter = "")
        {
            // Store DLL Value and build marshall.
            this.JDll = new J2534Dll(InputPath);
            this.DeviceNumber = DeviceNumber;

            // Build API and marshall.
            this.ApiInstance = new J2534ApiInstance(InputPath.ToDescriptionString());
            this.ApiMarshall = new J2534ApiMarshaller(this.ApiInstance);

            // Build API Instance.
            this.ApiInstance.SetupJApiInstance();

            // Build channels, set status output.
            this.DeviceStatus = PTInstanceStatus.INITIALIZED;
            this.J2534Version = this.ApiInstance.J2534Version;
            this.DeviceChannels = J2534Channel.BuildDeviceChannels(this);

            // Open and close the device. Read version while open.
            this.PTOpen(NameFilter);
            this.ApiMarshall.PassThruReadVersion(this.DeviceId, out string FwVer, out string DllVer, out string JApiVer);
            this.DeviceFwVersion = FwVer; this.DeviceDLLVersion = DllVer; this.DeviceApiVersion = JApiVer;
            this.PTClose();
        }

        /// <summary>
        /// Builds a new JDevice without any configuration on it.
        /// </summary>
        private J2534Device() { this.DeviceStatus = PTInstanceStatus.FREED; }
        /// <summary>
        /// Deconstructs the device object and members
        /// </summary>
        ~J2534Device()
        {
            // Set this to a new instance with FREED as the status.
            switch (this.DeviceNumber)
            {
                // Device 1
                case JDeviceNumber.PTDevice1:
                    _jDeviceInstance1 = new J2534Device() { DeviceStatus = PTInstanceStatus.FREED };
                    break;

                // Device 2
                case JDeviceNumber.PTDevice2:
                    _jDeviceInstance2 = new J2534Device() { DeviceStatus = PTInstanceStatus.FREED };
                    break;
            }
        }

        // ---------------------- INSTANCE VALUES AND SETUP FOR DEVICE HERE ---------------

        // Device information.
        public JDeviceNumber DeviceNumber { get; private set; }
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
        public bool IsOpen = false;
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
            return $"J2534 Device: {this.DeviceName} ({this.J2534Version.ToDescriptionString()})";
        }
        /// <summary>
        /// builds a detailed output information string about the J2534 device in question
        /// </summary>
        /// <returns></returns>
        public string ToDetailedString()
        {
            // Build string information here.
            string OutputDetailsString = 
                $"Device: {this.DeviceName} ({this.J2534Version.ToDescriptionString()})" +
                $"\n--> Instance Infromation: " +
                $"\n    \\__ Device Id:      {this.DeviceId}" +
                $"\n    \\__ Device Name:    {this.DeviceName}" +
                $"\n    \\__ Device Version: {this.J2534Version.ToDescriptionString()}" +
                $"\n    \\__ Device Status:  {(this.IsOpen ? "OPEN - " : "NOT OPEN - ")} {(this.IsConnected ? " CONNECTED" : " NOT CONNECTED")}" +
                $"\n--> Device Setup Information:" +
                $"\n    \\__ DLL Version:    {this.DeviceDLLVersion}" +
                $"\n    \\__ FW Version:     {this.DeviceFwVersion}" +
                $"\n    \\__ API Version:    {this.DeviceApiVersion}" +
                $"\n--> Device Channel Information:" +
                $"\n    \\__ Channel Count:  {this.DeviceChannels.Length} Channels" +
                $"\n    \\__ Filter Count:   {this.DeviceChannels.Length * new PassThruConstants(this.J2534Version).MaxFilters} Filters Max" +
                $"\n    \\__ Periodic Count: {this.DeviceChannels.Length * new PassThruConstants(this.J2534Version).MaxPeriodicMsgs} Periodic Msgs Max";

            // Return the output stirng here.
            return OutputDetailsString;
        }


        // ------------------- J2534 DEVICE OBJECT CTOR WITH SINGLETON ----------------------

        /// <summary>
        /// Builds a new Device instance using the DLL Given
        /// </summary>
        /// <param name="Dll">DLL To build from</param>
        internal static J2534Device BuildJ2534Device(J2534Dll Dll, string DeviceNameFilter = "")
        { 
            // Return Device 1 instance.
            if (_jDeviceInstance1?.DeviceStatus != PTInstanceStatus.INITIALIZED)
                return _jDeviceInstance1 ?? (_jDeviceInstance1 = new J2534Device(JDeviceNumber.PTDevice1, Dll, DeviceNameFilter));

            // Return device 2 instance
            if (_jDeviceInstance2?.DeviceStatus != PTInstanceStatus.INITIALIZED)
                return _jDeviceInstance2 ?? (_jDeviceInstance2 = new J2534Device(JDeviceNumber.PTDevice2, Dll, DeviceNameFilter));

            // Throw if here since none of our slots are open.
            throw new AccessViolationException("Can not build instance of a third J2534 Device object!");
        }
        /// <summary>
        /// Builds a new Device instance using the DLL Given
        /// </summary>
        /// <param name="Dll">DLL To build from</param>
        internal static J2534Device BuildJ2534Device(PassThruPaths Dll, string DeviceNameFilter = "")
        {
            // Return Device 1 instance.
            if (_jDeviceInstance1?.DeviceStatus != PTInstanceStatus.INITIALIZED)
                return _jDeviceInstance1 ?? (_jDeviceInstance1 = new J2534Device(JDeviceNumber.PTDevice1, Dll, DeviceNameFilter));

            // Return device 2 instance
            if (_jDeviceInstance2?.DeviceStatus != PTInstanceStatus.INITIALIZED)
                return _jDeviceInstance2 ?? (_jDeviceInstance2 = new J2534Device(JDeviceNumber.PTDevice2, Dll, DeviceNameFilter));

            // Throw if here since none of our slots are open.
            throw new AccessViolationException("Can not build instance of a third J2534 Device object!");
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
        public void PTOpen(string DeviceName = "")
        {
            // Check for no name gvien.
            if (DeviceName == "")
            {
                // Pull all device names out and find next open one.
                var FreeDevice = this.JDll.FindConnectedDeviceNames().FirstOrDefault(Name => !Name.ToUpper().Contains("IN USE"));
                DeviceName = FreeDevice ?? throw new AccessViolationException("No free J2534 devices could be located!");
            }

            // Stet name and open the device here.
            this.DeviceName = DeviceName;
            this.ApiMarshall.PassThruOpen(this.DeviceName, out this.DeviceId);
            this.IsOpen = true;
        }
        /// <summary>
        /// Closes the currently open device object.
        /// </summary>
        public void PTClose()
        {
            // Close device using the marshall.
            this.ApiMarshall.PassThruClose(this.DeviceId);
            this.IsOpen = false;
        }

        /// <summary>
        /// Builds a new PTChannel for this device object.
        /// </summary>
        /// <param name="ChannelIndex">Index of channel</param>
        /// <param name="Protocol">Channel protocol</param>
        /// <param name="ChannelFlags">Connect flags</param>
        /// <param name="ChannelBaud">Channel baud rate</param>
        public void PTConnect(int ChannelIndex, ProtocolId Protocol, uint ChannelFlags, uint ChannelBaud)
        {
            // Issue the connect command and store our channel
            this.ApiMarshall.PassThruConnect(this.DeviceId, Protocol, ChannelFlags, ChannelBaud, out uint ChannelId);
            this.DeviceChannels[ChannelIndex].ConnectChannel(ChannelId, ConnectProtocol, ChannelFlags, ChannelBaud);
        }
        /// <summary>
        /// Disconnects the channel values.
        /// </summary>
        /// <param name="ChannelIndex">Channel to remove</param>
        public void PTDisconnect(int ChannelIndex)
        {
            // Disconnect from marshall and remove from channel set.
            this.ApiMarshall.PassThruDisconnect(this.DeviceChannels[ChannelIndex].ChannelId);
            this.DeviceChannels[ChannelIndex].DisconnectChannel();
        }
    }
}
