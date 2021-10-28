using System;
using System.Runtime.InteropServices;
using JBoxInvoker.PassThruLogic.PassThruImport;
using JBoxInvoker.PassThruLogic.SupportingLogic;

namespace JBoxInvoker.PassThruLogic.J2534Api
{
    /// <summary>
    /// Used to marshall out API methods from an instance of the DLL Api
    /// </summary>
    public class J2534ApiMarshaller
    {
        // Class values for the marshall configuration
        public J2534ApiInstance ApiInstance { get; private set; }
        public PTInstanceStatus MarshallStatus { get; private set; }

        // Reflected API Values.
        public JVersion ApiVersion => ApiInstance.ApiVersion;           // Version of the API
        public PTInstanceStatus ApiStatus => ApiInstance.ApiStatus;     // Status of the API
        public JDeviceNumber DeviceNumber => ApiInstance.DeviceNumber;  // Device Number from the API

        // -------------------------------- CONSTRUCTOR FOR A NEW J2534 API MARSHALL -------------------------------

        /// <summary>
        /// Builds a new J2354 API Marshalling object.
        /// </summary>
        /// <param name="Api">Api to marshall out.</param>
        public J2534ApiMarshaller(J2534ApiInstance Api)
        {
            // Store API Values.
            this.ApiInstance = Api;
            this.MarshallStatus = PTInstanceStatus.INITIALIZED;
        }

        // ----------------------------- MARSHALL API CALLS GENERATED FROM THE API --------------------------------

        /// <summary>
        /// Runs a new PTOpen command for the provided Device ID
        /// </summary>
        /// <param name="DeviceId">Device ID returned for this instance.</param>
        public void PassThruOpen(out uint DeviceId) { this.ApiInstance.PassThruOpen(out DeviceId); }
        public void PassThruOpen(string DeviceName, out uint DeviceId)
        {
            IntPtr NameAsPtr = Marshal.StringToHGlobalAnsi(DeviceName);
            try { this.ApiInstance.PassThruOpen(NameAsPtr, out DeviceId); }
            finally { Marshal.FreeHGlobal(NameAsPtr); }
        }
        /// <summary>
        /// Runs a PTClose for the provided Device ID
        /// </summary>
        /// <param name="DeviceId">ID of device to close down</param>
        public void PassThruClose(uint DeviceId) { this.ApiInstance.PassThruClose(DeviceId);}
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
            this.ApiInstance.PassThruConnect(DeviceId, Protocol, ConnectFlags, ConnectBaud, out ChannelId);
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
            this.ApiInstance.PassThruConnect(DeviceId, Protocol, ConnectFlags, ConnectBaud, out ChannelId);
        }
        /// <summary>
        /// Runs a PassThru disconnect method on the device ID given
        /// </summary>
        /// <param name="ChannelId">ID Of the channel to drop out.</param>
        public void PassThruDisconnect(uint ChannelId) { this.ApiInstance.PassThruDisconnect(ChannelId); }
    }
}
