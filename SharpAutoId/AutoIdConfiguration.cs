using System;
using System.IO;
using System.Linq;
using System.Reflection;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SharpLogging;
using SharpWrapper.PassThruTypes;

namespace SharpAutoId
{
    /// <summary>
    /// Helper class which holds all the AutoID configurations used for AutoID routines
    /// </summary>
    public class AutoIdConfiguration
    {
        #region Custom Events
        #endregion //Custom Events

        #region Fields

        // Logger object for configuration of an AutoID JSON Reader
        private static SharpLogger _configurationLogger;
        private static ProtocolId[] _supportedProtocols;
        private static AutoIdConfiguration[] _supportedAutoIdRoutines;

        #endregion //Fields

        #region Properties

        // Class values for pulling in new information about an AutoID routine
        public BaudRate ConnectBaud { get; set; }
        public PassThroughConnect ConnectFlags { get; set; }
        public ProtocolId AutoIdType { get; set; }
        public FilterObject[] RoutineFilters { get; set; }
        public MessageObject[] RoutineCommands { get; set; }

        #endregion //Properties

        #region Structs and Classes

        /// <summary>
        /// Message object pulled from the app settings json file for Auto ID routines
        /// </summary>
        public struct MessageObject
        {
            // Flags and message information
            public string MessageData;
            public TxFlags MessageFlags;
            public ProtocolId MessageProtocol;
        }

        /// <summary>
        /// Filter object pulled from the app settings json file for auto ID routines
        /// </summary>
        public struct FilterObject
        {
            // Flags and Type
            public TxFlags FilterFlags;
            public FilterDef FilterType;
            public ProtocolId FilterProtocol;

            // Filter content values
            public MessageObject FilterMask;
            public MessageObject FilterPattern;
            public MessageObject FilterFlowControl;
        }

        #endregion //Structs and Classes



        // ------------------------------------------------------------------------------------------------------------------------------------------

        // List of all currently supported protocols for Auto ID routines
        public static ProtocolId[] SupportedProtocols => _supportedProtocols ??= _loadSupportedProtocols();
        public static AutoIdConfiguration[] SupportedAutoIdRoutines => _supportedAutoIdRoutines ??= _loadSupportedConfigurations();

        // All Loaded AutoID routines for this instance
        public static Tuple<ProtocolId, AutoIdConfiguration>[] LoadedRoutines
        {
            get
            {
                // Store our AutoID Protocols and Routines here
                var Protocols = SupportedProtocols
                    .OrderBy(ProcObj => ProcObj)
                    .ToArray();
                var Routines = SupportedAutoIdRoutines
                    .OrderBy(ConfigObj => ConfigObj.AutoIdType)
                    .ToArray();

                // Now build our tuple object.
                var ZippedRoutines = Protocols
                    .Zip(Routines, (ProcObj, RoutineObj) =>
                        new Tuple<ProtocolId, AutoIdConfiguration>(ProcObj, RoutineObj))
                    .ToArray();

                // Return the build list of routines here
                _configurationLogger.WriteLog("ZIPPED PROTOCOLS AND ROUTINES OK! RETURNING THEM NOW...", LogType.InfoLog);
                return ZippedRoutines;
            }
        }

        // ------------------------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Gets an auto ID routine for the given protocol value.
        /// </summary>
        /// <param name="ProtocolToUse">Protocol To use for the AutoID</param>
        /// <returns>Routine matching the given protocol or null</returns>
        public static AutoIdConfiguration GetRoutine(ProtocolId ProtocolToUse)
        {
            // Find our routine.
            var RoutineLocated = SupportedAutoIdRoutines.FirstOrDefault(RoutineObj => RoutineObj.AutoIdType == ProtocolToUse);
            _configurationLogger.WriteLog(
                RoutineLocated == null ? "NO ROUTINE WAS FOUND! RETURNING NULL!" : $"RETURNING ROUTINE FOR PROTOCOL {ProtocolToUse} NOW...",
                RoutineLocated == null ? LogType.ErrorLog : LogType.InfoLog
            );

            // Return the located routine here
            return RoutineLocated;
        }

        /// <summary>
        /// Pulls a new resource from a given file name
        /// </summary>
        /// <param name="ResourceFileName">Name of the file</param>
        /// <param name="ObjectName">Object name</param>
        /// <returns></returns>
        private static object _allocateResource(string ResourceFileName, string ObjectName)
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
        /// Loads and stores the supported protocols for our playback sessions
        /// </summary>
        /// <returns>The protocols supported for the different configurations we support</returns>
        private static ProtocolId[] _loadSupportedProtocols()
        {
            // Pull the Resources here and then convert them into a protocol list and return them out here
            var LoadedProtocols = JArray.FromObject(_allocateResource("AutoIdRoutines.json", "SupportedProtocols"))
                .Select(ValueObject => ValueObject.ToObject<ProtocolId>())
                .ToArray();

            // Return the loaded protocols
            return LoadedProtocols;
        }
        /// <summary>
        /// Loads and stores the default Auto ID configurations for our SharpSessions
        /// </summary>
        /// <returns>The supported AutoID configurations</returns>
        private static AutoIdConfiguration[] _loadSupportedConfigurations()
        {
            // Load in the simulation configuration values from our JSON configuration and store them
            var LoadedConfigurations = JArray.FromObject(_allocateResource("AutoIdRoutines.json", "CommandRoutines"))
                .Select(ValueObject => JsonConvert.DeserializeObject<AutoIdConfiguration>(ValueObject.ToString()))
                .ToArray();

            // Return the loaded configurations
            return LoadedConfigurations;
        }
    }
}
