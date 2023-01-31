using System.IO;
using System.Linq;
using System.Reflection;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SharpLogger;
using SharpLogger.LoggerObjects;
using SharpLogger.LoggerSupport;
using SharpWrapper.J2534Objects;
using SharpWrapper.PassThruTypes;

namespace SharpSimulator
{
    /// <summary>
    /// Default simulation configuration layout
    /// </summary>
    public class SimulationConfig
    {
        #region Custom Events
        #endregion // Custom Events

        #region Fields

        // Simulation reader base configuration values pulled from JSON or defined by the user
        public uint ReaderTimeout;                          // Timeout for each read routine
        public uint ReaderMsgCount;                         // The number of messages to read
        public uint ResponseTimeout;                        // The timeout for sending responses

        // Basic Channel Configurations
        public BaudRate ReaderBaudRate;                     // Baudrate for the current channel
        public ProtocolId ReaderProtocol;                   // Protocol for the current channel
        public PassThroughConnect ReaderChannelFlags;       // Flags for the current channel

        // Reader configuration filters and IOCTLs
        public J2534Filter[] ReaderFilters;                 // Filters to apply to our reader channel
        public PassThruStructs.SConfigList ReaderConfigs;   // The configurations to apply as IOCTLs for the channel

        #endregion // Fields

        #region Properties

        // Logger object for configuration of an Simulation JSON Reader
        private static SubServiceLogger ConfigLogger => (SubServiceLogger)LoggerQueue.SpawnLogger("SimJsonConfigLogger", LoggerActions.SubServiceLogger);

        // List of all configurations and all supported protocols
        public static ProtocolId[] SupportedProtocols => JArray.FromObject(_allocateResource("DefaultSimConfigurations.json", "SupportedProtocols"))
            .Select(ValueObject => ValueObject.ToObject<ProtocolId>())
            .ToArray();
        public static SimulationConfig[] SupportedConfigurations => JArray.FromObject(_allocateResource("DefaultSimConfigurations.json", "SimulationConfigurations"))
            .Select(ValueObject => JsonConvert.DeserializeObject<SimulationConfig>(ValueObject.ToString()))
            .ToArray();

        #endregion // Properties

        #region Structs and Classes
        #endregion // Structs and Classes

        // ------------------------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Builds a new configuration object and sets defaults to null/empty
        /// </summary>
        public SimulationConfig(ProtocolId ProtocolInUse, BaudRate BaudRate)
        {
            // Store protocol and BaudRate
            this.ReaderBaudRate = BaudRate;
            this.ReaderProtocol = ProtocolInUse;

            // Store basic values here
            this.ReaderMsgCount = 1;
            this.ReaderTimeout = 100;
            this.ResponseTimeout = 500;
            this.ReaderChannelFlags = 0x00;

            // Setup basic empty array for filters with a max count of 10
            this.ReaderFilters = new J2534Filter[10];
            this.ReaderConfigs = new PassThruStructs.SConfigList(0);
        }
        
        // ------------------------------------------------------------------------------------------------------------------------------------------

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
    }
}
