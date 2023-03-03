using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SharpLogging;

namespace SharpWrapperTests
{
    /// <summary>
    /// Static class used to run common routines on the Injector unit test cases
    /// </summary>
    public static class TestInitializers
    {
        #region Custom Events
        #endregion //Custom Events

        #region Fields

        // Constants for logging output and setting up search routines
        private static SharpLogger _testInvokersLogger;                // Private backing logger object used to write content during tests

        // Constants for the logger output format strings
        private static readonly int _splittingLineSize = 120;          // Size of the splitting lines to write in console output
        private static readonly string _splittingLineChar = "=";       // Character to use in the splitting line output

        // Static field holding information about our test log files 
        public static readonly string BaseOutputPath = Path.Combine(Directory.GetCurrentDirectory(), "TestOutput");
        public static readonly string TestJ2534LogsPath = Path.Combine(Directory.GetCurrentDirectory(), "TestJ2534Logs");

        #endregion //Fields

        #region Properties

        // Public property holding all the test logs found on our system
        public static IEnumerable<string> TestLogFiles => Directory
            .GetFiles(TestJ2534LogsPath, "*.*", SearchOption.AllDirectories)
            .Where(FileName => Path.GetFileName(FileName).StartsWith("CarDAQ") || Path.GetFileName(FileName).StartsWith("GDP"))
            .Where(FilePath => Path.GetExtension(FilePath) == ".txt")
            .ToList();
        public static IEnumerable<string> TestLogSets => Directory
            .GetDirectories(TestJ2534LogsPath, "*", SearchOption.TopDirectoryOnly)
            .OrderBy(PathValue => PathValue)
            .ToList();

        // Public properties holding information about all the log files we can test with
        public static IEnumerable<int> TestLogYears => Directory
            .GetDirectories(TestJ2534LogsPath, "*", SearchOption.AllDirectories)
            .Select(DirPath => DirPath.Split(Path.DirectorySeparatorChar).Last())
            .Select(DirPath => int.Parse(DirPath.Split('_').First()))
            .ToList();
        public static IEnumerable<string> TestLogMakes => Directory
            .GetDirectories(TestJ2534LogsPath, "*", SearchOption.AllDirectories)
            .Select(DirPath => DirPath.Split(Path.DirectorySeparatorChar).Last())
            .Select(DirPath => DirPath.Split('_')[1])
            .ToList();
        public static IEnumerable<string> TestLogModels => Directory
            .GetDirectories(TestJ2534LogsPath, "*", SearchOption.AllDirectories)
            .Select(DirPath => DirPath.Split(Path.DirectorySeparatorChar).Last())
            .Select(DirPath => DirPath.Split('_')[2])
            .ToList();
        public static IEnumerable<string> TestLogVINs => Directory
            .GetDirectories(TestJ2534LogsPath, "*", SearchOption.AllDirectories)
            .Select(DirPath => DirPath.Split(Path.DirectorySeparatorChar).Last())
            .Where(DirPath => DirPath.Split('_').Last().Length >= 17)
            .ToList();

        #endregion //Properties

        #region Structs and Classes
        #endregion //Structs and Classes

        // ------------------------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Static instance constructor for our test invoker suite. This ensures logging is ready when run
        /// </summary>
        /// <exception cref="InvalidOperationException">Thrown when configurations are invalid</exception>
        static TestInitializers()
        {
            // Spawn a new test invokers class logger instance and configure our logging
            SeparateConsole();

            // Define a new log broker configuration and setup the log broker
            string OutputLogFolder = Path.Combine(BaseOutputPath, "SharpWrapperLogging");
            SharpLogBroker.BrokerConfiguration BrokerConfiguration = new SharpLogBroker.BrokerConfiguration()
            {
                LogFilePath = OutputLogFolder,                                  // Path to the log file to write
                MinLogLevel = LogType.TraceLog,                                 // The lowest level of logging
                MaxLogLevel = LogType.FatalLog,                                 // The highest level of logging
                LogBrokerName = "SharpWrapperTests",                            // Name of the logging session
                LogFileName = "SharpWrapperTests_Logging_$LOGGER_TIME.log",     // Name of the log file to write
            };

            // Using the built configuration object, we can now initialize our log broker.
            if (!SharpLogBroker.InitializeLogging(BrokerConfiguration))
                throw new InvalidOperationException("Error! Failed to configure a new SharpLogging session!");

            // Spawn in our new logger instance and pass it out
            _testInvokersLogger = new SharpLogger(LoggerActions.UniversalLogger);
            _testInvokersLogger.WriteLog("\t--> SharpWrapper main test invoker logger checking in! Ready to log test methods...", LogType.WarnLog);
        }

