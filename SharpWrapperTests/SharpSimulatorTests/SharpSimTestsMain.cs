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

            // Build a new session for testing output here
            var ChannelLoader = new SimulationLoader();
            ChannelLoader.AddSimChannel(
                SimLoadingTestData.Protocol, 
                SimLoadingTestData.BaudRate,
                SimLoadingTestData.ChannelFlags,
                SimLoadingTestData.BuiltFilters, 
                SimLoadingTestData.PairedMessages
            );

            // Build a new player, configure our reader and start reading output
            var SimulationPlayer = new SimulationPlayer(ChannelLoader, JVersion.V0404, "CarDAQ-Plus 3");

            // Store event helpers for simulation objects
            // SimulationPlayer.SimMessageProcessed += (SimSender, SimArgs) => {
            //     LogBroker.Logger.WriteLog("PROCESSED SIMULATION EVENT FOR MESSAGES OK!", LogType.InfoLog);
            // };
            // SimulationPlayer.SimChannelChanged += (SimSender, SimArgs) => {
            //     LogBroker.Logger.WriteLog("PROCESSED SIMULATION EVENT FOR CHANNELS OK!", LogType.InfoLog);
            // };

            // Setup default configuration values for our reader channel here
            SimulationPlayer.SetDefaultMessageValues(50);
            SimulationPlayer.SetDefaultConnectionType(ProtocolId.ISO15765, 0x00, 500000);
            SimulationPlayer.SetDefaultConfigurations(new[] { new Tuple<ConfigParamId, uint>(ConfigParamId.CAN_MIXED_FORMAT, 1) });
            SimulationPlayer.SetDefaultMessageFilters(new[] { new J2534Filter()
            {
                FilterFlags = 0x00,
                FilterFlowCtl = "",
                FilterMask = "00 00 00 00",
                FilterPattern = "00 00 00 00",
                FilterProtocol = ProtocolId.CAN,
                FilterType = FilterDef.PASS_FILTER,
            }});

            // Begin reading here and then wait for 60 seconds
            SimulationPlayer.SetupSimulationReader();
            SimulationPlayer.StartSimulationReader();
            while (SimulationPlayer.SimulationReading) continue;
        }
    }
}
