using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text.RegularExpressions;

namespace SharpExpressions.PassThruRegex
{
    /// <summary>
    ///  Model object for imported regular expression values for a command part
    /// </summary>
    public class PassThruRegex
    {
        #region Custom Events
        #endregion // Custom Events

        #region Fields
        
        // Private static collection of PassThruRegex objects that represent all built expression values
        private static PassThruRegex[] _passThruRegexStore;

        #endregion // Fields

        #region Properties

        // Public facing collection of PassThruRegex objects that we can query for expression objects by name
        public static ObservableCollection<PassThruRegexModel> PassThruRegexStore
        {
            get
            {
                // Check if this value has been configured yet or not.
                _passThruRegexStore ??= GeneratePassThruRegexModels();
                return _passThruRegexStore;
            }
        }

        // Public facing properties for the regex object.
        public string ExpressionName { get; set; }
        public string ExpressionPattern { get; set; }
        public int[] ExpressionValueGroups { get; set; }
        public PassThruCommandType ExpressionType { get; set; }

        #endregion // Properties

        #region Structs and Classes
        #endregion // Structs and Classes

        // ------------------------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Json constuctor for this object type
        /// </summary>
        [JsonConstructor]
        public PassThruRegex() {  }
        /// <summary>
        /// Makes a new regex model object from the input values given
        /// </summary>
        /// <param name="ExpressionName"></param>
        /// <param name="ExpressionPattern"></param>
        /// <param name="ExpressionGroups"></param>
        public PassThruRegex(string ExpressionName, string ExpressionPattern, PassThruCommandType ExpressionType = PassThruCommandType.NONE, int ExpressionGroup = 0)
        {
            // Store model object values here.
            this.ExpressionName = ExpressionName;
            this.ExpressionType = ExpressionType;
            this.ExpressionPattern = ExpressionPattern;
            this.ExpressionValueGroups = new int[] { ExpressionGroup };
        }
        /// <summary>
        /// Makes a new regex model object from the input values given
        /// </summary>
        /// <param name="ExpressionName"></param>
        /// <param name="ExpressionPattern"></param>
        /// <param name="ExpressionGroups"></param>
        public PassThruRegex(string ExpressionName, string ExpressionPattern, PassThruCommandType ExpressionType = PassThruCommandType.NONE, int[] ExpressionGroups = null)
        {
            // Store model object values here.
            this.ExpressionName = ExpressionName;
            this.ExpressionType = ExpressionType;
            this.ExpressionPattern = ExpressionPattern;
            this.ExpressionValueGroups = ExpressionGroups ?? new int[] { 0 };
        }

        // ------------------------------------------------------------------------------------------------------------------------------------------
        
        /// <summary>
        /// Processes the input line content and parses it for the regex we passed in.
        /// </summary>
        /// <param name="InputLines">Lines to check</param>
        /// <returns>Value matched.</returns>
        public bool Evaluate(string InputLines, out string[] ResultStrings)
        {
            // Build a regex, find our results.
            var MatchResults = new Regex(this.ExpressionPattern).Match(InputLines);

            // If failed, return an empty string. If all groups, return here too.
            if (!MatchResults.Success) {
                ResultStrings = new[] { "REGEX_FAILED" };
                return false;
            }

            // If no groups given, return full match
            if (this.ExpressionValueGroups.All(IndexObj => IndexObj == 0)) {
                ResultStrings = new[] { MatchResults.Value }; return true;
            }

            // Loop our pulled values out and store them
            List<string> PulledValues = new List<string>();
            for (int GroupIndex = 0; GroupIndex < MatchResults.Groups.Count; GroupIndex++) { 
                PulledValues.Add(MatchResults.Groups[GroupIndex].Value.Trim());
            }

            // Build output and return it.
            ResultStrings = PulledValues.ToArray();
            return true;
        }

        // ------------------------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Loads in all the PassThru regex strings and stores them in the static regex store.
        /// Configures a new set of  objects.
        /// </summary>
        /// <returns></returns>
        public static PassThruRegex[] GeneratePassThruRegexModels()
        {
            // Pull the objects from the settings store that relate to our expressions and then build an object from them.
            RegexStoreLogger.WriteLog($"REBUILDING STORE VALUES FOR INJECTOR REGEX COMMAND OBJECTS NOW...", LogType.WarnLog);
            var RegexModelArray = FulcrumSettingsShare.InjectorRegexSettings.SettingsEntries.Select(SettingObj =>
            {
                // Find our group binding values here.
                var GroupValueMatch = Regex.Match(SettingObj.SettingValue.ToString(), @"\*GROUPS_\(((\d+|\d,)+)\)\*");
                var SettingGroups = GroupValueMatch.Success ? GroupValueMatch.Groups[1].Value : "0";

                // Now build an array of int values for groups.
                int[] ArrayOfGroups;
                try
                {
                    // Try to parse out values here. If Failed default to all
                    if (!SettingGroups.Contains(",")) { ArrayOfGroups = new[] { int.Parse(SettingGroups) }; }
                    else
                    {
                        // Split content out, parse values, and return.
                        var SplitGroupValues = SettingGroups.Split(',');
                        ArrayOfGroups = SplitGroupValues.Select(int.Parse).ToArray();
                    }
                }
                catch { ArrayOfGroups = new[] { 0 }; }

                // Build our new object for the model of regex now.
                var SettingNameSplit = SettingObj.SettingName.Split(' ').ToArray();
                var RegexName = string.Join("", SettingNameSplit.Where(StringObj => !StringObj.Contains("Regex")))
                    .Trim();
                Enum.TryParse(RegexName.Replace("PassThru", "PT"), out PassThruCommandType ExpressionType);
                var RegexPattern = SettingObj.SettingValue.ToString().Replace(GroupValueMatch.Value, string.Empty)
                    .Trim();

                // Return our new output object here.
                return new PassThruRegexModel(
                    RegexName,          // Name of command. Just the input setting with no spaces
                    RegexPattern,       // Pattern used during regex operations (No group value)
                    ExpressionType,     // Type of expression. Defined for PTCommands or none for base
                    ArrayOfGroups       // Index set of groups to use
                );
            }).ToArray();

            // Store new values and move onto selection
            RegexStoreLogger.WriteLog($"BUILT A TOTAL OF {RegexModelArray.Length} OUTPUT OBJECTS!", LogType.InfoLog);
            _passThruRegexStore = new ObservableCollection<PassThruRegexModel>(RegexModelArray);
            return PassThruRegexStore;
        }
        /// <summary>
        /// Finds a PTRegex Model for the given PTCommand type
        /// </summary>
        /// <param name="InputCommandType">Type to return</param>
        /// <returns>Regex model for this PT Type if passed, or null if nothing found.</returns>
        public static PassThruRegexModel GetRegexForCommand(PassThruCommandType InputCommandType)
        {
            // Find the command instance by name. Replace the input enum with a string and return.
            return PassThruRegexStore.GetRegexByName(InputCommandType.ToString());
        }

    }
}
