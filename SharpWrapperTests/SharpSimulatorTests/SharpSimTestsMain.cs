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
            SimulationPlayer.SetDefaultMessageValues(100, 1);
            SimulationPlayer.SetDefaultConnectionType(ProtocolId.ISO15765, 0x00, 500000);
            SimulationPlayer.SetDefaultConfigurations(new[] { new Tuple<ConfigParamId, uint>(ConfigParamId.CAN_MIXED_FORMAT, 1) });
            SimulationPlayer.SetDefaultMessageFilters(new[] 
            {
                // Passing all 0x07 0xXX Addresses
                new J2534Filter() 
                {
                    FilterFlags = 0x00,
                    FilterFlowCtl = "",
                    FilterMask = "00 00 07 00",
                    FilterPattern = "00 00 07 00",
                    FilterProtocol = ProtocolId.CAN,
                    FilterType = FilterDef.PASS_FILTER,
                },

                // Blocking out the 0x07 0x72 address (Used for testing on my GM Moudle)
                new J2534Filter()                
                {
                    FilterFlags = 0x00,
                    FilterFlowCtl = "",
                    FilterMask = "00 00 07 72",
                    FilterPattern = "00 00 07 72",
                    FilterProtocol = ProtocolId.CAN,
                    FilterType = FilterDef.BLOCK_FILTER,
                },
            });

            // Begin reading here and then wait for 60 seconds
            SimulationPlayer.SetupSimulationReader();
            SimulationPlayer.StartSimulationReader();
            while (SimulationPlayer.SimulationReading) continue;
        }
    }
}
