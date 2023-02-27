using System;
using System.Collections.Generic;
using System.Linq;
using SharpWrapper.J2534Api;
using SharpWrapper.PassThruImport;
using SharpWrapper.PassThruTypes;

namespace SharpWrapper.J2534Objects
{
    public class J2534Dll : IComparable
    {
        // DLL Version.
        public JVersion DllVersion { get; }
        public SharpSessionStatus JDllStatus { get; private set; }

        // DLL Class values.
        public string Name { get; }
        public string Vendor { get; }
        public string LongName { get; }
        public string FunctionLibrary { get; }
        public List<ProtocolId> SupportedProtocols { get; }

        // --------------------- DLL OBJECT CTOR AND OVERLOAD VALUES -------------------

        /// <summary>
        /// Builds a new instance of a J2534 DLL
        /// </summary>
        /// <param name="NameOfDLL"></param>
        internal J2534Dll(string PathOfDLL)
        {
            // Build new importing object and apply values to it.
            if (!PassThruImportDLLs.FindDllFromPath(PathOfDLL, out var LocatedDll))
                throw new InvalidOperationException($"Failed to locate any DLLs with the path provided! ({PathOfDLL})");

            // Store values onto here.
            Name = LocatedDll.Name;
            Vendor = LocatedDll.Vendor;
            FunctionLibrary = PathOfDLL;
            LongName = LocatedDll.LongName;
            DllVersion = LocatedDll.DllVersion;
            JDllStatus = SharpSessionStatus.INITIALIZED;
            SupportedProtocols = LocatedDll.SupportedProtocols;
        }
        /// <summary>
        /// Builds a new J2534 DLL based on the provided values.
        /// </summary>
        /// <param name="NameOfDLL"></param>
        /// <param name="VendorValue"></param>
        /// <param name="ShortName"></param>
        /// <param name="FunctionLib"></param>
        internal J2534Dll(string NameOfDLL, string VendorValue, string ShortName, string FunctionLib, List<ProtocolId> ProtocolList)
        {
            // Store DLL Values.
            Name = ShortName;
            Vendor = VendorValue;
            LongName = NameOfDLL;
            FunctionLibrary = FunctionLib;
            SupportedProtocols = ProtocolList;

            // Set Version.
            JDllStatus = SharpSessionStatus.INITIALIZED;
            DllVersion = FunctionLibrary.Contains("0500") ? JVersion.V0500 : JVersion.V0404;
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
            var ApiInstance = new J2534ApiInstance(FunctionLibrary);
            var NextName = ""; uint NextVersion = 0; var NextAddress = "";

            // Temp no error output for failed setup
            PassThruException JExThrown = new PassThruException(J2534Err.STATUS_NOERROR);

            switch (this.DllVersion)
            {
                // FOR VERSION 0.404 ONLY!
                case JVersion.V0404:
                    try
                    {
                        // Build API instance and get our next PT Device instance. 
                        if (ApiInstance.SetupJApiInstance()) ApiInstance.InitNexTPassThruDevice();
                        else break; 

                        // Loop all the name values pulled out of our init routine
                        while (NextName != null)
                        {
                            // Build Temp Device object and init the next PTDevice.
                            ApiInstance.GetNextPassThruDevice(out NextName, out NextVersion, out NextAddress);
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

                    // TODO: DO SOMETHING WITH THIS EXCEPTION INFO!
                    catch (PassThruException JEx) { JExThrown = JEx; }
                    break;

                // FOR VERSION 0.500 ONLY!
                case JVersion.V0500:
                    try
                    {
                        // Find device count. Then get the devices.
                        if (ApiInstance.SetupJApiInstance()) ApiInstance.InitNexTPassThruDevice();
                        else break;

                        // Check our Device count value here and build output JDevices
                        ApiInstance.PassThruScanForDevices(out uint LocatedDeviceCount);
                        for (int SDeviceIndex = 0; SDeviceIndex < LocatedDeviceCount; SDeviceIndex++)
                        {
                            // Build new SDevice from Marshall call and store it into the list of devices now.
                            ApiInstance.PassThruGetNextDevice(out PassThruStructsNative.SDEVICE NextSDevice);
                            PossibleDevices.Add(J2534ApiMarshaller.CopySDeviceFromNative(NextSDevice));
                        }
                    }
                    // TODO: DO SOMETHING WITH THIS EXCEPTION INFO!
                    catch (PassThruException JEx) { JExThrown = JEx; }
                    break;
            }

            // If no devices found, set our DLL to null. This will help compare later on
            if (PossibleDevices.Count == 0) { JDllStatus = SharpSessionStatus.NULL; }

            // Return device list and free device instance.
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
            return FindConnectedSDevices().Select(DeviceObj => DeviceObj.DeviceName).ToList();
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
                $"J2534 DLL: {Name} ({DllVersion.ToDescriptionString()})",
                $"--> DLL Information:",
                $"    \\__ Version: {DllVersion.ToDescriptionString()}",
                $"    \\__ DLL Vendor: {Vendor}",
                $"    \\__ DLL Long Name: {LongName}",
                $"    \\__ DLL Function Library: {FunctionLibrary}",
                $"    \\__ DLL Supported Protocols: {SupportedProtocols.Count}"
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
            return string.CompareOrdinal(Name, DllObj.Name);
        }
    }
}
