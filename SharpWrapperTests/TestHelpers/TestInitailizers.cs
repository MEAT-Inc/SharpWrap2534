using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace SharpWrapperTests.TestHelpers
{
    /// <summary>
    /// Static class used to run common routines on the ALS Suite unit test cases
    /// </summary>
    internal static class LoggerTestHelpers
    {
        // Constants for logging output
        private static readonly int _splittingLineSize = 120;               // Size of the splitting lines to write in console output
        private static readonly string _splittingLineChar = "=";            // Character to use in the splitting line output

        // Static binding flags used for reflection inside invoker instances
        public static readonly BindingFlags SearchFlags =
            BindingFlags.NonPublic | BindingFlags.Instance |
            BindingFlags.Public | BindingFlags.FlattenHierarchy;

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
        /// Logs out the names of all the built backing invokers for a Unit Test suite instance
        /// </summary>
        /// <param name="TestSuite">The sending suite instance we need to log information for</param>
        public static bool ValidateBackingInvokersBuilt(object TestSuite)
        {
            // Store the test suite type first and find the fields values needed for it
            Type TestSuiteType = TestSuite.GetType();
            List<Tuple<FieldInfo, object>> InvokerFields = TestSuiteType
                .GetFields(SearchFlags)
                .Where(FieldObj => FieldObj.Name.EndsWith("Invoker"))
                .Select(InvokerField => new Tuple<FieldInfo, object>(InvokerField, InvokerField.GetValue(TestSuite)))
                .ToList();

            // Build a list of name values with a number attached to each one of them now
            List<string> InvokerStates = InvokerFields
                .Select(InvokerField =>
                {
                    // Store basic information about each invoker field here
                    string InvokerName = InvokerField.Item1.Name;
                    int InvokerIndex = InvokerFields.IndexOf(InvokerField) + 1;
                    string InvokerState = InvokerField.Item2 == null
                        ? "Creation Failed!"
                        : "Created Successfully";

                    // Build a new string for the invoker instance and return it out
                    return $"{InvokerIndex}) {InvokerName} - {InvokerState}";
                }).ToList();

            // Join the built list of backing invoker names 
            Console.WriteLine($"\t--> Backing invokers for test suite {TestSuiteType.Name} are being shown below:");
            foreach (var InvokerState in InvokerStates) Console.WriteLine($"\t\t--> {InvokerState}");

            // Assert all built invoker objects are real and ready
            return InvokerFields.All(InvokerFieldValue => InvokerFieldValue.Item2 != null);
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
