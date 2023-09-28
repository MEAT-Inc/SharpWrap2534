using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SharpLogging;
using SharpWrapper;
using SharpWrapper.J2534Objects;
using SharpWrapper.PassThruTypes;

namespace SharpSimulator
{
    /// <summary>
    /// Plays back the contents of a SimulationLoader
    /// </summary>
    public class PassThruSimulationPlayer
    {
        #region Custom Events

        // Events to fire when a simulation channel is changed or a message is processed
        public event EventHandler<SimChannelEventArgs> SimChannelChanged;       // Channel change event
        public event EventHandler<SimMessageEventArgs> SimMessageProcessed;     // Message processed event

        /// <summary>
        /// Processes an action for a new channel being changed around
        /// </summary>
        /// <param name="ChannelArgs"></param>
        protected virtual void SimChannelModified(SimChannelEventArgs ChannelArgs)
        {
            // Find the event handler and invoke it
            EventHandler<SimChannelEventArgs> EventHandlerCalled = this.SimChannelChanged;
            EventHandlerCalled?.Invoke(this, ChannelArgs);
        }
        /// <summary>
        /// Processes an action for a new message being read and replied to
        /// </summary>
        /// <param name="MessageArgs"></param>
        protected virtual void SimMessageReceived(SimMessageEventArgs MessageArgs)
        {
            // Find the event handler and invoke it
            EventHandler<SimMessageEventArgs> EventHandlerCalled = this.SimMessageProcessed;
            EventHandlerCalled?.Invoke(this, MessageArgs);
        }

        #endregion // Custom Events

        #region Fields

        // Basic information about this simulation player
        private readonly Guid _playerGuid;             
        private readonly SharpLogger _simPlayingLogger;
        public readonly Sharp2534Session SimulationSession;     
        private List<PassThruSimulationChannel> _simulationChannels;

        // TokenSource and token used to cancel a simulation while running in an Async thread
        private CancellationToken _readerCancelToken;
        private CancellationTokenSource _readerTokenSource;

        #endregion // Fields

        #region Properties

        // Collection of channels for our simulations
        public PassThruSimulationChannel[] SimulationChannels => this._simulationChannels.ToArray();

        // Values for our reader configuration.
        public uint ReaderTimeout { get; private set; }
        public uint ReaderMessageCount { get; private set; }
        public uint SenderResponseTimeout { get; private set; }

        // Other Reader Configuration Values and States
        public bool SimulationReading { get; private set; }
        public bool ResponsesEnabled { get; private set; }

        // Simulation channel (physical on device) configuration for playback
        public J2534Channel PhysicalChannel { get; private set; }
        public J2534Filter[] DefaultMessageFilters { get; private set; }
        public PassThruStructs.SConfigList DefaultConfigParamConfig { get; private set; }
        public Tuple<ProtocolId, PassThroughConnect, BaudRate> DefaultConnectionConfig { get; private set; }

        // Properties of all channels for the simulation that have been built out from this generator
        public BaudRate[] BaudRates => this.SimulationChannels.Select(SimChannel => SimChannel.ChannelBaudRate).ToArray();
        public PassThroughConnect[] ChannelFlags => this.SimulationChannels.Select(SimChannel => SimChannel.ChannelConnectFlags).ToArray();
        public ProtocolId[] ChannelProtocols => this.SimulationChannels.Select(SimChannel => SimChannel.ChannelProtocol).ToArray();
        public J2534Filter[][] ChannelFilters => this.SimulationChannels.Select(SimChannel => SimChannel.MessageFilters).ToArray();

        // Message pairing collections holding information about all messages read or written for a simulation
        public PassThruSimulationChannel.SimulationMessagePair[][] PairedSimulationMessages => this.SimulationChannels
            .Select(SimChannel => SimChannel.MessagePairs)
            .ToArray();
        public PassThruStructs.PassThruMsg[] MessagesToRead => (PassThruStructs.PassThruMsg[])PairedSimulationMessages
            .SelectMany(MsgSet => MsgSet.Select(MsgPair => MsgPair.MessageRead)
                .ToArray());
        public PassThruStructs.PassThruMsg[][] MessagesToWrite => (PassThruStructs.PassThruMsg[][])PairedSimulationMessages
            .SelectMany(MsgSet => MsgSet.Select(MsgPair => MsgPair.MessageResponses)
                .ToArray());

        #endregion // Properties

        #region Structs and Classes

        /// <summary>
        /// Fired off when a new session creates a simulation channel object
        /// </summary>
        public class SimChannelEventArgs : EventArgs
        {
            // Event objects for this event
            public DateTime TimeProcessed { get; private set; }         // Time this event is processed
            public Sharp2534Session Session { get; private set; }      // Controlling Session
            public J2534Device SessionDevice { get; private set; }      // Device controlled by the session
            public J2534Channel SessionChannel { get; private set; }    // Channel being controlled for this simulation

            // ------------------------------------------------------------------------------------------------------------------------------------------

            /// <summary>
            /// Pulls in the current session instance and stores values for this event onto our class
            /// </summary>
            /// <param name="InputSession"></param>
            public SimChannelEventArgs(Sharp2534Session InputSession)
            {
                // Store session objects here
                this.Session = InputSession;
                this.TimeProcessed = DateTime.Now;
                this.SessionDevice = this.Session.JDeviceInstance;
                this.SessionChannel = this.SessionDevice.DeviceChannels.First(ChObj => ChObj.ChannelId != 0);
            }
        }
        /// <summary>
        /// Event args for a new simulation message being processed and responded to
        /// </summary>
        public class SimMessageEventArgs : EventArgs
        {
            // Event objects for this event
            public readonly Sharp2534Session Session;      // Controlling Session
            public readonly J2534Device SessionDevice;      // Device controlled by the session
            public readonly J2534Channel SessionChannel;    // Channel being controlled for this simulation

