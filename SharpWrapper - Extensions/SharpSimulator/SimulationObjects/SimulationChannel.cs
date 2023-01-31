using System;
using System.Linq;
using Newtonsoft.Json;
using SharpExpressions;
using SharpExpressions.PassThruExpressions;
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

        /// <summary>
        /// Builds a Channel object from a set of input expressions
        /// </summary>
        /// <param name="GroupedExpression">Expression set to convert</param>
        /// <param name="ChannelId">ID of the channel object to create</param>
        /// <returns>Builds a channel session object to simulate (converted to JSON)</returns>
        public static SimulationChannel BuildChannelsFromExpressions(this PassThruExpression[] GroupedExpression, uint ChannelId)
        {
            // Find all the PTFilter commands first and invert them.
            var PTConnectCommands = GroupedExpression
                .Where(ExpObj => ExpObj.TypeOfExpression == PassThruExpressionType.PTConnect)
                .Cast<PassThruConnectExpression>()
                .ToArray();
            var PTFilterCommands = GroupedExpression
                .Where(ExpObj => ExpObj.TypeOfExpression == PassThruExpressionType.PTStartMsgFilter)
                .Cast<PassThruStartMessageFilterExpression>()
                .ToArray();
            var PTReadCommands = GroupedExpression
                .Where(ExpObj => ExpObj.TypeOfExpression == PassThruExpressionType.PTReadMsgs)
                .Cast<PassThruReadMessagesExpression>()
                .ToArray();
            var PTWriteCommands = GroupedExpression
                .Where(ExpObj => ExpObj.TypeOfExpression == PassThruExpressionType.PTWriteMsgs)
                .Cast<PassThruWriteMessagesExpression>()
                .ToArray();

            // Find the ProtocolID and Current Channel ID. Then build a sim channel
            if (PTConnectCommands.Length == 0) return null;
            var ConnectCommand = PTConnectCommands.FirstOrDefault();
            var ChannelFlags = (PassThroughConnect)Convert.ToUInt32(ConnectCommand.ConnectFlags, 16);
            var ProtocolInUse = (ProtocolId)Enum.Parse(typeof(ProtocolId), ConnectCommand.ProtocolId.Split(':')[1]);
            var ChannelBaud = (BaudRate)Enum.Parse(typeof(BaudRate), Enum.GetNames(typeof(ProtocolId))
                .Select(BaudValue => BaudValue
                    .Split('_')
                    .OrderByDescending(StringPart => StringPart.Length)
                    .FirstOrDefault())
                .FirstOrDefault(ProtocolName => ProtocolInUse.ToString().Contains(ProtocolName)) + "_" + ConnectCommand.BaudRate);

            // Build simulation channel here and return it out
            if (PTReadCommands.Length == 0 || PTWriteCommands.Length == 0) return null;
            var NextChannel = new SimulationChannel(ChannelId, ProtocolInUse, ChannelFlags, ChannelBaud);
            NextChannel.StoreMessageFilters(PTFilterCommands);
            NextChannel.StoreMessagesRead(PTReadCommands);
            NextChannel.StoreMessagesWritten(PTWriteCommands);
            NextChannel.StorePassThruPairs(GroupedExpression);

            // Log information about the built out command objects.
            _expExtLogger.WriteLog(
                $"PULLED OUT THE FOLLOWING INFO FROM OUR COMMANDS (CHANNEL ID {ChannelId}):" +
                $" {PTConnectCommands.Length} PT CONNECTS" +
                $" | {PTFilterCommands.Length} FILTERS" +
                $" | {PTReadCommands.Length} READ COMMANDS" +
                $" | {PTWriteCommands.Length} WRITE COMMANDS" +
                $" | {NextChannel.MessagePairs.Length} MESSAGE PAIRS TOTAL",
                LogType.InfoLog
            );

            // Return a new tuple of our object for the command output
            return NextChannel;
        }

    }
}
