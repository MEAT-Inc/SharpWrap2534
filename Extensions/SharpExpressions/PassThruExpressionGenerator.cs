﻿using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using SharpExpressions.PassThruExpressions;
using SharpLogging;

namespace SharpExpressions
{
    /// <summary>
    /// Class used to build an expressions setup object from an input log file.
    /// </summary>
    public class PassThruExpressionsGenerator
    {
        #region Custom Events

        // Event handler for progress updates while an expression set is building
        public EventHandler<ExpressionProgressEventArgs> OnGeneratorProgress;

        #endregion // Custom Events

        #region Fields

        // Logger object and private log contents read in to this generator
        private readonly string _logFileContents;                         // The input content of our log file when loaded
        private readonly SharpLogger _expressionsLogger;                  // Logger object used to help debug this generator

        #endregion // Fields

        #region Properties

        // Expressions file output information
        public string PassThruLogFile { get; private set; }                     // The input log file being used to convert
        public string ExpressionsFile { get; private set; }                     // Path to the newly built expressions file
        public PassThruExpression[] ExpressionsBuilt { get; private set; }      // The actual expressions objects built for the input log file

        #endregion // Properties

        #region Structs and Classes

        /// <summary>
        /// Event args for progress during a simulation building routine
        /// </summary>
        public class ExpressionProgressEventArgs : EventArgs
        {
            // Properties holding the needed information about the generation routine
            public readonly int MaxSteps;                 // The number of total steps to run
            public readonly int CurrentSteps;             // The current number of steps to run
            public readonly double CurrentProgress;       // The current progress percentage value

            // --------------------------------------------------------------------------------------------------------------------------------------
            
            /// <summary>
            /// Builds a new event arg object to invoke for progress events while simulations are building
            /// </summary>
            /// <param name="Current">Current step number</param>
            /// <param name="Max">Total number of steps to run</param>
            internal ExpressionProgressEventArgs(int Current, int Max)
            {
                // Store values and calculate percentage
                this.MaxSteps = Max;
                this.CurrentSteps = Current;
                this.CurrentProgress = ((double)CurrentSteps / (double)MaxSteps) * 100.0;
            }
        }

        #endregion // Structs and Classes

        // ------------------------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Builds a new expressions generator from a given input content file set.
        /// </summary>
        /// <param name="LogFileName">Name of the log file to load into this generator</param>
        private PassThruExpressionsGenerator(string LogFileName)
        {
            // Store our File nam e and contents here
            this.PassThruLogFile = LogFileName;
            this._logFileContents = File.ReadAllText(LogFileName);
            string LoggerName = $"ExpGeneratorLogger_{Path.GetFileNameWithoutExtension(this.PassThruLogFile)}";
            this._expressionsLogger = new SharpLogger(LoggerActions.UniversalLogger, LoggerName);
            this._expressionsLogger.WriteLog("BUILT NEW SETUP FOR AN EXPRESSIONS GENERATOR OK! READY TO BUILD OUR EXPRESSIONS FILE!", LogType.InfoLog);
        }

