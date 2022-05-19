using NLog.Config;
using SharpLogger;
using SharpSimLoader;
using SharpWrap2534.SupportingLogic;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

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
            SimulationPlayer.ConfigureReader(20, 1); SimulationPlayer.StartSimReader();
        }
    }
}
