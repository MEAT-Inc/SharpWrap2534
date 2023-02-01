using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Newtonsoft.Json;
using SharpExpressions;
using SharpLogger;
using SharpLogger.LoggerObjects;
using SharpLogger.LoggerSupport;
using SharpSimulator.SupportingLogic;
using SharpWrapper.J2534Objects;
using SharpWrapper.PassThruTypes;

namespace SharpSimulator
{
    /// <summary>
    /// Takes a set of PT Expression objects and converts them into simulation ready commands.
    /// </summary>
    public class PassThruSimulationGenerator
    {
        #region Custom Events
        
        // Event handler for progress updates while a simulation is building
        public EventHandler<SimulationProgressEventArgs> OnGeneratorProgress;

        #endregion // Custom Events

        #region Fields

        // Logger object used to help provided debug information about a simulation being built
        private readonly SubServiceLogger _simulationLogger;

        #endregion // Fields

        #region Properties

        // Public facing fields holding information about this simulation generator
        public string PassThruLogFile { get; private set; }
        public string ExpressionsFile { get; private set; }
        public string SimulationFile { get; private set; }

        // Built dictionary objects that are used to help configure the simulations
        public PassThruSimulationChannel[] SimulationChannels { get; private set; }
        public PassThruExpression[] ExpressionsLoaded { get; private set; }

        // Properties of all channels for the simulation that have been built out from this generator
        public BaudRate[] BaudRates => this.SimulationChannels.Select(SimChannel => SimChannel.ChannelBaudRate).ToArray();
        public PassThroughConnect[] ChannelFlags => this.SimulationChannels.Select(SimChannel => SimChannel.ChannelConnectFlags).ToArray();
        public ProtocolId[] ChannelProtocols => this.SimulationChannels.Select(SimChannel => SimChannel.ChannelProtocol).ToArray();
        public J2534Filter[][] ChannelFilters => this.SimulationChannels.Select(SimChannel => SimChannel.MessageFilters).ToArray();

        // Message pairing collections holding information about all messages read or written for a simulation
        public PassThruSimulationChannel.SimulationMessagePair[][] PairedSimulationMessages => this.SimulationChannels
            .Select(SimChannel => SimChannel.MessagePairs)
            .ToArray();
        public PassThruStructs.PassThruMsg[] MessagesToRead => (PassThruStructs.PassThruMsg[])PairedSimulationMessages
            .SelectMany(MsgSet => MsgSet.Select(MsgPair => MsgPair.MessageRead)
                .ToArray());
        public PassThruStructs.PassThruMsg[][] MessagesToWrite => (PassThruStructs.PassThruMsg[][])PairedSimulationMessages
            .SelectMany(MsgSet => MsgSet.Select(MsgPair => MsgPair.MessageResponses)
                .ToArray());

        #endregion // Properties

        #region Structs and Classes

        /// <summary>
        /// Event args for progress during a simulation building routine
        /// </summary>
        public class SimulationProgressEventArgs : EventArgs
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
            internal SimulationProgressEventArgs(int Current, int Max)
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
        /// Builds a new Simulation generator based on an expressions collection and a provided log file name
        /// </summary>
        /// <param name="PassThruLogName">Name of the simulation to use for writing our output</param>
        /// <param name="GeneratedExpressions">The expressions to be used for building our simulation</param>
        public PassThruSimulationGenerator(string PassThruLogName, IEnumerable<PassThruExpression> GeneratedExpressions)
        {
            // Store the name of the simulation and configure our logger
            this.PassThruLogFile = PassThruLogName;
            this.ExpressionsLoaded = GeneratedExpressions.ToArray();

            // Finally build our logger object and exit out of this constructor
            string SimGeneratorName = Path.GetFileNameWithoutExtension(this.PassThruLogFile);
            string LoggerName = $"SimGeneratorLogger_{Path.GetFileNameWithoutExtension(SimGeneratorName)}";
            this._simulationLogger = (SubServiceLogger)LoggerQueue.SpawnLogger(LoggerName, LoggerActions.SubServiceLogger);
            this._simulationLogger.WriteLog($"READY TO BUILD NEW SIMULATION FROM {this.ExpressionsLoaded.Length} INPUT EXPRESSIONS...", LogType.WarnLog);
        }

