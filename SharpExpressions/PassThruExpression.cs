using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using SharpExpressions.PassThruExpressions;
using SharpLogging;

namespace SharpExpressions
{
    /// <summary>
    /// This class instance is used to help configure the Regex tools and commands needed to perform highlighting on output from
    /// the shim DLL.
    /// </summary>
    public class PassThruExpression
    {
        // Logger instance for the expression in use. This should only ever log failures
        protected readonly SharpLogger _expressionLogger;

        // String Values for Command content
        public readonly string CommandLines;
        public readonly string[] SplitCommandLines;

        // Time values for the Regex on the command.
        public readonly PassThruExpressionTypes TypeOfExpression;
        public readonly PassThruExpressionRegex TimeRegex = PassThruExpressionRegex.LoadedExpressions[PassThruExpressionTypes.CommandTime];
        public readonly PassThruExpressionRegex StatusCodeRegex = PassThruExpressionRegex.LoadedExpressions[PassThruExpressionTypes.CommandStatus];

        // Input command time and result values for regex searching.
        [PassThruProperty("Time Issued", "", new[] { "Timestamp Valid", "Invalid Timestamp" })]
        public readonly string ExecutionTime;
        [PassThruProperty("J2534 Status", "0:STATUS_NOERROR", new[] { "Command Passed", "Command Failed" })]
        public readonly string JStatusCode;

        // Public facing property telling us if the expression passed or not
        public bool ExpressionPassed
        {
            get
            {
                // Find all the fields objects we need to use for checking the results
                var ResultFieldInfos = this.GetType()
                    .GetFields(BindingFlags.NonPublic | BindingFlags.Instance)
                    .Where(FieldObj => FieldObj.FieldType != typeof(SharpLogger))
                    .ToArray();

                // Compare the results pulled in against the desired default values for the expressions
                foreach (var FieldObj in ResultFieldInfos)
                {
                    // Pull the ResultAttribute object.
                    var CurrentValue = FieldObj.GetValue(this).ToString().Trim();
                    var ResultAttribute = (PassThruPropertyAttribute)FieldObj.GetCustomAttributes(typeof(PassThruPropertyAttribute)).FirstOrDefault();

                    // Check if our value is valid now
                    bool ReturnState = ResultAttribute != null && ResultAttribute.ResultState(CurrentValue) == ResultAttribute.ResultValue;
                    if (!ReturnState) return false;
                }

                // Return passed at this point since all values worked out correctly
                return true;
            }
        }

