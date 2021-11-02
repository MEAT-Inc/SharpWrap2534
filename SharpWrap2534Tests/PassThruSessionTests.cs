using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;
using SharpWrap2534;
using SharpWrap2534.SupportingLogic;

namespace SharpWrap2534Tests
{
    [TestClass]
    [TestCategory("J2534 Logic")]
    public class PassThruSessionTests
    {
        // Split output string value.
        private static readonly string SepString = "------------------------------------------------------";

        [TestMethod]
        [TestCategory("J2534 Session")]
        public void BuildNewJ2534Session()
        {
            // Log infos
            Console.WriteLine(SepString + "\nTests Running...\n");
            Console.WriteLine("--> Building new SharpSession instance now...");

            // Builds a new J2534 Session object using a CarDAQ Plus 3 DLL.
            var SharpSession = new Sharp2534Session(JVersion.V0404, "CarDAQ-Plus 3");
            Console.WriteLine("--> SharpSession built OK!");
            Console.WriteLine("--> Session opened and built a new CarDAQ Plus 3 device instance without issues!\n");

            // Print session infos.
            Console.WriteLine("--> Device and DLL information for this session are being show below.");
            Console.WriteLine(SharpSession.ToDetailedString());
            Console.WriteLine("\n" + SepString);

            // Check the bool results for loading.
            Assert.IsTrue(SharpSession.SessionStatus == PTInstanceStatus.INITIALIZED, "Failed to configure a J2534 Session using a sharp instance!");
        }
    }
}
