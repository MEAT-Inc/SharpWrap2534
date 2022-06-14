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
                MessageFilters = SimLoadingTestData.SimChannelFilters,
            };

            // Build a new session for testing output here
            var ChannelLoader = new SimulationLoader();
            ChannelLoader.AddSimChannel(TestChannel);

            // Build a new player, configure our reader and start reading output
            var SimulationPlayer = new SimulationPlayer(ChannelLoader, JVersion.V0404, "CarDAQ-Plus 3");
            var SimConfiguration = SimulationConfigLoader.LoadSimulationConfig(ProtocolId.ISO15765);

            // Setup default configuration values for our reader channel here
            SimulationPlayer.SetResponsesEnabled(true);
            SimulationPlayer.SetDefaultConfigurations(SimConfiguration.ReaderConfigs);
            SimulationPlayer.SetDefaultMessageFilters(SimConfiguration.ReaderFilters);
            SimulationPlayer.SetDefaultMessageValues(SimConfiguration.ReaderTimeout, SimConfiguration.ReaderMsgCount);
            SimulationPlayer.SetDefaultConnectionType(
                SimConfiguration.ReaderProtocol, 
                SimConfiguration.ReaderChannelFlags,
                SimConfiguration.ReaderBaudRate
            );

            // Begin reading here and then wait for 60 seconds
            SimulationPlayer.InitializeSimReader();
            SimulationPlayer.StartSimulationReader();
            while (SimulationPlayer.SimulationReading) continue;
        }
    }
}