            // Messages processed by our sim event
            public readonly bool ResponsePassed;
            public readonly PassThruStructs.PassThruMsg ReadMessage;
            public readonly PassThruStructs.PassThruMsg[] Responses;

            // Bindable string properties for these events 
            public DateTime TimeProcessed { get; private set; }      
            public string ReadMessageString => ReadMessage.DataToHexString();
            public string[] ResponseStrings => Responses.Select(ResponseObj => ResponseObj.DataToHexString()).ToArray();

            // ------------------------------------------------------------------------------------------------------------------------------------------

            /// <summary>
            /// Builds a new event argument helper for this session
            /// </summary>
            /// <param name="InputSession">Session to process</param>
            /// <param name="MessageRead">Message read</param>
            /// <param name="MessagesReplied">Responses sent out</param>
            public SimMessageEventArgs(Sharp2534Session InputSession, bool ResponseSent, PassThruStructs.PassThruMsg MessageRead, PassThruStructs.PassThruMsg[] MessagesReplied)
            {
                // Store session objects here
                this.Session = InputSession;
                this.TimeProcessed = DateTime.Now;
                this.SessionDevice = this.Session.JDeviceInstance;
                this.SessionChannel = this.SessionDevice.DeviceChannels.First(ChObj => ChObj.ChannelId != 0);

                // Store Messages here
                this.ResponsePassed = ResponseSent;
                this.ReadMessage = MessageRead;
                this.Responses = MessagesReplied;
            }
        }

        #endregion // Structs and Classes

