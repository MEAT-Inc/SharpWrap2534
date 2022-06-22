using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using SharpWrap2534.PassThruTypes;

namespace SharpWrap2534Tests
{
    /// <summary>
    /// Tests built for testing JSON conversions in the wrapper project
    /// </summary>
    public class JsonConvertTests
    {
        /// <summary>
        /// Executes all tests listed in this class
        /// </summary>
        public static bool ExecuteTests()
        {
            // Convert tests execute here
            var ResultsList = new[]
            {
                TestJsonMessageWrite(),    // Testing JSON convert from a message to string
                TestJsonMessageRead()      // Testing JSON read into a message from a string
            };

            // Return if all tests passed or not.
            return ResultsList.All(ResultObj => ResultObj);
        }

        /// <summary>
        /// Tests conversion of messages out to JSON
        /// </summary>
        /// <returns>True if converted. False if not.</returns>
        private static bool TestJsonMessageWrite()
        {
            // Build message to convert out
            var MessageToConvert = new PassThruStructs.PassThruMsg()
            {
                DataSize = 10,
                ProtocolId = ProtocolId.ISO15765,
                TxFlags = TxFlags.ISO15765_FRAME_PAD,
                Data = new byte[] { 0x00, 0x00, 0x07, 0xE8, 0x41, 0x00, 0xBF, 0xFF, 0xB9, 0x93 },
            };

            // Convert into a string value here
            try 
            {
                string BuiltMsgString = JsonConvert.SerializeObject(MessageToConvert);
                return true;
            }
            catch { return false; }
        }
        /// <summary>
        /// Tests the reading of a JSON string to a message
        /// </summary>
        /// <returns>True if conversion passes. False if it does not.</returns>
        private static bool TestJsonMessageRead()
        {
            // Setup our JSON string here
            string JsonToParse = "{\"ProtocolId\": \"ISO15765\", \"RxStatus\": \"No RxStatus\", \"TxFlags\": \"ISO15765_FRAME_PAD\", \"Timestamp\": \"10ms\", \"DataSize\": \"12 Bytes\", \"ExtraDataIndex\": 0, \"Data\": \"0x00 0x00 0x07 0xDF 0x02 0x09 0x02 0x00 0x00 0x00 0x00 0x00\"}";

            // Run the conversion
            try {
                PassThruStructs.PassThruMsg ConvertedFromJSON = JsonConvert.DeserializeObject<PassThruStructs.PassThruMsg>(JsonToParse);
                return true;
            }
            catch { return false; }
        }
    }
}
