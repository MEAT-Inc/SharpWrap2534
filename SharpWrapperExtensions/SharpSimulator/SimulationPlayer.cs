using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using SharpLogger.LoggerObjects;
using SharpLogger.LoggerSupport;
using SharpSimulator.SimulationEvents;
using SharpWrap2534;
using SharpWrap2534.J2534Objects;
using SharpWrap2534.PassThruTypes;
using SharpWrap2534.SupportingLogic;

namespace SharpSimulator
{
    /// <summary>
    /// Plays back the contents of a SimulationLoader
    /// </summary>
    public class SimulationPlayer
    {
        // Logger object 
        private readonly Guid _playerGuid;
        private readonly SubServiceLogger _simPlayingLogger;

        // Simulation Session Helpers
        public readonly SimulationLoader InputSimulation;
        public readonly Sharp2534Session SimulationSession;

        // Channel objects and default configuration
        public J2534Channel SimulationChannel { get; private set; }
        public J2534Filter[] DefaultMessageFilters { get; private set; }
        public PassThruStructs.SConfigList DefaultConfigParamConfig { get; private set; }
        public Tuple<ProtocolId, uint, uint> DefaultConnectionConfig { get; private set; }

        // Values for our reader configuration.
        public uint ReaderTimeout { get; private set; }
        public uint ReaderMessageCount { get; private set; }

        // Other Reader Configuration Values and States
        public bool SimulationReading { get; private set; }
        public bool ResponsesEnabled { get; private set; }

        // ------------------------------------------------------------------------------------------------------------------------------------------

        // Task configuration objects for our simulations
        private CancellationToken _readerCancelToken;
        private CancellationTokenSource _readerTokenSource;

        // ------------------------------------------------------------------------------------------------------------------------------------------

        // Events for processing sim actions outside this project
        public event EventHandler<SimChannelEventArgs> SimChannelChanged;
        public event EventHandler<SimMessageEventArgs> SimMessageProcessed;

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

