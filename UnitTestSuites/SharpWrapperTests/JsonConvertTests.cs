using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using SharpWrap2534.J2534Objects;
using SharpWrap2534.PassThruTypes;
using SharpWrap2534.SupportingLogic;

namespace SharpWrap2534Tests
{
    /// <summary>
    /// Tests built for testing JSON conversions in the wrapper project
    /// </summary>
    public class JsonConvertTests
    {
        /// <summary>
        /// Executes all tests in this class
        /// </summary>
        /// <returns></returns>
        public static bool ExecuteTests()
        {
            // Convert tests execute here
            bool TestResults = new[]
            {
                // Message Object Conversions
                ExecuteMessageTests(),    // Testing JSON operations for messages
                ExecuteFilterTests(),     // Testing JSON operations for filters
            }.All(ResultValue => ResultValue);

            // Return if all tests passed or not.
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.BackgroundColor = TestResults ? ConsoleColor.DarkGreen : ConsoleColor.DarkRed;

            // Log output and return result
            Console.WriteLine($"ALL TESTS WITHIN METHOD {MethodBase.GetCurrentMethod()?.Name} EXECUTED! RESULT: {(TestResults ? "PASSED!" : "FAILED!")}");
            Console.ForegroundColor = ConsoleColor.White; Console.BackgroundColor = ConsoleColor.Black;
            return TestResults;
        }
        
        /// <summary>
        /// Executes all message tests listed in this class
        /// </summary>
        public static bool ExecuteMessageTests()
        {
            // Convert tests execute here
            var TestResults = new[]
            {
                // Message Object Conversions
                TestJsonMessageWrite(),    // Testing JSON convert from a message to string
                TestJsonMessageRead(),     // Testing JSON read into a message from a string
            }.All(ResultValue => ResultValue); ;

            // Return if all tests passed or not.
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.BackgroundColor = TestResults ? ConsoleColor.DarkGreen : ConsoleColor.DarkRed;

            // Log output and return result
            Console.WriteLine($"ALL TESTS WITHIN METHOD {MethodBase.GetCurrentMethod()?.Name} EXECUTED! RESULT: {(TestResults ? "PASSED!" : "FAILED!")}");
            Console.ForegroundColor = ConsoleColor.White; Console.BackgroundColor = ConsoleColor.Black;
            return TestResults;

        }
        /// <summary>
        /// Executes all filter tests listed in this class
        /// </summary>
        public static bool ExecuteFilterTests()
        {
            // Convert tests execute here
            bool TestResults = new[]
            {
                // Filter Object Conversions
                TestJsonFilterWrite(),    // Testing JSON convert from filter to string
                TestJsonFilterRead(),     // Testing JSON read into a filter from a string
            }.All(ResultValue => ResultValue);

            // Return if all tests passed or not.
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.BackgroundColor = TestResults ? ConsoleColor.DarkGreen : ConsoleColor.DarkRed;

            // Log output and return result
            Console.WriteLine($"ALL TESTS WITHIN METHOD {MethodBase.GetCurrentMethod()?.Name} EXECUTED! RESULT: {(TestResults ? "PASSED!" : "FAILED!")}");
            Console.ForegroundColor = ConsoleColor.White; Console.BackgroundColor = ConsoleColor.Black;
            return TestResults;
        }


        // ------------------------------------------------------------------------------------------------------------------------------------------

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

            try 
            {
                // Convert into a string value here
                string BuiltMsgString = JsonConvert.SerializeObject(MessageToConvert, Formatting.None);
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
            string JsonToParse = "{ \"ProtocolId\": \"ISO15765\", \"RxStatus\": \"NO_RX_STATUS\", \"TxFlags\": \"ISO15765_FRAME_PAD\", \"Timestamp\": \"0ms\", \"DataSize\": \"10 Bytes\", \"ExtraDataIndex\": \"0\", \"Data\": \"0x00 0x00 0x07 0xE8 0x41 0x00 0xBF 0xFF 0xB9 0x93\"}";

            // Run the conversion
            try
            {
                var ConvertedMessage = JsonConvert.DeserializeObject<PassThruStructs.PassThruMsg>(JsonToParse);
                return true;
            }
            catch { return false; }
        }

        // ------------------------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Tests writing a JSON filter output from a given definition
        /// </summary>
        /// <returns>True if write passes. False if not.</returns>
        private static bool TestJsonFilterWrite()
        {
            // Build a filter to convert out
            var FilterToConvert = new J2534Filter()
            {
                FilterId = 0,
                FilterFlags = TxFlags.ISO15765_FRAME_PAD,
                FilterMask = "00 00 FF FF",
                FilterPattern = "00 00 07 E0",
                FilterFlowCtl = "00 00 07 E8",
                FilterProtocol = ProtocolId.ISO15765,
                FilterType = FilterDef.FLOW_CONTROL_FILTER,
                FilterStatus = PTInstanceStatus.INITIALIZED,
            };

            try
            {
                // Convert into a string value here
                string BuiltFilterString = JsonConvert.SerializeObject(FilterToConvert, Formatting.None);
                return true;
            }
            catch { return false; }
        }
        /// <summary>
        /// Tests reading a JSON filter output into a desired definition
        /// </summary>
        /// <returns>True if read passes. False if not.</returns>
        private static bool TestJsonFilterRead()
        {
            // Setup our JSON string here
            string JsonToParse = "{ \"FilterFlags\": \"ISO15765_FRAME_PAD\",\"FilterType\": \"FLOW_CONTROL_FILTER\",\"FilterProtocol\": \"ISO15765\",\"FilterStatus\": \"INITIALIZED\", \"FilterId\": 0, \"FilterMask\": \"0x00 0x00 0xFF 0xFF\", \"FilterPattern\": \"0x00 0x00 0x07 0xE0\", \"FilterFlowCtl\": \"0x00 0x00 0x07 0xE8\" }";

            // Run the conversion
            try
            {
                var ConvertedFilter = JsonConvert.DeserializeObject<J2534Filter>(JsonToParse);
                return true;
            }
            catch { return false; }
        }
    }
}
