using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
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
        /// <summary>
        /// Setup routine for tests for loading in new simulation objects
        /// </summary>
        [TestInitialize]
        public void SetUpSimTests()
        {
            // Init Logging here.
            NLog.LogManager.Configuration = new LoggingConfiguration();
            string LoggingOutputPath = Path.Combine(Directory.GetCurrentDirectory(), "SharpLoggingOutput");
            SharpLogger.LogBroker.ConfigureLoggingSession("SharpSimLoggingTests", LoggingOutputPath);
        }

        // ------------------------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Loads in a new Channel object configuration setup for the Simulation Channel
        /// </summary>
        [TestMethod]
        public void BuildSimLoader()
        {
            // Build a new Simulation Channel
            var ChannelLoader = new SimulationLoader();
            int NewIndex = ChannelLoader.AddSimChannel(SimLoadingTestData.Protocol, SimLoadingTestData.BuiltFilters, SimLoadingTestData.PairedMessages);
            Assert.AreNotEqual(-1, NewIndex, "ERROR! FAILED TO ADD NEW SIMULATION CHANNEL SINCE THERE WAS AN INVALID INDEX!");
        }
        /// <summary>
        /// Consumes a SimLoader from the previous test cases and attempts to start the background reader.
        /// </summary>
        [TestMethod] 
        public void ConsumeBuiltLoader()
        {
            // Build a new Simulation Channel
            var ChannelLoader = new SimulationLoader();
            int NewIndex = ChannelLoader.AddSimChannel(SimLoadingTestData.Protocol, SimLoadingTestData.BuiltFilters, SimLoadingTestData.PairedMessages);
            Assert.AreNotEqual(-1, NewIndex, "ERROR! FAILED TO ADD NEW SIMULATION CHANNEL SINCE THERE WAS AN INVALID INDEX!");

            // Pull in the old channel built and build a player
            Assert.IsNotNull(ChannelLoader, "ERROR! FAILED TO LOAD NEW CHANNEL OBJECT FROM TEST METHOD BuildSimLoader!");
            var SimulationPlayer = new SimulationPlayer(ChannelLoader, JVersion.V0404, "CarDAQ-Plus 3");

            // Configure the reader object, build startup task
            var StartupTask = SimulationPlayer.BuildReaderTask();
            Assert.IsNotNull(StartupTask, "ERROR! FAILED TO BUILD SIMULATION READER TASK! THIS IS FATAL!");
        }
        /// <summary>
        /// Boots and runs a simulation task for setting up new information for channels
        /// </summary>
        [TestMethod]
        public void StartSimulationReader()
        {
            // Build a new Simulation Channel
            var ChannelLoader = new SimulationLoader();
            int NewIndex = ChannelLoader.AddSimChannel(SimLoadingTestData.Protocol, SimLoadingTestData.BuiltFilters, SimLoadingTestData.PairedMessages);
            Assert.AreNotEqual(-1, NewIndex, "ERROR! FAILED TO ADD NEW SIMULATION CHANNEL SINCE THERE WAS AN INVALID INDEX!");

            // Pull in the old channel built and build a player
            Assert.IsNotNull(ChannelLoader, "ERROR! FAILED TO LOAD NEW CHANNEL OBJECT FROM TEST METHOD BuildSimLoader!");
            var SimulationPlayer = new SimulationPlayer(ChannelLoader, JVersion.V0404, "CarDAQ-Plus 3");

            // Configure the reader object, build startup task
            var StartupTask = SimulationPlayer.BuildReaderTask();
            Assert.IsNotNull(StartupTask, "ERROR! FAILED TO BUILD SIMULATION READER TASK! THIS IS FATAL!");

            // Start the reader task, wait 10 seconds, stop it.
            SimulationPlayer.ConfigureReader(20, 1);
            StartupTask.Wait(new CancellationTokenSource(30000).Token);
        }
    }
}
