using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using SharpExpressions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SharpLogging;
using System;

namespace SharpWrapperTests.SharpExpressions
{
    /// <summary>
    /// Test suite holding our basic test cases for the SharpExpressions package
    /// </summary>
    [TestClass]
    public class SharpExpressionsTests
    {
        #region Custom Events
        #endregion //Custom Events

        #region Fields

        // Private fields used to help configure testing routines/methods. Created and built out once for each test
        private SharpLogger _expTestLogger;     // Logger object for this test class
        private List<string> _testLogFiles;     // Test log files used for conversions
        private List<string[]> _testLogSets;    // Multiple log files which need to be combined before converting

        #endregion //Fields

        #region Properties
        #endregion //Properties

        #region Structs and Classes
        #endregion //Structs and Classes

        // ------------------------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// CTOR for this test class.
        /// Contains a startup routine used to build a new instance of the SharpWrapper projects to configure resources
        /// </summary>
        [TestInitialize]
        public void InitializeExpressionsTests()
        {
            // Initialize the console output for this test suite, and build our backing invokers/fields
            TestInitializers.LogTestSuiteSetupStarting(this);

            // Perform any needed logic inside this block for each test method setup
            this._testLogFiles = TestInitializers.GetRandomTestLogs(5).ToList();
            this._testLogSets = TestInitializers.GetRandomTestLogSets(2).ToList();

            // Log setup complete and testing is ready to run
            TestInitializers.LogTestSuiteStartupEnded(this);
        }

        // ------------------------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Test method which will pick a log file from the given input and attempt to build expressions from it
        /// </summary>
        [TestMethod("Generate From PassThru File")]
        public void GenerateExpressionsFromPassThruFile()
        {
            // Configure our logging instance and start the test
            TestInitializers.InitializeTestLogging(out this._expTestLogger);
            this._expTestLogger.WriteLog("Starting tests to generate expressions from user picked log files now...");

            // Build an expression generator and build our output log files based on the user selected log file
            string RequestedLogFile = TestInitializers.RequestTestLog();
            if (!RequestedLogFile.EndsWith(".txt")) throw new InvalidOperationException("Error! Must provide txt files for this test!");
            var BuiltGenerator = PassThruExpressionsGenerator.LoadPassThruLogFile(RequestedLogFile);
            PassThruExpression[] OutputExpressions = BuiltGenerator.GenerateLogExpressions();
            Assert.IsTrue(OutputExpressions.Length != 0, $"Error! No expressions were found for file {RequestedLogFile}!");

            // Save the output file and make sure it's real
            string BaseExpFileName = Path.GetFileNameWithoutExtension(RequestedLogFile);
            string BuiltExpressionFile = BuiltGenerator.SaveExpressionsFile(BaseExpFileName, TestInitializers.ExpressionsOutputPath);
            Assert.IsTrue(File.Exists(BuiltExpressionFile), $"Error! Built expression file {BuiltExpressionFile} does not exist!");

            // Log our test method is complete here
            TestInitializers.LogTestMethodCompleted();
        }
        /// <summary>
        /// Test method which will pick a collection of log files from the given input and attempt to build expressions from it
        /// </summary>
        [TestMethod("Generate From PassThru Folder")]
        public void GenerateExpressionsFromPassThruFiles()
        {
            // Configure our logging instance and start the test
            TestInitializers.InitializeTestLogging(out this._expTestLogger);
            this._expTestLogger.WriteLog("Starting tests to generate expressions from user picked log files now...");

            // Request new input log files and ensure they're all .txt files
            string[] RequestedLogFiles = TestInitializers.RequestTestLogs().ToArray();
            if (RequestedLogFiles.Any(LogFile => !LogFile.EndsWith(".txt")))
                throw new InvalidOperationException("Error! Must provide txt files for this test!");

            // Build an expression generator and build our output log files based on the user selected log file
            var BuiltGenerator = PassThruExpressionsGenerator.LoadPassThruLogFiles(RequestedLogFiles);
            PassThruExpression[] OutputExpressions = BuiltGenerator.GenerateLogExpressions();
            Assert.IsTrue(OutputExpressions.Length != 0, $"Error! No expressions were found for file {BuiltGenerator.PassThruLogFile}!");

            // Save the output file and make sure it's real
            string BaseExpFileName = Path.GetFileNameWithoutExtension(BuiltGenerator.PassThruLogFile);
            string BuiltExpressionFile = BuiltGenerator.SaveExpressionsFile(BaseExpFileName, TestInitializers.ExpressionsOutputPath);
            Assert.IsTrue(File.Exists(BuiltExpressionFile), $"Error! Built expression file {BuiltExpressionFile} does not exist!");

            // Log our test method is complete here
            TestInitializers.LogTestMethodCompleted();
        }

