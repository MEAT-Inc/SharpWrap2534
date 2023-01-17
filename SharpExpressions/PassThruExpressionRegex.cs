using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
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
        private static PassThruExpressionRegex[] _expressionsLoaded;

        #endregion // Fields

        #region Properties

        // Public facing collection of PassThruRegex objects that we can query for expression objects by name
        public static PassThruExpressionRegex[] ExpressionsLoaded
        {
            get
            {
                // Check if this value has been configured yet or not.
                _expressionsLoaded ??= _generateRegexModels();
                return _expressionsLoaded;
            }
        }

        // Public facing properties for the regex object.
        public string ExpressionName { get; set; }
        public string ExpressionPattern { get; set; }
        public int[] ExpressionValueGroups { get; set; }
        public PassThruExpressionType ExpressionType { get; set; }

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
        internal PassThruExpressionRegex(string ExpressionName, string ExpressionPattern, PassThruExpressionType ExpressionType = PassThruExpressionType.NONE, int ExpressionGroup = 0)
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
        internal PassThruExpressionRegex(string ExpressionName, string ExpressionPattern, PassThruExpressionType ExpressionType = PassThruExpressionType.NONE, int[] ExpressionGroups = null)
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
            if (!MatchResults.Success)
            {
                ResultStrings = new[] { "REGEX_FAILED" };
                return false;
            }

            // If no groups given, return full match
            if (this.ExpressionValueGroups.All(IndexObj => IndexObj == 0))
            {
                ResultStrings = new[] { MatchResults.Value }; return true;
            }

            // Loop our pulled values out and store them
            List<string> PulledValues = new List<string>();
            for (int GroupIndex = 0; GroupIndex < MatchResults.Groups.Count; GroupIndex++)
            {
                PulledValues.Add(MatchResults.Groups[GroupIndex].Value.Trim());
            }

            // Build output and return it.
            ResultStrings = PulledValues.ToArray();
            return true;
        }

        // ------------------------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Finds a PTRegex Model for the given PTCommand type
        /// </summary>
        /// <param name="InputExpressionType">Type to return</param>
        /// <returns>Regex model for this PT Type if passed, or null if nothing found.</returns>
        public static PassThruExpressionRegex GetRegexByType(PassThruExpressionType InputExpressionType)
        {
            // Find the command instance by name. Replace the input enum with a string and return.
            return _expressionsLoaded.FirstOrDefault(RegexObj => RegexObj.ExpressionType == InputExpressionType);
        }

        /// <summary>
        /// Loads in all the PassThru regex strings and stores them in the static regex store.
        /// This routine will load our Regex string values from the packaged JSON Configuration file.
        /// We can override this or change these values using any JSON helper class type
        /// </summary>
        /// <returns>A built collection of all the RegexModels built for the contents of our input JSON configuration file</returns>
        public static PassThruExpressionRegex[] _generateRegexModels()
        {
            // Load in all the regex values found and convert them all into regex objects to use for parsing
            JArray LoadedRegexArray = JArray.FromObject(_allocateResource("ExpressionRegexValues.json", "ExpressionRegexValues"));
            foreach (var RegexJToken in LoadedRegexArray)
            {
                // Store the name of the regex and get the pattern for it now
                string RegexName = RegexJToken["RegexName"].ToString();
                string RegexValue = RegexJToken["RegexPattern"].ToString();
                Match RegexGroups = Regex.Match(RegexValue, @"\*GROUPS_\(([^\)]+)\)\*");

                // Parse out the group values and and the pattern itself. Then build a new regex model object
                string FullGroupsString = RegexGroups.Groups[1].Value;
                int[] GroupValues = FullGroupsString.Split(',').Select(int.Parse).ToArray();
                string RegexPattern = RegexGroups.Groups[0].Value.Replace(FullGroupsString, string.Empty).Trim();
                string ExpressionTypeString = RegexName
                    .Replace("Regex", string.Empty)
                    .Replace(" ", string.Empty);

                // Now build the expression type string value to pull in an enum type for the regex
                Enum.TryParse(ExpressionTypeString, out PassThruExpressionType ExpressionType);
            }

            return _expressionsLoaded;
        }
        /// <summary>
        /// Pulls a new resource from a given file name
        /// </summary>
        /// <param name="ResourceFileName">Name of the file</param>
        /// <param name="ObjectName">Object name</param>
        /// <returns></returns>
        private static object _allocateResource(string ResourceFileName, string ObjectName = null)
        {
            // Get the current Assembly
            var CurrentAssy = Assembly.GetExecutingAssembly();
            var AssyResc = CurrentAssy.GetManifestResourceNames().Single(RescName => RescName.Contains(ResourceFileName));
            using (Stream RescStream = CurrentAssy.GetManifestResourceStream(AssyResc))
            using (StreamReader RescReader = new StreamReader(RescStream))
            {
                // Build basic object and then return it to be pulled from
                string RescFileContent = RescReader.ReadToEnd();
                JObject RescObject = JObject.Parse(RescReader.ReadToEnd());
                return ObjectName == null ? RescObject : RescObject[ObjectName];
            }
        }
    }
}