        // ------------------------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Builds a new Simulation generator based on an expressions generator
        /// This is used only by the static CTORs to allow easier configuration of simulations based on log files
        /// </summary>
        /// <param name="GeneratorToSimulate">The built expressions generator which should hold all expression objects</param>
        private PassThruSimulationGenerator(PassThruExpressionsGenerator GeneratorToSimulate)
        {
            // Store the expressions file name/path and convert it into a collection of expressions
            this.PassThruLogFile = GeneratorToSimulate.PassThruLogFile;
            this.ExpressionsFile = GeneratorToSimulate.ExpressionsFile;
            this.ExpressionsLoaded = GeneratorToSimulate.ExpressionsBuilt.Length == 0
                ? GeneratorToSimulate.GenerateLogExpressions()
                : GeneratorToSimulate.ExpressionsBuilt;

            // Finally build our logger object and exit out of this constructor
            string SimGeneratorName = Path.GetFileNameWithoutExtension(this.PassThruLogFile);
            string LoggerName = $"SimGeneratorLogger_{Path.GetFileNameWithoutExtension(SimGeneratorName)}";
            this._simulationLogger = (SubServiceLogger)LoggerQueue.SpawnLogger(LoggerName, LoggerActions.SubServiceLogger);
            this._simulationLogger.WriteLog($"READY TO BUILD NEW SIMULATION FROM {this.ExpressionsLoaded.Length} INPUT EXPRESSIONS...", LogType.WarnLog);
        }
        /// <summary>
        /// Spawns a new SimulationGenerator from a PassThru log file.
        /// </summary>
        /// <param name="PassThruLogFile">The log file to load into expressions. This MUST be a normal PassThru log!</param>
        /// <returns>A new simulation generator ready to load all content inside of the input log file</returns>
        public static PassThruSimulationGenerator LoadPassThruLogFile(string PassThruLogFile)
        {
            // Build an expressions generator and then use it to build a new simulation generator
            var ExpressionGenerator = PassThruExpressionsGenerator.LoadPassThruLogFile(PassThruLogFile);
            return new PassThruSimulationGenerator(ExpressionGenerator);
        }
        /// <summary>
        /// Spawns a new SimulationGenerator from a PassThru expressions file
        /// </summary>
        /// <param name="ExpressionsFile">The expressions file to load and convert. This MUST be an Expressions log!</param>
        /// <returns>A new simulation generator ready to load all content inside of the input expressions file</returns>
        public static PassThruSimulationGenerator LoadExpressionsFile(string ExpressionsFile)
        {
            // Build an expressions generator and then use it to build a new simulation generator
            var ExpressionGenerator = PassThruExpressionsGenerator.LoadExpressionsFile(ExpressionsFile);
            return new PassThruSimulationGenerator(ExpressionGenerator);
        }

