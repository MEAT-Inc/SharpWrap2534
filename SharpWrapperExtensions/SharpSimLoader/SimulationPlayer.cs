using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using SharpLogger.LoggerObjects;
using SharpLogger.LoggerSupport;
using SharpWrap2534;
using SharpWrap2534.J2534Objects;
using SharpWrap2534.PassThruTypes;
using SharpWrap2534.SupportingLogic;

namespace SharpSimLoader
{
    /// <summary>
    /// Plays back the contents of a SimulationLoader
    /// </summary>
    public class SimulationPlayer
    {
        // Logger object 
        private readonly Guid PlayerGUID;
        private readonly SubServiceLogger _simPlayingLogger;

        // Simulation Session Helper
        private bool _simulationReading = false;
        public readonly SimulationLoader InputSimulation;
        public readonly Sharp2534Session SimulationSession;

        // Values for our reader configuration.
        public uint ReaderTimeout { get; private set; }
        public uint ReaderMessageCount { get; private set; }

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
            PassThruDLL ??= "NO_DLL"; PassThruDevice ??= "NO_DEVICE";
            this.SimulationSession = Sharp2534Session.OpenSession(
                Version,
                PassThruDLL == "NO_DLL" ? "" : PassThruDLL,
                PassThruDevice == "NO_DEVICE" ? "" : PassThruDevice
            );

            // Log Built new Session
            this.PlayerGUID = Guid.NewGuid();
            this._simPlayingLogger = new SubServiceLogger($"SimPlaybackLogger_{this.PlayerGUID.ToString().ToUpper()}");
            this._simPlayingLogger.WriteLog("BUILT NEW SIMULATION PLAYBACK LOGGER OK!", LogType.InfoLog);
            this._simPlayingLogger.WriteLog("READY TO PLAY BACK THE LOADED CONTENTS OF THE PROVIDED SIMULATION LOADER!");
            this._simPlayingLogger.WriteLog($"NEWLY BUILT SESSION:\n{this.SimulationSession.ToDetailedString()}", LogType.TraceLog);

            // Open up a PT Device, read the voltage value, and begin reading messages over and over.
            this._simPlayingLogger.WriteLog($"STARTING A NEW SIM READER FOR DEVICE {this.SimulationSession.DeviceInfoString}", LogType.WarnLog);
            this.SimulationSession.PTOpen(); this.SimulationSession.PTReadVoltage(out var VoltsRead);
            this._simPlayingLogger.WriteLog($"PULLED IN A NEW VOLTAGE VALUE OF {VoltsRead}!", LogType.InfoLog);
            if (VoltsRead < 12.0) this._simPlayingLogger.WriteLog("WARNING! INPUT VOLTAGE IS LESS THAN 12.0 VOLTS!", LogType.ErrorLog);
        }


