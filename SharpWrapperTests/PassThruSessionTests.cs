﻿using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;
using SharpWrap2534;
using SharpWrap2534.PassThruTypes;
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
            var SharpSession = Sharp2534Session.OpenSession(JVersion.V0404, "CarDAQ-Plus 3", "CarDAQ-Plus 3 #011534");
            Console.WriteLine("--> SharpSession built OK!");

            // Open and connect now then disconnect
            SharpSession.PTOpen();
            var OpenedChannel = SharpSession.PTConnect(0, ProtocolId.ISO15765, 0x00, 500000, out uint ChannelId);
            Console.WriteLine("--> Pulled new channel instance out OK!");

            // Test operations
            OpenedChannel.ClearRxBuffer(); 
            OpenedChannel.ClearTxBuffer(); 
            Console.WriteLine("--> Clear TX and RX buffers passed OK!");

            // Disconnect and close object.
            SharpSession.PTDisconnect(0);
            SharpSession.PTClose(); 
            Console.WriteLine("--> Session opened and built a new CarDAQ Plus 3 device instance without issues!");

            // Print session infos.
            Console.WriteLine("--> Device and DLL information for this session are being show below.\n");
            Console.WriteLine(SharpSession.ToDetailedString());
            Console.WriteLine("\n" + SepString);

            // Check the bool results for loading and close
            Sharp2534Session.CloseSession(SharpSession);
            Assert.IsTrue(SharpSession.SessionStatus == PTInstanceStatus.INITIALIZED, "Failed to configure a J2534 Session using a sharp instance!");
        }
    }
}
