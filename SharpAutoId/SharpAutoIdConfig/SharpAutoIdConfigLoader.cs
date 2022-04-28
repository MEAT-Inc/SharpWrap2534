using System;
using System.IO;
using System.Linq;
using Newtonsoft.Json.Linq;
using SharpAutoId.SharpAutoIdModels;
using SharpLogger;
using SharpLogger.LoggerObjects;
using SharpLogger.LoggerSupport;
using SharpWrap2534.PassThruTypes;

namespace SharpAutoId.SharpAutoIdConfig
{
    public static class SharpAutoIdConfigLoader
    {
        // Logger object for configuration of an AutoID JSON Reader
        private static SubServiceLogger ConfigLogger => (SubServiceLogger)LogBroker.LoggerQueue.GetLoggers(LoggerActions.SubServiceLogger)
            .FirstOrDefault(LoggerObj => LoggerObj.LoggerName.StartsWith("AutoIdConfigLogger")) ?? new SubServiceLogger("AutoIdConfigLogger");

        // ------------------------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Finds our config JSON File from the program files of the running application
        /// </summary>
        /// <param name="NameFilter">Filter to use for name of the file</param>
        /// <returns>Path to the found file</returns>
        private static string GetConfigJsonFile(string NameFilter = null)
        {
            // Find the path of the current application.
            // Search all directories for a json file with the name needed
            string SearchPattern = NameFilter ?? "SharpAutoIdConfigContent" + ".json";
            string[] LocatedFiles = Directory.GetFiles(Directory.GetCurrentDirectory(), SearchPattern, SearchOption.AllDirectories);

            // Return the first matching file name.
            ConfigLogger.WriteLog($"FOUND CONFIGURATION FILE NAMED {LocatedFiles}", LogType.InfoLog);
            return LocatedFiles.FirstOrDefault();
        }

        /// <summary>
        /// List of all currently supported protocols for Auto ID routines
        /// </summary>
        public static ProtocolId[] SupportedProtocols
        {
            get
            {
                // Read in JSON Content from our configuration file. Log values and return output.
                string JsonConfigFile = GetConfigJsonFile("SharpAutoIdConfigContent");
                JObject PulledJsonObject = JObject.Parse(File.ReadAllText(JsonConfigFile));
                var PulledProtocols = PulledJsonObject["SupportedProtocols"].Value<ProtocolId[]>();
                ConfigLogger.WriteLog($"PROTOCOLS SUPPORTED: {string.Join(",", PulledProtocols)}", LogType.InfoLog);
                return PulledProtocols;
            }
        }
        /// <summary>
        /// Auto ID Routine commands and other information needed to build our AutoID session
        /// </summary>
        public static AutoIdConfiguration[] SupportedCommandRoutines
        {
            get
            {
                // Read in JSON Content from our configuration file. Log values and return output.
                string JsonConfigFile = GetConfigJsonFile("SharpAutoIdConfigContent");
                JObject PulledJsonObject = JObject.Parse(File.ReadAllText(JsonConfigFile));
                var PulledAutoIdRoutines = PulledJsonObject["CommandRoutines"].Value<AutoIdConfiguration[]>();
                ConfigLogger.WriteLog($"PULLED A TOTAL OF {PulledAutoIdRoutines.Length} SUPPORTED AUTO ID ROUTINES!", LogType.InfoLog);
                return PulledAutoIdRoutines;
            }
        }
        /// <summary>
        /// All Loaded AutoID routines for this instance
        /// </summary>
        public static Tuple<ProtocolId, AutoIdConfiguration>[] LoadedRoutines
        {
            get
            {
                // Store our AutoID Protocols and Routines here
                var Protocols = SupportedProtocols
                    .OrderBy(ProcObj => ProcObj)
                    .ToArray();
                var Routines = SupportedCommandRoutines
                    .OrderBy(ConfigObj => ConfigObj.AutoIdType)
                    .ToArray();

                // Now build our tuple object.
                var ZippedRoutines = Protocols
                    .Zip(Routines, (ProcObj, RoutineObj) =>
                        new Tuple<ProtocolId, AutoIdConfiguration>(ProcObj, RoutineObj))
                    .ToArray();

                // Return the build list of routines here
                ConfigLogger.WriteLog("ZIPPED PROTOCOLS AND ROUTINES OK! RETURNING THEM NOW...", LogType.InfoLog);
                return ZippedRoutines;
            }
        }

        // ------------------------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Gets an auto ID routine for the given protocol value.
        /// </summary>
        /// <param name="ProtocolToUse">Protocol To use for the AutoID</param>
        /// <returns>Routine matching the given protocol or null</returns>
        internal static AutoIdConfiguration GetRoutineObject(ProtocolId ProtocolToUse)
        {
            // Find our routine.
            var RoutineLocated = SupportedCommandRoutines.FirstOrDefault(RoutineObj => RoutineObj.AutoIdType == ProtocolToUse);
            ConfigLogger.WriteLog(
                RoutineLocated == null ? "NO ROUTINE WAS FOUND! RETURNING NULL!" : $"RETURNING ROUTINE FOR PROTOCOL {ProtocolToUse} NOW...",
                RoutineLocated == null ? LogType.ErrorLog : LogType.InfoLog
            );

            // Return the located routine here
            return RoutineLocated;
        }
    }
}