        // ------------------------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Uses an existing PT Instance that will read commands over and over waiting for content.
        /// </summary>
        /// <param name="InputSession">Session object to use for our simulations</param>
        public PassThruSimulationPlayer(Sharp2534Session InputSession)
        {
            // Store class values and build a simulation loader.
            this.ResponsesEnabled = true;
            this.SimulationSession = InputSession;
            this._simulationChannels = new List<PassThruSimulationChannel>();

            // Log Built new Session
            this._playerGuid = Guid.NewGuid();
            string LoggerName = $"SimPlaybackLogger_{this._playerGuid.ToString().ToUpper().Substring(0, 5)}";
            this._simPlayingLogger = new SharpLogger(LoggerActions.UniversalLogger, LoggerName);
            this._simPlayingLogger.WriteLog("BUILT NEW SIMULATION PLAYBACK LOGGER OK!", LogType.InfoLog);
            this._simPlayingLogger.WriteLog("READY TO PLAY BACK THE LOADED CONTENTS OF THE PROVIDED SIMULATION LOADER!");

            // Open up a PT Device, read the voltage value, and begin reading messages over and over.
            this._simPlayingLogger.WriteLog($"STARTING A NEW SIM READER FOR DEVICE {this.SimulationSession.DeviceName}", LogType.WarnLog);
            this.SimulationSession.PTOpen(); this.SimulationSession.PTReadVoltage(out var VoltsRead);
            this._simPlayingLogger.WriteLog($"PULLED IN A NEW VOLTAGE VALUE OF {VoltsRead}!", LogType.InfoLog);
            if (VoltsRead < 12.0) this._simPlayingLogger.WriteLog("WARNING! INPUT VOLTAGE IS LESS THAN 12.0 VOLTS!", LogType.ErrorLog);
        }
        /// <summary>
        /// Uses an existing PT Instance that will read commands over and over waiting for content.
        /// </summary>
        /// <param name="SimChannels">Channels to simulate</param>
        /// <param name="InputSession">Session object to use for our simulations</param>
        public PassThruSimulationPlayer(IEnumerable<PassThruSimulationChannel> SimChannels, Sharp2534Session InputSession)
        {
            // Store class values and build a simulation loader.
            this.ResponsesEnabled = true;
            this.SimulationSession = InputSession;
            this._simulationChannels = SimChannels.ToList();

            // Log Built new Session
            this._playerGuid = Guid.NewGuid();
            string LoggerName = $"SimPlaybackLogger_{this._playerGuid.ToString().ToUpper().Substring(0, 5)}";
            this._simPlayingLogger = new SharpLogger(LoggerActions.UniversalLogger, LoggerName);
            this._simPlayingLogger.WriteLog("BUILT NEW SIMULATION PLAYBACK LOGGER OK!", LogType.InfoLog);
            this._simPlayingLogger.WriteLog("READY TO PLAY BACK THE LOADED CONTENTS OF THE PROVIDED SIMULATION LOADER!");

            // Open up a PT Device, read the voltage value, and begin reading messages over and over.
            this._simPlayingLogger.WriteLog($"STARTING A NEW SIM READER FOR DEVICE {this.SimulationSession.DeviceName}", LogType.WarnLog);
            this.SimulationSession.PTOpen(); this.SimulationSession.PTReadVoltage(out var VoltsRead);
            this._simPlayingLogger.WriteLog($"PULLED IN A NEW VOLTAGE VALUE OF {VoltsRead}!", LogType.InfoLog);
            if (VoltsRead < 12.0) this._simPlayingLogger.WriteLog("WARNING! INPUT VOLTAGE IS LESS THAN 12.0 VOLTS!", LogType.ErrorLog);
        }
        /// <summary>
        /// Spawns a new Simulation playback helper for the provided simulation channels
        /// </summary>
        /// <param name="Version">J2534 Version for the simulation</param>
        /// <param name="PassThruDLL">The name of the DLL we wish to use</param>
        /// <param name="PassThruDevice">The name of the device we wish to use</param>
        public PassThruSimulationPlayer(JVersion Version = JVersion.V0404, string PassThruDLL = null, string PassThruDevice = null)
        {
            // Store class values and build a simulation loader.
            this.ResponsesEnabled = true;
            PassThruDLL ??= "NO_DLL"; PassThruDevice ??= "NO_DEVICE";
            this.SimulationSession = Sharp2534Session.OpenSession(
                Version,
                PassThruDLL == "NO_DLL" ? "" : PassThruDLL,
                PassThruDevice == "NO_DEVICE" ? "" : PassThruDevice
            );

            // Log Built new Session
            this._playerGuid = Guid.NewGuid();
            string LoggerName = $"SimPlaybackLogger_{this._playerGuid.ToString().ToUpper().Substring(0, 5)}";
            this._simPlayingLogger = new SharpLogger(LoggerActions.UniversalLogger, LoggerName);
            this._simPlayingLogger.WriteLog("BUILT NEW SIMULATION PLAYBACK LOGGER OK!", LogType.InfoLog);
            this._simPlayingLogger.WriteLog("READY TO PLAY BACK THE LOADED CONTENTS OF THE PROVIDED SIMULATION LOADER!");

            // Open up a PT Device, read the voltage value, and begin reading messages over and over.
            this._simPlayingLogger.WriteLog($"STARTING A NEW SIM READER FOR DEVICE {this.SimulationSession.DeviceName}", LogType.WarnLog);
            this.SimulationSession.PTOpen(); this.SimulationSession.PTReadVoltage(out var VoltsRead);
            this._simPlayingLogger.WriteLog($"PULLED IN A NEW VOLTAGE VALUE OF {VoltsRead}!", LogType.InfoLog);
            if (VoltsRead < 12.0) this._simPlayingLogger.WriteLog("WARNING! INPUT VOLTAGE IS LESS THAN 12.0 VOLTS!", LogType.ErrorLog);
        }
        /// <summary>
        /// Spawns a new Simulation playback helper for the provided simulation channels
        /// </summary>
        /// <param name="SimChannels">Channels to simulate</param>
        /// <param name="Version">J2534 Version for the simulation</param>
        /// <param name="PassThruDLL">The name of the DLL we wish to use</param>
        /// <param name="PassThruDevice">The name of the device we wish to use</param>
        public PassThruSimulationPlayer(IEnumerable<PassThruSimulationChannel> SimChannels, JVersion Version = JVersion.V0404, string PassThruDLL = null, string PassThruDevice = null)
        {
            // Store class values and build a simulation loader.
            this.ResponsesEnabled = true;
            this._simulationChannels = SimChannels.ToList();
            PassThruDLL ??= "NO_DLL"; PassThruDevice ??= "NO_DEVICE";
            this.SimulationSession = Sharp2534Session.OpenSession(
                Version,
                PassThruDLL == "NO_DLL" ? "" : PassThruDLL,
                PassThruDevice == "NO_DEVICE" ? "" : PassThruDevice
            );

            // Log Built new Session
            this._playerGuid = Guid.NewGuid();
            string LoggerName = $"SimPlaybackLogger_{this._playerGuid.ToString().ToUpper().Substring(0, 5)}";
            this._simPlayingLogger = new SharpLogger(LoggerActions.UniversalLogger, LoggerName);
            this._simPlayingLogger.WriteLog("BUILT NEW SIMULATION PLAYBACK LOGGER OK!", LogType.InfoLog);
            this._simPlayingLogger.WriteLog("READY TO PLAY BACK THE LOADED CONTENTS OF THE PROVIDED SIMULATION LOADER!");

            // Open up a PT Device, read the voltage value, and begin reading messages over and over.
            this._simPlayingLogger.WriteLog($"STARTING A NEW SIM READER FOR DEVICE {this.SimulationSession.DeviceName}", LogType.WarnLog);
            this.SimulationSession.PTOpen(); this.SimulationSession.PTReadVoltage(out var VoltsRead);
            this._simPlayingLogger.WriteLog($"PULLED IN A NEW VOLTAGE VALUE OF {VoltsRead}!", LogType.InfoLog);
            if (VoltsRead < 12.0) this._simPlayingLogger.WriteLog("WARNING! INPUT VOLTAGE IS LESS THAN 12.0 VOLTS!", LogType.ErrorLog);
        }

