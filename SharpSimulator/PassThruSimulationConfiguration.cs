using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SharpWrapper.J2534Objects;
using SharpWrapper.PassThruTypes;
using SharpLogging;
using SharpSimulator.PassThruSimulationSupport;

namespace SharpSimulator
{
    /// <summary>
    /// Default simulation configuration layout
    /// </summary>
    // TODO: Configure JSON Converter for the configuration objects
    // [JsonConverter(typeof(PassThruSimConfigJsonConverter))]
    public class PassThruSimulationConfiguration
    {
        #region Custom Events
        #endregion // Custom Events

        #region Fields

        // Private backing fields for static configuration
        private static SharpLogger _configurationLogger;                                 // Static logger to write information for static methods
        private static ProtocolId[] _supportedProtocols;                                 // Supported simulation default protocols
        private static PassThruSimulationConfiguration[] _supportedConfigurations;       // Supported default simulation configurations

        #endregion // Fields

        #region Properties

        // Name of this simulation configuration 
        public string ConfigurationName { get; set; }                                    // Name of the configuration. Defaults to protocol

        // Simulation reader base configuration values pulled from JSON or defined by the user
        public uint ReaderTimeout { get; set; }                                          // Timeout for each read routine
        public uint ReaderMsgCount { get; set; }                                         // The number of messages to read
        public uint ResponseTimeout { get; set; }                                        // The timeout for sending responses
        public uint ResponseAttempts { get; set; }                                       // The number of attempts for responses

        // Basic Channel Configurations                                                  
        public BaudRate ReaderBaudRate { get; set; }                                     // Baudrate for the current channel
        public ProtocolId ReaderProtocol { get; set; }                                   // Protocol for the current channel
        public PassThroughConnect ReaderChannelFlags { get; set; }                       // Flags for the current channel

        // Reader configuration filters and IOCTLs                                       
        public List<J2534Filter> ReaderFilters { get; set; }                              // Filters to apply to our reader channel
        public PassThruStructs.SConfigList ReaderConfigs { get; set; }                    // The configurations to apply as IOCTLs for the channel

        // List of all configurations and all supported protocols for playback during simulations
        public static ProtocolId[] SupportedProtocols => _supportedProtocols ??= _loadSupportedProtocols();
        public static PassThruSimulationConfiguration[] SupportedConfigurations => _supportedConfigurations ??= _loadSupportedConfigurations();

        #endregion // Properties

        #region Structs and Classes
        #endregion // Structs and Classes

        // ------------------------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Default CTOR for a configuration object. Used mainly by JSON converter
        /// </summary>
        [JsonConstructor]
        internal PassThruSimulationConfiguration()
        {
            // Setup a new configuration logger if possible
            _configurationLogger ??= new SharpLogger(LoggerActions.UniversalLogger);
        }
        /// <summary>
        /// Builds a new configuration object. Used to build a configuration step by step
        /// </summary>
        /// <param name="ConfigurationName">Optional name of our configuration</param>
        public PassThruSimulationConfiguration(string ConfigurationName)
        {
            // Setup a new configuration logger if possible and store the name of the configuration
            this.ConfigurationName = ConfigurationName;
            _configurationLogger ??= new SharpLogger(LoggerActions.UniversalLogger);
        }
        /// <summary>
        /// Builds a new configuration object and sets defaults to null/empty
        /// </summary>
        /// <param name="ProtocolInUse">Protocol for the configuration</param>
        /// <param name="BaudRate">BaudRate of the simulation</param>
        /// <param name="ConfigurationName">Optional name of our configuration</param>
        public PassThruSimulationConfiguration(ProtocolId ProtocolInUse, BaudRate BaudRate, string ConfigurationName = null)
        {
            // Setup a new configuration logger if possible
            _configurationLogger ??= new SharpLogger(LoggerActions.UniversalLogger);

            // Store protocol and BaudRate
            this.ReaderBaudRate = BaudRate;
            this.ReaderProtocol = ProtocolInUse;

            // Configure the name of the simulation configuration
            this.ConfigurationName = !string.IsNullOrWhiteSpace(ConfigurationName) 
                ? ConfigurationName : $"{this.ReaderProtocol}_{this.ReaderProtocol}";

            // Store basic values here
            this.ReaderMsgCount = 1;
            this.ReaderTimeout = 100;
            this.ResponseTimeout = 500;
            this.ReaderChannelFlags = 0x00;

            // Setup basic empty array for filters with a max count of 10
            this.ReaderFilters = new List<J2534Filter>();
            this.ReaderConfigs = new PassThruStructs.SConfigList(0);
        }

        // ------------------------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Gets an auto ID routine for the given protocol value.
        /// </summary>
        /// <param name="ConfigurationName">Name of the configuration being returned</param>
        /// <returns>Routine matching the given protocol or null</returns>
        public static PassThruSimulationConfiguration LoadSimulationConfig(string ConfigurationName)
        {
            // Find our routine.
            var RoutineLocated = SupportedConfigurations.FirstOrDefault(RoutineObj => RoutineObj.ConfigurationName == ConfigurationName);
            _configurationLogger.WriteLog(
                RoutineLocated == null ? "NO CONFIG WAS FOUND! RETURNING NULL!" : $"RETURNING CONFIG \"{ConfigurationName}\" NOW...",
                RoutineLocated == null ? LogType.ErrorLog : LogType.InfoLog
            );

            // Return the located routine here
            return RoutineLocated;
        }
        /// <summary>
        /// Gets an auto ID routine for the given protocol value.
        /// </summary>
        /// <param name="ProtocolToUse">Protocol To use for the AutoID</param>
        /// <returns>Routine matching the given protocol or null</returns>
        public static PassThruSimulationConfiguration LoadSimulationConfig(ProtocolId ProtocolToUse)
        {
            // Find our routine.
            var RoutineLocated = SupportedConfigurations.FirstOrDefault(RoutineObj => RoutineObj.ReaderProtocol == ProtocolToUse);
            _configurationLogger.WriteLog(
                RoutineLocated == null ? "NO CONFIG WAS FOUND! RETURNING NULL!" : $"RETURNING CONFIG FOR PROTOCOL {ProtocolToUse} NOW...",
                RoutineLocated == null ? LogType.ErrorLog : LogType.InfoLog
            );

            // Return the located routine here
            return RoutineLocated;
        }

        // ------------------------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Pulls a new resource from a given file name
        /// </summary>
        /// <param name="ResourceFileName">Name of the file</param>
        /// <param name="ObjectName">Object name</param>
        /// <returns>A JObject which holds the content of the resource file</returns>
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
            // Load in the protocol ID values from our JSON configuration and store them
            var LoadedProtocols = JArray.FromObject(_allocateResource("DefaultSimConfigurations.json", "SupportedProtocols"))
                .Select(ValueObject => ValueObject.ToObject<ProtocolId>())
                .ToArray();

            // Return the loaded protocols
            return LoadedProtocols;
        }
        /// <summary>
        /// Loads and stores the default simulation configurations for our playback sessions
        /// </summary>
        /// <returns>The default simulation configurations we support</returns>
        private static PassThruSimulationConfiguration[] _loadSupportedConfigurations()
        {
            // Load in the simulation configuration values from our JSON configuration and store them
            var LoadedConfigurations = JArray.FromObject(_allocateResource("DefaultSimConfigurations.json", "SimulationConfigurations"))
                .Select(ValueObject => JsonConvert.DeserializeObject<PassThruSimulationConfiguration>(ValueObject.ToString()))
                .ToArray();

            // Return the loaded configurations
            return LoadedConfigurations;
        }
    }
}