        /// <summary>
        /// Test method which will pick a random log file from our collection of choices and attempt to build expressions from it
        /// </summary>
        [TestMethod("Generate From PassThru Logs (Random)")]
        public void GenerateExpressionsFromRandomPassThruFile()
        {
            // Configure our logging instance and start the test
            TestInitializers.InitializeTestLogging(out this._expTestLogger);
            this._expTestLogger.WriteLog("Starting tests to generate expressions from log files now...");

            // Iterate all the test files imported and generate expressions for all of them
            foreach (var TestLogFile in this._testLogFiles)
            {
                // Build an expression generator and build our output log files
                var BuiltGenerator = PassThruExpressionsGenerator.LoadPassThruLogFile(TestLogFile); 
                PassThruExpression[] OutputExpressions = BuiltGenerator.GenerateLogExpressions();
                Assert.IsTrue(OutputExpressions.Length != 0, $"Error! No expressions were found for file {TestLogFile}!");

                // Save the output file and make sure it's real
                string BaseExpFileName = Path.GetFileNameWithoutExtension(TestLogFile);
                string BuiltExpressionFile = BuiltGenerator.SaveExpressionsFile(BaseExpFileName, TestInitializers.ExpressionsOutputPath);
                Assert.IsTrue(File.Exists(BuiltExpressionFile), $"Error! Built expression file {BuiltExpressionFile} does not exist!");
            }

            // Log our test method is complete here
            TestInitializers.LogTestMethodCompleted();
        }
        /// <summary>
        /// Test method which will pick a random collection of log files from our choices and attempt to build expressions from them
        /// </summary>
        [TestMethod("Generate From PassThru Folder (Random)")]
        public void GenerateExpressionsFromRandomPassThruFiles()
        {
            // Configure our logging instance and start the test
            TestInitializers.InitializeTestLogging(out this._expTestLogger);
            this._expTestLogger.WriteLog("Starting tests to generate expressions from log files now...");

            // Iterate all the test files imported and generate expressions for all of them
            foreach (var TestLogSet in this._testLogSets)
            {
                // Build an expression generator and build our output log files
                var BuiltGenerator = PassThruExpressionsGenerator.LoadPassThruLogFiles(TestLogSet);
                PassThruExpression[] OutputExpressions = BuiltGenerator.GenerateLogExpressions();
                Assert.IsTrue(OutputExpressions.Length != 0, $"Error! No expressions were found for file {BuiltGenerator.PassThruLogFile}!");

                // Save the output file and make sure it's real
                string BaseExpFileName = Path.GetFileNameWithoutExtension(BuiltGenerator.PassThruLogFile);
                string BuiltExpressionFile = BuiltGenerator.SaveExpressionsFile(BaseExpFileName, TestInitializers.ExpressionsOutputPath);
                Assert.IsTrue(File.Exists(BuiltExpressionFile), $"Error! Built expression file {BuiltExpressionFile} does not exist!");
            }

            // Log our test method is complete here
            TestInitializers.LogTestMethodCompleted();
        }
    }
}