        // ------------------------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Loads a simulation file into the playback helper by parsing the JSON contents of the provided file
        /// </summary>
        /// <param name="SimulationFile">Full path to the simulation file we're looking to play back</param>
        /// <returns>True if the simulation is loaded. False if it is not</returns>
        public bool LoadSimulationFile(string SimulationFile)
        {
            // Load the file content in and store it as a JArray
            string SimFileContent = File.ReadAllText(SimulationFile);
            this._simPlayingLogger.WriteLog($"LOADING AND PARSING SIMULATION FILE {SimulationFile} NOW...", LogType.WarnLog);

            // Parse all the channels loaded in from the file and return the result
            var PulledChannels = JArray.Parse(SimFileContent);
            return this.LoadSimulationFile(PulledChannels);
        }
        /// <summary>
        /// Loads a simulation into the playback helper by parsing a JSON Array of channels
        /// </summary>
        /// <param name="SimulationChannels">JArray holding the channels of the simulation</param>
        /// <returns>True if the simulation is loaded. False if it is not</returns>
        public bool LoadSimulationFile(JArray SimulationChannels)
        {
            // Load the file and parse all the JSON contents from it to build our channels
            int FailedCounter = 0;
            this._simPlayingLogger.WriteLog($"LOADING AND PARSING SIMULATION FILE CONTENTS NOW...", LogType.WarnLog);

            // Iterate all the channels loaded in the JSON file and parse them
            this._simulationChannels = new List<PassThruSimulationChannel>();
            foreach (var ChannelInstance in SimulationChannels.Children())
            {
                try
                {
                    // Try and build our channel here
                    JToken ChannelToken = ChannelInstance.Last;
                    if (ChannelToken == null)
                        throw new InvalidDataException("Error! Input channel was seen to be an invalid layout!");

                    // Now using the JSON Converter, unwrap the channel into a simulation object and store it on our player
                    PassThruSimulationChannel BuiltChannel = ChannelToken.First.ToObject<PassThruSimulationChannel>();
                    this._simulationChannels.Add(BuiltChannel);
                }
                catch (Exception ConvertEx)
                {
                    // Log failures out here
                    FailedCounter++;
                    this._simPlayingLogger.WriteLog("FAILED TO CONVERT SIMULATION CHANNEL FROM JSON TO OBJECT!", LogType.ErrorLog);
                    this._simPlayingLogger.WriteLog("EXCEPTION AND CHANNEL OBJECT ARE BEING LOGGED BELOW...", LogType.WarnLog);
                    this._simPlayingLogger.WriteLog($"SIM CHANNEL JSON:\n{ChannelInstance.ToString(Formatting.Indented)}", LogType.TraceLog);
                    this._simPlayingLogger.WriteException("EXCEPTION THROWN:", ConvertEx);
                }
            }

            // Log out that we're loaded up and return out true once done
            this._simPlayingLogger.WriteLog($"IMPORTED SIMULATION FILE {SimulationChannels} CORRECTLY!", LogType.InfoLog);
            this._simPlayingLogger.WriteLog($"PULLED IN A TOTAL OF {this._simulationChannels.Count} INPUT SIMULATION CHANNELS INTO OUR LOADER WITHOUT FAILURE!", LogType.InfoLog);
            this._simPlayingLogger.WriteLog($"ENCOUNTERED A TOTAL OF {FailedCounter} FAILURES WHILE LOADING CHANNELS!", LogType.InfoLog);
            return true;
        }
        /// <summary>
        /// Appends a new simulation channel into our loader using an input channel object
        /// </summary>
        /// <param name="ChannelToAdd">Channel to store on our loader</param>
        /// <returns>The index of the channel added</returns>
        public bool AddSimulationChannel(PassThruSimulationChannel ChannelToAdd)
        {
            // Store all values of our channel here
            this._simulationChannels = this.SimulationChannels
                .Append(ChannelToAdd)
                .ToList();

            // Find new index and return it. Check the min index of the filters and the channels then the messages.
            this._simPlayingLogger.WriteLog($"ADDED NEW VALUES FOR A SIMULATION CHANNEL {ChannelToAdd.ChannelId} WITHOUT ISSUES!", LogType.InfoLog);
            return this._simulationChannels.Contains(ChannelToAdd);
        }
        /// <summary>
        /// Removes a channel by the ID value passed in
        /// </summary>
        /// <param name="ChannelId">ID of the channel to remove</param>
        /// <returns>True if removed. False if not.</returns>
        public bool RemoveSimulationChannel(int ChannelId)
        {
            // Find the channel to remove and pull it out.
            this._simPlayingLogger.WriteLog($"TRYING TO REMOVE CHANNEL WITH ID {ChannelId}...");
            this._simulationChannels = this.SimulationChannels
                .Where(SimChannel => SimChannel.ChannelId != ChannelId)
                .ToList();

            // Check if it exists or not.
            this._simPlayingLogger.WriteLog($"{(this.SimulationChannels.Any(SimChannel => SimChannel.ChannelId == ChannelId) ? "FAILED TO REMOVE CHANNEL OBJECT!" : "CHANNEL REMOVED OK!")}");
            return this.SimulationChannels.All(SimChannel => SimChannel.ChannelId != ChannelId);
        }
        /// <summary>
        /// Removes a simulation channel from the list of all channel objects
        /// </summary>
        /// <param name="ChannelToRemove">Channel to pull out of our list of input channels</param>
        /// <returns>True if removed. False if not</returns>
        public bool RemoveSimulationChannel(PassThruSimulationChannel ChannelToRemove)
        {
            // Find the channel to remove and pull it out.
            this._simPlayingLogger.WriteLog($"TRYING TO REMOVE CHANNEL WITH ID {ChannelToRemove.ChannelId}...");
            this._simulationChannels = this.SimulationChannels
                .Where(SimChannel => SimChannel.ChannelId != ChannelToRemove.ChannelId)
                .ToList();

            // Check if it exists or not.
            this._simPlayingLogger.WriteLog($"{(this.SimulationChannels.Contains(ChannelToRemove) ? "FAILED TO REMOVE CHANNEL OBJECT!" : "CHANNEL REMOVED OK!")}");
            return !this.SimulationChannels.Contains(ChannelToRemove);
        }

