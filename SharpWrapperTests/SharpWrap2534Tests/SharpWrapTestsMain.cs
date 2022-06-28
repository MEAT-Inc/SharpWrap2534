using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using SharpWrap2534;
using SharpWrap2534.PassThruTypes;

namespace SharpWrap2534Tests
{
    /// <summary>
    /// Main testing class for our SharpWrapper project
    /// </summary>
    public class SharpWrapTestsMain
    {
        /// <summary>
        /// Main entry point for testing
        /// </summary>
        /// <param name="args"></param>
        public static void Main(string[] args)
        {
            // Log Starting Tests 
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.BackgroundColor = ConsoleColor.DarkGray;
            Console.WriteLine("STARTING SHARPWRAP2534 TEST ROUTINES NOW...");
            Console.ForegroundColor = ConsoleColor.White;
            Console.BackgroundColor = ConsoleColor.Black;
            Console.WriteLine();

            // Test our execution routines here
            if (!JsonConvertTests.ExecuteMessageTests()) 
                throw new InvalidOperationException("MESSAGE JSON CONVERSION ROUTINES FAILED!");

            // Test our execution routines here
            if (!JsonConvertTests.ExecuteFilterTests())
                throw new InvalidOperationException("FILTER JSON CONVERSION ROUTINES FAILED!");

            // Log Done With Tests
            Console.WriteLine();
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.BackgroundColor = ConsoleColor.DarkGray;
            Console.WriteLine("ALL REQUESTED SHARPWRAP2534 TESTS HAVE BEEN EXECUTED AND PASSED!");
            Console.ForegroundColor = ConsoleColor.White;
            Console.BackgroundColor = ConsoleColor.Black;
            Console.WriteLine();

            // Read a new line to stop the window from closing right away
            Console.ReadLine();
        }
    }
}
