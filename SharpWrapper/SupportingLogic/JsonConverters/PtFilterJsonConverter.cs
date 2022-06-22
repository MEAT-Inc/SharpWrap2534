using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SharpWrap2534.J2534Objects;
using SharpWrap2534.PassThruTypes;

namespace SharpWrap2534.SupportingLogic.JsonConverters
{
    /// <summary>
    /// Converts a J2534 filter object around with a specified format routine
    /// </summary>
    internal class PtFilterJsonConverter : JsonConverter
    {
        /// <summary>
        /// Sets if we can convert the input object or not.
        /// </summary>
        /// <param name="ObjectType"></param>
        /// <returns></returns>
        public override bool CanConvert(Type ObjectType) { return ObjectType == typeof(J2534Filter); }

        /// <summary>
        /// Writes a J2534 Filter object to JSON
        /// </summary>
        /// <param name="JWriter"></param>
        /// <param name="ValueObject"></param>
        /// <param name="JSerializer"></param>
        public override void WriteJson(JsonWriter JWriter, object? ValueObject, JsonSerializer JSerializer)
        {
            // Check if object is null. Build output
            if (ValueObject == null) { return; }
            J2534Filter CastFilter = (J2534Filter)ValueObject;

            // Now pull out the values for our filter and format them as desired
            string FilterFlagString = CastFilter.FilterFlags.ToString();
            string FilterTypeString = CastFilter.FilterType.ToString();
            string FilterProtocolString = CastFilter.FilterProtocol.ToString();
            string FilterStatusString = CastFilter.FilterStatus.ToString();

            // Build a custom object to output for our JSON
            var OutputObject = JObject.FromObject(new
            {
                // Converted enum values
                FilterFlags = FilterFlagString,
                FilterType = FilterTypeString,
                FilterProtocol = FilterProtocolString,
                FilterStatus = FilterStatusString,

                // Basic values for filter
                CastFilter.FilterId,
                CastFilter.FilterMask,
                CastFilter.FilterPattern,
                FilterFlowCtl = string.IsNullOrWhiteSpace(CastFilter.FilterFlowCtl) ? "No Flow Control" : CastFilter.FilterFlowCtl 
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
            TxFlags FlagsRead = InputObject["FilterFlags"].Type == JTokenType.Integer ?
                (TxFlags)InputObject["FilterFlags"].Value<uint>() : 
                (TxFlags)Enum.Parse(typeof(TxFlags), InputObject["FilterFlags"].Value<string>());
            FilterDef TypeRead = InputObject["FilterType"].Type == JTokenType.Integer ?
                (FilterDef)InputObject["FilterType"].Value<uint>() :
                (FilterDef)Enum.Parse(typeof(FilterDef), InputObject["FilterType"].Value<string>());
            ProtocolId ProtocolRead = InputObject["FilterProtocol"].Type == JTokenType.Integer ?
                (ProtocolId)InputObject["FilterProtocol"].Value<uint>() :
                (ProtocolId)Enum.Parse(typeof(ProtocolId), InputObject["FilterProtocol"].Value<string>());
            PTInstanceStatus StatusRead = InputObject["FilterStatus"].Type == JTokenType.Integer ?
                (PTInstanceStatus)InputObject["FilterStatus"].Value<uint>() :
                (PTInstanceStatus)Enum.Parse(typeof(PTInstanceStatus), InputObject["FilterStatus"].Value<string>());

            // Filter content values
            uint IdRead = InputObject["FilterId"].Value<uint>();
            string MaskRead = InputObject["FilterMask"].Value<string>();
            string PatternRead = InputObject["FilterPattern"].Value<string>();
            string FlowCtlRead = InputObject["FilterFlowCtl"].Value<string>();
            if (FlowCtlRead == "No Flow Control") FlowCtlRead = string.Empty;

            // Return built output object
            return new J2534Filter()
            {
                // Setup for enum values
                FilterFlags = FlagsRead,
                FilterType = TypeRead,
                FilterProtocol = ProtocolRead,
                FilterStatus = StatusRead,

                // Filter content values
                FilterId = IdRead,
                FilterMask = MaskRead,
                FilterPattern = PatternRead,
                FilterFlowCtl = FlowCtlRead
            };
        }
    }
}