        // ------------------------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Applies an entire simulation configuration onto the simulation player 
        /// </summary>
        /// <param name="SimConfiguration">The configuration we're looking to set on the playback helper</param>
        public void SetPlaybackConfiguration(PassThruSimulationConfiguration SimConfiguration)
        {
            // Log out the configuration we're setting and apply the values 
            this._simPlayingLogger. WriteLog($"APPLYING CONFIGURATION {SimConfiguration.ConfigurationName} TO SIMULATION PLAYER NOW...");
            this.SetDefaultConfigurations(SimConfiguration.ReaderConfigs);
            this.SetDefaultMessageFilters(SimConfiguration.ReaderFilters);
            this.SetDefaultConnectionType(
                SimConfiguration.ReaderProtocol,
                SimConfiguration.ReaderChannelFlags,
                SimConfiguration.ReaderBaudRate);
            this.SetDefaultMessageValues(
                SimConfiguration.ReaderTimeout,
                SimConfiguration.ReaderMsgCount,
                SimConfiguration.ResponseTimeout);

            // Log out that our configuration has been set and exit out 
            this._simPlayingLogger.WriteLog($"APPLIED ALL CONFIGURATION VALUES FOR CONFIGURATION {SimConfiguration.ConfigurationName} CORRECTLY!", LogType.InfoLog);
        }
        /// <summary>
        /// Toggles if we allow responses to our messages or not.
        /// </summary>
        /// <param name="ResponsesEnabled">Value to set for responses being on or off</param>
        public void SetResponsesEnabled(bool ResponsesEnabled)
        {
            // Store the new value here and log it
            this.ResponsesEnabled = ResponsesEnabled;
            this._simPlayingLogger.WriteLog($"RESPONSES ARE NOW {(ResponsesEnabled ? "ENABLED!" : "DISABLED!")}", LogType.InfoLog);
        }
        /// <summary>
        /// Stores new values for our reader configuration on our output
        /// </summary>
        /// <param name="TimeoutValue">Timeout on each read command</param>
        /// <param name="MessageCount">Messages to read</param>
        public void SetDefaultMessageValues(uint ReadTimeoutValue = 100, uint MessageCount = 10, uint SenderTimeoutValue = 500)
        {
            // Store new values here and log them out
            this.ReaderTimeout = ReadTimeoutValue;
            this.ReaderMessageCount = MessageCount;
            this.SenderResponseTimeout = SenderTimeoutValue;

            // Log our stored values out as trace log.
            this._simPlayingLogger.WriteLog($"STORED NEW READER CONFIGURATION! VALUES SET:\n" +
                $"{this.ReaderMessageCount} MESSAGES TO READ\n" +
                $"{this.ReaderTimeout} TIMEOUT ON EACH READ COMMAND\n" +
                $"{this.SenderResponseTimeout} TIMEOUT ON EACH RESPONSE COMMAND",
                LogType.TraceLog
            );
        }
        /// <summary>
        /// Stores default channel configurations for each new channel built
        /// </summary>
        /// <param name="Protocol">Protocol to store</param>
        /// <param name="ConnectionFlags"></param>
        /// <param name="ChannelBaudrate"></param>
        /// <returns></returns>
        public bool SetDefaultConnectionType(ProtocolId Protocol, PassThroughConnect ConnectionFlags, BaudRate ChannelBaudRate)
        {
            // Store our configuration values here
            this.DefaultConnectionConfig = new Tuple<ProtocolId, PassThroughConnect, BaudRate>(Protocol, ConnectionFlags, ChannelBaudRate);
            this._simPlayingLogger.WriteLog("STORED NEW CONFIGURATION FOR CHANNEL SETUP OK!", LogType.InfoLog);
            this._simPlayingLogger.WriteLog("CHANGES WILL NOT TAKE PLACE UNTIL THE NEXT TIME A CHANNEL IS CLOSED AND REOPENED!", LogType.InfoLog);
            return true;
        }
        /// <summary>
        /// Applies a set of given channel configuration values to our setup simulation channel object
        /// </summary>
        /// <param name="DefaultConfiguration">Tuple array of config IDs and values to setup</param>
        /// <returns>True if setup, false if not.</returns>
        public bool SetDefaultConfigurations(PassThruStructs.SConfigList DefaultConfiguration)
        {
            // Ensure our channel object is not null at this point.
            this.DefaultConfigParamConfig = DefaultConfiguration;
            if (this.PhysicalChannel == null) {
                this._simPlayingLogger.WriteLog("NOT STORING DEFAULT CONFIGURATIONS SINCE THE SIMULATION CHANNEL OBJECT IS CURRENTLY NULL OR IT IS CURRENTLY READING!", LogType.InfoLog);
                return true;
            }

            // If the channel is not null, then build our output configurations
            this._simPlayingLogger.WriteLog("SETTING SCONFIG LIST WITH OUR CONFIGURATION VALUES NOW...");
            foreach (var TupleObject in DefaultConfiguration.ConfigList)
            {
                // Issue out an IOCTL for each configuration
                this._simPlayingLogger.WriteLog($"SETTING CONFIGURATION ID PAIR: {TupleObject.SConfigParamId} -- {TupleObject.SConfigValue}");
                try { this.PhysicalChannel.SetConfig(TupleObject.SConfigParamId, TupleObject.SConfigValue); }
                catch (Exception SetConfigEx)
                {
                    // Log failure, return false
                    this._simPlayingLogger.WriteLog($"FAILED TO SET NEW CONFIGURATION VALUE FOR ID {TupleObject.SConfigParamId}!", LogType.ErrorLog);
                    this._simPlayingLogger.WriteException("EXCEPTION IS BEING LOGGED BELOW", SetConfigEx);
                    return false;
                }
            }

            // Return passed and log information
            this._simPlayingLogger.WriteLog($"CONFIGURED ALL REQUESTED {DefaultConfiguration.ConfigList.Count} CONFIG TUPLE PAIRS OK!", LogType.InfoLog);
            return true;
        }
        /// <summary>
        /// Establish a set of default filters to run on our channel for simulation playback
        /// </summary>
        /// <param name="DefaultFilters"></param>
        /// <returns></returns>
        public bool SetDefaultMessageFilters(J2534Filter[] DefaultFilters)
        {
            // Ensure our channel object is not null at this point.
            this.DefaultMessageFilters = DefaultFilters;
            if (this.PhysicalChannel == null) {
                this._simPlayingLogger.WriteLog("NOT SETTING DEFAULT FILTERS SINCE THE SIMULATION CHANNEL OBJECT IS CURRENTLY NULL OR IT IS CURRENTLY READING!", LogType.InfoLog);
                return true;
            }

            // If the channel is not null, then store our message filters
            foreach (var FilterObject in DefaultFilters)
            {
                // Log out information about the filter 
                this._simPlayingLogger.WriteLog(
                    $"SETTING FILTER INSTANCE FOR FILTER: " +
                    $"{FilterObject.FilterMask} -- {FilterObject.FilterPattern} " +
                    $"{(!string.IsNullOrWhiteSpace(FilterObject.FilterFlowCtl) ? $" -- {FilterObject.FilterFlowCtl}" : string.Empty)}");

                // Setup filter using the PTStartFilter method
                try { this.PhysicalChannel.StartMessageFilter(FilterObject); }
                catch (Exception SetFilterEx)
                {
                    // Log failure, return false
                    this._simPlayingLogger.WriteLog($"FAILED TO SET NEW FILTER INSTANCE DUE TO AN EXCEPTION FROM THE J2534 API!", LogType.ErrorLog);
                    this._simPlayingLogger.WriteException("EXCEPTION IS BEING LOGGED BELOW", SetFilterEx);
                    return false;
                }
            }

            // Return passed and log information
            this._simPlayingLogger.WriteLog($"CONFIGURED ALL REQUESTED {DefaultFilters.Length} FILTER INSTANCES OK!", LogType.InfoLog);
            return true;
        }

