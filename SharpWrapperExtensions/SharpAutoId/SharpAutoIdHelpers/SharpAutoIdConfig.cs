using System;
using System.IO;
using System.Linq;
using System.Reflection;
using Newtonsoft.Json.Linq;
using SharpAutoId.SharpAutoIdModels;
using SharpLogger;
using SharpLogger.LoggerObjects;
using SharpLogger.LoggerSupport;
using SharpWrap2534.PassThruTypes;

namespace SharpAutoId.SharpAutoIdHelpers
{
    public class SharpAutoIdConfig
    {
        // Logger object for configuration of an AutoID JSON Reader
        private static SubServiceLogger ConfigLogger => (SubServiceLogger)LogBroker.LoggerQueue.GetLoggers(LoggerActions.SubServiceLogger)
            .FirstOrDefault(LoggerObj => LoggerObj.LoggerName.StartsWith("AutoIdConfigLogger")) ?? new SubServiceLogger("AutoIdConfigLogger");

        // ------------------------------------------------------------------------------------------------------------------------------------------

        // List of all currently supported protocols for Auto ID routines
        public static ProtocolId[] SupportedProtocols => 
            JArray.FromObject(AllocateResource("AutoIdRoutines.json", "SupportedProtocols"))
                .Value<ProtocolId[]>();
        public static SharpIdConfiguration[] SupportedCommandRoutines =>
            JArray.FromObject(AllocateResource("AutoIdRoutines.json", "SupportedCommandRoutines"))
                .Value<SharpIdConfiguration[]>();
        

        // All Loaded AutoID routines for this instance
        public static Tuple<ProtocolId, SharpIdConfiguration>[] LoadedRoutines
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
                        new Tuple<ProtocolId, SharpIdConfiguration>(ProcObj, RoutineObj))
                    .ToArray();

                // Return the build list of routines here
                ConfigLogger.WriteLog("ZIPPED PROTOCOLS AND ROUTINES OK! RETURNING THEM NOW...", LogType.InfoLog);
                return ZippedRoutines;
            }
        }

        // ------------------------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Pulls a new resource from a given file name
        /// </summary>
        /// <param name="ResourceFileName">Name of the file</param>
        /// <param name="ObjectName">Object name</param>
        /// <returns></returns>
        private static JObject AllocateResource(string ResourceFileName, string ObjectName)
        {
            // Get the current Assembly
            var CurrentAssy = Assembly.GetExecutingAssembly();
            var AssyResc = CurrentAssy.GetManifestResourceNames().Single(RescName => RescName.Contains(ResourceFileName));
            using (Stream RescStream = CurrentAssy.GetManifestResourceStream(AssyResc))
            using (StreamReader RescReader = new StreamReader(RescStream))
            {
                // Build basic object and then return it to be pulled from
                JObject RescObject = JObject.Parse(RescReader.ReadToEnd());
                if (RescObject[ObjectName] == null) return RescObject;
                if (RescObject[ObjectName].Type == JTokenType.Array)
                    return JObject.FromObject(RescObject[ObjectName]);

                // Try and return our requested part of the object
                try { return (JObject)RescObject[ObjectName]; }
                catch { return RescObject; }
            }
        }

        /// <summary>
        /// Gets an auto ID routine for the given protocol value.
        /// </summary>
        /// <param name="ProtocolToUse">Protocol To use for the AutoID</param>
        /// <returns>Routine matching the given protocol or null</returns>
        internal static SharpIdConfiguration GetRoutine(ProtocolId ProtocolToUse)
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