        // ------------------------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Uses the input Expressions objects on this generator and converts them into a collection of simulation objects
        /// These are then written to a file for our simulation output
        /// </summary>
        /// <returns>The collection of built simulation channels from our log file</returns>
        public PassThruSimulationChannel[] GenerateLogSimulation()
        {
            // Start by pulling in our grouped simulation channel objects
            var GroupedExpressions = this._generateGroupedIds();

            // Build a dictionary for return output objects and log we're starting to update our values
            var SimChannelsBuilt = new Dictionary<uint, PassThruSimulationChannel>();
            this._simulationLogger.WriteLog("BUILDING CHANNEL OBJECTS FROM CHANNEL ID VALUES NOW...", LogType.WarnLog);

            // Loop all the expression sets built in parallel and generate a simulation channel for them
            int LoopsCompleted = 0; int TotalLoops = GroupedExpressions.Count;
            Parallel.ForEach(GroupedExpressions, ExpressionSet =>
            {
                try
                {
                    // Pull the Channel ID and the expression objects here and build a channel from it
                    uint SimChannelId = ExpressionSet.Key;
                    PassThruExpression[] ChannelExpressions = ExpressionSet.Value;
                    PassThruSimulationChannel BuiltChannel = ChannelExpressions.BuildChannelsFromExpressions(SimChannelId);

                    // If our channel object is not null, then store it on our output collection now
                    if (BuiltChannel != null)
                    {
                        // Lock the output collection to avoid thread issues and store the new channel
                        lock (SimChannelsBuilt)
                        {
                            // Now insert this expression object based on what keys are in the output collection of expressions
                            if (!SimChannelsBuilt.ContainsKey(SimChannelId)) SimChannelsBuilt.Add(SimChannelId, BuiltChannel);
                            else throw new InvalidDataException($"ERROR! CAN NOT APPEND A SIM CHANNEL WITH ID {SimChannelId} SINCE IT EXISTS ALREADY!");
                        }
                    }
                }
                catch (Exception BuildChannelCommandEx)
                {
                    // Log failures out and find out why the fails happen then move to our progress routine or move to next iteration
                    this._simulationLogger.WriteLog($"FAILED TO GENERATE A SIMULATION CHANNEL FROM A SET OF EXPRESSIONS!", LogType.WarnLog);
                    this._simulationLogger.WriteLog("EXCEPTION THROWN IS LOGGED BELOW", BuildChannelCommandEx, new[] { LogType.WarnLog, LogType.TraceLog });
                }

                // Invoke a progress update here if needed
                this.OnGeneratorProgress?.Invoke(this, new SimulationProgressEventArgs(LoopsCompleted++, TotalLoops));
            });

            // Log information about the simulation generation routine and exit out
            this._simulationLogger.WriteLog($"BUILT CHANNEL SIMULATION OBJECTS OK!", LogType.InfoLog);
            this._simulationLogger.WriteLog($"A TOTAL OF {SimChannelsBuilt.Count} CHANNELS HAVE BEEN BUILT!", LogType.InfoLog);
            this.SimulationChannels = SimChannelsBuilt.Values.ToArray();
            return this.SimulationChannels;
        }
        /// <summary>
        /// Takes an input set of PTSimulations and writes them to a file object desired.
        /// </summary>
        /// <param name="BaseFileName">Input file name to save our output simulation file content as</param>
        /// <param name="OutputLogFileFolder">Optional folder to store the output file in. Defaults to the injector folder</param>
        /// <returns>Path of our built simulation file</returns>
        public string SaveSimulationFile(string BaseFileName = "", string OutputLogFileFolder = null)
        {
            // First build our output location for our file.
            // string OutputFolder = ValueLoaders.GetConfigValue<string>("FulcrumInjectorConstants.InjectorLogging.DefaultExpressionsPath");
            OutputLogFileFolder ??= "C:\\Program Files (x86)\\MEAT Inc\\FulcrumShim\\FulcrumInjector\\FulcrumSimulations";
            string FinalOutputPath = Path.Combine(OutputLogFileFolder, Path.GetFileNameWithoutExtension(BaseFileName)) + ".ptSim";

            // Get a logger object for saving expression sets.
            string LoggerName = $"{Path.GetFileNameWithoutExtension(BaseFileName)}_SimulationsLogger";
            var ExpressionLogger = (SubServiceLogger)LoggerQueue.SpawnLogger(LoggerName, LoggerActions.SubServiceLogger);

            // Find output path and then build final path value.             
            Directory.CreateDirectory(OutputLogFileFolder);
            if (!Directory.Exists(Path.GetDirectoryName(FinalOutputPath))) { Directory.CreateDirectory(Path.GetDirectoryName(FinalOutputPath)); }
            ExpressionLogger.WriteLog($"BASE OUTPUT LOCATION FOR SIMULATIONS IS SEEN TO BE {Path.GetDirectoryName(FinalOutputPath)}", LogType.InfoLog);

            // Log information about the expression set and output location
            ExpressionLogger.WriteLog($"SAVING A TOTAL OF {this.SimulationChannels.Length} SIMULATION OBJECTS NOW...", LogType.InfoLog);
            ExpressionLogger.WriteLog($"EXPRESSION SET IS BEING SAVED TO OUTPUT FILE: {FinalOutputPath}", LogType.InfoLog);

            try
            {
                // Now Build output string content from each expression object.
                ExpressionLogger.WriteLog("CONVERTING TO STRINGS NOW...", LogType.WarnLog);
                Tuple<uint, PassThruSimulationChannel>[] ChannelsConstructed = this.SimulationChannels
                    .Select(SimChannel => new Tuple<uint, PassThruSimulationChannel>(SimChannel.ChannelId, SimChannel))
                    .ToArray();

                // Log information and write output.
                string OutputJsonValues = JsonConvert.SerializeObject(ChannelsConstructed, Formatting.Indented);
                ExpressionLogger.WriteLog($"CONVERTED INPUT OBJECTS INTO A JSON OUTPUT STRING OK!", LogType.WarnLog);
                ExpressionLogger.WriteLog("WRITING OUTPUT CONTENTS NOW...", LogType.WarnLog);
                File.WriteAllText(FinalOutputPath, OutputJsonValues);
                ExpressionLogger.WriteLog("DONE WRITING OUTPUT SIMULATIONS CONTENT!");

                // Check to see if we aren't in the default location. If not, store the file in both the input spot and the injector directory
                if (BaseFileName.Contains(Path.DirectorySeparatorChar) && !BaseFileName.Contains("FulcrumLogs"))
                {
                    // Find the base path, get the file name, and copy it into here.
                    string LocalDirectory = Path.GetDirectoryName(BaseFileName);
                    string CopyLocation = Path.Combine(LocalDirectory, Path.GetFileNameWithoutExtension(FinalOutputPath)) + ".ptSim";
                    File.Copy(FinalOutputPath, CopyLocation, true);

                    // Remove the Expressions Logger. Log done and return
                    ExpressionLogger.WriteLog("COPIED OUTPUT SIMULATION FILE INTO THE BASE SIMULATION FILE LOCATION!");
                }

                // Remove the Expressions Logger. Log done and return
                this.SimulationFile = FinalOutputPath;
                return FinalOutputPath;
            }
            catch (Exception WriteEx)
            {
                // Log failures. Return an empty string.
                ExpressionLogger.WriteLog("FAILED TO SAVE OUR OUTPUT EXPRESSION SETS! THIS IS FATAL!", LogType.FatalLog);
                ExpressionLogger.WriteLog("EXCEPTION FOR THIS INSTANCE IS BEING LOGGED BELOW", WriteEx);

                // Return nothing.
                return string.Empty;
            }
        }

