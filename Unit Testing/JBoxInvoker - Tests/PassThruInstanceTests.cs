using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using JBoxInvoker.PassThruLogic;
using JBoxInvoker.PassThruLogic.SupportingLogic;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace JBoxInvoker___Tests
{
    [TestClass]
    public class PassThruInstanceTests
    {
        // Split output string value.
        private static readonly string SepString = "------------------------------------------------------";

        [TestMethod]
        [TestCategory("J2534 Instance")]
        public void SetupJInstanceTest()
        { 
            // Log infos
            Console.WriteLine(SepString + "\nTests Running...\n");
            Console.WriteLine("--> Building new J2534 instance for Devices 1 and 2...");

            // Build instances
            var LoaderInstanceDev1 = J2534Instance.JApiInstance(JDeviceNumber.PTDevice1);
            var LoaderInstanceDev2 = J2534Instance.JApiInstance(JDeviceNumber.PTDevice2);
            Console.WriteLine("--> Built new loader instances OK!");
            
            // Load modules into memory.
            bool Loaded0404 = LoaderInstanceDev1.SetupJInstance(PassThruPaths.CarDAQPlus3_0404);
            bool Loaded0500 = LoaderInstanceDev2.SetupJInstance(PassThruPaths.CarDAQPlus3_0500);
            Console.WriteLine("--> Loading process ran without errors!");

            // Check the bool results for loading.
            Console.WriteLine("\n" + SepString);
            Assert.IsTrue(Loaded0404 && Loaded0500, "Setup J2534 instance loader OK for both V0404 and V0500!");
        }
    }
}
