using System;
using CommandLine;
using SharpAutoId.SharpAutoIdHelpers;
using SharpWrap2534;
using SharpWrap2534.PassThruTypes;
using SharpWrap2534.SupportingLogic;

namespace SharpAutoIdTests
{
    /// <summary>
    /// Main entry class point for reading a VIN number from a P4 Box automatically.
    /// </summary>
    public class SharpAutoIdTestsMain
    {
        /// <summary>
        /// Class argument object for parsing input object values
        /// </summary>
        private class ReaderArgs
        {
            [Option("DllName", Required = false, Default = "CarDAQ-Plus 3", HelpText = "Sets the name of the J2534 device to use for VIN reading")]
            public string DLLName { get; set; }
            [Option("DeviceName", Required = false, Default = "", HelpText = "Sets the name of the J2534 device to use for VIN reading")]
            public string DeviceName { get; set; }
            [Option("-V500", Required = false, Default = false, HelpText = "Sets if the use of our J2534 Version 0.500 DLL should be used or not.")]
            public bool UseV500 { get; set; }
        }

        // ------------------------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Main Entry point for this application
        /// </summary>
        /// <param name="args"></param>
        public static void Main(string[] args)
        {
            // Print some title information, sleep for about 5 seconds.
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WindowWidth = 175; Console.BufferWidth = 500;
            Console.WriteLine("PULLING IN VIN NUMBER FOR OUR P4 BOX NOW...");
            Console.ForegroundColor = ConsoleColor.White;

            // Parse out our option values here and run our AutoID Routine
            Parser.Default.ParseArguments<ReaderArgs>(args)
                .WithParsed(RunAutoId)
                .WithNotParsed((_) => Console.WriteLine("PARSE FAILED! NOT EXECUTING ANYTHING HERE!"));
        }

        /// <summary>
        /// Runs the reader arguments into our SharpSession and pulls a VIN Number
        /// </summary>
        /// <param name="ArgsPassed"></param>
        private static void RunAutoId(ReaderArgs ArgsPassed)
        {
            // Build our new SharpSession here
            JVersion Version = ArgsPassed.UseV500 ? JVersion.V0500 : JVersion.V0404;
            Sharp2534Session AutoIdSession = Sharp2534Session.OpenSession(Version, ArgsPassed.DLLName, ArgsPassed.DeviceName);

            // Now Build an AutoID routine output session
            var AutoIdHelper = AutoIdSession.SpawnAutoIdHelper(ProtocolId.ISO15765);
            AutoIdHelper.ConnectChannel(out _);

            // Now Read our vin number and close out.
            AutoIdHelper.RetrieveVinNumber(out var VinPulled);
            Sharp2534Session.CloseSession(AutoIdSession);

            // Print out the VIN Number value pulled
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine($"VIN NUMBER: {VinPulled}");
            Console.ForegroundColor = ConsoleColor.White;

            // ReadLine to wait for user to exit this application
            Console.ReadLine();
        }
    }
}
