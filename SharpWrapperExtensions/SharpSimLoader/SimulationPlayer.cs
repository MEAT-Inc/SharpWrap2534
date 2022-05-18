using System;
using System.Collections.Generic;
using System.Linq;
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
                JVersion.V0404,
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
        public Task BuildReaderTask(ProtocolId Protocol = ProtocolId.ISO15765, uint BaudRate = 500000, uint ConnectFlags = 0x00)
        {
            // If the voltage check passes, then begin reading values in.
            Task StartReading = new Task(() =>
            {
                // Toggle reading state, and begin reading values out.
                if (this._simulationReading) {
                    this._simPlayingLogger.WriteLog("CAN NOT START A NEW READER SESSION WHILE A PREVIOUS ONE IS CURRENTLY BEING EXECUTED!", LogType.ErrorLog);
                    return;
                }

                // Connect a new Channel value
                bool NeedsNewChannel = false;
                this.SimulationSession.PTOpen();
                var ChannelBuilt = this.SimulationSession.PTConnect(0, Protocol, ConnectFlags, BaudRate, out uint ChannelIdBuilt);
                if (ChannelBuilt == null) throw new InvalidOperationException("FAILED TO OPEN A NEW CHANNEL FOR OUR SIMULATION ROUTINE! THIS IS FATAL!");
                this._simPlayingLogger.WriteLog($"OPENED OUR J2534 DEVICE OK! NAME OF DEVICE IS {this.SimulationSession.DeviceName}!", LogType.InfoLog);
                this._simPlayingLogger.WriteLog("STARTING BACKGROUND READER OPERATIONS NOW...", LogType.InfoLog);

                // Read all the values out in the background now.
                while (true)
                {
                    // Control Objects for setting channels
                    if (NeedsNewChannel)
                    {
                        // Build channel. Setup pass filter for CAN Channel
                        this.SimulationSession.PTDisconnect(0);
                        ChannelBuilt = this.SimulationSession.PTConnect(0, Protocol, ConnectFlags, BaudRate, out ChannelIdBuilt);
                        if (Protocol == ProtocolId.ISO15765) ChannelBuilt.SetConfig(ConfigParamId.CAN_MIXED_FORMAT, 1);
                        ChannelBuilt.StartMessageFilter(FilterDef.PASS_FILTER, "00 00 07 00", "00 00 07 00", null);

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

                        // Now using those messages try and figure out what channel we need to open up.
                        int IndexOfMessageFound = this.GetIndexOfSimChannel(ReadMessage);
                        if (IndexOfMessageFound != -1) 
                        {
                            // Mark a new channel is needed and build new one for configuration of messages
                            NeedsNewChannel = true;
                            if (!this.SetupSimChannel(IndexOfMessageFound)) 
                                throw new InvalidOperationException("FAILED TO CONFIGURE NEW SIMULATION CHANNEL!");

                            // Now try and reply to a given message value here
                            if (!this.RespondToMessage(IndexOfMessageFound))
                                throw new InvalidOperationException("FAILED TO RESPOND TO A GIVEN INPUT MESSAGE!");

                            // Log passed response output and then continue on
                            this._simPlayingLogger.WriteLog("RESPONSE TO MESSAGE SENT OUT OK!", LogType.InfoLog);
                            continue;
                        }

                        // Log no messages matched the given input message
                        this._simPlayingLogger.WriteLog("NO MESSAGES FOUND MATCHING THE INPUT MESSAGE!", LogType.TraceLog);
                    }
                }
            });

            // Return booted reading ok.
            this._simPlayingLogger.WriteLog($"STARTED READ ROUTINE OK FOR DEVICE {this.SimulationSession.DeviceInfoString}!", LogType.InfoLog);
            return StartReading;
        }

        // ------------------------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Finds the index of the simulation channel in use for a given message.
        /// Returns the index and allows user to configure filters on the fly for it.
        /// </summary>
        /// <param name="MessageToFind">Message to locate</param>
        /// <returns></returns>
        private int GetIndexOfSimChannel(PassThruStructs.PassThruMsg MessageToFind)
        {
            // Find the message that was read in and passed here. 
            var MessageSetPulled = this.InputSimulation.PairedSimulationMessages
                .FirstOrDefault(SimMsgSet => SimMsgSet.Item1.Data == MessageToFind.Data);
            if (MessageSetPulled == null) return -1;

            // Now Find the index of the message set to return out
            return this.InputSimulation.PairedSimulationMessages.IndexOf(MessageSetPulled);
        }
        /// <summary>
        /// Configures a new Simulation channel for a given input index value
        /// </summary>
        /// <param name="IndexOfChannel">Channel index to apply from</param>
        /// <returns></returns>
        private bool SetupSimChannel(int IndexOfChannel)
        {
            // Check the index value
            if (IndexOfChannel < 0 || IndexOfChannel >= this.InputSimulation.PairedSimulationMessages.Count)
                throw new InvalidOperationException($"CAN NOT APPLY CHANNEL OF INDEX {IndexOfChannel} SINCE IT IS OUT OF RANGE!");

            // Store channel messages and filters
            var ProtocolValue = this.InputSimulation.ChannelProtocols[IndexOfChannel];
            var FiltersToApply = this.InputSimulation.ChannelFilters[IndexOfChannel];

            // Close the current channel, build a new one using the given protocol and then setup our filters.
            this.SimulationSession.PTDisconnect(0);
            var ChannelBuilt = this.SimulationSession.PTConnect(0, ProtocolValue, 0x00, 500000, out uint ChannelIdBuilt);
            if (ProtocolValue == ProtocolId.ISO15765) ChannelBuilt.SetConfig(ConfigParamId.CAN_MIXED_FORMAT, 1);

            // Now apply all of our filter objects
            foreach (var ChannelFilter in FiltersToApply) { ChannelBuilt.StartMessageFilter(ChannelFilter); }
            this._simPlayingLogger.WriteLog($"BUILT NEW CHANNEL WITH ID {ChannelIdBuilt} AND SETUP ALL FILTERS FOR THE GIVEN CHANNEL OK!", LogType.InfoLog);
            return true;
        }
        /// <summary>
        /// Responds to a given input message value
        /// </summary>
        /// <param name="MessageSetIndex">Index of messages to respond from</param>
        private bool RespondToMessage(int MessageSetIndex)
        {
            // Pull out the message set, then find the response messages and send them out
            var PulledMessages = this.InputSimulation.PairedSimulationMessages[MessageSetIndex];
            this._simPlayingLogger.WriteLog($"WRITING OUT A TOTAL OF {PulledMessages.Item2.Length} MESSAGES...", LogType.TraceLog);

            // Now issue each one out to the simulation interface
            return this.SimulationSession.PTWriteMessages(PulledMessages.Item2);
        }
    }
}
