using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SharpLogger;
using SharpLogger.LoggerObjects;
using SharpLogger.LoggerSupport;
using SharpSimulator.SimulationObjects;
using SharpWrap2534.PassThruTypes;

namespace SharpSimulator
{
    /// <summary>
    /// Pulls in a simulation configuration for a specified protocol value
    /// </summary>
    public static class SimulationConfigLoader
    {
        // Logger object for configuration of an AutoID JSON Reader
        private static SubServiceLogger ConfigLogger => (SubServiceLogger)LogBroker.LoggerQueue.GetLoggers(LoggerActions.SubServiceLogger)
            .FirstOrDefault(LoggerObj => LoggerObj.LoggerName.StartsWith("AutoIdConfigLogger")) ?? new SubServiceLogger("AutoIdConfigLogger");

        // ------------------------------------------------------------------------------------------------------------------------------------------
        
        // List of all configurations and all supported protocols
        public static ProtocolId[] SupportedProtocols
        {
            get
            {
                // Pull the Resources here and then convert them into a protocol list and return them out here
                var BuiltArray = JArray.FromObject(AllocateResource("DefaultSimConfigurations.json", "SupportedProtocols"));
                return BuiltArray.Values().Select(ValueObject => ValueObject.ToObject<ProtocolId>()).ToArray();
            }
        }
        public static SimulationConfig[] SupportedConfigurations
        {
            get
            {
                // Pull the Resources here and then convert them into a configuration list and return them out here
                var BuiltArray = JArray.FromObject(AllocateResource("DefaultSimConfigurations.json", "SimulationConfigurations"));
                return BuiltArray.Select(ValueObject => JsonConvert.DeserializeObject<SimulationConfig>(ValueObject.ToString())).ToArray();
            }
        }

        // ------------------------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Pulls a new resource from a given file name
        /// </summary>
        /// <param name="ResourceFileName">Name of the file</param>
        /// <param name="ObjectName">Object name</param>
        /// <returns></returns>
        private static object AllocateResource(string ResourceFileName, string ObjectName)
        {
            // Get the current Assembly
            var CurrentAssy = Assembly.GetExecutingAssembly();
            var AssyResc = CurrentAssy.GetManifestResourceNames().Single(RescName => RescName.Contains(ResourceFileName));
            using (Stream RescStream = CurrentAssy.GetManifestResourceStream(AssyResc))
            using (StreamReader RescReader = new StreamReader(RescStream))
            {
                // Build basic object and then return it to be pulled from
                JObject RescObject = JObject.Parse(RescReader.ReadToEnd());
                return RescObject[ObjectName] ?? RescObject;
            }
        }

        /// <summary>
        /// Gets an auto ID routine for the given protocol value.
        /// </summary>
        /// <param name="ProtocolToUse">Protocol To use for the AutoID</param>
        /// <returns>Routine matching the given protocol or null</returns>
        public static SimulationConfig LoadSimulationConfig(ProtocolId ProtocolToUse)
        {
            // Find our routine.
            var RoutineLocated = SupportedConfigurations.FirstOrDefault(RoutineObj => RoutineObj.ReaderProtocol == ProtocolToUse);
            ConfigLogger.WriteLog(
                RoutineLocated == null ? "NO CONFIG WAS FOUND! RETURNING NULL!" : $"RETURNING CONFIG FOR PROTOCOL {ProtocolToUse} NOW...",
                RoutineLocated == null ? LogType.ErrorLog : LogType.InfoLog
            );

            // Return the located routine here
            return RoutineLocated;
        }
    }
}
