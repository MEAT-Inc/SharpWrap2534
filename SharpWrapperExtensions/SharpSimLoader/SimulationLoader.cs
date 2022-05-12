﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SharpLogger.LoggerObjects;
using SharpLogger.LoggerSupport;
using SharpWrap2534.J2534Objects;
using SharpWrap2534.PassThruTypes;

namespace SharpSimLoader
{
    /// <summary>
    /// Contains logic for loading in new J2534 Simulations
    /// </summary>
    public class SimulationLoader
    {
        // Logger object
        private readonly SubServiceLogger _simLoaderLogger;

        // Properties of all channels for the simulation
        public List<ProtocolId> ChannelProtocols { get; }                       // Protocols
        public List<J2534Filter[]> ChannelFilters { get; }                      // Filters
        public List<PassThruStructs.PassThruMsg[]> MessagesToRead { get; }      // Messages Read
        public List<PassThruStructs.PassThruMsg[]> MessagesToWritten { get; }   // Messages Written

        // ------------------------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Builds a new Simulation Channel Loader
        /// </summary>
        public SimulationLoader()
        {
            // Setup all our default values for our class lists here.
            this.ChannelProtocols = new List<ProtocolId>();
            this.ChannelFilters = new List<J2534Filter[]>();
            this.MessagesToRead = new List<PassThruStructs.PassThruMsg[]>();
            this.MessagesToWritten = new List<PassThruStructs.PassThruMsg[]>();

            // Build a logger object.
            this._simLoaderLogger = new SubServiceLogger($"SimLoadingLogger");
            this._simLoaderLogger.WriteLog($"BUILT A NEW SIM LOADING LOGGER WITH GUID VALUE: {this._simLoaderLogger.LoggerGuid}");
        }
        

        /// <summary>
        /// Adds in a new simulation channel object based on the given input values.
        /// </summary>
        /// <param name="Protocol">Protocol of Channel</param>
        /// <param name="Filters">Filters of the channel</param>
        /// <param name="ToRead">Messages To Read and Respond to</param>
        /// <param name="ToWrite">Response Messages</param>
        /// <returns>Index of the newest built channel</returns>
        public int AddSimChannel(ProtocolId Protocol, J2534Filter[] Filters, PassThruStructs.PassThruMsg[] ToRead, PassThruStructs.PassThruMsg[] ToWrite)
        {
            // Store all new values here.
            this.ChannelProtocols.Add(Protocol);
            this.ChannelFilters.Add(Filters);
            this.MessagesToRead.Add(ToRead);
            this.MessagesToWritten.Add(ToWrite);
            this._simLoaderLogger.WriteLog("ADDED NEW VALUES FOR A SIMULATION CHANNEL WITHOUT ISSUES!", LogType.InfoLog);

            // Find new index and return it. Check the min index of the filters and the channels then the messages.
            return Math.Min(
                Math.Min(this.ChannelProtocols.Count, this.ChannelFilters.Count), 
                Math.Min(this.MessagesToRead.Count, this.MessagesToWritten.Count)
            );
        }
        /// <summary>
        /// Removes an old simulation channel object.
        /// </summary>
        /// <param name="ChannelIndex">Index of the channel to kick</param>
        /// <returns>True if removed. False if index was invalid.</returns>
        public bool RemoveSimChannel(int ChannelIndex)
        {
            // Try and remove content values here.
            if (ChannelIndex <= -1) {
                this._simLoaderLogger.WriteLog($"CHANNEL INDEX WAS {ChannelIndex} FOR REMOVE COMMAND! CAN NOT BE LESS THAN 0!", LogType.ErrorLog);
                return false; 
            }
            if (ChannelIndex > this.ChannelFilters.Count ||
                ChannelIndex > this.ChannelProtocols.Count ||
                ChannelIndex > this.MessagesToRead.Count ||
                ChannelIndex > this.MessagesToWritten.Count) {
                this._simLoaderLogger.WriteLog($"CHANNEL INDEX {ChannelIndex} WAS OUT OF BOUNDS FOR ONE OR MORE CHANNEL SET OBJECTS!", LogType.ErrorLog);
                return false;
            }

            // Now pull the values out.
            this.ChannelFilters.RemoveAt(ChannelIndex);
            this.ChannelProtocols.RemoveAt(ChannelIndex);
            this.MessagesToRead.RemoveAt(ChannelIndex);
            this.MessagesToWritten.RemoveAt(ChannelIndex);
            this._simLoaderLogger.WriteLog($"REMOVED ALL VALUES FOR CHANNEL INDEX VALUE {ChannelIndex}", LogType.InfoLog);
            return true;
        }
    }
}