        // ------------------------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Prints out a splitting line in the console window during unit tests
        /// </summary>
        /// <param name="LineSize">Size of the line to print in characters</param>
        /// <param name="LineChar">The character to print for the line</param>
        public static void SeparateConsole(int LineSize = -1, string LineChar = null)
        {
            // Setup line size and character values and print out the splitting line
            LineChar ??= _splittingLineChar;
            LineSize = LineSize == -1 ? _splittingLineSize : LineSize;

            // Print out the splitting line and exit out
            Console.WriteLine(string.Join(string.Empty, Enumerable.Repeat(LineChar, LineSize)) + "\n");
        }
        /// <summary>
        /// Logs out that test suites are building and starting for a given test class
        /// </summary>
        /// <param name="TestSuite">The sending test suite which tests are running for</param>
        public static void LogTestSuiteSetupStarting(object TestSuite)
        {
            // Split the console output, log starting up and exit out
            SeparateConsole();
            Console.WriteLine($"Starting Setup for Test Class {TestSuite.GetType().Name} now...");
        }
        /// <summary>
        /// Logs out that test suites are ready to run for a given test class
        /// </summary>
        /// <param name="TestSuite">The sending test suite which tests are running for</param>
        public static void LogTestSuiteStartupEnded(object TestSuite)
        {
            // Write the completed state value, split the console output, and exit out
            Console.WriteLine($"\t--> DONE! All required invokers and backing objects for test class {TestSuite.GetType().Name} have been built correctly!\n");
            SeparateConsole();
        }
        /// <summary>
        /// Logs out that a test method has completed without issues
        /// </summary>
        /// <param name="Message">An optional message to log out when this routine is done</param>
        /// <param name="CallingName">Name of the method which has been run</param>
        public static void LogTestMethodCompleted(string Message = "", [CallerMemberName] string CallingName = "")
        {
            // Log passed and exit out of this test method
            Console.WriteLine();
            SeparateConsole();
            Console.WriteLine($"Test method {CallingName} has completed without issues!");
            if (!string.IsNullOrWhiteSpace(Message)) Console.WriteLine(Message);
            Console.WriteLine();
            SeparateConsole();
        }

        // ------------------------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Starts up a new instance of our log broker object and passes out a new logger for this test session
        /// </summary>
        /// <param name="TestMethodLogger">A logger object to be used for the current test method</param>
        /// <param name="SendingMethod">The name of the method being used to call this routine</param>
        public static void InitializeTestLogging(out SharpLogger TestMethodLogger, [CallerMemberName] string SendingMethod = "")
        {
            // Spawn in our new logger instance and pass it out
            TestMethodLogger = new SharpLogger(LoggerActions.UniversalLogger, SendingMethod);
            TestMethodLogger.WriteLog($"\t--> Logger instance for test method {SendingMethod} is live!", LogType.InfoLog);
        }