        // ------------------------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Spins up a new PT Instance that will read commands over and over waiting for content.
        /// </summary>
        /// <param name="Loader">Simulation Loader</param>
        /// <param name="PassThruDevice">Forced Device Name</param>
        /// <param name="PassThruDLL">Forced DLL Name</param>
        public SimulationPlayer(SimulationLoader Loader, JVersion Version = JVersion.V0404, string PassThruDLL = null, string PassThruDevice = null)
        {
            // Store class values and build a simulation loader.
            this.InputSimulation = Loader;
            this.ResponsesEnabled = true;
            PassThruDLL ??= "NO_DLL"; PassThruDevice ??= "NO_DEVICE";
            this.SimulationSession = Sharp2534Session.OpenSession(
                Version,
                PassThruDLL == "NO_DLL" ? "" : PassThruDLL,
                PassThruDevice == "NO_DEVICE" ? "" : PassThruDevice
            );

            // Log Built new Session
            this._playerGuid = Guid.NewGuid();
            this._simPlayingLogger = new SubServiceLogger($"SimPlaybackLogger_{this._playerGuid.ToString().ToUpper().Substring(0, 5)}");
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
        public void SetDefaultMessageValues(uint TimeoutValue = 100, uint MessageCount = 10)
        {
            // Store new values here and log them out
            this.ReaderTimeout = TimeoutValue;
            this.ReaderMessageCount = MessageCount;

            // Log our stored values out as trace log.
            this._simPlayingLogger.WriteLog($"STORED NEW READER CONFIGURATION! VALUES SET {MessageCount}:\n" +
                $"{this.ReaderMessageCount} MESSAGES TO READ\n" +
                $"{this.ReaderTimeout} TIMEOUT ON EACH READ COMMAND",
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
        public bool SetDefaultConnectionType(ProtocolId Protocol, uint ConnectionFlags, uint ChannelBaudrate)
        {
            // Store our configuration values here
            this.DefaultConnectionConfig = new Tuple<ProtocolId, uint, uint>(Protocol, ConnectionFlags, ChannelBaudrate);
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
            if (this.SimulationChannel == null) {
                this._simPlayingLogger.WriteLog("NOT STORING DEFAULT CONFIGURATIONS SINCE THE SIMULATION CHANNEL OBJECT IS CURRENTLY NULL OR IT IS CURRENTLY READING!", LogType.InfoLog);
                return true;
            }

            // If the channel is not null, then build our output configurations
            this._simPlayingLogger.WriteLog("SETTING SCONFIG LIST WITH OUR CONFIGURATION VALUES NOW...");
            foreach (var TupleObject in DefaultConfiguration.ConfigList)
            {
                // Issue out an IOCTL for each configuration
                this._simPlayingLogger.WriteLog($"SETTING CONFIGURATION ID PAIR: {TupleObject.SConfigParamId} -- {TupleObject.SConfigValue}");
                try { this.SimulationChannel.SetConfig(TupleObject.SConfigParamId, TupleObject.SConfigValue); }
                catch (Exception SetConfigEx)
                {
                    // Log failure, return false
                    this._simPlayingLogger.WriteLog($"FAILED TO SET NEW CONFIGURATION VALUE FOR ID {TupleObject.SConfigParamId}!", LogType.ErrorLog);
                    this._simPlayingLogger.WriteLog("EXCEPTION IS BEING LOGGED BELOW", SetConfigEx);
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
            if (this.SimulationChannel == null) {
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
                try { this.SimulationChannel.StartMessageFilter(FilterObject); }
                catch (Exception SetFilterEx)
                {
                    // Log failure, return false
                    this._simPlayingLogger.WriteLog($"FAILED TO SET NEW FILTER INSTANCE DUE TO AN EXCEPTION FROM THE J2534 API!", LogType.ErrorLog);
                    this._simPlayingLogger.WriteLog("EXCEPTION IS BEING LOGGED BELOW", SetFilterEx);
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
                this.SetDefaultConnectionType(ProtocolId.ISO15765, 0x00, 500000);
            }

            // Connect a new Channel value
            this.SimulationChannel = this.SimulationSession.PTConnect(
                0, 
                this.DefaultConnectionConfig.Item1, 
                this.DefaultConnectionConfig.Item2, 
                this.DefaultConnectionConfig.Item3,
                out uint ChannelIdBuilt
            );
            
            // Log channel built and check to make sure it is not null
            if (this.SimulationChannel == null) throw new InvalidOperationException("FAILED TO OPEN A NEW CHANNEL FOR OUR SIMULATION ROUTINE! THIS IS FATAL!");
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
            if (this.SimulationChannel == null) throw new InvalidOperationException("CAN NOT BEGIN SIMULATION READING ON A NULL CHANNEL!");
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
            var MessagesRead = this.SimulationChannel.PTReadMessages(ref MessageCountRef, this.ReaderTimeout);

            // Now check out our read data values and prepare to operate on them based on the values.
            if (MessagesRead.Length == 0 || !ResponsesEnabled) return;
            foreach (var ReadMessage in MessagesRead)
            {
                // TODO: WHAT THE ACTUAL FUCK DID I WRITE HERE??? THIS WORKS BUT I DO NOT UNDERSTAND HOW
                // Now using those messages try and figure out what channel we need to open up.
                // Finds the Index of the channel object and the index of the message object on the channel
                int IndexOfMessageFound = -1; int IndexOfMessageSet = -1;
                foreach (var ChannelMessagePair in this.InputSimulation.PairedSimulationMessages)
                {
                    // Check each of the messages found on each channel object
                    foreach (var MessageSet in ChannelMessagePair)
                    {
                        if (!ReadMessage.DataString.Contains(MessageSet.MessageRead.DataString)) continue;
                        IndexOfMessageSet = this.InputSimulation.PairedSimulationMessages.ToList().IndexOf(ChannelMessagePair);
                        IndexOfMessageFound = ChannelMessagePair.ToList().IndexOf(MessageSet);
                    }
                }

                // Using the index found now build our output values
                if (IndexOfMessageFound == -1)
                {
                    // Build and send a new message set event out even if there was no response processed
                    this.SimMessageReceived(new SimMessageEventArgs(this.SimulationSession, false, ReadMessage, null));
                    continue;
                }

                // Mark a new channel is needed and build new one for configuration of messages
                try
                {
                    // Setup a response channel object
                    if (!this._generateResponseChannel(IndexOfMessageSet))
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
                    return;
                }
            }
        }
        /// <summary>
        /// Configures a new Simulation channel for a given input index value
        /// </summary>
        /// <param name="IndexOfMessageSet">Channel index to apply from</param>
        /// <returns></returns>
        private bool _generateResponseChannel(int IndexOfMessageSet)
        {
            // Check the index value
            if (IndexOfMessageSet < 0 || IndexOfMessageSet >= this.InputSimulation.PairedSimulationMessages.Length)
                throw new InvalidOperationException($"CAN NOT APPLY CHANNEL OF INDEX {IndexOfMessageSet} SINCE IT IS OUT OF RANGE!");

            // Store channel messages and filters
            this.SimulationSession.PTDisconnect(0);
            var ChannelFlags = this.InputSimulation.ChannelFlags[IndexOfMessageSet];
            var ChannelBaudRate = this.InputSimulation.BaudRates[IndexOfMessageSet];
            var ProtocolValue = this.InputSimulation.ChannelProtocols[IndexOfMessageSet];
            var FiltersToApply = this.InputSimulation.ChannelFilters[IndexOfMessageSet];

            // Close the current channel, build a new one using the given protocol and then setup our filters.
            this.SimulationChannel = this.SimulationSession.PTConnect(0, ProtocolValue, ChannelFlags, ChannelBaudRate, out uint ChannelIdBuilt);
            foreach (var ChannelFilter in FiltersToApply) { this.SimulationChannel.StartMessageFilter(ChannelFilter); }
            
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
            var PulledMessages = this.InputSimulation.PairedSimulationMessages[IndexOfMessageSet][IndexOfMessageFound];

            // Log message contents out
            this._simPlayingLogger.WriteLog($"--> READ MESSAGE [0]: {BitConverter.ToString(PulledMessages.MessageRead.Data)}", LogType.InfoLog);
            for (int RespIndex = 0; RespIndex < PulledMessages.MessageResponses.Length; RespIndex += 1)
                this._simPlayingLogger.WriteLog($"   --> SENT MESSAGE [{RespIndex}]: {BitConverter.ToString(PulledMessages.MessageResponses[RespIndex].Data)}");

            // Now issue each one out to the simulation interface
            try
            {
                // Try and send the message, indicate passed sending routine
                this.SimulationChannel.PTWriteMessages(PulledMessages.MessageResponses, 10);
                this.SimMessageReceived(new SimMessageEventArgs(this.SimulationSession, true, PulledMessages.MessageRead, PulledMessages.MessageResponses));

                // Disconnect our channel and exit this routine
                this.SimulationSession.PTDisconnect(0);
                return true;
            }
            catch
            {
                // Log failed to send output, set sending failed.
                this._simPlayingLogger.WriteLog($"ATTEMPT TO SEND MESSAGE RESPONSE FAILED!", LogType.ErrorLog);
                this.SimMessageReceived(new SimMessageEventArgs(this.SimulationSession, false, PulledMessages.MessageRead, PulledMessages.MessageResponses));

                // Disconnect our channel and exit this routine
                this.SimulationSession.PTDisconnect(0); 
                return false;
            }
        }
    }
}
