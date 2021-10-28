using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using JBoxInvoker.PassThruLogic.J2534Api;
using JBoxInvoker.PassThruLogic.SupportingLogic;

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
        
        private J2534Device(JDeviceNumber DeviceNumber, J2534Dll Dll)
        {
            // Store DLL Value and build marshall.
            this.JDll = Dll;
            this.DeviceNumber = DeviceNumber;

            // Build API and marshall.
            this.ApiInstance = new J2534ApiInstance(this.DeviceNumber);
            this.ApiInstance.SetupJApiInstance(Dll.FunctionLibrary.FromDescriptionString<PassThruPaths>());
            this.ApiMarshall = new J2534ApiMarshaller(this.ApiInstance);

            // Set Status.
            this.DeviceStatus = PTInstanceStatus.INITIALIZED;
        }

        // ---------------------- INSTANCE VALUES AND SETUP FOR DEVICE HERE ---------------

        // Device information.
        public JDeviceNumber DeviceNumber;
        public PTInstanceStatus DeviceStatus;

        // Device Members.
        public J2534Dll JDll;
        internal J2534ApiInstance ApiInstance;
        internal J2534ApiMarshaller ApiMarshall;

        // ------------------------- J2534 DEVICE OBJECT CTOR ----------------------------

        /// <summary>
        /// Builds a new Device instance using the DLL Given
        /// </summary>
        /// <param name="Dll">DLL To build from</param>
        public static J2534Device BuildJ2534Device(JDeviceNumber DeviceNumber, J2534Dll Dll)
        {
            // Return device one instance
            if (DeviceNumber == JDeviceNumber.PTDevice1)
                return _jDeviceInstance1 ?? (_jDeviceInstance1 = new J2534Device(DeviceNumber, Dll));

            // Return device 2 instance
            return _jDeviceInstance2 ?? (_jDeviceInstance2 = new J2534Device(DeviceNumber, Dll));
        }
    }
}
