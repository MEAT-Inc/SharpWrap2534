using System;
using Newtonsoft.Json;
using SharpLogger.LoggerObjects;
using SharpLogger.LoggerSupport;
using SharpSimulator.SupportingLogic;
using SharpWrapper.J2534Objects;
using SharpWrapper.PassThruTypes;

namespace SharpSimulator.SimulationObjects
{
    /// <summary>
    /// Simulation Channel object used for easy importing and sharing simulation data
    /// </summary>
    [JsonConverter(typeof(SimulationChannelJsonConverter))]
    public class SimulationChannel
    {
        // Channel ID Built and Logger
        public readonly uint ChannelId;
        public readonly BaudRate ChannelBaudRate;
        public readonly ProtocolId ChannelProtocol;
        public readonly PassThroughConnect ChannelConnectFlags;

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
        public SimulationChannel(uint ChannelId, ProtocolId ProtocolInUse, PassThroughConnect ChannelFlags, BaudRate ChannelBaud)
        {
            // Store the Channel ID
            this.ChannelId = (uint)ChannelId;
            this.ChannelProtocol = ProtocolInUse;
            this.ChannelBaudRate = ChannelBaud;
            this.ChannelConnectFlags = ChannelFlags;

            // Init empty values for our channel objects
            this.MessageFilters = Array.Empty<J2534Filter>();
            this.MessagePairs = Array.Empty<SimulationMessagePair>();
            this.MessagesRead = Array.Empty<PassThruStructs.PassThruMsg>(); 
            this.MessagesSent = Array.Empty<PassThruStructs.PassThruMsg>();
        }
    }
}
