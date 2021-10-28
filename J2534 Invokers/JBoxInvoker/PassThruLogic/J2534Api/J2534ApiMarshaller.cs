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
    }
}
