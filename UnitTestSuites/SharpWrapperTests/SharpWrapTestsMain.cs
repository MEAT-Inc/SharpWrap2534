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

            // Execute all the tests
            if (!SharpSessionTests.ExecuteTests()) Console.WriteLine("FAILED TO SETUP SHARP SESSION!");
            if (!SharpJsonConvertTests.ExecuteTests()) Console.WriteLine("FAILED TO EXECUTE JSON ROUTINES!");

            // Log Done With Tests
            Console.WriteLine();
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.BackgroundColor = ConsoleColor.DarkGray;
            Console.WriteLine("ALL REQUESTED SHARPWRAP2534 TESTS HAVE BEEN EXECUTED! CHECK THE CONSOLE FOR RESULTS!");
            Console.ForegroundColor = ConsoleColor.White;
            Console.BackgroundColor = ConsoleColor.Black;
            Console.WriteLine();

            // Read a new line to stop the window from closing right away
            Console.ReadLine();
        }
    }
}