        // ------------------------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Spawns a new PassThruExpressionGenerator from a PassThru log file.
        /// </summary>
        /// <param name="PassThruLogFile">The log file to load into expressions. This MUST be a normal PassThru log!</param>
        /// <returns>A new expressions generator ready to load all content inside of the input log file</returns>
        public static PassThruExpressionsGenerator LoadPassThruLogFile(string PassThruLogFile)
        {
            // Build and return a new generator using this static constructors
            PassThruExpressionsGenerator OutputGenerator = new PassThruExpressionsGenerator(PassThruLogFile);
            return OutputGenerator;
        }
        /// <summary>
        /// Spawns a new PassThruExpressionGenerator from a PassThru expressions file
        /// </summary>
        /// <param name="ExpressionsFile">The expressions file to load and convert. This MUST be an Expressions log!</param>
        /// <returns>A new expressions generator ready to load all content inside of the input expressions file</returns>
        public static PassThruExpressionsGenerator LoadExpressionsFile(string ExpressionsFile)
        {
            // Convert the input expressions file into a log file and build a generator from it
            string SplitPattern = PassThruExpressionRegex.LoadedExpressions[PassThruExpressionTypes.ImportExpressionsSplit].ExpressionPattern;
            string ReplacePattern = PassThruExpressionRegex.LoadedExpressions[PassThruExpressionTypes.ImportExpressionsReplace].ExpressionPattern;

            // Read the contents of the file and store them. Split them out based on the expression splitting line entries
            string InputExpressionContent = File.ReadAllText(ExpressionsFile);
            string[] ExpressionStringsSplit = Regex.Split(InputExpressionContent, SplitPattern);

            // Now find JUST the log file content values and store them.
            string[] LogLinesPulled = ExpressionStringsSplit.Select(ExpressionEntrySet =>
            {
                // Regex match our content values desired
                string RegexLogLinesFound = Regex.Replace(ExpressionEntrySet, ReplacePattern, string.Empty);
                string[] SplitRegexLogLines = RegexLogLinesFound
                    .Split('\n')
                    .Where(LogLine =>
                        LogLine.Length > 3 &&
                        !LogLine.Contains("No Parameters") &&
                        !LogLine.Contains("No Messages Found!") &&
                        !string.IsNullOrWhiteSpace(LogLine))
                    .Select(LogLine => LogLine.Substring(3))
                    .ToArray();

                // Now trim the padding edges off and return
                string OutputRegexStrings = string.Join("\n", SplitRegexLogLines);
                return OutputRegexStrings;
            }).ToArray();

            // Convert pulled strings into one whole object. Convert the log content into an expression here
            string CombinedOutputLogLines = string.Join("\n", LogLinesPulled);
            string OutputFileName = Path.GetFileName(Path.ChangeExtension(ExpressionsFile, ".txt"));

            // Build the final output file path and write it out 
            string OutputFilePath = Path.Combine(Path.GetTempPath(), OutputFileName);
            if (File.Exists(OutputFilePath)) File.Delete(OutputFilePath);
            File.WriteAllText(OutputFilePath, CombinedOutputLogLines);

            // Now check if the conversions folder exists and copy the output file into there if needed
            string InjectorConversions = "C:\\Program Files (x86)\\MEAT Inc\\FulcrumShim\\FulcrumInjector\\FulcrumConversions";
            if (Directory.Exists(InjectorConversions))
            {
                // Copy the output file into the injector conversions folder now
                string InjectorCopy = Path.Combine(InjectorConversions, OutputFileName);
                if (File.Exists(InjectorCopy)) File.Delete(InjectorCopy);
                File.Copy(OutputFilePath, InjectorCopy);
            }

            // Now build a new generator using the built output log file and return it
            PassThruExpressionsGenerator BuiltGenerator = new PassThruExpressionsGenerator(OutputFilePath);
            return BuiltGenerator;
        }

