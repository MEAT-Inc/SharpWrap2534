using NLog.Config;
using SharpLogger;
using SharpSimulator;
using SharpWrap2534.SupportingLogic;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using SharpLogger.LoggerSupport;
using SharpSimulator.SimulationObjects;
using SharpWrap2534.J2534Objects;
using SharpWrap2534.PassThruTypes;

namespace SharpSimulatorTests
{
    /// <summary>
    /// Main Setup class for testing sharp simulator
    /// </summary>
    public class SharpSimTestsMain
    {
        /// <summary>
        /// Main Entry point for sharp simulator test methods
        /// </summary>
        /// <param name="args"></param>
        public static void Main(string[] args)
        {
            // Init Logging here.
            NLog.LogManager.Configuration = new LoggingConfiguration();
            string LoggingOutputPath = Path.Combine(Directory.GetCurrentDirectory(), "SharpLoggingOutput");
            LogBroker.ConfigureLoggingSession("SharpSimLoggingTests", LoggingOutputPath);
            LogBroker.BrokerInstance.FillBrokerPool();

            // Build a new Simulation Channel and store message pairs on it
            var TestChannel = new SimulationChannel(0, SimLoadingTestData.Protocol, SimLoadingTestData.BaudRate, SimLoadingTestData.ChannelFlags)
            {
                // Store messages onto our simulation channel
                MessagePairs = SimLoadingTestData.PairedMessages,
                MessageFilters = SimLoadingTestData.BuiltFilters,
            };

            // Build a new session for testing output here
            var ChannelLoader = new SimulationLoader();
            ChannelLoader.AddSimChannel(TestChannel);

            // Build a new player, configure our reader and start reading output
            var SimulationPlayer = new SimulationPlayer(ChannelLoader, JVersion.V0404, "CarDAQ-Plus 3");

            // Setup default configuration values for our reader channel here
            SimulationPlayer.SetResponsesEnabled(true);
            SimulationPlayer.SetDefaultMessageValues(100, 1);
            SimulationPlayer.SetDefaultConnectionType(ProtocolId.ISO15765, 0x00, 500000);
            SimulationPlayer.SetDefaultConfigurations(SimLoadingTestData.ReaderConfigs);
            SimulationPlayer.SetDefaultMessageFilters(SimLoadingTestData.ReaderFilters);

            // Begin reading here and then wait for 60 seconds
            SimulationPlayer.InitalizeSimReader();
            SimulationPlayer.StartSimulationReader();
            while (SimulationPlayer.SimulationReading) continue;
        }
    }
}
