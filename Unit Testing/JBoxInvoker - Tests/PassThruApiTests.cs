using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using JBoxInvoker.PassThruLogic;
using JBoxInvoker.PassThruLogic.J2534Api;
using JBoxInvoker.PassThruLogic.SupportingLogic;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace JBoxInvoker___Tests
{
    /// <summary>
    /// Used to test information about the passthru instance object built.
    /// </summary>
    [TestClass]
    [TestCategory("J2534 Logic")]
    public class PassThruApiTests
    {
        // Split output string value.
        private static readonly string SepString = "------------------------------------------------------";

        /// <summary>
        /// Builds a V0404 CDP3 and V0500 CDP3 DLL API init method set.
        /// </summary>
        [TestMethod]
        [TestCategory("J2534 API Instance")]
        public void SetupJInstanceTest()
        { 
            // Log infos
            Console.WriteLine(SepString + "\nTests Running...\n");
            Console.WriteLine("--> Building new J2534 instance for Devices 1 and 2...");

            // Build instances
            var LoaderInstanceDev1 = new J2534ApiInstance(JDeviceNumber.PTDevice1);
            var LoaderInstanceDev2 = new J2534ApiInstance(JDeviceNumber.PTDevice2);
            Console.WriteLine("--> Built new loader instances OK!");
            
            // Load modules into memory.
            bool Loaded0404 = LoaderInstanceDev1.SetupJApiInstance(JDeviceNumber.PTDevice1, PassThruPaths.CarDAQPlus3_0404);
            bool Loaded0500 = LoaderInstanceDev2.SetupJApiInstance(JDeviceNumber.PTDevice2,PassThruPaths.CarDAQPlus3_0500);
            Console.WriteLine("--> Loading process ran without errors!");

            // Release devices.
            LoaderInstanceDev1 = null;
            LoaderInstanceDev2 = null;
            Console.WriteLine("--> Released devices 1 and 2 OK!");
            Console.WriteLine("\n" + SepString);

            // Check the bool results for loading.
            Assert.IsTrue(Loaded0404 && Loaded0500, "Setup J2534 instance loader OK for both V0404 and V0500!");
        }

        /// <summary>
        /// Tests building an invalid instance type for the given object.
        /// </summary>
        [TestMethod]
        [TestCategory("J2534 API Instance")]
        public void CheckExistingDevice()
        {
            // Log infos
            Console.WriteLine(SepString + "\nTests Running...\n");
            Console.WriteLine("--> Building new J2534 instance for Device 1...");

            // Build instances
            var LoaderInstanceDev1 = new J2534ApiInstance(JDeviceNumber.PTDevice1);
            bool ShouldPassLoad = LoaderInstanceDev1.SetupJApiInstance(JDeviceNumber.PTDevice1, PassThruPaths.CarDAQPlus3_0404);
            Assert.IsTrue(ShouldPassLoad, "Failed Loaded DLL for a CDP3! This is a serious issue!");
            Console.WriteLine("--> Loaded initial DLL call for instance OK!");

            // Try building again with incorrect DLL.
            bool ShouldFailLoad = LoaderInstanceDev1.SetupJApiInstance(JDeviceNumber.PTDevice1, PassThruPaths.CarDAQPlus4_0404);
            Assert.IsFalse(ShouldFailLoad, "Failed to ensure only one DLL instance can exist for device type!"); 
            Console.WriteLine("--> Loading procedure failed as expected!");

            // Assert true and log split line.
            Console.WriteLine("\n" + SepString);
            Console.WriteLine("Test Results\n");
            Console.WriteLine("--> Passed check for single DLL configuration OK!");
            Console.WriteLine("\n" + SepString);

            // Assert conditions for test outcome
            Assert.IsFalse(ShouldFailLoad, "Setup J2534 instance loader and passed type setup test!");
        }
    }
}
