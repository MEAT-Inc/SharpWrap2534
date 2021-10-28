using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using JBoxInvoker.PassThruLogic.J2534Api;
using JBoxInvoker.PassThruLogic.SupportingLogic;

namespace JBoxInvoker.PassThruLogic
{
    /// <summary>
    /// Instance object for the API built in the PassThru logic class.
    /// </summary>
    public sealed class J2534Instance
    {
        // ------------------------------------ SINGLETON CONFIGURATION -----------------------------------

        // Singleton schema for this class object. Two total instances can exist. Device 1/2
        private static J2534Instance _jApiInstance1;
        private static J2534Instance _jApiInstance2;
        private J2534Instance(JDeviceNumber DeviceNumber)
        {
            // Store Number and status values.
            this.DeviceNumber = DeviceNumber;
            this.Status = PTInstanceStatus.NULL;
        } 
        
        /// <summary>
        /// Deconstructor for this type class.
        /// </summary>
        ~J2534Instance()
        {
            // Release the DLL used and make a new delegate set.
            this.JDllImporter = null;
            this.DelegateSet = new PassThruDelegates();
        }

        /// <summary>
        /// Gets our singleton instance object of this class.
        /// </summary>
        public static J2534Instance JApiInstance(JDeviceNumber DeviceNumber)
        {
            // Return device one instance
            if (DeviceNumber == JDeviceNumber.PTDevice1) 
                return _jApiInstance1 ?? (_jApiInstance1 = new J2534Instance(DeviceNumber));

            // Return device 2 instance
            return _jApiInstance2 ?? (_jApiInstance2 = new J2534Instance(DeviceNumber));
        }

        // ------------------------------------ CLASS VALUES FOR J2534 API ---------------------------------

        // JDevice Number.
        public PTInstanceStatus Status { get; private set; }
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
        public bool SetupJInstance(PassThruPaths JApiDllType)
        {
            // Check if this has been run or not.
            // If one and two are set we can't run, or if the type doesn't match existing.
            if (_jApiInstance1?.Status == PTInstanceStatus.INITIALIZED &&
                _jApiInstance2?.Status == PTInstanceStatus.INITIALIZED) return false;
            if (this.J2534DllType != default && this.J2534DllType != JApiDllType) return false;

            // Check status value.
            if (this.Status == PTInstanceStatus.INITIALIZED) return false;

            // Set the version and build our delegate/Importer objects
            this.J2534DllType = JApiDllType;
            this.J2534DllPath = this.J2534DllType.ToDescriptionString();
            this.ApiVersion = this.J2534DllPath.Contains("0500") ? JVersion.V0500 : JVersion.V0404;

            // Build instance values for delegates and importer
            this.DelegateSet = new PassThruDelegates();
            this.JDllImporter = new PassThruImporter(this.J2534DllPath);
            this.JDllImporter.MapDelegateMethods(out this.DelegateSet);

            // Set the status value.
            this.Status = PTInstanceStatus.INITIALIZED;

            // Return passed.
            return true;
        }
        /// <summary>
        /// Destroys a device instance object and returns it.
        /// </summary>
        /// <returns>True if device is released and was real. False if not released.</returns>
        public bool ReleaseJInstance()
        { 
            // Release device here and return passed.
            switch (this.DeviceNumber)
            {
                // If devices are null, then return false.
                case JDeviceNumber.PTDevice1 when _jApiInstance1 == null:
                    return false;
                case JDeviceNumber.PTDevice2 when _jApiInstance2 == null:
                    return false;

                // Null out the instance for device 1 and return. Null out class values.
                case JDeviceNumber.PTDevice1:
                    _jApiInstance1.DelegateSet = null;
                    _jApiInstance1.J2534DllPath = null;
                    _jApiInstance1.JDllImporter = null;
                    _jApiInstance1.J2534DllType = default;
                    _jApiInstance1.Status = PTInstanceStatus.FREED;
                    return true;

                // Null out the instance for device 2 and return. Null out class values.
                case JDeviceNumber.PTDevice2:
                    _jApiInstance2.DelegateSet = null;
                    _jApiInstance2.J2534DllPath = null;
                    _jApiInstance2.JDllImporter = null;
                    _jApiInstance2.J2534DllType = default;
                    _jApiInstance2.Status = PTInstanceStatus.FREED;
                    return true;

                // Default out is false. Can't modify an invalid device ID Value.
                default: return false;
            }
        }
    }
}
