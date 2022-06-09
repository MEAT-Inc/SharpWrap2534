﻿using System;
using SharpLogger.LoggerObjects;
using SharpLogger.LoggerSupport;
using SharpWrap2534.J2534Objects;
using SharpWrap2534.PassThruTypes;

namespace SharpSimulator.SimulationObjects
{
    /// <summary>
    /// Simulation Channel object used for easy importing and sharing simulation data
    /// </summary>
    public class SimulationChannel
    {
        // Channel ID Built and Logger
        public readonly uint ChannelId;
        public readonly uint ChannelBaudRate;
        public readonly uint ChannelConnectFlags;
        public readonly ProtocolId ChannelProtocol;
        private readonly SubServiceLogger SimChannelLogger;

        // Class Values for a channel to simulate
        public J2534Filter[] MessageFilters;
        public SimulationMessagePair[] MessagePairs;
        public PassThruStructs.PassThruMsg[] MessagesSent;
        public PassThruStructs.PassThruMsg[] MessagesRead;

        // ------------------------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Builds a new Channel Simulation object from the given channel ID
        /// </summary>
        /// <param name="ChannelId"></param>
        public SimulationChannel(int ChannelId, ProtocolId ProtocolInUse, uint ChannelBaud, uint ChannelFlags)
        {
            // Store the Channel ID
            this.ChannelId = (uint)ChannelId;
            this.ChannelProtocol = ProtocolInUse;
            this.ChannelBaudRate = ChannelBaud;
            this.ChannelConnectFlags = ChannelFlags;

            // Init empty values for our channel objects
            this.MessageFilters = Array.Empty<J2534Filter>();
            this.MessagePairs = new SimulationMessagePair[] { };
            this.MessagesRead = Array.Empty<PassThruStructs.PassThruMsg>(); 
            this.MessagesSent = Array.Empty<PassThruStructs.PassThruMsg>();

            // Log new information output
            this.SimChannelLogger = new SubServiceLogger($"SimChannelLogger_ID-{this.ChannelId}");
            this.SimChannelLogger.WriteLog($"BUILT NEW SIM CHANNEL OBJECT FOR CHANNEL ID {this.ChannelId}!", LogType.InfoLog);
        }
    }
}