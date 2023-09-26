using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SharpExpressions;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Win32;
using SharpSimulator;
using SharpLogging;
using SharpWrapper.PassThruTypes;

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
        /// Test method which generates all simulation configurations from the generator configuration
        /// JSON file
        /// </summary>
        [TestMethod("Generate Simulation Configurations")]
        public void GenerateSimulationConfigurations()
        {
            // Configure our logging instance and start the test
            TestInitializers.InitializeTestLogging(out this._simTestLogger);
            this._simTestLogger.WriteLog("Starting tests to generate simulation configurations now...");

            // Build all of our simulation configurations here
            int ConfigurationCount = (int)PassThruSimulationConfiguration.SupportedConfigurations?.Length;
            Assert.IsTrue(ConfigurationCount != 0, "Error! No simulation configurations were generated!");
            this._simTestLogger.WriteLog($"Built configurations correctly! Loaded in {ConfigurationCount} configurations for simulations!");
        }

        /// <summary>
        /// Test method which will pick a log file from the given input and attempt to build expressions from it
        /// </summary>
        [TestMethod("Generate From PassThru File")]
        public void GenerateSimulationFromPassThruFile()
        {
            // Configure our logging instance and start the test
            TestInitializers.InitializeTestLogging(out this._simTestLogger);
            this._simTestLogger.WriteLog("Starting tests to generate a simulation from user picked log file now...");
            
            // Build an expression generator and build our output log files based on the user selected log file
            string RequestedLogFile = TestInitializers.RequestTestLog();
            if (!RequestedLogFile.EndsWith(".txt")) throw new InvalidOperationException("Error! Must provide txt files for this test!");
            var InputGenerator = PassThruExpressionsGenerator.LoadPassThruLogFile(RequestedLogFile);
            PassThruExpression[] OutputExpressions = InputGenerator.GenerateLogExpressions(TestInitializers.DebugExpressionGenerators);
            Assert.IsTrue(OutputExpressions.Length != 0, $"Error! No expressions were found for file {RequestedLogFile}!");

            // Save the output expressions file and make sure it's real
            string BaseExpFileName = Path.GetFileNameWithoutExtension(RequestedLogFile);
            string BuiltExpressionFile = InputGenerator.SaveExpressionsFile(BaseExpFileName, TestInitializers.ExpressionsOutputPath);
            Assert.IsTrue(File.Exists(BuiltExpressionFile), $"Error! Built expression file {BuiltExpressionFile} does not exist!");

            // Now once we've validated expressions were built, generate our simulations
            var BuiltSimGenerator = new PassThruSimulationGenerator(RequestedLogFile, OutputExpressions);
            PassThruSimulationChannel[] SimulationChannels = BuiltSimGenerator.GenerateLogSimulation(TestInitializers.DebugSimulationGenerators);
            Assert.IsTrue(SimulationChannels.Length != 0, $"Error! No simulation channels were built for file {RequestedLogFile}!");

            // Save the output file and make sure it's real
            string BaseSimFileName = Path.GetFileNameWithoutExtension(RequestedLogFile);
            string BuiltSimulationsFile = BuiltSimGenerator.SaveSimulationFile(BaseSimFileName, TestInitializers.SimulationsOutputPath);
            Assert.IsTrue(File.Exists(BuiltSimulationsFile), $"Error! Built simulation file {BuiltSimulationsFile} does not exist!");

            // Log our test method is complete here
            TestInitializers.LogTestMethodCompleted();
        }
        /// <summary>
        /// Test method which will pick a collection of log files from the given input and attempt to build a simulation from them
        /// </summary>
        [TestMethod("Generate From PassThru Folder")]
        public void GenerateSimulationFromPassThruFiles()
        {
            // Configure our logging instance and start the test
            TestInitializers.InitializeTestLogging(out this._simTestLogger);
            this._simTestLogger.WriteLog("Starting tests to generate a simulation from user picked log files now...");
            
            // Request new input log files and ensure they're all .txt files
            string[] RequestedLogFiles = TestInitializers.RequestTestLogs().ToArray();
            if (RequestedLogFiles.Any(LogFile => !LogFile.EndsWith(".txt")))
                throw new InvalidOperationException("Error! Must provide txt files for this test!");

            // Build an expression generator and build our output log files based on the user selected log file
            var InputGenerator = PassThruExpressionsGenerator.LoadPassThruLogFiles(RequestedLogFiles);
            PassThruExpression[] OutputExpressions = InputGenerator.GenerateLogExpressions(TestInitializers.DebugExpressionGenerators);
            Assert.IsTrue(OutputExpressions.Length != 0, $"Error! No expressions were found for file {InputGenerator.PassThruLogFile}!");

            // Save the output expressions file and make sure it's real
            string BaseExpFileName = Path.GetFileNameWithoutExtension(InputGenerator.PassThruLogFile);
            string BuiltExpressionFile = InputGenerator.SaveExpressionsFile(BaseExpFileName, TestInitializers.ExpressionsOutputPath);
            Assert.IsTrue(File.Exists(BuiltExpressionFile), $"Error! Built expression file {BuiltExpressionFile} does not exist!");

            // Now once we've validated expressions were built, generate our simulations
            var BuiltSimGenerator = new PassThruSimulationGenerator(InputGenerator.PassThruLogFile, OutputExpressions);
            PassThruSimulationChannel[] SimulationChannels = BuiltSimGenerator.GenerateLogSimulation(TestInitializers.DebugSimulationGenerators);
            Assert.IsTrue(SimulationChannels.Length != 0, $"Error! No simulation channels were built for file {RequestedLogFiles}!");

            // Save the output file and make sure it's real
            string BaseSimFileName = Path.GetFileNameWithoutExtension(InputGenerator.PassThruLogFile);
            string BuiltSimulationsFile = BuiltSimGenerator.SaveSimulationFile(BaseSimFileName, TestInitializers.SimulationsOutputPath);
            Assert.IsTrue(File.Exists(BuiltSimulationsFile), $"Error! Built simulation file {BuiltSimulationsFile} does not exist!");

            // Log our test method is complete here
            TestInitializers.LogTestMethodCompleted();
        }
        /// <summary>
        /// Test method which will pick a log file from the given input and attempt to build expressions from it
        /// </summary>
        [TestMethod("Generate From Expressions File")]
        public void GenerateSimulationFromExpressionsFile()
        {
            // Configure our logging instance and start the test
            TestInitializers.InitializeTestLogging(out this._simTestLogger);
            this._simTestLogger.WriteLog("Starting tests to generate a simulation from user picked expression file now...");
            
            // Build an expression generator and build our output log files based on the user selected log file
            string RequestedExpFile = TestInitializers.RequestTestLog();
            if (!RequestedExpFile.EndsWith(".ptExp")) throw new InvalidOperationException("Error! Must provide ptExp files for this test!");
            PassThruExpressionsGenerator InputFileGenerator = PassThruExpressionsGenerator.LoadExpressionsFile(RequestedExpFile);

            // Now once we've validated expressions were built, generate our simulations
            var BuiltSimGenerator = new PassThruSimulationGenerator(RequestedExpFile, InputFileGenerator.ExpressionsBuilt);
            PassThruSimulationChannel[] SimulationChannels = BuiltSimGenerator.GenerateLogSimulation(TestInitializers.DebugSimulationGenerators);
            Assert.IsTrue(SimulationChannels.Length != 0, $"Error! No simulation channels were built for file {RequestedExpFile}!");

            // Save the output file and make sure it's real
            string BaseSimFileName = Path.GetFileNameWithoutExtension(RequestedExpFile);
            string BuiltSimulationsFile = BuiltSimGenerator.SaveSimulationFile(BaseSimFileName, TestInitializers.SimulationsOutputPath);
            Assert.IsTrue(File.Exists(BuiltSimulationsFile), $"Error! Built simulation file {BuiltSimulationsFile} does not exist!");

            // Log our test method is complete here
            TestInitializers.LogTestMethodCompleted();
        }

        /// <summary>
        /// Test method which will pick a random log file from our collection of choices and attempt to build a simulation from it
        /// </summary>
        [TestMethod("Generate From PassThru Files (Random)")]
        public void GenerateSimulationsFromRandomPassThruFile()
        {
            // Configure our logging instance and start the test
            TestInitializers.InitializeTestLogging(out this._simTestLogger);
            this._simTestLogger.WriteLog("Starting tests to generate simulations from PassThru log files now...");
            
            // Iterate all the test files imported and generate expressions for all of them
            foreach (var TestLogFile in this._testLogFiles)
            {
                // Now once we've validated expressions were built, generate our simulations
                var BuiltSimGenerator = PassThruSimulationGenerator.LoadPassThruLogFile(TestLogFile);
                PassThruSimulationChannel[] SimulationChannels = BuiltSimGenerator.GenerateLogSimulation(TestInitializers.DebugSimulationGenerators);
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
        /// Test method which will pick a random collection of log files and attempt to build a simulation from them
        /// </summary>
        [TestMethod("Generate From PassThru Folder (Random)")]
        public void GenerateSimulationsFromRandomPassThruFiles()
        {
            // Configure our logging instance and start the test
            TestInitializers.InitializeTestLogging(out this._simTestLogger);
            this._simTestLogger.WriteLog("Starting tests to generate a simulation from user picked log files now...");

            // Iterate all the test files imported and generate expressions for all of them
            foreach (var TestLogSet in this._testLogSets)
            {
                // Build an expression generator and build our output log files based on the user selected log file
                var InputGenerator = PassThruExpressionsGenerator.LoadPassThruLogFiles(TestLogSet);
                PassThruExpression[] OutputExpressions = InputGenerator.GenerateLogExpressions(TestInitializers.DebugExpressionGenerators);
                Assert.IsTrue(OutputExpressions.Length != 0, $"Error! No expressions were found for file {InputGenerator.PassThruLogFile}!");

                // Save the output expressions file and make sure it's real
                string BaseExpFileName = Path.GetFileNameWithoutExtension(InputGenerator.PassThruLogFile);
                string BuiltExpressionFile = InputGenerator.SaveExpressionsFile(BaseExpFileName, TestInitializers.ExpressionsOutputPath);
                Assert.IsTrue(File.Exists(BuiltExpressionFile), $"Error! Built expression file {BuiltExpressionFile} does not exist!");

                // Now once we've validated expressions were built, generate our simulations
                var BuiltSimGenerator = new PassThruSimulationGenerator(InputGenerator.PassThruLogFile, OutputExpressions);
                PassThruSimulationChannel[] SimulationChannels = BuiltSimGenerator.GenerateLogSimulation(TestInitializers.DebugSimulationGenerators);
                Assert.IsTrue(SimulationChannels.Length != 0, $"Error! No simulation channels were built for file {InputGenerator.PassThruLogFile}!");

                // Save the output file and make sure it's real
                string BaseSimFileName = Path.GetFileNameWithoutExtension(InputGenerator.PassThruLogFile);
                string BuiltSimulationsFile = BuiltSimGenerator.SaveSimulationFile(BaseSimFileName, TestInitializers.SimulationsOutputPath);
                Assert.IsTrue(File.Exists(BuiltSimulationsFile), $"Error! Built simulation file {BuiltSimulationsFile} does not exist!");
            }

            // Log our test method is complete here
            TestInitializers.LogTestMethodCompleted();
        }

        /// <summary>
        /// Test method used to run the simulation playback helper for a given simulation file
        /// </summary>
        [TestMethod("Run Generated Simulation")]
        public void ExecuteGeneratedSimulation()
        {
            // Configure our logging instance and start the test
            TestInitializers.InitializeTestLogging(out this._simTestLogger);
            this._simTestLogger.WriteLog("Starting tests to play a generated simulation from user picked file now...");

            // Request the user provide a simulation for the test and attempt to load it
            string RequestedSimFile = TestInitializers.RequestTestLog();
            if (!RequestedSimFile.EndsWith(".ptSim")) throw new InvalidOperationException("Error! Must provide .ptSim files for this test!");

            // Build a playback helper and let it allocate a new PassThru device, then load our simulation file
            PassThruSimulationPlayer SimulationPlayer = new PassThruSimulationPlayer(JVersion.V0404, "CarDAQ-Plus 3");
            Assert.IsTrue(SimulationPlayer.SimulationSession != null, "Error! SharpSession was not built for simulation helper!");
            this._simTestLogger.WriteLog($"Built SharpSession for playback correctly! Session details are being shown below.");
            this._simTestLogger.WriteLog(SimulationPlayer.SimulationSession.ToDetailedString());

            // Load the simulation file into our playback helper and configure the playback setup routines
            Assert.IsTrue(SimulationPlayer.LoadSimulationFile(RequestedSimFile), "Error! Failed to load simulation file into a playback helper!");
            PassThruSimulationConfiguration MixedModeConfiguration = PassThruSimulationConfiguration.LoadSimulationConfig("ISO15765 - Mixed Mode");
            Assert.IsTrue(MixedModeConfiguration != null, "Error! Failed to find mixed mode configuration for playback!");

            // Apply the configuration values to our playback helper here
            SimulationPlayer.SetResponsesEnabled(true);
            SimulationPlayer.SetPlaybackConfiguration(MixedModeConfiguration);
            this._simTestLogger.WriteLog("Configured simulation playback helper without issues! Simulation is ready to run!");

            // Boot the simulation here if needed
            this._simTestLogger.WriteLog("Spinning up simulation reader now. At this point, this test has passed");
            SimulationPlayer.InitializeSimReader();
            SimulationPlayer.StartSimulationReader();

            // Wait while true
            while (true) { }
        }
    }
}
