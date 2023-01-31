using System;
using System.Linq;
using System.Reflection;
using SharpWrapper;
using SharpWrapper.PassThruTypes;

namespace SharpWrapperTests
{
    /// <summary>
    /// Test class used to build a new SharpSession and test the output.
    /// </summary>
    internal class SharpSessionTests
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
                // Session setup testing
                ExecuteSessionSetup(),  
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
        /// Sets up a new SharpSession using a CarDAQ plus 3 and returns true if it builds OK
        /// </summary>
        /// <returns>True if the session is built, false it it's not.</returns>
        public static bool ExecuteSessionSetup()
        {
            // Builds a new J2534 Session object using a CarDAQ Plus 3 DLL.
            var SharpSession = Sharp2534Session.OpenSession(JVersion.V0404, "CarDAQ-Plus 3");

            // Once the instance is built, the device begins in a closed state. 
            // To open it, simply run PTOpen command on the instance.
            SharpSession.PTOpen();

            // Once open, you can call the method ToDetailedString().
            // This call builds a massive output string that contains detailed information on the DLL and device objects built.
            Console.WriteLine(SharpSession.ToDetailedString());

            // Once the Session exists, connecting to a channel is as simple as issuing the 
            // PTConnect call from the session.
            // Once a channel is opened, you can send messages on it by calling the index of it.
            var OpenedChannel = SharpSession.PTConnect(0, ProtocolId.ISO15765, 0x00, BaudRate.CAN_500000, out uint ChannelIdBuilt);
            OpenedChannel.ClearTxBuffer();
            OpenedChannel.ClearRxBuffer();

            // Then once done with a channel, issue a PTDisconnect on the index provided.
            // When done with the session object, issue a PTClose to clean up the device.
            SharpSession.PTDisconnect(0);
            SharpSession.PTClose();

            // When totally done with a session, it's best to dispose it using the CloseSession routine
            Sharp2534Session.CloseSession(SharpSession);

            // Return passed output
            return true;
        }
    }
}
