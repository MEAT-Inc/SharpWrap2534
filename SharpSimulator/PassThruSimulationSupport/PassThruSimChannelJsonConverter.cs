using System;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SharpWrapper.J2534Objects;
using SharpWrapper.PassThruTypes;

namespace SharpSimulator.PassThruSimulationSupport
{
    /// <summary>
    /// Class used to convert Simulation channels into readable format in JSON
    /// </summary>
    internal class PassThruSimChannelJsonConverter: JsonConverter
    {
        /// <summary>
        /// Sets if we can convert the input object or not.
        /// </summary>
        /// <param name="ObjectType"></param>
        /// <returns></returns>
        public override bool CanConvert(Type ObjectType) { return ObjectType == typeof(PassThruSimulationChannel); }

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
            PassThruSimulationChannel CastChannel = (PassThruSimulationChannel)ValueObject;

            // Pull out the values for the channel and format them as desired
            string ProtocolString = CastChannel.ChannelProtocol.ToString();
            string ConnectFlagsString = CastChannel.ChannelConnectFlags.ToString();

            // Create our dynamic object for JSON output
            var OutputObject = JObject.FromObject(new
            {
                // Channel Properties
                CastChannel.ChannelId,
                CastChannel.ChannelBaudRate,
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
            ProtocolId ProtocolRead = InputObject["ChannelProtocol"].Type == JTokenType.Integer ?
                (ProtocolId)InputObject["ChannelProtocol"].Value<uint>() :
                (ProtocolId)Enum.Parse(typeof(ProtocolId), InputObject["ChannelProtocol"].Value<string>());
            PassThroughConnect ConnectFlagsRead = InputObject["ChannelConnectFlags"].Type == JTokenType.Integer ?
                (PassThroughConnect)InputObject["ChannelConnectFlags"].Value<uint>() :
                (PassThroughConnect)Enum.Parse(typeof(PassThroughConnect), InputObject["ChannelConnectFlags"].Value<string>());

            // Find the BaudRate value here
            string BaudRateString = InputObject["ChannelBaudRate"].Value<string>();
            var BaudRateRead = (BaudRate)Enum.Parse(typeof(BaudRate), Enum.GetNames(typeof(BaudRate))
                .Where(BaudObj => BaudObj.Contains(ProtocolRead.ToString()))
                .FirstOrDefault(BaudObj => BaudObj.Contains(BaudRateString)));

            // Basic pulled uint values and other 
            uint IdRead = InputObject["ChannelId"].Value<uint>();
            J2534Filter[] FiltersRead = InputObject["MessageFilters"].ToObject<J2534Filter[]>();
            PassThruSimulationChannel.SimulationMessagePair[] PairsRead = InputObject["MessagePairs"].ToObject<PassThruSimulationChannel.SimulationMessagePair[]>();
            PassThruStructs.PassThruMsg[] ReadMessagesSent = InputObject["MessagesSent"].ToObject<PassThruStructs.PassThruMsg[]>();
            PassThruStructs.PassThruMsg[] ReadMessagesRead = InputObject["MessagesRead"].ToObject<PassThruStructs.PassThruMsg[]>();

            // Return built output object
            return new PassThruSimulationChannel(IdRead, ProtocolRead, ConnectFlagsRead, BaudRateRead)
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
