using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using JBoxInvoker.PassThruLogic.J2534Api;
using JBoxInvoker.PassThruLogic.PassThruImport;
using JBoxInvoker.PassThruLogic.PassThruTypes;
using JBoxInvoker.PassThruLogic.SupportingLogic;

// For comparing name values
using static System.String;

namespace JBoxInvoker.PassThruLogic.J2534Objects
{
    public class J2534Dll : IComparable
    {
        // DLL Version.
        public JVersion DllVersion { get; private set; }

        // DLL Class values.
        public string Name { get; private set; }
        public string LongName { get; private set; }
        public string FunctionLibrary { get; private set; }
        public string Vendor { get; private set; }
        public List<ProtocolId> SupportedProtocols = new List<ProtocolId>();

        // --------------------- DLL OBJECT CTOR AND OVERLOAD VALUES -------------------

        /// <summary>
        /// Builds a new instance of a J2534 DLL
        /// </summary>
        /// <param name="NameOfDLL"></param>
        public J2534Dll(PassThruPaths PathOfDLL)
        {
            // Build new importing object and apply values to it.
            if (!PassThruImportDLLs.FindDllFromPath(PathOfDLL, out var LocatedDll))
                throw new InvalidOperationException($"Failed to locate any DLLs with the path provided! ({PathOfDLL.ToDescriptionString()})");

            // Store values onto here.
            this.Name = LocatedDll.Name;
            this.Vendor = LocatedDll.Vendor;
            this.LongName = LocatedDll.LongName;
            this.DllVersion = LocatedDll.DllVersion;
            this.FunctionLibrary = PathOfDLL.ToDescriptionString();
        }
        /// <summary>
        /// Builds a new J2534 DLL based on the provided values.
        /// </summary>
        /// <param name="NameOfDLL"></param>
        /// <param name="VendorValue"></param>
        /// <param name="ShortName"></param>
        /// <param name="FunctionLib"></param>
        public J2534Dll(string NameOfDLL, string VendorValue, string ShortName, string FunctionLib, List<ProtocolId> ProtocolList)
        {
            // Store DLL Values.
            this.Name = ShortName;
            this.Vendor = VendorValue;
            this.LongName = NameOfDLL;
            this.FunctionLibrary = FunctionLib;
            this.SupportedProtocols = ProtocolList;

            // Set Version.
            this.DllVersion = this.FunctionLibrary.Contains("0500") ? JVersion.V0500 : JVersion.V0404;
        }

        // ---------------------- DEVICE LOCATION HELPERS FOR DLLS ----------------------

        /// <summary>
        /// Gets a list of all possible devices for this DLL instance.
        /// </summary>
        /// <returns>List of all the SDevices found in the system</returns>
        public List<PassThruStructs.SDevice> FindConnectedSDevices()
        {
            // List of output devices.
            List<PassThruStructs.SDevice> PossibleDevices = new List<PassThruStructs.SDevice>();

            // Build temp device and init the value type output of it.
            var ApiInstance = new J2534ApiInstance(this.FunctionLibrary);
            var NextName = ""; uint NextVersion = 0; var NextAddress = "";

            try
            {
                // Setup temp values for name, address, and version
                ApiInstance.SetupJApiInstance();
                ApiInstance.InitNexTPassThruDevice();
                while (NextName != null)
                {

                    // Build Temp Device object and init the next PTDevice.
                    ApiInstance.GetNextPassThruDevice(out NextName, out NextVersion, out NextAddress);

                    // Store new SDevice instance values.
                    PossibleDevices.Add(new PassThruStructs.SDevice
                    {
                        // Set name values and other infos.
                        DeviceName = NextName,
                        DeviceAvailable = 1,
                        DeviceConnectMedia = 1,
                        DeviceConnectSpeed = 1,
                        DeviceDllFWStatus = 1,
                        DeviceSignalQuality = 1,
                        DeviceSignalStrength = 1
                    });
                }
            }
            catch { }

            // Return device list and free device instance.
            ApiInstance = null;
            return PossibleDevices.Where(DeviceObj => !string.IsNullOrWhiteSpace(DeviceObj.DeviceName))
                .Select(DeviceObj => DeviceObj)
                .ToList();
        }
        /// <summary>
        /// Returns the list of SDevices as a list of names.
        /// </summary>
        /// <returns>Strings of the names of the found passthru devices.</returns>
        public List<string> FindConnectedDeviceNames()
        {
            // Builds a string list from an SDeviceList.
            return this.FindConnectedSDevices().Select(DeviceObj => DeviceObj.DeviceName).ToList();
        }

        // --------------------- COMPARATOR AND STRING OPERATIONS ------------------------

        /// <summary>
        /// Print the name of the DLL built.
        /// </summary>
        /// <returns>String of the DLL Object.</returns>
        public override string ToString() { return Name; }
        /// <summary>
        /// Returns built string of the DLL properties and stores them
        /// </summary>
        /// <returns>Formatted built DLL String value.</returns>
        public string ToDetailedString()
        {
            // Build output string.
            string[] OutputStrings = new string[]
            {
                $"J2534 DLL: {this.Name}",
                $"\\__ Version: {this.DllVersion.ToDescriptionString()}",
                $"\\__ DLL Vendor: {this.Vendor}",
                $"\\__ DLL Long Name: {this.LongName}",
                $"\\__ DLL Function Library: {this.FunctionLibrary}",
                $"\\__ DLL Supported Protocols: {this.SupportedProtocols.Count}"
            };

            // Combine into string and return.
            return string.Join("\n", OutputStrings);
        }
        /// <summary>
        /// Useful for comparing DLL Types in a combobox/array
        /// </summary>
        public int CompareTo(object DLLAsObject)
        {
            J2534Dll DllObj = (J2534Dll)DLLAsObject;
            return CompareOrdinal(this.Name, DllObj.Name);
        }
    }
}
