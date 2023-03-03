using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using SharpExpressions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SharpLogging;

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
        /// Contains a startup routine used to build a new instance of the ALS App to configure resources
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
        /// Test method which will pick a random log file from our collection of choices and attempt to build expressions from it
        /// </summary>
        [TestMethod("Generate Expressions From Logs")]
        public void GenerateExpressionsFromFiles()
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
                string BaseFileName = Path.GetFileNameWithoutExtension(TestLogFile);
                string BaseFolder = Path.Combine(TestInitializers.BaseOutputPath, "OutputExpressions");
                string BuiltExpressionFile = BuiltGenerator.SaveExpressionsFile(BaseFileName, BaseFolder);
                Assert.IsTrue(File.Exists(BuiltExpressionFile), $"Error! Built expression file {BuiltExpressionFile} does not exist!");
            }

            // Log our test method is complete here
            TestInitializers.LogTestMethodCompleted();
        }
    }
}
