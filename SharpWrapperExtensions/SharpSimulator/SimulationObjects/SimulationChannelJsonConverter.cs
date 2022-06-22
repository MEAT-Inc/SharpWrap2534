using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SharpWrap2534.J2534Objects;
using SharpWrap2534.PassThruTypes;

namespace SharpSimulator.SimulationObjects
{
    /// <summary>
    /// Class used to convert Simulation channels into readable format in JSON
    /// </summary>
    internal class SimulationChannelJsonConverter: JsonConverter
    {
        /// <summary>
        /// Sets if we can convert the input object or not.
        /// </summary>
        /// <param name="ObjectType"></param>
        /// <returns></returns>
        public override bool CanConvert(Type ObjectType) { return ObjectType == typeof(SimulationChannel); }

        /// <summary>
        /// Writes a J2534 Filter object to JSON
        /// </summary>
        /// <param name="JWriter"></param>
        /// <param name="ValueObject"></param>
        /// <param name="JSerializer"></param>
        public override void WriteJson(JsonWriter JWriter, object? ValueObject, JsonSerializer JSerializer)
        {
            // Check if value object is null. Build output
            if (ValueObject == null) return;
            SimulationChannel CastChannel = (SimulationChannel)ValueObject;

            // Pull out the values for the channel and format them as desired
            string BaudRateString = CastChannel.ChannelBaudRate.ToString();
            string ProtocolString = CastChannel.ChannelProtocol.ToString();
            string ConnectFlagsString = CastChannel.ChannelConnectFlags.ToString();

            // Create our dynamic object for JSON output
            var OutputObject = JObject.FromObject(new
            {
                // Channel Properties
                CastChannel.ChannelId,
                BaudRate = BaudRateString,
                ChannelProtocol = ProtocolString,
                ChannelConnectFlags = ConnectFlagsString,

                // Message objects
                CastChannel.MessageFilters,
                CastChannel.MessagePairs,
                CastChannel.MessagesSent,
                CastChannel.MessagesRead
            });

            // Now write this built object.
            JWriter.WriteRawValue(JsonConvert.SerializeObject(OutputObject, Formatting.Indented));
        }
        /// <summary>
        /// Reads a J2534 object from JSON
        /// </summary>
        /// <param name="JReader"></param>
        /// <param name="ObjectType"></param>
        /// <param name="ExistingValue"></param>
        /// <param name="JSerializer"></param>
        /// <returns></returns>
        public override object? ReadJson(JsonReader JReader, Type ObjectType, object? ExistingValue, JsonSerializer JSerializer)
        {
            // Check if input is null. Build object from it.
            JObject InputObject = JObject.Load(JReader);
            if (InputObject.HasValues == false) { return default; }

            // Enum values pulled in here
            BaudRate BaudRateRead = InputObject["ChannelBaudRate"].Type == JTokenType.Integer ?
                (BaudRate)InputObject["ChannelBaudRate"].Value<uint>() :
                (BaudRate)Enum.Parse(typeof(ProtocolId), InputObject["ChannelBaudRate"].Value<string>());
            ProtocolId ProtocolRead = InputObject["ChannelProtocol"].Type == JTokenType.Integer ?
                (ProtocolId)InputObject["ChannelProtocol"].Value<uint>() :
                (ProtocolId)Enum.Parse(typeof(ProtocolId), InputObject["ChannelProtocol"].Value<string>());
            PassThroughConnect ConnectFlagsRead = InputObject["PassThroughConnect"].Type == JTokenType.Integer ?
                (PassThroughConnect)InputObject["PassThroughConnect"].Value<uint>() :
                (PassThroughConnect)Enum.Parse(typeof(PassThroughConnect), InputObject["PassThroughConnect"].Value<string>());

            // Basic pulled uint values and other 
            uint IdRead = InputObject["ChannelId"].Value<uint>();
            J2534Filter[] FiltersRead = InputObject["MessageFilters"].Value<J2534Filter[]>();
            SimulationMessagePair[] PairsRead = InputObject["MessagePairs"].Value<SimulationMessagePair[]>();
            PassThruStructs.PassThruMsg[] ReadMessagesSent = InputObject["MessagesSent"].Value<PassThruStructs.PassThruMsg[]>();
            PassThruStructs.PassThruMsg[] ReadMessagesRead = InputObject["MessagesRead"].Value<PassThruStructs.PassThruMsg[]>();

            // Return built output object
            return new SimulationChannel(IdRead, ProtocolRead, ConnectFlagsRead, BaudRateRead)
            {
                // Store channel configuration values here
                MessageFilters = FiltersRead,
                MessagePairs = PairsRead,
                MessagesSent = ReadMessagesSent,
                MessagesRead = ReadMessagesRead
            };
        }
    }
}
