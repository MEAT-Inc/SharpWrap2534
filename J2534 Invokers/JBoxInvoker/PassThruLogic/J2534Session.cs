using System;
using System.Linq;
using JBoxInvoker.PassThruLogic.SupportingLogic;
using JBoxInvoker.PassThruLogic.PassThruImport;
using JBoxInvoker.PassThruLogic.J2534Objects;

namespace JBoxInvoker.PassThruLogic
{
    /// <summary>
    /// Contains the base information about our J2534 instance objects and types.
    /// </summary>
    public class J2534Session
    {     
        // DLL and Device Instance for our J2534 Box.       
        public readonly J2534Dll JDeviceDll;                // The DLL Instance in use.
        public readonly J2534Device JDeviceInstance;        // The Device instance in use.

        // ---------------------------------------------------------------------------------------------------------------------

        // Status of this session instance, device, and DLL objects
        public PTInstanceStatus SessionStatus => 
            this.DllStatus == PTInstanceStatus.INITIALIZED && this.DeviceStatus == PTInstanceStatus.INITIALIZED ?
                PTInstanceStatus.INITIALIZED : 
                PTInstanceStatus.NULL;
        public PTInstanceStatus DllStatus => this.JDeviceDll.JDllStatus;
        public PTInstanceStatus DeviceStatus => this.JDeviceInstance.DeviceStatus;

        // ---------------------------------------------------------------------------------------------------------------------

        // DLL Information and device information.
        public JVersion DllVersion => this.JDeviceDll.DllVersion;
        public JVersion DeviceVersion => this.JDeviceInstance.J2534Version;
        public string DllName => this.JDeviceDll.LongName;
        public string DeviceName => this.JDeviceInstance.DeviceName;
        public uint DeviceId => this.JDeviceInstance.DeviceId;

        // ---------------------------------------------------------------------------------------------------------------------

        // The Tostring override will return a combination of the following configuraiton setups.
        public string DeviceDllInfoString => this.JDeviceDll.ToDetailedString();
        public string DeviceInfoString => this.JDeviceInstance.ToDetailedString();

        /// <summary>
        /// Tostring override which contains detailed information about this instance object.
        /// </summary>
        /// <returns>String of the instance session</returns>
        public override string ToString()
        {
            // Build output string.
            return 
                $"J2534 DLL:    {this.JDeviceDll.LongName} ({this.JDeviceDll.DllVersion.ToDescriptionString()})\n" +
                $"J2534 Device: {this.DeviceName} ({this.JDeviceInstance.J2534Version.ToDescriptionString()})";
        }
        /// <summary>
        /// Builds detailed output info string. 
        /// Contains names, versions, the DLL path, the Device FW, API, and Dll Version, and other info.
        /// </summary>
        /// <returns></returns>
        public string ToDetailedString()
        {
            // Builds combo string of detailed output information about the DLL now.
            return this.DeviceDllInfoString + "\n" + this.DeviceInfoString;
        }

        // ---------------------------------------------------------------------------------------------------------------------

        // Device Channel Information, filters, and periodic messages.
        public J2534Channel[] DeviceChannels => this.JDeviceInstance.DeviceChannels;
        public J2534Filter[][] ChannelFilters => this.JDeviceInstance.DeviceChannels.Select(ChObj => ChObj.JChannelFilters).ToArray();
        public J2534PeriodicMessage[][] ChannelPeriodicMsgs => this.JDeviceInstance.DeviceChannels.Select(ChObj => ChObj.JChannelPeriodicMessages).ToArray();

        // ----------------------------------------------------------------------------------------------------------------------
   
        /// <summary>
        /// Builds a new J2534 Session instance object using the DLL Name provided.
        /// </summary>
        /// <param name="DllNameFilter">Dll to use</param>
        /// <param name="DeviceNameFilter">Name of the device To use.</param>
        /// <param name="Version">Version of the API</param>
        public J2534Session(JVersion Version, string DllNameFilter, string DeviceNameFilter = "")
        {
            // Build new J2534 DLL For the version and DLL name provided first.
            if (!PassThruImportDLLs.FindDllByName(DllNameFilter, Version, out this.JDeviceDll))
                throw new NullReferenceException($"No J2534 DLLs with the name filter '{DllNameFilter}' were located matching the version given!");

            // Now build our new device object. Find a possible device based on the filter given.
            var LocatedDevicesForDLL = this.JDeviceDll.FindConnectedDeviceNames();
            if (LocatedDevicesForDLL.Count == 0)
                throw new NullReferenceException("No devices for the DLL specified exist on the system at this time!");
            if (DeviceNameFilter != "" && LocatedDevicesForDLL.FirstOrDefault(NameValue => NameValue.Contains(DeviceNameFilter)) == null)
                throw new NullReferenceException($"No devices were found matching the name filter of '{DeviceNameFilter}' provided!");

            // Build device now using the name value desired.
            string NewDeviceName = DeviceNameFilter == "" ? 
                LocatedDevicesForDLL.FirstOrDefault() : 
                LocatedDevicesForDLL.FirstOrDefault(DeviceName => DeviceName.Contains(DeviceNameFilter));

            // Try to build the new session object inside try/catch for when it naturally fails out for some reason.
            try { this.JDeviceInstance = J2534Device.BuildJ2534Device(this.JDeviceDll); }
            catch (Exception InitJ2534FailureEx)
            {
                // Build new compound init Exception and throw it.
                Exception FailedInitException = new InvalidOperationException(
                    "Failed to build new Device Session for the provided device and DLL configuration!",
                    InitJ2534FailureEx
                );
            }
        }
    }
}
