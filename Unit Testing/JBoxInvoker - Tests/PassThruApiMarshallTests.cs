using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using JBoxInvoker.PassThruLogic.J2534Api;
using JBoxInvoker.PassThruLogic.SupportingLogic;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace JBoxInvoker___Tests
{
    [TestClass]
    [TestCategory("J2534 Logic")]
    public class PassThruApiMarshallTests
    {
        // Split output string value.
        private static readonly string SepString = "------------------------------------------------------";

        /// <summary>
        /// Builds a V0404 CDP3 and V0500 CDP3 DLL API init method set.
        /// </summary>
        [TestMethod]
        [TestCategory("J2534 API Marshall Instance")]
        public void SetupJApiMarshallTest()
        {
            // Log infos
            Console.WriteLine(SepString + "\nTests Running...\n");
            Console.WriteLine("--> Building new J2534 API Marshall instance for Devices 1 and 2...");

            // Build instances
            var LoaderInstanceDev1 = new J2534ApiInstance(JDeviceNumber.PTDevice1);
            var MarshallInstanceDev1 = new J2534ApiMarshaller(LoaderInstanceDev1);
            Console.WriteLine("--> Built new loader instances OK!");
            Console.WriteLine("--> Built new API Marshalling instances OK!");

            // Load modules into memory.
            bool Loaded0404 = LoaderInstanceDev1.SetupJApiInstance(PassThruPaths.CarDAQPlus3_0404);
            Console.WriteLine("--> Loading process ran without errors!");

            // Release devices.
            LoaderInstanceDev1 = null;
            Console.WriteLine("--> Released API and DLL for device 1 OK!");
            Console.WriteLine("\n" + SepString);

            // Check the bool results for loading.
            Assert.IsTrue(Loaded0404 && (MarshallInstanceDev1.ApiStatus == PTInstanceStatus.INITIALIZED && MarshallInstanceDev1.MarshallStatus == PTInstanceStatus.INITIALIZED),
                "Setup J2534 instance loader OK!");
        }
    }
}