        // ------------------------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Builds a new J2534 Channel for us to use for simulation reading
        /// </summary>
        /// <returns>True if channel is built. False if it fails to build</returns>
        public bool InitializeSimReader()
        {
            // Check if channel configuration was setup or not.
            if (this.DefaultConnectionConfig == null) {
                this._simPlayingLogger.WriteLog("WARNING! CONNECTION CONFIGURATION IS NULL! SETTING DEFAULT CHANNEL CONNECTIONS NOW!", LogType.WarnLog);
                this.SetDefaultConnectionType(ProtocolId.ISO15765, PassThroughConnect.NO_CONNECT_FLAGS, BaudRate.ISO15765_500000);
            }

            // Connect a new Channel value
            this.PhysicalChannel = this.SimulationSession.PTConnect(
                0, 
                this.DefaultConnectionConfig.Item1,
                this.DefaultConnectionConfig.Item2,
                this.DefaultConnectionConfig.Item3,
                out uint ChannelIdBuilt
            );
            
            // Log channel built and check to make sure it is not null
            if (this.PhysicalChannel == null) throw new InvalidOperationException("FAILED TO OPEN A NEW CHANNEL FOR OUR SIMULATION ROUTINE! THIS IS FATAL!");
            this._simPlayingLogger.WriteLog("BUILT NEW SIMULATION CHANNEL WITH GIVEN INPUT VALUES OK!", LogType.InfoLog);

            // Check if we need to build default configuration for filters of config params
            if (this.DefaultConfigParamConfig.NumberOfParams != 0) { 
                this._simPlayingLogger.WriteLog("SETTING UP DEFAULT READER CONFIGURATION NOW...");
                if (!this.SetDefaultConfigurations(this.DefaultConfigParamConfig)) return false;
            }
            if (this.DefaultMessageFilters != null) {
                this._simPlayingLogger.WriteLog("SETTING UP DEFAULT READER FILTERS NOW...");
                if (!this.SetDefaultMessageFilters(this.DefaultMessageFilters)) return false;
            }

            // Clear out the TX and RX buffers from the last channel instance to avoid overflows
            this.PhysicalChannel.ClearTxBuffer(); 
            this.PhysicalChannel.ClearRxBuffer();
            this._simPlayingLogger.WriteLog("CLEARED OUT TX AND RX BUFFERS FOR OUR NEW CHANNEL WITHOUT ISSUES!", LogType.InfoLog);

            // Return the built channel here
            this._simPlayingLogger.WriteLog("SETUP NEW SIMULATION READER CHANNEL AND ALL CONFIGURATIONS/FILTERS OK!", LogType.InfoLog);
            return true;
        }
        /// <summary>
        /// Starts a reader channel and begins processing values for our messages read in
        /// </summary>
        public void StartSimulationReader()
        {
            // Ensure the channel we need is built and not active
            if (this.PhysicalChannel == null) throw new InvalidOperationException("CAN NOT BEGIN SIMULATION READING ON A NULL CHANNEL!");
            if (this.SimulationReading) throw new InvalidOperationException("CAN NOT START A NEW SIMULATION READER WHEN THE CHANNEL IS ALREADY RUNNING!");

            // Now start a looping task to get read messages and write the responses to them.
            this.SimulationReading = true;
            this._readerTokenSource = new CancellationTokenSource();
            this._readerCancelToken = this._readerTokenSource.Token;
            Task.Run(() =>
            {
                // Log starting sim reading routines
                this._simPlayingLogger.WriteLog("STARTING SIMULATION READER TASK ROUTINE NOW...", LogType.InfoLog);
                while (!this._readerCancelToken.IsCancellationRequested) 
                { 
                    // Execute Response method and check for output exception
                    try { this._processSimulationMessages(); }
                    catch (Exception ProcessingEx)
                    {
                        // Check the type of exception and determine what to do based on it.
                        if (ProcessingEx.Message == "SETUP_READER_EXCEPTION") this.StopSimulationReader();
                        if (ProcessingEx.Message == "GENERATE_RESPONSE_CHANNEL_EXCEPTION") this.StopSimulationReader();

                        // Generic response failures can move on.
                        if (ProcessingEx.Message == "MESSAGE_RESPONSE_EXCEPTION") continue;
                    }
                }
            }, _readerCancelToken);
        }
        /// <summary>
        /// Stops the reader tasks for our simulations
        /// </summary>
        public void StopSimulationReader()
        {
            // Stop the reader task using the token source above.
            if (this._readerTokenSource == null || this._readerCancelToken.IsCancellationRequested)
                throw new InvalidOperationException("NO NEED TO CANCEL AN ALREADY STOPPED READER TASK!");

            // Stop the task now
            this.SimulationReading = false;
            this._simPlayingLogger.WriteLog("STOPPING READER TASK FOR SIMULATION CHANNEL NOW...", LogType.WarnLog);
            this._readerTokenSource.Cancel();

            // Close the device instance out here.
            this.SimulationSession.PTDisconnect(0);
            this._simPlayingLogger.WriteLog("CLOSED CHANNEL INDEX FOR OUR DESIRED SIMULATION CHANNEL OK!", LogType.InfoLog);
        }
        