        // ------------------------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Splits an input content string into a set fo PT Command objects which are split into objects.
        ///
        /// PreReqs:
        ///     1) Load in the log file at the path specified in the CTOR of this class
        ///     2) Store the log contents in a class variable
        ///
        /// Execution:
        ///     1) Log bullshit about this process
        ///     2) Load the regex for time string parsing and compile it (for spood)
        ///     3) Process all matches in the log file content and split into an array of substrings
        ///     4) Setup empty lists for the following value types
        ///        - Output Commands - Strings of the log lines pulled for each PT command
        ///        - Output Expressions - Built Expression objects from the input log file
        ///        - Expressions File Content - Holds all the expression objects as strings
        ///     5) Loop all of the matches found in step 3 and run the following operations
        ///        - Pull a match object and get the index of it
        ///        - Get the index of the next match (or the end of the file) and find the length of our substring
        ///        - Pull a substring value from the input log contents and store it in the Output Commands list
        ///        - Using that substring, build an expression object if the log line content is supported
        ///        - Once an expression is built, convert it to a string and store the value of it
        ///     6) Check if progress updating should be done or not and do it if needed
        ///     7) Clean up our output list objects and prune null values out.
        ///     8) Store the built values on this class instance to return out our built expression objects
        ///     9) Log completed building and return the collection of built expressions
        /// </summary>
        /// <returns>Returns a set of file objects which contain the PT commands from a file.</returns>
        public PassThruExpression[] GenerateLogExpressions()
        {
            // Log building expression log command line sets now
            this._expressionsLogger.WriteLog($"CONVERTING INPUT LOG FILE {this.PassThruLogFile} INTO AN EXPRESSION SET NOW...", LogType.InfoLog);

            // Store our regex matches and regex object for the time string values located here
            var TimeRegex = PassThruExpressionRegex.LoadedExpressions[PassThruExpressionTypes.CommandTime].ExpressionRegex;
            var TimeMatches = TimeRegex.Matches(this._logFileContents).Cast<Match>().ToArray();

            // Build an output list of lines for content, find our matches from a built expressions Regex, and generate output lines
            var OutputCommands = Enumerable.Repeat(string.Empty, TimeMatches.Length).ToArray();
            var OutputFileContent = Enumerable.Repeat(string.Empty, TimeMatches.Length).ToArray();
            var OutputExpressions = Enumerable.Repeat(new PassThruExpression(), TimeMatches.Length).ToArray();

            // Store an int value to track our loop count based on the number of iterations built now
            int LoopsCompleted = 0;

            // Loop all the time matches in order and find the index of the next one. Take all content between the index values found
            ParallelOptions ParseOptions = new ParallelOptions() { MaxDegreeOfParallelism = 3 };
            Parallel.For(0, TimeMatches.Length, ParseOptions, MatchIndex =>
            {
                // Pull our our current match object here and store it
                Match CurrentMatch = TimeMatches[MatchIndex];

                // Store the index and the string value of this match object here
                string FileSubString = string.Empty;
                int StartingIndex = CurrentMatch.Index;
                string MatchContents = CurrentMatch.Value;

                try
                {
                    // Find the index values of the next match now and store it to
                    Match NextMatch = MatchIndex + 1 == TimeMatches.Length
                        ? TimeMatches[MatchIndex]
                        : TimeMatches[MatchIndex + 1];

                    // Pull a substring of our file contents here and store them now
                    int EndingIndex = NextMatch.Index;
                    int FileSubstringLength = EndingIndex - StartingIndex;
                    FileSubString = this._logFileContents.Substring(StartingIndex, FileSubstringLength);
                    OutputCommands[MatchIndex] = FileSubString;

                    // If we've got the zero messages error line, then just return on
                    bool IsEmpty = string.IsNullOrWhiteSpace(FileSubString);
                    bool HasBuffEmpty = FileSubString.Contains("16:BUFFER_EMPTY");
                    bool HasComplete = FileSubString.Contains("PTReadMsgs() complete");
                    if (!IsEmpty && !HasBuffEmpty && !HasComplete)
                    {
                        // Take the split content values, get our ExpressionTypes, and store the built expression object here
                        PassThruExpressionTypes ExpressionTypes = FileSubString.ToPassThruCommandType();
                        PassThruExpression NextClassObject = ExpressionTypes.ToPassThruExpression(FileSubString);
                        OutputExpressions[MatchIndex] = NextClassObject;

                        // Now store the expression object as a string for our output file content values
                        string ExpressionString = NextClassObject.ToString();
                        OutputFileContent[MatchIndex] = ExpressionString;
                    }
                }
                catch (Exception GenerateExpressionEx)
                {
                    // Log failures out and find out why the fails happen then move to our progress routine or move to next iteration
                    this._expressionsLogger.WriteLog($"FAILED TO GENERATE AN EXPRESSION FROM INPUT COMMAND {MatchContents} (Index: {MatchIndex})!", LogType.WarnLog);
                    this._expressionsLogger.WriteException("EXCEPTION THROWN IS LOGGED BELOW", GenerateExpressionEx, new[] { LogType.WarnLog, LogType.TraceLog });
                }

                // Update progress values if needed now using the event for the progress checker
                this.OnGeneratorProgress?.Invoke(this, new ExpressionProgressEventArgs(LoopsCompleted++, TimeMatches.Length));
            });

            // Prune all null values off the array of expressions
            OutputExpressions = OutputExpressions.Where(ExpressionObj => ExpressionObj.TypeOfExpression != PassThruExpressionTypes.NONE).ToArray();

            // Log done building log command line sets and expressions
            this._expressionsLogger.WriteLog($"DONE BUILDING EXPRESSION SETS FROM INPUT FILE {this.PassThruLogFile}!", LogType.InfoLog);
            this._expressionsLogger.WriteLog($"BUILT A TOTAL OF {OutputExpressions.Length} LOG LINE SETS OK!", LogType.InfoLog);
            this.OnGeneratorProgress?.Invoke(this, new ExpressionProgressEventArgs(100, 100));

            // Return the built set of commands.
            this.ExpressionsBuilt = OutputExpressions.ToArray();
            return this.ExpressionsBuilt;
        }
        /// <summary>
        /// Takes an input set of PTExpressions and writes them to a file object desired.
        /// </summary>
        /// <param name="BaseFileName">Input file name to save our output expressions file content as</param>
        /// <param name="OutputLogFileFolder">Optional folder to store the output file in. Defaults to the injector folder</param>
        /// <returns>Path of our built expression file</returns>
        public string SaveExpressionsFile(string BaseFileName = "", string OutputLogFileFolder = null)
        {
            // First build our output location for our file.
            OutputLogFileFolder ??= "C:\\Program Files (x86)\\MEAT Inc\\FulcrumShim\\FulcrumInjector\\FulcrumExpressions";
            string FinalOutputPath = Path.Combine(OutputLogFileFolder, Path.GetFileNameWithoutExtension(BaseFileName)) + ".ptExp";

            // Get a logger object for saving expression sets.
            string LoggerName = $"{Path.GetFileNameWithoutExtension(BaseFileName)}_ExpressionsLogger";
            var ExpressionLogger = new SharpLogger(LoggerActions.UniversalLogger, LoggerName);

            // Find output path and then build final path value.             
            if (!Directory.Exists(Path.GetDirectoryName(FinalOutputPath))) { Directory.CreateDirectory(Path.GetDirectoryName(FinalOutputPath)); }
            ExpressionLogger.WriteLog($"BASE OUTPUT LOCATION FOR EXPRESSIONS IS SEEN TO BE {Path.GetDirectoryName(FinalOutputPath)}", LogType.InfoLog);

            // Log information about the expression set and output location
            ExpressionLogger.WriteLog($"SAVING A TOTAL OF {this.ExpressionsBuilt.Length} EXPRESSION OBJECTS NOW...", LogType.InfoLog);
            ExpressionLogger.WriteLog($"EXPRESSION SET IS BEING SAVED TO OUTPUT FILE: {FinalOutputPath}", LogType.InfoLog);

            try
            {
                // Now Build output string content from each expression object.
                ExpressionLogger.WriteLog("COMBINING EXPRESSION OBJECTS INTO AN OUTPUT FILE NOW...", LogType.WarnLog);
                if (this.ExpressionsBuilt == null)
                    throw new InvalidOperationException("ERROR! CAN NOT SAVE AN EXPRESSIONS FILE THAT HAS NOT BEEN GENERATED!");

                // Build the string contents for our expression objects now
                string[] ExpressionsContentSplit = ExpressionsBuilt
                    .Where(ExpressionObj => ExpressionObj.TypeOfExpression != PassThruExpressionTypes.NONE)
                    .Select(ExpressionObj => ExpressionObj.ToString())
                    .Where(StringSet => !string.IsNullOrWhiteSpace(StringSet))
                    .ToArray();

                // Log information and write output to the desired output file now and move on
                ExpressionLogger.WriteLog($"CONVERTED INPUT OBJECTS INTO A TOTAL OF {ExpressionsContentSplit.Length} LINES OF TEXT!", LogType.WarnLog);
                ExpressionLogger.WriteLog("WRITING OUTPUT CONTENTS NOW...", LogType.WarnLog);
                File.WriteAllText(FinalOutputPath, string.Join("\n", ExpressionsContentSplit));
                ExpressionLogger.WriteLog("DONE WRITING OUTPUT EXPRESSIONS CONTENT!");

                // Check to see if we aren't in the default location. If not, store the file in both the input spot and the injector directory
                if (BaseFileName.Contains(Path.DirectorySeparatorChar) && !BaseFileName.Contains("FulcrumLogs"))
                {
                    // Find the base path, get the file name, and copy it into here.
                    string LocalDirectory = Path.GetDirectoryName(BaseFileName);
                    string CopyLocation = Path.Combine(LocalDirectory, Path.GetFileNameWithoutExtension(FinalOutputPath)) + ".ptExp";
                    File.Copy(FinalOutputPath, CopyLocation, true);

                    // Remove the Expressions Logger. Log done and return
                    ExpressionLogger.WriteLog("COPIED OUTPUT EXPRESSIONS FILE INTO THE BASE EXPRESSION FILE LOCATION!");
                }

                // Store the path to our final expressions file and exit out
                this.ExpressionsFile = FinalOutputPath;
                return FinalOutputPath;
            }
            catch (Exception WriteEx)
            {
                // Log failures. Return an empty string.
                ExpressionLogger.WriteLog("FAILED TO SAVE OUR OUTPUT EXPRESSION SETS! THIS IS FATAL!", LogType.FatalLog);
                ExpressionLogger.WriteException("EXCEPTION FOR THIS INSTANCE IS BEING LOGGED BELOW", WriteEx);

                // Return nothing.
                return string.Empty;
            }
        }
    }
}