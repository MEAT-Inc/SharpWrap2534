using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Win32;
using SharpWrapper.J2534Objects;
using SharpWrapper.PassThruTypes;

namespace SharpWrapper.PassThruImport
{
    /// <summary>
    /// This class contains the logic needed to build and use new PassThru DLLs from the J2534 DLL object type.
    /// </summary>
    public class PassThruImportDLLs
    {
        #region Custom Events
        #endregion //Custom Events

        #region Fields

        // PT Support key values. Holds both 0404 locations and 0500 locations
        public readonly RegistryKey PassThruSupportKey_0404;
        public readonly RegistryKey PassThruSupportKey_0500;
        public readonly RegistryKey PassThruSupportKey_0404_6432;
        public readonly RegistryKey PassThruSupportKey_0500_6432;

        // Key information and DLL Values for each of the different key locations found
        public readonly string[] DllKeyValues_0404;
        public readonly string[] DllKeyValues_0500;
        public readonly string[] DllKeyValues_0404_6432;
        public readonly string[] DllKeyValues_0500_6432;

        // List of all located DLL Values for both versions and store locations
        public readonly List<J2534Dll> LocatedJ2534DLLs;

        #endregion //Fields

        #region Properties
        #endregion //Properties

        #region Structs and Classes
        #endregion //Structs and Classes

        // ------------------------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Builds a new DLL importing object.
        /// </summary>
        public PassThruImportDLLs()
        {
            // Build fresh list object and init the registry values for both 0404 and 0500, as well as both possible locations
            PassThruSupportKey_0404 = Registry.LocalMachine.OpenSubKey(PassThruConstants.V0404_PASSTHRU_REGISTRY_PATH, false);
            PassThruSupportKey_0500 = Registry.LocalMachine.OpenSubKey(PassThruConstants.V0500_PASSTHRU_REGISTRY_PATH, false);
            PassThruSupportKey_0404_6432 = Registry.LocalMachine.OpenSubKey(PassThruConstants.V0404_PASSTHRU_REGISTRY_PATH_6432, false);
            PassThruSupportKey_0500_6432 = Registry.LocalMachine.OpenSubKey(PassThruConstants.V0500_PASSTHRU_REGISTRY_PATH_6432, false);

            // Pull in all of our V0404 and V0500 Keys and values from the needed registry locations
            DllKeyValues_0404 = PassThruSupportKey_0404?.GetSubKeyNames().Select(KeyValue => KeyValue).ToArray() ?? Array.Empty<string>();
            DllKeyValues_0500 = PassThruSupportKey_0500?.GetSubKeyNames().Select(KeyValue => KeyValue).ToArray() ?? Array.Empty<string>();
            DllKeyValues_0404_6432 = PassThruSupportKey_0404_6432?.GetSubKeyNames().Select(KeyValue => KeyValue).ToArray() ?? Array.Empty<string>();
            DllKeyValues_0500_6432 = PassThruSupportKey_0500_6432?.GetSubKeyNames().Select(KeyValue => KeyValue).ToArray() ?? Array.Empty<string>();

            // Store located key values and exit out of this instance
            LocatedJ2534DLLs = new List<J2534Dll>();
            LocatedJ2534DLLs.AddRange(this._getDLLsForKeyList(PassThruSupportKey_0404, DllKeyValues_0404));
            LocatedJ2534DLLs.AddRange(this._getDLLsForKeyList(PassThruSupportKey_0500, DllKeyValues_0500));
            LocatedJ2534DLLs.AddRange(this._getDLLsForKeyList(PassThruSupportKey_0404_6432, DllKeyValues_0404_6432));
            LocatedJ2534DLLs.AddRange(this._getDLLsForKeyList(PassThruSupportKey_0500_6432, DllKeyValues_0500_6432));
        }

        // ------------------------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Loads and returns all supported J2534 DLLs for our system
        /// </summary>
        /// <returns>A Collection of built J2534 DLL instances</returns>
        public static IEnumerable<J2534Dll> FindAllInstalledDLLs()
        {
            // Build a new DLL import instance and load in all the found DLLs
            var DllImport = new PassThruImportDLLs();
            return DllImport.LocatedJ2534DLLs;
        }

        /// <summary>
        /// Finds the DLL provided based on the function lib name.
        /// </summary>
        /// <returns>True if a DLL is found. False if not.</returns>
        public static bool FindDllFromPath(string PathOfDll, out J2534Dll DllFound)
        {
            // Build list of DLLs here.
            var DLLsInstalled = new PassThruImportDLLs().LocatedJ2534DLLs;
            DllFound = DLLsInstalled.FirstOrDefault(DllObj => DllObj.FunctionLibrary == PathOfDll);

            // Return output based on DLL Value.
            return DllFound != null;
        }
        /// <summary>
        /// Finds a DLL for the given name and version.
        /// </summary>
        /// <param name="DllName">Name to find</param>
        /// <param name="Version">DLL Version</param>
        /// <param name="DllFound">DLL Located</param>
        /// <returns>True if a DLL is located. False if not.</returns>
        public static bool FindDllByName(string DllName, JVersion Version, out J2534Dll DllFound)
        {
            // Build list of DLLs here.
            var DLLsInstalled = new PassThruImportDLLs().LocatedJ2534DLLs;
            DllFound = DLLsInstalled.FirstOrDefault(DllObj => DllObj.Name.ToUpper().Contains(DllName.ToUpper()) && DllObj.DllVersion == Version);

            // Return output based on DLL Value.
            return DllFound != null;
        }

        /// <summary>
        /// Builds an array of new J2534 DLLs
        /// </summary>
        /// <param name="PassThruKey">DLL Parent Key</param>
        /// <param name="DllKeys">DLL name</param>
        /// <returns>A collection of built J2534 DLL instances for the given registry key and lookup locations</returns>
        private J2534Dll[] _getDLLsForKeyList(RegistryKey PassThruKey, string[] DllKeys)
        {
            // Build array set here.
            var BuiltDLLs = DllKeys.Select(DllValue =>
            {
                // Build new DLL and get infos. Check our DLL Version first.
                RegistryKey DeviceKey = PassThruKey.OpenSubKey(DllValue);
                if (DeviceKey == null) return null;

                // Find values here.
                string VendorValue = (string)DeviceKey.GetValue("Vendor", "");
                string ShortName = (string)DeviceKey.GetValue("Name", "");
                string FunctionLibrary = (string)DeviceKey.GetValue("FunctionLibrary", "");

                // Build a temporary list to hold our DLL protocols
                List<ProtocolId> SupportedProtocols = new List<ProtocolId>();

                // Look at all the supported protocols for this DLL
                List<string> DeviceProtocols = DeviceKey.GetSubKeyNames().ToList();
                foreach (var ProtocolKey in DeviceProtocols)
                {
                    // Check to see if we support this protocol or not
                    if ((int)DeviceKey.GetValue(ProtocolKey, 0) == 0) continue;
                    if (!Enum.TryParse(ProtocolKey, out ProtocolId SupportedProtocol)) continue;

                    // If we found this protocol, then add it to our list of output protocols
                    SupportedProtocols.Add(SupportedProtocol);
                }

                // Build and return the new DLL instance for this entry and exit out
                return new J2534Dll(DllValue, VendorValue, ShortName, FunctionLibrary, SupportedProtocols);
            }).Where(JDll => JDll != null).ToArray();

            // Return built Values
            return BuiltDLLs;
        }
    }
}
