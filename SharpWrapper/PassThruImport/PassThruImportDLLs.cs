using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Win32;
using SharpWrap2534.J2534Objects;
using SharpWrap2534.PassThruTypes;
using SharpWrap2534.SupportingLogic;

namespace SharpWrap2534.PassThruImport
{
    /// <summary>
    /// This class contains the logic needed to build and use new PassThru DLLs from the J2534 DLL object type.
    /// </summary>
    public class PassThruImportDLLs
    {
        // PT Support key value.
        public readonly RegistryKey PassThruSupportKey_0404; 
        public readonly RegistryKey PassThruSupportKey_0500; 

        // Key information and DLL Values.
        public readonly string[] DllKeyValues_0404;
        public readonly string[] DllKeyValues_0500;

        // List of all located DLL Values
        public readonly J2534Dll[] LocatedJ2534DLLs;

        // --------------------------------------------------------------------------------

        /// <summary>
        /// Builds a new DLL importing object.
        /// </summary>
        public PassThruImportDLLs()
        {
            // Build fresh list object and init the registry values. (0404)
            PassThruSupportKey_0404 = Registry.LocalMachine.OpenSubKey(PassThruConstants.V0404_PASSTHRU_REGISTRY_PATH, false) ??
                                      Registry.LocalMachine.OpenSubKey(PassThruConstants.V0404_PASSTHRU_REGISTRY_PATH_6432, false);

            // Build fresh list object and init the registry values. (0500)
            PassThruSupportKey_0500 = Registry.LocalMachine.OpenSubKey(PassThruConstants.V0500_PASSTHRU_REGISTRY_PATH, false) ??
                                      Registry.LocalMachine.OpenSubKey(PassThruConstants.V0500_PASSTHRU_REGISTRY_PATH_6432, false);

            // Get our DLL Key values here.
            DllKeyValues_0404 = PassThruSupportKey_0404?.GetSubKeyNames().Select(KeyValue => KeyValue).ToArray();
            DllKeyValues_0500 = PassThruSupportKey_0500?.GetSubKeyNames().Select(KeyValue => KeyValue).ToArray();

            // Store located key values.
            LocatedJ2534DLLs = new[]
            {
                GetDLLsForKeyList(PassThruSupportKey_0404, DllKeyValues_0404),
                GetDLLsForKeyList(PassThruSupportKey_0500, DllKeyValues_0500),
            }.SelectMany(DllSet => DllSet).ToArray();
        }
        /// <summary>
        /// Builds an array of new J2534 DLLs
        /// </summary>
        /// <param name="PassThruKey">DLL Parent Key</param>
        /// <param name="DllKeys">DLL name</param>
        /// <returns></returns>
        private J2534Dll[] GetDLLsForKeyList(RegistryKey PassThruKey, string[] DllKeys)
        {
            // Build array set here.
            var BuiltDLLs = DllKeys.Select(DllValue =>
            {
                // Build new DLL and get infos. Check our DLL Version first.
                RegistryKey DeviceKey = PassThruKey.OpenSubKey(DllValue);

                // Find values here.
                string VendorValue = (string)DeviceKey.GetValue("Vendor", "");
                string ShortName = (string)DeviceKey.GetValue("Name", "");
                string FunctionLibrary = (string)DeviceKey.GetValue("FunctionLibrary", "");

                // Build protocol List
                List<ProtocolId> SupportedProtocols = Enum.GetValues(typeof(ProtocolId)).Cast<ProtocolId>()
                    .Where(ProcId => (int)DeviceKey.GetValue(ProcId.ToString(), 0) == 1)
                    .ToList();

                // Build and return.
                return new J2534Dll(DllValue, VendorValue, ShortName, FunctionLibrary, SupportedProtocols);
            }).ToArray();

            // Return built Values
            return BuiltDLLs;
        }

        // ----------------------------------------------------------------------------------------------

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
        /// Finds a DLL for the given nmame and version.
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
    }
}