        // ------------------------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// ToString override to show the strings of the command here.
        /// </summary>
        /// <returns>String formatted table of the output.</returns>
        public override string ToString()
        {
            // Find Field object values here.
            var ResultFieldInfos = this.GetType()
                .GetMembers(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                .Where(MemberObj => MemberObj.GetCustomAttribute(typeof(PassThruPropertyAttribute)) != null)
                .ToArray();

            // Build default Tuple LIst value set and apply new values into it from property attributes
            var RegexResultTuples = new List<Tuple<string, string, string>>() {
                new("J2534 Command", this.TypeOfExpression.ToString(), this.ExpressionPassed ? "Parse Passed" : "Parse Failed")
            };

            // Now find ones with the attribute and pull value
            RegexResultTuples.AddRange(ResultFieldInfos.Select(MemberObj =>
            {
                // Pull the ResultAttribute object.
                FieldInfo InvokerField = (FieldInfo)MemberObj;
                string CurrentValue = InvokerField.GetValue(this).ToString().Trim();

                // Trim the length of the string for our output here. If the values are larger than 60 chars across.
                // Show they are being truncated here as well
                if (CurrentValue.Length >= 60) CurrentValue = CurrentValue.Substring(0, 49) + " (Truncated)";

                // Now cast the result attribute of the member and store the value of it.
                var ResultValue = (PassThruPropertyAttribute)MemberObj
                    .GetCustomAttributes(typeof(PassThruPropertyAttribute))
                    .FirstOrDefault();

                // Build our output tuple object here. Compare current value to the desired one and return a state value.
                return new Tuple<string, string, string>(ResultValue.ResultName, CurrentValue, ResultValue.ResultState(CurrentValue));
            }).ToArray());

            // Build a text table object here.
            string RegexValuesOutputString = RegexResultTuples.ToStringTable(
                new[] { "Value Name", "Determined Value", "Value Status" },
                RegexObj => RegexObj.Item1,
                RegexObj => RegexObj.Item2,
                RegexObj => RegexObj.Item3
            );

            // Split lines, build some splitting strings, and return output.
            string SplitString = string.Join("", Enumerable.Repeat("=", 100));
            string[] SplitTable = RegexValuesOutputString.Split('\n')
                .Select(StringObj => "   " + StringObj.Trim())
                .ToArray();

            // Store string to replace and build new list of strings
            var NewLines = new List<string>() { SplitString, "\r" };
            foreach (string CommandLine in this.SplitCommandLines)
            {
                // Clean out starting newlines from commands if needed
                string CleanedCommandLine = CommandLine;
                if (CleanedCommandLine.StartsWith("\n")) CleanedCommandLine = CommandLine.Substring(1);

                // If we're looking at a command line, make sure we tab it over accordingly
                string[] SplitCommandLine = CleanedCommandLine.Replace("  ", " ").Split(' ').ToArray();
                bool IsCommandData = !CleanedCommandLine.Contains("\\__") && SplitCommandLine.All(BytePart => BytePart.Length == 2);
                NewLines.Add((IsCommandData ? "\t   " : "   ") + CleanedCommandLine);
            }
            
            // NOTE: Removed to fix formatting for output content
            // NewLines.Add("\n");

            // Add our breakdown contents here.
            NewLines.Add(SplitTable[0]);
            NewLines.AddRange(SplitTable.Skip(1).Take(SplitTable.Length - 2));
            NewLines.Add(SplitTable.FirstOrDefault()); NewLines.Add("\n");

            // Check the type of this object. If it matches the types with extra content then build the values for it now.
            if (this.GetType() == typeof(PassThruReadMessagesExpression) || this.GetType() == typeof(PassThruWriteMessagesExpression))
            {
                // Log information, pull in new split table contents
                NewLines.AddRange(this.FindMessageContents(out _)
                    .Split('\n')
                    .Where(LineObj => !string.IsNullOrEmpty(LineObj))
                    .Select(LineObj => "   " + LineObj)
                    .Append("\n")
                    .ToArray());

                // Log added new content
                // this._expressionLogger.WriteLog("PULLED IN NEW MESSAGES CONTENTS CORRECTLY!", LogType.InfoLog);
            }
            if (this.GetType() == typeof(PassThruStartMessageFilterExpression))
            {
                // Append the new values for the messages into our output strings now.
                NewLines.AddRange(this.FindFilterContents(out _)
                    .Split('\n')
                    .Where(LineObj => !string.IsNullOrEmpty(LineObj))
                    .Select(LineObj => "   " + LineObj)
                    .Append("\n")
                    .ToArray());

                // Log added new content
                // this._expressionLogger.WriteLog("PULLED IN NEW MESSAGES FOR FILTER CONTENTS CORRECTLY!", LogType.InfoLog);
            }
            if (this.GetType() == typeof(PassThruIoctlExpression))
            {
                // Append the new values for the Ioctl values into our output strings now.
                NewLines.AddRange(this.FindIoctlParameters(out _)
                    .Split('\n')
                    .Where(LineObj => !string.IsNullOrEmpty(LineObj))
                    .Select(LineObj => "   " + LineObj)
                    .Append("\n")
                    .ToArray());

                // Log added new content
                // this._expressionLogger.WriteLog("PULLED IN NEW IOCTL VALUES FOR COMMAND CONTENTS CORRECTLY!", LogType.InfoLog);
            }

            // Remove double newlines. Command lines are split with \r so this doesn't apply.
            NewLines.Add(SplitString);
            RegexValuesOutputString = string.Join("\n", NewLines).Replace("\n\n", "\n");
            return RegexValuesOutputString;
        }

        // ------------------------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// A Default constructor for the PassThruExpression object type.
        /// This is used to spawn in a default/null value for our expression object instances
        /// </summary>
        public PassThruExpression()
        {
            // Store the none type for our expression and exit out
            this.TypeOfExpression = PassThruExpressionTypes.NONE;
            this._expressionLogger = 
                SharpLogBroker.FindLoggers("PassThruExpressionLogger").FirstOrDefault() 
                ?? new SharpLogger(LoggerActions.UniversalLogger, "PassThruExpressionLogger");
        }
        /// <summary>
        /// Builds a new set of PassThruCommand Regex Operations
        /// </summary>
        /// <param name="CommandInput">Input command string</param>
        public PassThruExpression(string CommandInput, PassThruExpressionTypes ExpressionType)
        {
            // Store input lines
            this.CommandLines = CommandInput;
            this.TypeOfExpression = ExpressionType;
            this.SplitCommandLines = CommandInput.Split('\r');

            // TODO: Determine if we should actually do one logger per command type
            // this._expressionLogger =
            //     SharpLogBroker.FindLoggers($"{this.TypeOfExpression.ToDescriptionString()}Logger").FirstOrDefault()
            //     ?? new SharpLogger(LoggerActions.UniversalLogger, $"{this.TypeOfExpression.ToDescriptionString()}Logger");

            // Build a new Expression logger for this command type
            this._expressionLogger = 
                SharpLogBroker.FindLoggers("PassThruExpressionLogger").FirstOrDefault()
                ?? new SharpLogger(LoggerActions.UniversalLogger, "PassThruExpressionLogger");

            // Find command issue request values. (Pull using Base Class)
            this.TimeRegex.Evaluate(CommandInput, out var TimeStrings);
            var FieldsToSet = this.GetExpressionProperties(true);
            if (!this.StatusCodeRegex.Evaluate(CommandInput, out var StatusCodeStrings))
            {
                // BUG: Defaulting to including new default status code string values when this parse fails. This is because a lot of commands don't have a 
                // Try and find the end of the command in a different way
                // this._expressionLogger.WriteLog($"FAILED TO REGEX OPERATE ON ONE OR MORE TYPES FOR EXPRESSION TYPE {this.GetType().Name}!");
                StatusCodeStrings = new[]
                {
                    $"{TimeStrings[2]} 0:STATUS_NOERROR",
                    $"{TimeStrings[2]}",
                    "0:STATUS_NOERROR"
                };
            }

            // Find our values to store here and add them to our list of values.
            List<string> StringsToApply = new List<string>();
            StringsToApply.AddRange(this.TimeRegex.ExpressionValueGroups
                .Where(NextIndex => NextIndex <= TimeStrings.Length)
                .Select(NextIndex => TimeStrings[NextIndex]));
            StringsToApply.AddRange(this.StatusCodeRegex.ExpressionValueGroups
                .Where(NextIndex => NextIndex <= StatusCodeStrings.Length)
                .Select(NextIndex => StatusCodeStrings[NextIndex]));

            // Now apply values using base method and exit out of this routine
            bool StorePassed = this.SetExpressionProperties(FieldsToSet, StringsToApply.ToArray());
            if (!StorePassed) throw new InvalidOperationException("FAILED TO SET BASE CLASS VALUES FOR EXPRESSION OBJECT!");
        }

        // ------------------------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Gets the list of properties linked to a regex group and returns them in order of decleration
        /// </summary>
        public FieldInfo[] GetExpressionProperties(bool BaseClassValues = false)
        {
            // Determine the type of base property to use
            var DeclaredTypeExpected = BaseClassValues ?
                typeof(PassThruExpression) : this.GetType();

            // Pull our property values here.
            var PropertiesLocated = this.GetType()
                .GetFields().Where(FieldObj => FieldObj.DeclaringType == DeclaredTypeExpected)
                .Where(PropObj => Attribute.IsDefined(PropObj, typeof(PassThruPropertyAttribute)))
                .OrderBy(PropObj => ((PassThruPropertyAttribute)PropObj.GetCustomAttributes(typeof(PassThruPropertyAttribute), false).Single()).LineNumber)
                .ToArray();

            // Return them here.
            return PropertiesLocated;
        }
        /// <summary>
        /// Sets the values of the output regex strings onto this class object ptExpression values
        /// </summary>
        /// <param name="FieldValueStrings">Strings to store</param>
        /// <param name="FieldObjects">Property infos</param>
        /// <returns>True if set. False if not</returns>
        protected internal bool SetExpressionProperties(FieldInfo[] FieldObjects, string[] FieldValueStrings)
        {
            // Make sure the count of properties matches the count of lines.
            if (FieldValueStrings.Length != FieldObjects.Length) {
                this._expressionLogger.WriteLog("EXPRESSIONS FOR FIELDS AND VALUES ARE NOT EQUAL SIZES! THIS IS FATAL!", LogType.FatalLog);
                this._expressionLogger.WriteLog($"INPUT PASSTHRU LOG LINES ARE BEING LOGGED BELOW\n\t{this.CommandLines}");
                return false;
            }

            // Loop the field objects and apply a new value one by one.
            for (int FieldIndex = 0; FieldIndex < FieldObjects.Length; FieldIndex++)
            {
                // Pull field value. Try and set it.
                var CurrentField = FieldObjects[FieldIndex];
                try { CurrentField.SetValue(this, FieldValueStrings[FieldIndex]); }
                catch (Exception SetEx)
                {
                    // Throw an exception output for this error type.
                    this._expressionLogger.WriteLog($"EXCEPTION THROWN DURING EXPRESSION VALUE STORE FOR COMMAND TYPE {this.GetType().Name}!", LogType.ErrorLog);
                    this._expressionLogger.WriteLog($"INPUT PASSTHRU LOG LINES ARE BEING LOGGED BELOW\n\t{this.CommandLines}");
                    this._expressionLogger.WriteException("EXCEPTION IS BEING LOGGED BELOW", SetEx);
                    return false;
                }
            }

            // Log passed, return output.
            // this._expressionLogger.WriteLog($"UPDATED EXPRESSION VALUES FOR A TYPE OF {this.GetType().Name} OK!");
            return true;
        }
    }
}