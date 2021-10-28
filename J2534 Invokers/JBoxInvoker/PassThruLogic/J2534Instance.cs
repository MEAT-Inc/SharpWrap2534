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
        private static J2534Instance _jApiInstance_1 = null;
        private static J2534Instance _jApiInstance_2 = null;
        private J2534Instance(JDeviceNumber DeviceNumber) { this.DeviceNumber = DeviceNumber; }
        
        /// <summary>
        /// Gets our singleton instance object of this class.
        /// </summary>
        public static J2534Instance JApiInstance(JDeviceNumber DeviceNumber)
        {
            // Return device one instance
            if (DeviceNumber == JDeviceNumber.PTDevice1) 
                return _jApiInstance_1 ?? (_jApiInstance_1 = new J2534Instance(DeviceNumber));

            // Return device 2 instance
            return _jApiInstance_2 ?? (_jApiInstance_2 = new J2534Instance(DeviceNumber));
        }

        // ------------------------------------ CLASS VALUES FOR J2534 API ---------------------------------

        // JDevice Number.
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
            if (this.J2534DllType != default && this.J2534DllType != JApiDllType)
                return false;
            
            // Set the version and build our delegate/Importer objects
            this.J2534DllType = JApiDllType;
            this.J2534DllPath = this.J2534DllType.ToDescriptionString();
            this.ApiVersion = this.J2534DllPath.Contains("0500") ? JVersion.V0500 : JVersion.V0404;

            // Build instance values for delegates and importer
            this.DelegateSet = new PassThruDelegates();
            this.JDllImporter = new PassThruImporter(this.J2534DllPath);
            this.JDllImporter.MapDelegateMethods(out this.DelegateSet);

            // Return passed.
            return true;
        }
    }
}
