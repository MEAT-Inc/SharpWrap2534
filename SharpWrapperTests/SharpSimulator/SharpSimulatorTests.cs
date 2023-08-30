using Microsoft.VisualStudio.TestTools.UnitTesting;
using SharpExpressions;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using SharpSimulator;
using SharpLogging;

namespace SharpWrapperTests.SharpSimulator
{
    /// <summary>
    /// Test suite holding our basic test cases for the SharpSimulator package
    /// </summary>
    [TestClass]
    public class SharpSimulatorTests
    {
        #region Custom Events
        #endregion //Custom Events

        #region Fields

        // Private fields used to help configure testing routines/methods. Created and built out once for each test
        private SharpLogger _simTestLogger;     // Logger object for this test class
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
        /// Contains a startup routine used to build a new instance of the ALS App to configure resources
        /// </summary>
        [TestInitialize]
        public void InitializeSimulationTests()
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
        /// Test method which will pick a random log file from our collection of choices and attempt to build expressions from it
        /// </summary>
        [TestMethod("Generate From Expressions Sets")]
        public void GenerateSimulationsFromExpressionsSets()
        {
            // Configure our logging instance and start the test
            TestInitializers.InitializeTestLogging(out this._simTestLogger);
            this._simTestLogger.WriteLog("Starting tests to generate simulations from expression sets now...");

            // Iterate all the test files imported and generate expressions for all of them
            foreach (var TestLogFile in this._testLogFiles)
            {
                // Build an expression generator and build our output log files
                var BuiltExpGenerator = PassThruExpressionsGenerator.LoadPassThruLogFile(TestLogFile);
                PassThruExpression[] OutputExpressions = BuiltExpGenerator.GenerateLogExpressions();
                string BuiltExpressionFile = BuiltExpGenerator.SaveExpressionsFile(
                    Path.GetFileNameWithoutExtension(TestLogFile),
                    TestInitializers.ExpressionsOutputPath);

                // Make sure we've got expressions built and the file is real
                Assert.IsTrue(OutputExpressions.Length != 0, $"Error! No expressions were found for file {TestLogFile}!");
                Assert.IsTrue(File.Exists(BuiltExpressionFile), $"Error! Built expression file {BuiltExpressionFile} does not exist!");

                // Now once we've validated expressions were built, generate our simulations
                var BuiltSimGenerator = new PassThruSimulationGenerator(TestLogFile, OutputExpressions);
                PassThruSimulationChannel[] SimulationChannels = BuiltSimGenerator.GenerateLogSimulation();
                Assert.IsTrue(SimulationChannels.Length != 0, $"Error! No simulation channels were built for file {TestLogFile}!");

                // Save the output file and make sure it's real
                string BaseSimFileName = Path.GetFileNameWithoutExtension(TestLogFile);
                string BuiltSimulationsFile = BuiltSimGenerator.SaveSimulationFile(BaseSimFileName, TestInitializers.SimulationsOutputPath);
                Assert.IsTrue(File.Exists(BuiltSimulationsFile), $"Error! Built simulation file {BuiltSimulationsFile} does not exist!");
            }

            // Log our test method is complete here
            TestInitializers.LogTestMethodCompleted();
        }
        /// <summary>
        /// Test method which will pick a random log file from our collection of choices and attempt to build expressions from it
        /// </summary>
        [TestMethod("Generate From PassThru Logs")]
        public void GenerateSimulationsFromPassThruFiles()
        {
            // Configure our logging instance and start the test
            TestInitializers.InitializeTestLogging(out this._simTestLogger);
            this._simTestLogger.WriteLog("Starting tests to generate simulations from PassThru log files now...");

            // Iterate all the test files imported and generate expressions for all of them
            foreach (var TestLogFile in this._testLogFiles)
            {
                // Now once we've validated expressions were built, generate our simulations
                var BuiltSimGenerator = PassThruSimulationGenerator.LoadPassThruLogFile(TestLogFile);
                PassThruSimulationChannel[] SimulationChannels = BuiltSimGenerator.GenerateLogSimulation();
                Assert.IsTrue(SimulationChannels.Length != 0, $"Error! No simulation channels were built for file {TestLogFile}!");

                // Save the output file and make sure it's real
                string BaseSimFileName = Path.GetFileNameWithoutExtension(TestLogFile);
                string BuiltSimulationsFile = BuiltSimGenerator.SaveSimulationFile(BaseSimFileName, TestInitializers.SimulationsOutputPath);
                Assert.IsTrue(File.Exists(BuiltSimulationsFile), $"Error! Built simulation file {BuiltSimulationsFile} does not exist!");
            }

            // Log our test method is complete here
            TestInitializers.LogTestMethodCompleted();
        }
        /// <summary>
        /// Test method which will pick a random log file from our collection of choices and attempt to build expressions from it
        /// </summary>
        [TestMethod("Generate From Expressions Files")]
        public void GenerateSimulationsFromExpressionsFiles()
        {
            // Configure our logging instance and start the test
            TestInitializers.InitializeTestLogging(out this._simTestLogger);
            this._simTestLogger.WriteLog("Starting tests to generate simulations from expressions files now...");

            // Iterate all the test files imported and generate expressions for all of them
            foreach (var TestLogFile in this._testLogFiles)
            {
                // Build an expression generator and build our output log files
                var BuiltExpGenerator = PassThruExpressionsGenerator.LoadPassThruLogFile(TestLogFile);
                PassThruExpression[] OutputExpressions = BuiltExpGenerator.GenerateLogExpressions();
                string BuiltExpressionFile = BuiltExpGenerator.SaveExpressionsFile(
                    Path.GetFileNameWithoutExtension(TestLogFile),
                    TestInitializers.ExpressionsOutputPath);

                // Make sure we've got expressions built and the file is real
                Assert.IsTrue(OutputExpressions.Length != 0, $"Error! No expressions were found for file {TestLogFile}!");
                Assert.IsTrue(File.Exists(BuiltExpressionFile), $"Error! Built expression file {BuiltExpressionFile} does not exist!");

                // Now once we've validated expressions were built, generate our simulations
                var BuiltSimGenerator = PassThruSimulationGenerator.LoadExpressionsFile(BuiltExpressionFile);
                PassThruSimulationChannel[] SimulationChannels = BuiltSimGenerator.GenerateLogSimulation();
                Assert.IsTrue(SimulationChannels.Length != 0, $"Error! No simulation channels were built for file {TestLogFile}!");

                // Save the output file and make sure it's real
                string BaseSimFileName = Path.GetFileNameWithoutExtension(TestLogFile);
                string BuiltSimulationsFile = BuiltSimGenerator.SaveSimulationFile(BaseSimFileName, TestInitializers.SimulationsOutputPath);
                Assert.IsTrue(File.Exists(BuiltSimulationsFile), $"Error! Built simulation file {BuiltSimulationsFile} does not exist!");
            }

            // Log our test method is complete here
            TestInitializers.LogTestMethodCompleted();
        }
    }
}