        // ------------------------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Helper method used to return back a random test log file
        /// </summary>
        /// <param name="LogCount">The number of log files we wish to get back</param>
        /// <returns>The full path to a test log file</returns>
        public static IEnumerable<string> GetRandomTestLogs(int LogCount = 1)
        {
            // Find our random file here and return it out
            Random LogFilePicker = new Random();
            var LocalLogSet = TestLogFiles.ToArray();
            int LocalLogCount = LocalLogSet.Length - 1;

            // Validate our input args are usable
            if (LogCount < 1)
            {
                // Find an index value and return out the content found
                int NextFileIndex = LogFilePicker.Next(0, LocalLogCount);
                yield return LocalLogSet[NextFileIndex];
            }
            if (LogCount > LocalLogSet.Length)
            {
                // If the index value is greater than the max size, return it all
                foreach (string LogPath in LocalLogSet) yield return LogPath;
            }
            else
            {
                // Iterate through the files as many times as needed and return them out
                List<string> ReturnedFiles = new List<string>();
                while (ReturnedFiles.Count != LogCount)
                {
                    // Find the next file, store it, and return it out if not found already
                    string NextFile = LocalLogSet[LogFilePicker.Next(0, LocalLogCount)];
                    if (ReturnedFiles.Contains(NextFile)) Thread.Sleep(100);

                    // Store the file and exit out
                    ReturnedFiles.Add(NextFile);
                    yield return NextFile;
                }
            }
        }
        /// <summary>
        /// Helper method used to return back a set of random test log files.
        /// </summary>
        /// <param name="SetCount">The number of sets of files we wish to return back</param>
        /// <returns>The full path to a test log file</returns>
        public static IEnumerable<string[]> GetRandomTestLogSets(int SetCount = 1)
        {
            // Find our random file here and return it out
            Random LogSetPicker = new Random();
            var LocalFolderSets = TestLogSets.ToArray();
            int LocalSetCount =  LocalFolderSets.Length - 1;

            // Validate our input args are usable
            if (SetCount < 1)
            {
                // Find an index value and return out the content found
                int NextSetIndex = LogSetPicker.Next(0, LocalSetCount);
                yield return Directory.GetFiles(LocalFolderSets[NextSetIndex]);
            }
            if (SetCount >  LocalFolderSets.Length)
            {
                // If the index value is greater than the max size, return it all
                foreach (string LogSetPath in LocalFolderSets)
                    yield return Directory.GetFiles(LogSetPath);
            }
            else
            {
                // Iterate through the files as many times as needed and return them out
                List<string> ReturnedSets = new List<string>();
                while (ReturnedSets.Count != SetCount)
                {
                    // Find the next file, store it, and return it out if not found already
                    string NextSetPath =  LocalFolderSets[LogSetPicker.Next(0, LocalSetCount)];
                    if (ReturnedSets.Contains(NextSetPath)) Thread.Sleep(100);

                    // Store the file and exit out
                    ReturnedSets.Add(NextSetPath);
                    yield return Directory.GetFiles(NextSetPath);
                }
            }
        }

        // ------------------------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Used to write out debug information when an exception is processed during a unit test
        /// </summary>
        /// <param name="Message">Message to log out to the console"</param>
        /// <param name="ThrownException">The exception which we should log</param>
        /// <param name="AssertFailure">When true, the test suite asserts a failed test result</param>
        /// <param name="SendingMethod">The name of the method who threw this exception</param>
        public static void AssertException(string Message, Exception ThrownException, bool AssertFailure = true, [CallerMemberName] string SendingMethod = "")
        {
            // Split up our console
            Console.WriteLine();
            SeparateConsole();

            // Print out the requested debug information
            Console.WriteLine(Message + "\n");
            Console.WriteLine($"Exception Message: {ThrownException.Message}");
            Console.WriteLine($"Exception Stack Trace:\n{ThrownException.StackTrace}\n");

            // If the inner exception is not null, log it out
            if (ThrownException.InnerException != null)
            {
                Console.WriteLine($"\tInner Exception Message: " + ThrownException.InnerException.Message
                    .Split(new[] { "\r", "\n" }, StringSplitOptions.RemoveEmptyEntries)
                    .FirstOrDefault());

                // Clean up the inner stack trace messages and print them out
                string InnerStackMessage = ThrownException.InnerException.StackTrace;
                var InnerStackSplit = InnerStackMessage
                    .Split(new[] { "\r", "\n" }, StringSplitOptions.RemoveEmptyEntries)
                    .Select(StringPart => $"\t\t{StringPart.Trim()}");

                // Build the final inner stack trace message and log it out
                InnerStackMessage = string.Join("\n", InnerStackSplit);
                Console.WriteLine($"\tInner Exception Stack Trace:\n{InnerStackMessage}\n");
            }

            // Split the console once more and throw the failure if requested to do so
            SeparateConsole();
            if (!AssertFailure) return; 
            Assert.Fail($"{Message} -- [{ThrownException.GetType().Name}] -- Thrown from method: {SendingMethod}!");
        }
    }
}
