using System;
using Newtonsoft.Json;
using SharpSimulator.SupportingLogic;
using SharpWrapper.J2534Objects;
using SharpWrapper.PassThruTypes;

namespace SharpSimulator
{
    /// <summary>
    /// Simulation Channel object used for easy importing and sharing simulation data
    /// </summary>
    [JsonConverter(typeof(SimulationChannelJsonConverter))]
    public class SimulationChannel
    {
        #region Custom Events
        #endregion // Custom Events

        #region Fields

        // Readonly field information about the channel being simulated
        public readonly uint ChannelId; 
        public readonly BaudRate ChannelBaudRate;
        public readonly ProtocolId ChannelProtocol;
        public readonly PassThroughConnect ChannelConnectFlags;

        // The actual content of the channel which we need to simulate
        public J2534Filter[] MessageFilters;
        public SimulationMessagePair[] MessagePairs;
        public PassThruStructs.PassThruMsg[] MessagesSent;
        public PassThruStructs.PassThruMsg[] MessagesRead;

        #endregion // Fields

        #region Properties
        #endregion // Properties

        #region Structs and Classes

        /// <summary>
        /// Structure for grouping message objects on simulation channels in a more cleaned up manner
        /// </summary>
        public struct SimulationMessagePair
        {
            // Message read in and the responses to it.
            public PassThruStructs.PassThruMsg MessageRead;
            public PassThruStructs.PassThruMsg[] MessageResponses;

            // ------------------------------------------------------------------------------------------------------------------------------------------

            /// <summary>
            /// Builds a new message pairing for our simulation objects
            /// </summary>
            /// <param name="ReadMessage"></param>
            /// <param name="ResponseMessages"></param>
            public SimulationMessagePair(PassThruStructs.PassThruMsg ReadMessage, PassThruStructs.PassThruMsg[] ResponseMessages)
            {
                // Store values here
                this.MessageRead = ReadMessage;
                this.MessageResponses = ResponseMessages;
            }
        }

        #endregion // Structs and Classes

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