        // ------------------------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Reads in a set of messages using the configured message count and timeout value 
        /// </summary>
        /// <returns>True if read and replied to. False if not.</returns>
        private void _processSimulationMessages()
        {
            // Setup ref count and read messages
            uint MessageCountRef = this.ReaderMessageCount;
            PassThruStructs.PassThruMsg[] MessagesRead;
            try { MessagesRead = this.PhysicalChannel.PTReadMessages(ref MessageCountRef, this.ReaderTimeout); }
            catch { MessagesRead = Array.Empty<PassThruStructs.PassThruMsg>(); }

            // Make sure we actually got some data back first.
            if (MessagesRead.Length == 0) return;

            // Purge TX and RX Buffers
            this.PhysicalChannel.ClearTxBuffer(); 
            this.PhysicalChannel.ClearRxBuffer();

            // Now check out our read data values and prepare to operate on them based on the values.
            foreach (var ReadMessage in MessagesRead)
            {
                // TODO: WHAT THE ACTUAL FUCK DID I WRITE HERE??? THIS WORKS BUT I DO NOT UNDERSTAND HOW
                // Now using those messages try and figure out what channel we need to open up.
                // Finds the Index of the channel object and the index of the message object on the channel
                int IndexOfMessageFound = -1; int IndexOfMessageSet = -1;
                foreach (var ChannelMessagePair in this.PairedSimulationMessages)
                {
                    // Check each of the messages found on each channel object
                    foreach (var MessageSet in ChannelMessagePair)
                    {
                        // Build the message data for sent and read
                        string ReadMessageData = ReadMessage.DataToHexString(true);
                        string SentMessageData = MessageSet.MessageRead.DataToHexString(true);

                        // Check if we have this string value or not.
                        if (!ReadMessageData.Contains(SentMessageData)) continue;
                        IndexOfMessageSet = this.PairedSimulationMessages.ToList().IndexOf(ChannelMessagePair);
                        IndexOfMessageFound = ChannelMessagePair.ToList().IndexOf(MessageSet);
                    }
                }
                
                // If no message was found for the given input, move onto the next one
                if (IndexOfMessageFound == -1 || IndexOfMessageSet == -1) continue;

                // If no responses exist for the given input, move onto the next message
                var MessagesToSend = this.PairedSimulationMessages[IndexOfMessageSet][IndexOfMessageFound];
                if (MessagesToSend.MessageResponses.Length == 0) continue;

                // Mark a new channel is needed and build new one for configuration of messages
                try
                {
                    // Setup a response channel object
                    if (!this._generateResponseChannel(IndexOfMessageSet, IndexOfMessageFound))
                        throw new Exception(
                            "GENERATE_RESPONSE_CHANNEL_EXCEPTION",
                            new InvalidOperationException("FAILED TO BUILD RESPONSE CHANNEL OBJECT!")
                        );

                    // Now try and reply to a given message value here
                    if (!this._generateSimulationResponses(IndexOfMessageSet, IndexOfMessageFound))
                        throw new Exception(
                            "MESSAGE_RESPONSE_EXCEPTION",
                            new InvalidOperationException("FAILED TO RESPOND TO A GIVEN INPUT MESSAGE!")
                        );
                    
                    // Return passed and setup a base channel object again
                    if (!this.InitializeSimReader())
                        throw new Exception(
                            "SETUP_READER_EXCEPTION",
                            new InvalidOperationException("FAILED TO RECONFIGURE READER CHANNEL!")
                        );

                    // Return passed and move onto next configuration
                    return;
                }
                catch (Exception RespEx)
                {
                    // Log failures, move on to next attempt
                    this._simPlayingLogger.WriteLog("FAILED TO EXECUTE ONE OR MORE SIM ACTIONS! LOGGING EXCEPTION BELOW");
                    this._simPlayingLogger.WriteLog($"EXCEPTION THROWN: {RespEx}");

                    // Return passed and setup a base channel object again
                    if (!this.InitializeSimReader())
                        throw new Exception(
                            "SETUP_READER_EXCEPTION",
                            new InvalidOperationException("FAILED TO RECONFIGURE READER CHANNEL!")
                        );

                    // Return passed and move onto next configuration
                    return;
                }
            }
        }
        /// <summary>
        /// Configures a new Simulation channel for a given input index value
        /// </summary>
        /// <param name="IndexOfMessageSet">Channel index to apply from</param>
        /// <param name="IndexOfMessageFound">Index of messages to respond from</param>
        /// <returns></returns>
        private bool _generateResponseChannel(int IndexOfMessageSet, int IndexOfMessageFound)
        {
            // Check the index value
            if (IndexOfMessageSet < 0 || IndexOfMessageSet >= this.PairedSimulationMessages.Length)
                throw new InvalidOperationException($"CAN NOT APPLY CHANNEL OF INDEX {IndexOfMessageSet} SINCE IT IS OUT OF RANGE!");

            // Store channel messages and filters
            this.SimulationSession.PTDisconnect(0);
            var ChannelFlags = this.ChannelFlags[IndexOfMessageSet];
            var ChannelBaudRate = this.BaudRates[IndexOfMessageSet];
            var ProtocolValue = this.ChannelProtocols[IndexOfMessageSet];
            var FiltersToApply = this.ChannelFilters[IndexOfMessageSet];

            // Once we know there's messages for us to send back, we do so now
            var PulledMessages = this.PairedSimulationMessages[IndexOfMessageSet][IndexOfMessageFound];
            var FilterFlowCtl = PulledMessages.MessageResponses.FirstOrDefault(MsgObj => MsgObj.RxStatus == 0).DataToHexString();
            bool CanMatchFilter = FiltersToApply.Any(FilterObj => string.IsNullOrWhiteSpace(FilterFlowCtl) || FilterFlowCtl.Contains(FilterObj.FilterFlowCtl));

            // Close the current channel, build a new one using the given protocol and then setup our filters.
            this.PhysicalChannel = this.SimulationSession.PTConnect(0, ProtocolValue, ChannelFlags, ChannelBaudRate, out uint ChannelIdBuilt);
            foreach (var ChannelFilter in FiltersToApply)
            {
                // If we're able to do filter matching, do it here
                if (CanMatchFilter && !FilterFlowCtl.Contains(ChannelFilter.FilterFlowCtl)) continue;

                // Try and set each filter for the channel. Skip duplicate filters
                try
                {
                    this.PhysicalChannel.StartMessageFilter(ChannelFilter);
                    this._simPlayingLogger.WriteLog($"Started Filter: {ChannelFilter.FilterMask} | {ChannelFilter.FilterPattern} | {ChannelFilter.FilterFlowCtl}", LogType.ErrorLog);
                }
                catch
                {
                    // Log out what our duplicate/invalid filter was
                    this._simPlayingLogger.WriteLog($"Error! Filter was unable to be set for requested simulation channel!", LogType.ErrorLog);
                    this._simPlayingLogger.WriteLog($"Filter: {ChannelFilter.FilterMask} | {ChannelFilter.FilterPattern} | {ChannelFilter.FilterFlowCtl}", LogType.ErrorLog);
                }
            }

            // Build output message events here
            this.SimChannelModified(new SimChannelEventArgs(this.SimulationSession));
            return true;
        }
        /// <summary>
        /// Responds to a given input message value
        /// </summary>
        /// <param name="IndexOfMessageSet">Index of the message set/channel object we're using</param>
        /// <param name="IndexOfMessageFound">Index of messages to respond from</param>
        private bool _generateSimulationResponses(int IndexOfMessageSet, int IndexOfMessageFound)
        {
            // Pull out the message set, then find the response messages and send them out
            this._simPlayingLogger.WriteLog(string.Join("", Enumerable.Repeat("=", 100)));
            var PulledMessages = this.PairedSimulationMessages[IndexOfMessageSet][IndexOfMessageFound];

            // Log message contents out and then log the responses out if we are going to be sending them
            this._simPlayingLogger.WriteLog($"--> READ MESSAGE [0]: {BitConverter.ToString(PulledMessages.MessageRead.Data)}", LogType.InfoLog);
            if (!ResponsesEnabled)
            {
                // Fake a reply output event and disconnect our channel
                this.SimulationSession.PTDisconnect(0);
                this.SimMessageReceived(new SimMessageEventArgs(this.SimulationSession, false, PulledMessages.MessageRead, PulledMessages.MessageResponses));
                return true;
            }

            try
            {
                // Try and send the message, indicate passed sending routine
                this.PhysicalChannel.PTWriteMessages(PulledMessages.MessageResponses, this.SenderResponseTimeout);
                this.SimulationSession.PTDisconnect(0);

                // Attempt to send output events in a task to stop hanging our response operations
                this.SimMessageReceived(new SimMessageEventArgs(this.SimulationSession, true, PulledMessages.MessageRead, PulledMessages.MessageResponses));
                for (int RespIndex = 0; RespIndex < PulledMessages.MessageResponses.Length; RespIndex += 1)
                    this._simPlayingLogger.WriteLog($"   --> SENT MESSAGE [{RespIndex}]: {BitConverter.ToString(PulledMessages.MessageResponses[RespIndex].Data)}");

                // Return passed sending output
                return true;
            }
            catch (Exception SendResponseException)
            {
                // Disconnect our channel and exit this routine
                this.SimulationSession.PTDisconnect(0);
                
                // Log failed to send output, set sending failed.
                this._simPlayingLogger.WriteLog($"ATTEMPT TO SEND MESSAGE RESPONSE FAILED!", LogType.ErrorLog);
                this._simPlayingLogger.WriteException("EXCEPTION IS BEING LOGGED BELOW", SendResponseException);
                this.SimMessageReceived(new SimMessageEventArgs(this.SimulationSession, false, PulledMessages.MessageRead, PulledMessages.MessageResponses));

                // Return failed sending output
                return false;
            }
        }
    }
}