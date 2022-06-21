using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SharpWrap2534.PassThruTypes;

namespace SharpWrap2534.SupportingLogic
{
    /// <summary>
    /// Json Conversion helper for PTMessages.
    /// Used mainly to write the data in 0x00 format to JSON and convert it back when pulled in.
    /// </summary>
    public class PtMessageJsonConverter : JsonConverter
    {
        /// <summary>
        /// Sets if we can convert into JSON and from JSON or not.
        /// </summary>
        /// <param name="ObjectType"></param>
        /// <returns></returns>
        public override bool CanConvert(Type ObjectType) { return ObjectType == typeof(PassThruStructs.PassThruMsg); }

        /// <summary>
        /// Writes out a new PTMessage as JSON
        /// </summary>
        /// <param name="JWriter"></param>
        /// <param name="ValueObject"></param>
        /// <param name="JSerializer"></param>
        public override void WriteJson(JsonWriter JWriter, object? ValueObject, JsonSerializer JSerializer)
        {
            // Check if object is null. Build output
            if (ValueObject == null) { return; }
            PassThruStructs.PassThruMsg CastMessage = (PassThruStructs.PassThruMsg)ValueObject;

            // Sample JSON message object output
            /*
             * {
             *   ProtocolId: "ISO15765",
             *   RxStatus: "No RxStatus",
             *   TxFlags: "ISO15765_FRAME_PAD",
             *   Timestamp: "10ms",
             *   DataSize: 12 Bytes
             *   ExtraDataIndex: 0,
             *   Data: 0x00 0x00 0x07 0xDF 0x02 0x09 0x02 0x00 0x00 0x00 0x00 0x00
             * }
             */

            // Get all the string JSON properties we want to use for this object
            string ProtocolString = CastMessage.ProtocolId.ToString();
            string RxStatusString = CastMessage.RxStatus == 0 ? "No RxStatus" : ((RxStatus)CastMessage.RxStatus).ToString();
            string TxFlagsString = CastMessage.TxFlags == 0 ? "No TxFlags" : ((TxFlags)CastMessage.TxFlags).ToString();
            string TimeStampString = CastMessage.Timestamp + "ms";
            string DataSizeString = CastMessage.DataSize + (CastMessage.DataSize == 1 ? " Byte" : " Bytes");
            string ExtraDataIndexString = CastMessage.ExtraDataIndex.ToString();
            string DataValueString = CastMessage.DataToHexString(true);

            // Create our dynamic object for JSON output
            var OutputObject = JObject.FromObject(new
            {
                ProtocolID = CastMessage.ProtocolId,              
                CastSettingEntry.SettingValue,               // Setting Value
                CastSettingEntry.SettingDescription,         // Description of the setting
                SettingControlType = TypeOfControlString,    // Setting UI Control Type
            });

            // Now write this built object.
            JWriter.WriteRaw(JsonConvert.SerializeObject(OutputObject, Formatting.Indented));
        }
        /// <summary>
        /// Reads in a new JSON object as a PTMessage
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

            // Select the array of paths here.
            string SettingName = InputObject["SettingName"]?.Value<string>();
            object SettingValue = InputObject["SettingValue"]?.Value<object>();
            string SettingDescription = InputObject["SettingDescription"]?.Value<string>();
            Enum.TryParse(InputObject["SettingControlType"]?.Value<object>()?.ToString(), out ControlTypes SettingControlType);

            // Return built output object
            return new SettingsEntryModel(SettingName, SettingValue, SettingControlType, SettingDescription);
        }
    }
}
