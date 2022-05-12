using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SharpLogger.LoggerObjects;
using SharpLogger.LoggerSupport;
using SharpWrap2534;
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
        private readonly SubServiceLogger _simPlayingLogger;

        // Simulation Session Helper
        private bool _simulationReading = false;
        public readonly SimulationLoader InputSimulation;
        public readonly Sharp2534Session SimulationSession;

        // Values for our reader configuration.
        public uint ReaderTimeout { get; private set; }
        public int ReaderMessageCount { get; private set; }

        // ------------------------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Spins up a new PT Instance that will read commands over and over waiting for content.
        /// </summary>
        /// <param name="Loader">Simulation Loader</param>
        /// <param name="PassThruDevice">Forced Device Name</param>
        /// <param name="PassThruDLL">Forced DLL Name</param>
        public SimulationPlayer(SimulationLoader Loader, string PassThruDLL = null, string PassThruDevice = null)
        {
            // Store class values and build a simulation loader.
            this.InputSimulation = Loader;
            string LoggerNameEnd = PassThruDLL ?? "NO_DLL" + PassThruDevice ?? "NO_DEVICE";
            this._simPlayingLogger = new SubServiceLogger($"SimPlaybackLogger_{LoggerNameEnd}");
            this.SimulationSession = Sharp2534Session.OpenSession(JVersion.V0404, PassThruDLL, PassThruDevice);
            
            // Log Built new Session
            this._simPlayingLogger.WriteLog("BUILT NEW SIMULATION PLAYBACK LOGGER OK!", LogType.InfoLog);
            this._simPlayingLogger.WriteLog("READY TO PLAY BACK THE LOADED CONTENTS OF THE PROVIDED SIMULATION LOADER!");
            this._simPlayingLogger.WriteLog($"NEWLY BUILT SESSION:\n{this.SimulationSession.ToDetailedString()}", LogType.TraceLog);
        }


        /// <summary>
        /// Stores new values for our reader configuration on our output
        /// </summary>
        /// <param name="TimeoutValue"></param>
        /// <param name="MessageCount"></param>
        public void ConfigureReader(uint TimeoutValue = 100, int MessageCount = 10)
        {
            // Store new values here and log them out
            this.ReaderTimeout = TimeoutValue;
            this.ReaderMessageCount = MessageCount;

            // Log our stored values out as trace log.
            this._simPlayingLogger.WriteLog($"STORED NEW READER CONFIGURATION! VALUES SET{MessageCount}:\n" +
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
            // Open up a PT Device, read the voltage value, and begin reading messages over and over.
            this._simPlayingLogger.WriteLog($"STARTING A NEW SIM READER FOR DEVICE {this.SimulationSession.DeviceInfoString}", LogType.WarnLog);
            this.SimulationSession.PTOpen(); this.SimulationSession.PTReadVoltage(out var VoltsRead);
            this._simPlayingLogger.WriteLog($"PULLED IN A NEW VOLTAGE VALUE OF {VoltsRead}!", LogType.InfoLog);
            if (VoltsRead < 12.0) {
                this._simPlayingLogger.WriteLog("CAN NOT SIMULATE NEW CHANNELS WITH LESS THAN 12 VOLTS!", LogType.ErrorLog);
                return null;
            }

            // If the voltage check passes, then begin reading values in.
            Task StartReading = new Task(() =>
            {
                // Toggle reading state, and begin reading values out.
                if (this._simulationReading) {
                    this._simPlayingLogger.WriteLog("CAN NOT START A NEW READER SESSION WHILE A PREVIOUS ONE IS CURRENTLY BEING EXECUTED!", LogType.ErrorLog);
                    return;
                }

                // Indicate our simulation is now reading values in.
                this._simPlayingLogger.WriteLog("STARTING BACKGROUND READER OPERATIONS NOW...", LogType.InfoLog);

                // Connect a new Channel value
                this.SimulationSession.PTOpen();
                this.SimulationSession.PTConnect(0, Protocol, ConnectFlags, BaudRate, out uint ChannelIdBuilt);
                this._simPlayingLogger.WriteLog($"CONNECTED A NEW {Protocol} CHANNEL USING CHANNEL ID {ChannelIdBuilt} OK!", LogType.InfoLog);

                // Read all the values out in the background now.
                while (true)
                {
                    // Mark the reader is active, then read in our messages.
                    this._simulationReading = true;
                    this.SimulationSession.PTReadMessages((uint)this.ReaderMessageCount, this.ReaderTimeout);
                }

                // TODO: WRITE IN AUTO READING ROUTINE HERE! HOOK IN SOME KIND OF EVENT MONITOR THAT CAN PROCESS MESSAGES IN REAL TIME
                // TODO: SHOULD HOOK INTO THE SHARP LISTENER PROJECT I STARTED EARLIER
            });

            // Return booted reading ok.
            this._simPlayingLogger.WriteLog($"STARTED READ ROUTINE OK FOR DEVICE {this.SimulationSession.DeviceInfoString}!", LogType.InfoLog);
            return StartReading;
        }
    }
}
