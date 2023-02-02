using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.AccessControl;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace SharpExpressions
{
    /// <summary>
    ///  Model object for imported regular expression values for a command part
    /// </summary>
    public class PassThruExpressionRegex
    {
        #region Custom Events
        #endregion // Custom Events

        #region Fields
        
        // Private static collection of PassThruRegex objects that represent all built expression values
        private static Dictionary<PassThruExpressionType, PassThruExpressionRegex> _loadedExpressions;

        #endregion // Fields

        #region Properties

        // Public facing collection of PassThruRegex objects that we can query for expression objects by name
        public static Dictionary<PassThruExpressionType, PassThruExpressionRegex> LoadedExpressions => _loadedExpressions ??= _generateRegexModels();

        // Public facing properties for the regex object.
        public string ExpressionName { get; set; }
        public string ExpressionPattern { get; set; }
        public int[] ExpressionValueGroups { get; set; }
        public PassThruExpressionType ExpressionType { get; set; }

        // Regex object built from the provided input pattern
        public Regex ExpressionRegex => new Regex(this.ExpressionPattern.Trim(), RegexOptions.Compiled);

        #endregion // Properties

        #region Structs and Classes
        #endregion // Structs and Classes

        // ------------------------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// JSON constructor for this object type
        /// </summary>
        [JsonConstructor]
        internal PassThruExpressionRegex() {  }
        /// <summary>
        /// Makes a new regex model object from the input values given
        /// </summary>
        /// <param name="ExpressionName"></param>
        /// <param name="ExpressionPattern"></param>
        /// <param name="ExpressionGroups"></param>
        internal PassThruExpressionRegex(string ExpressionName, string ExpressionPattern, PassThruExpressionType ExpressionType = PassThruExpressionType.NONE, params int[] ExpressionGroups)
        {
            // Store the values for the new regex model on this instance and exit out
            this.ExpressionName = ExpressionName;
            this.ExpressionType = ExpressionType;
            this.ExpressionPattern = ExpressionPattern;
            this.ExpressionValueGroups = ExpressionGroups;

            // Store this regex object on our dictionary of built regex models now
            _loadedExpressions ??= new Dictionary<PassThruExpressionType, PassThruExpressionRegex>();
            if (_loadedExpressions.ContainsKey(ExpressionType)) _loadedExpressions[ExpressionType] = this;
            else _loadedExpressions.Add(ExpressionType, this);
        }

        /// <summary>
        /// Loads in all the PassThru regex strings and stores them in the static regex store.
        /// This routine will load our Regex string values from the packaged JSON Configuration file.
        /// We can override this or change these values using any JSON helper class type
        /// </summary>
        /// <returns>A built collection of all the RegexModels built for the contents of our input JSON configuration file</returns>
        private static Dictionary<PassThruExpressionType, PassThruExpressionRegex> _generateRegexModels()
        {
            // Load in all the regex values found and convert them all into regex objects to use for parsing
            JObject LoadedJsonObject;
            var CurrentAssy = Assembly.GetExecutingAssembly();
            var AssyResc = CurrentAssy.GetManifestResourceNames()
                .Single(RescName => RescName.Contains("ExpressionRegexValues.json"));
            using (Stream RescStream = CurrentAssy.GetManifestResourceStream(AssyResc))
            using (StreamReader RescReader = new StreamReader(RescStream))
                LoadedJsonObject = JObject.Parse(RescReader.ReadToEnd());

            // Iterate through all the objects found in the JArray of settings values and store them all on our share object
            JArray LoadedRegexArray = JArray.FromObject(LoadedJsonObject["ExpressionRegexValues"]);
            foreach (var RegexJToken in LoadedRegexArray)
            {
                // Store the name of the regex and get the pattern for it now
                string RegexName = RegexJToken["RegexName"].ToString();
                string RegexValue = RegexJToken["RegexPattern"].ToString();
                Match RegexGroups = Regex.Match(RegexValue, @"\*GROUPS_\(([^\)]+)\)\*");

                // Parse out the group values and and the pattern itself. Then build a new regex model object
                string FullGroupsString = RegexGroups.Groups[1].Value;
                int[] GroupValues = FullGroupsString.Split(',').Select(int.Parse).ToArray();
                string RegexPattern = $"{RegexValue.Replace(RegexGroups.Value, string.Empty)}".Trim();
                string ExpressionTypeString = RegexName.Replace("Regex", string.Empty).Replace(" ", string.Empty);

                // Now build the expression type string value to pull in an enum type for the regex and store it on our dictionary
                Enum.TryParse(ExpressionTypeString, out PassThruExpressionType ExpressionType);
                var BuiltExpression = new PassThruExpressionRegex(RegexName, RegexPattern, ExpressionType, GroupValues);
                if (!_loadedExpressions.ContainsValue(BuiltExpression))
                    throw new DataException($"Error! Failed to build a new regex model for type {ExpressionType}!");
            }

            // Return the built output dictionary of Regex models and types defined
            return _loadedExpressions;
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
    }
}