        // ------------------------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Converts an input set of expression objects into a grouped set of expressions paired off by a Channel ID Value
        /// </summary>
        /// <returns>A collection of built expression objects paired off by channel ID values</returns>
        private Dictionary<uint, PassThruExpression[]> _generateGroupedIds()
        {
            // Build a dictionary for return output objects and log we're starting to update our values
            var GroupedExpressions = new Dictionary<uint, PassThruExpression[]>();
            this._simulationLogger.WriteLog("GROUPING COMMANDS BY CHANNEL ID VALUES NOW...", LogType.WarnLog);

            // Store all the expressions where we don't have a type defined
            var ExpressionsToParse = this.ExpressionsLoaded
                .Where(ExpObj => ExpObj.TypeOfExpression != PassThruExpressionType.NONE)
                .ToArray();

            // Group off all the commands by channel ID and then convert them to paired objects
            int LoopsCompleted = 0; int TotalLoops = ExpressionsToParse.Length;
            Parallel.ForEach(this.ExpressionsLoaded, ExpressionObject =>
            {
                // Invoke a new progress event here for the generator if needed
                this.OnGeneratorProgress?.Invoke(this, new SimulationProgressEventArgs(LoopsCompleted++, TotalLoops));

                // Find our channel ID property and store it here as a uint value to pair off with
                FieldInfo DesiredProperty = ExpressionObject
                    .GetExpressionProperties()
                    .FirstOrDefault(FieldObj => FieldObj.Name
                        .Replace(" ", string.Empty).ToUpper()
                        .Contains("ChannelID".ToUpper()));
                
                // If the property found was null, then just use zero. Otherwise parse the value of it
                uint ChannelIdValue = DesiredProperty == null ? 0 : uint.Parse(DesiredProperty.GetValue(ExpressionObject).ToString());

                // Lock our output collection here to avoid thread issues
                lock (GroupedExpressions)
                {
                    // Now insert this expression object based on what keys are in the output collection of expressions
                    if (!GroupedExpressions.ContainsKey(ChannelIdValue))
                    {
                        // Build a new tuple object to store on the output collection
                        PassThruExpression[] ExpressionsArray = { ExpressionObject };
                        GroupedExpressions.Add(ChannelIdValue, ExpressionsArray);
                    }
                    else
                    {
                        // Pull the current tuple object set, update the list value for it, and store it
                        PassThruExpression[] ExpressionsFound = GroupedExpressions[ChannelIdValue];
                        ExpressionsFound = ExpressionsFound.Append(ExpressionObject).ToArray();
                        GroupedExpressions[ChannelIdValue] = ExpressionsFound;
                    }
                }
            });

            // Before exiting out, we need to remove the key for where the ChannelID Values are 0
            if (GroupedExpressions.ContainsKey(0) && !GroupedExpressions.Remove(0))
                throw new InvalidOperationException("Error! Failed to remove key 0 from the collection of expressions!");

            // Log done grouping, return the built ID values here as a dictionary with the Expressions and ID values
            this._simulationLogger.WriteLog("BUILT GROUPED SIMULATION COMMANDS OK!", LogType.InfoLog);
            return GroupedExpressions;
        }
    }
}