        /// <summary>
        /// Stores new values for our reader configuration on our output
        /// </summary>
        /// <param name="TimeoutValue">Timeout on each read command</param>
        /// <param name="MessageCount">Messages to read</param>
        public void ConfigureReader(uint TimeoutValue = 100, uint MessageCount = 10)
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
        /// Starts a simulation by opening up a CAN Channel for the given session instance.
        /// </summary>
        public void StartSimReader(uint ConnectFlags = 0x00)
        {
            // Toggle reading state, and begin reading values out.
            if (this._simulationReading) {
                this._simPlayingLogger.WriteLog("CAN NOT START A NEW READER SESSION WHILE A PREVIOUS ONE IS CURRENTLY BEING EXECUTED!", LogType.ErrorLog);
                return;
            }

            // Connect a new Channel value
            bool NeedsNewChannel = false;
            this.SimulationSession.PTOpen();
            var ChannelBuilt = this.SimulationSession.PTConnect(0, ProtocolId.ISO15765, ConnectFlags, 500000, out uint ChannelIdBuilt);
            if (ChannelBuilt == null) throw new InvalidOperationException("FAILED TO OPEN A NEW CHANNEL FOR OUR SIMULATION ROUTINE! THIS IS FATAL!");
            ChannelBuilt.SetConfig(ConfigParamId.CAN_MIXED_FORMAT, 1);
            ChannelBuilt.StartMessageFilter(new J2534Filter()
            {
                FilterFlags = 0x00,
                FilterFlowCtl = "",
                FilterMask = "00 00 00 00",
                FilterPattern = "00 00 00 00",
                FilterProtocol = ProtocolId.CAN,
                FilterType = FilterDef.PASS_FILTER,
            });

            // Return booted reading ok.
            this._simPlayingLogger.WriteLog($"OPENED OUR J2534 DEVICE OK! NAME OF DEVICE IS {this.SimulationSession.DeviceName}!", LogType.InfoLog);
            this._simPlayingLogger.WriteLog("STARTING BACKGROUND READER OPERATIONS NOW...", LogType.InfoLog);
            this._simPlayingLogger.WriteLog($"STARTED READ ROUTINE OK FOR DEVICE {this.SimulationSession.DeviceInfoString}!", LogType.InfoLog);

            // Read all the values out in the background now.
            while (true)
            {
                // Control Objects for setting channels
                if (NeedsNewChannel)
                {
                    // Build channel. Setup pass filter for CAN Channel
                    this.SimulationSession.PTDisconnect(0);
                    ChannelBuilt = this.SimulationSession.PTConnect(0, ProtocolId.ISO15765, ConnectFlags, 500000, out ChannelIdBuilt);
                    ChannelBuilt.SetConfig(ConfigParamId.CAN_MIXED_FORMAT, 1);
                    ChannelBuilt.StartMessageFilter(new J2534Filter()
                    {
                        FilterFlags = 0x00,
                        FilterFlowCtl = "",
                        FilterMask = "00 00 00 00",
                        FilterPattern = "00 00 00 00",
                        FilterProtocol = ProtocolId.CAN,
                        FilterType = FilterDef.PASS_FILTER,
                    });
                
                    // Toggle our new channel needed flags out.
                    NeedsNewChannel = false;
                }

                // Mark the reader is active, then read in our messages.
                this._simulationReading = true;
                uint MessageCountRef = this.ReaderMessageCount;
                var MessagesRead = ChannelBuilt.PTReadMessages(ref MessageCountRef, this.ReaderTimeout);

                // Now check out our read data values and prepare to operate on them based on the values.
                if (MessagesRead.Length == 0) continue;
                this._simPlayingLogger.WriteLog(string.Join("", Enumerable.Repeat("=", 100)));
                this._simPlayingLogger.WriteLog($"PULLED IN A TOTAL OF {MessagesRead.Length} NEW MESSAGES!");
                foreach (var ReadMessage in MessagesRead)
                {
                    // Find the index of our message and log it out with the contents built.
                    int IndexOfMessage = MessagesRead.ToList().IndexOf(ReadMessage);
                    string PulledMessageString = BitConverter.ToString(ReadMessage.Data);
                    this._simPlayingLogger.WriteLog($"--> MESSAGE [{IndexOfMessage}/{MessagesRead.Length}]: {PulledMessageString}", LogType.TraceLog);

                    // TODO: WHAT THE ACTUAL FUCK DID I WRITE HERE??? THIS WORKS BUT I DO NOT UNDERSTAND HOW
                    // Now using those messages try and figure out what channel we need to open up.
                    // Finds the Index of the channel object and the index of the message object on the channel
                    int IndexOfMessageFound = -1; int IndexOfMessageSet = -1;
                    foreach (var ChannelObject in this.InputSimulation.PairedSimulationMessages) {
                        foreach (var MessageSet in ChannelObject) {
                            if (!ReadMessage.DataString.Contains(MessageSet.Item1.DataString)) continue;
                            IndexOfMessageSet = this.InputSimulation.PairedSimulationMessages.ToList().IndexOf(ChannelObject);
                            IndexOfMessageFound = ChannelObject.IndexOf(MessageSet);
                        }
                    }

                    // Using the index found now build our output values
                    if (IndexOfMessageFound == -1) continue;

                    // Mark a new channel is needed and build new one for configuration of messages
                    NeedsNewChannel = true;
                    if (!this.SetupSimChannel(IndexOfMessageSet))
                        throw new InvalidOperationException("FAILED TO SETUP A NEW SIMULATION CHANNEL!");

                    // Now try and reply to a given message value here
                    if (!this.RespondToMessage(IndexOfMessageSet, IndexOfMessageFound))
                        throw new InvalidOperationException("FAILED TO RESPOND TO A GIVEN INPUT MESSAGE!");
                }
            };
        }

        // ------------------------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Configures a new Simulation channel for a given input index value
        /// </summary>
        /// <param name="IndexOfMessageFound">Channel index to apply from</param>
        /// <returns></returns>
        private bool SetupSimChannel(int IndexOfMessageSet)
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
            var ChannelBuilt = this.SimulationSession.PTConnect(0, ProtocolValue, ChannelFlags, ChannelBaudRate, out uint ChannelIdBuilt); 
            
            // Now apply all of our filter objects
            foreach (var ChannelFilter in FiltersToApply) { ChannelBuilt.StartMessageFilter(ChannelFilter); }
            this._simPlayingLogger.WriteLog($"BUILT NEW CHANNEL WITH ID {ChannelIdBuilt} AND SETUP ALL FILTERS FOR THE GIVEN CHANNEL OK!", LogType.InfoLog);
            return true;
        }
        /// <summary>
        /// Responds to a given input message value
        /// </summary>
        /// <param name="IndexOfMessageFound">Index of messages to respond from</param>
        private bool RespondToMessage(int IndexOfMessageSet, int IndexOfMessageFound)
        {
            // Pull out the message set, then find the response messages and send them out
            var PulledMessages = this.InputSimulation.PairedSimulationMessages[IndexOfMessageSet][IndexOfMessageFound];
            this._simPlayingLogger.WriteLog($"WRITING OUT A TOTAL OF {PulledMessages.Item2.Length} MESSAGES...", LogType.TraceLog);

            // Now issue each one out to the simulation interface
            for (int Count = 0; Count < 5; Count++) {
                try { return this.SimulationSession.PTWriteMessages(PulledMessages.Item2); }
                catch { continue; }
            }

            // Failed to send 5 times fail out
            return false;
        }
    }
}
