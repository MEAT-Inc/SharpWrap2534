using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SharpWrap2534.J2534Objects;
using SharpWrap2534.PassThruImport;
using SharpWrap2534.SupportingLogic;

namespace SharpWrap2534Tests
{
    [TestClass]
    [TestCategory("J2534 Logic")]
    [TestCategory("J2534 Device")]
    public class PassThruDeviceTests
    {
        // Split output string value.
        private static readonly string SepString = "------------------------------------------------------";

        [TestMethod]
        [TestCategory("J2534 Device and DLL Import")]
        public void BuildJ2534Device()
        {
            // Build new J2534 DLL from the import path.
            Console.WriteLine(SepString + "\nTests Running...\n");
            J2534Dll CarDAQ3_0404Dll = new J2534Dll(PassThruPaths.CarDAQPlus3_0404.ToDescriptionString());
            Assert.IsTrue(CarDAQ3_0404Dll.FunctionLibrary != null, "CarDAQ 3 DLL was not built correctly!");

            // Log info
            Console.WriteLine("--> CarDAQ Plus 3 DLL was built OK!");
            Console.WriteLine("--> Building CarDAQ Plus 3 device instance now...");

            // Get the devices here and print them out.
            var DevicesFound = CarDAQ3_0404Dll.FindConnectedDeviceNames();
            Assert.IsTrue(DevicesFound.Count != 0, "No devices for the CarDAQ+ 3 DLL could be found!");

            // Print device infos.
            Console.WriteLine("--> Device information is below");
            Console.WriteLine(string.Join("\n", DevicesFound.Select(DeviceObj => $"    Device #{DevicesFound.IndexOf(DeviceObj)}: {DeviceObj}").ToList()));

            // Build device isntance.
            var Cdp3Device = J2534Device.BuildJ2534Device(CarDAQ3_0404Dll);
            Console.WriteLine("--> Built new CarDAQ Plus 3 device OK!");
            Assert.IsTrue(Cdp3Device.DeviceChannels != null, "CarDAQ Plus 3 instance failed to startup!");
            Assert.IsTrue(Cdp3Device.DeviceName != null, "Device name was null!");
            Console.WriteLine($"--> Device opened was named {Cdp3Device.DeviceName}");

            // Write infos out to console
            Console.WriteLine(SepString);
            Console.WriteLine("\nTests completed without fatal exceptions!\n");

            // Print split line and check if passed.
            Console.WriteLine(SepString);
        }

        [TestMethod]
        [TestCategory("J2534 Device and DLL Import")]
        public void FindConnectedDevices()
        {
            // Build new J2534 DLL from the import path.
            Console.WriteLine(SepString + "\nTests Running...\n");

            // Build DLL importing list.
            var DLLImporter = new PassThruImportDLLs();
            var ListOfDLLs = DLLImporter.LocatedJ2534DLLs;
            Assert.IsTrue(ListOfDLLs.Length != 0, "No DLLs were found on the system!");

            // Print the infos for the base ones.
            List<bool> ResultsList = new List<bool>();
            Console.WriteLine($"\n{SepString}\nLooping Basic DLLs and finding their devices now...\n");
            foreach (string PTPath in DLLImporter.LocatedJ2534DLLs.Select(DLLObj => DLLObj.FunctionLibrary))
            {
                // Find the DLL object for the current DLL
                if (PTPath.Contains("Fulcrum") || PTPath.Contains("BDS")) continue;
                Console.WriteLine($"Testing Path: {PTPath}");
                if (!PassThruImportDLLs.FindDllFromPath(PTPath, out var NextDLL))
                {
                    // Log failures.
                    Console.WriteLine("--> Failed to import DLL!");
                    Console.WriteLine("--> No Dll was returned from the import call!");

                    // Check if our file is real or not.
                    if (!File.Exists(PTPath))
                        Console.WriteLine("--> The file specified at the path value given could not be found!");

                    // Print newline.
                    Console.WriteLine("");
                    ResultsList.Add(false);
                    continue;
                }

                // Print the DLL infos.
                Console.WriteLine($"--> DLL Long Name: {NextDLL.LongName}");
                Console.WriteLine("--> Building Devices now...");

                // Get the devices here and print them out.
                var DevicesFound = NextDLL.FindConnectedDeviceNames();
                if (DevicesFound.Count == 0) { Console.WriteLine("--> No devices were located for this DLL!\n"); }
                else
                {
                    // Print device infos.
                    Console.WriteLine("--> Device information is below");
                    Console.WriteLine(string.Join("\n", DevicesFound.Select(DeviceObj => $"    Device #{DevicesFound.IndexOf(DeviceObj)}: {DeviceObj}").ToList()));
                    Console.WriteLine("");
                }

                // Add passed.
                ResultsList.Add(true);
            }

            // Write infos out to console
            Console.WriteLine(SepString);
            Console.WriteLine("\nTests completed without fatal exceptions!\n");

            // Print split line and check if passed.
            Console.WriteLine(SepString);
            Assert.IsTrue(ResultsList.TrueForAll(ResultSet => ResultSet));
        }

        [TestMethod]
        [TestCategory("Read Device Voltage")]
        public void ReadJDeviceVoltage()
        {
            // Build new J2534 DLL from the import path.
            Console.WriteLine(SepString + "\nTests Running...\n");
            J2534Dll CarDAQ3_0404Dll = new J2534Dll(PassThruPaths.CarDAQPlus3_0404.ToDescriptionString());
            Assert.IsTrue(CarDAQ3_0404Dll.FunctionLibrary != null, "CarDAQ 3 DLL was not built correctly!");

            // Build device instance.
            var Cdp3Device = J2534Device.BuildJ2534Device(CarDAQ3_0404Dll);
            Console.WriteLine("--> Built new CarDAQ Plus 3 device OK!");
            Assert.IsTrue(Cdp3Device.DeviceChannels != null, "CarDAQ Plus 3 instance failed to startup!");
            Assert.IsTrue(Cdp3Device.DeviceName != null, "Device name was null!");
            Console.WriteLine($"--> Device opened was named {Cdp3Device.DeviceName}");

            // Read the voltage here
            var ReadVoltage = Cdp3Device.PTReadVBattery();
            Console.WriteLine($"--> Voltage Read: {ReadVoltage}");
            Assert.IsTrue(ReadVoltage != -1, "Voltage Value was -1! This means our IOCTL issue failed!");

            // Write infos out to console
            Console.WriteLine(SepString);
            Console.WriteLine("\nTests completed without fatal exceptions!\n");
            Console.WriteLine(SepString);
        }
    }
}