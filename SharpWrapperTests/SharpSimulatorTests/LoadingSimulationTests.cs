using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.IO;
using NLog.Config;
using SharpLogger.LoggerObjects;
using SharpSimLoader;
using SharpWrap2534.J2534Objects;
using SharpWrap2534.PassThruTypes;
using SharpWrap2534.SupportingLogic;

namespace SharpSimulatorTests
{
    /// <summary>
    /// Test cases for building a new simulation player and loader
    /// </summary>
    [TestClass]
    public class LoadingSimulationTests
    {
        // Start by building a new simulation channel object to load in.
        public readonly ProtocolId Protocol = ProtocolId.ISO15765;
        public readonly J2534Filter[] BuiltFilters = new[] { new J2534Filter()
        {
            FilterFlags = 0x40,
            FilterMask = "00 00 FF FF",
            FilterPattern = "00 00 07 E8",
            FilterFlowCtl = "00 00 07 E0"
        }};
        public readonly PassThruStructs.PassThruMsg[] MessagesToWrite = new[] { new PassThruStructs.PassThruMsg()
        {
            DataSize = 6,
            ProtocolID = ProtocolId.ISO15765,
            TxFlags = (uint)TxFlags.ISO15765_FRAME_PAD,
            Data = new byte[] { 0x00, 0x00, 0x07, 0xE8, 0x09, 0x42 },
        }};
        public readonly PassThruStructs.PassThruMsg[] MessagesToRead = new[] { new PassThruStructs.PassThruMsg()
        {
            DataSize = 6,
            ProtocolID = ProtocolId.ISO15765,
            TxFlags = (uint)TxFlags.ISO15765_FRAME_PAD,
            Data = new byte[] { 0x00, 0x00, 0x07, 0xDF, 0x09, 0x02 },
        }};

        // ------------------------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Loads in a new Channel object configuration setup for the Simulation Channel
        /// </summary>
        [TestMethod]
        public void BuildSimLoader()
        {
            // Init Logging here.
            NLog.LogManager.Configuration = new LoggingConfiguration();
            string LoggingOutputPath = Path.Combine(Directory.GetCurrentDirectory(), "SharpLoggingOutput");
            SharpLogger.LogBroker.ConfigureLoggingSession("SharpSimLoggingTests", LoggingOutputPath);

            // Build a new Simulation Channel
            var ChannelLoader = new SimulationLoader(); 
            int NewIndex = ChannelLoader.AddSimChannel(this.Protocol, this.BuiltFilters, this.MessagesToRead, this.MessagesToWrite);
            Assert.AreNotEqual(-1, NewIndex, "ERROR! FAILED TO ADD NEW SIMULATION CHANNEL SINCE THERE WAS AN INVALID INDEX!");
        }
        /// <summary>
        /// Consumes a SimLoader from the previous test cases and attempts to start the background reader.
        /// </summary>
        [TestMethod] 
        public void ConsumeBuiltLoader()
        {
            // Init Logging here.
            NLog.LogManager.Configuration = new LoggingConfiguration();
            string LoggingOutputPath = Path.Combine(Directory.GetCurrentDirectory(), "SharpLoggingOutput");
            SharpLogger.LogBroker.ConfigureLoggingSession("SharpSimLoggingTests", LoggingOutputPath);

            // Build a new Simulation Channel
            var ChannelLoader = new SimulationLoader();
            int NewIndex = ChannelLoader.AddSimChannel(this.Protocol, this.BuiltFilters, this.MessagesToRead, this.MessagesToWrite);
            Assert.AreNotEqual(-1, NewIndex, "ERROR! FAILED TO ADD NEW SIMULATION CHANNEL SINCE THERE WAS AN INVALID INDEX!");

            // Pull in the old channel built and build a player
            Assert.IsNotNull(ChannelLoader, "ERROR! FAILED TO LOAD NEW CHANNEL OBJECT FROM TEST METHOD BuildSimLoader!");
            var SimulationPlayer = new SimulationPlayer(ChannelLoader, JVersion.V0404, "CarDAQ-Plus 3");

            // Configure the reader object, build startup task
            SimulationPlayer.ConfigureReader(50);
            var StartupTask = SimulationPlayer.BuildReaderTask();
            Assert.IsNotNull(StartupTask, "ERROR! FAILED TO BUILD SIMULATION READER TASK! THIS IS FATAL!");
        }
    }
}
