using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using SharpExpressions.PassThruExpressions;
using SharpLogging;

namespace SharpExpressions
{
    /// <summary>
    /// Extensions for parsing out commands into new types of output for PT Regex Classes
    /// </summary>
    public static class PassThruExpressionExtensions
    {
        #region Custom Events
        #endregion //Custom Events

        #region Fields

        // Logger Object for the expressions extension methods. This should again only be run for exceptions
        private static readonly SharpLogger _expExtLogger;

        #endregion //Fields

        #region Properties
        #endregion //Properties

        #region Structs and Classes
        #endregion //Structs and Classes

        // ------------------------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// CTOR for this static class which is used to help configured our logging instance
        /// </summary>
        static PassThruExpressionExtensions()
        {
            // Spawn in our logger instance now
            _expExtLogger = new SharpLogger(LoggerActions.UniversalLogger);
            _expExtLogger.WriteLog("SIMULATION CHANNEL EXTENSION LOGGER HAS BEEN SPAWNED!", LogType.InfoLog);
        }

        // ------------------------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Pulls out all of our message content values and stores them into a list with details.
        /// </summary>
        public static string FindMessageContents(this PassThruExpression ExpressionObject, out List<string[]> MessageProperties)
        {
            // Check if not read or write types. 
            if (ExpressionObject.GetType() != typeof(PassThruReadMessagesExpression) && ExpressionObject.GetType() != typeof(PassThruWriteMessagesExpression))
            {
                _expExtLogger.WriteLog("CAN NOT USE THIS METHOD ON A NON READ OR WRITE COMMAND TYPE!", LogType.ErrorLog);
                MessageProperties = new List<string[]>();
                return string.Empty;
            }

            // Pull the object, find our matches based on our type object value.
            var MessageContentRegex = ExpressionObject.GetType() == typeof(PassThruReadMessagesExpression)
                ? PassThruExpressionRegex.LoadedExpressions[PassThruExpressionTypes.MessageReadInfo]
                : PassThruExpressionRegex.LoadedExpressions[PassThruExpressionTypes.MessageSentInfo];

            // Make our value lookup table here and output tuples
            bool IsReadExpression = ExpressionObject.GetType() == typeof(PassThruReadMessagesExpression);
            List<string> ResultStringTable = new List<string>() { "Message Number" };

            // Fill in strings for property type values here.
            if (IsReadExpression) ResultStringTable.AddRange(new[] { "TimeStamp", "Protocol ID", "Data Count", "RX Flags", "Flag Value", "Message Data" });
            else ResultStringTable.AddRange(new[] { "Protocol ID", "Data Count", "TX Flags", "Flag Value", "Message Data" });

            // Split input command lines by the "Msg[x]" identifier and then regex match all of the outputs.
            string[] SplitMessageLines = ExpressionObject.CommandLines.Split(new[] { "Msg" }, StringSplitOptions.None)
                .Where(LineObj => LineObj.StartsWith("["))
                .Select(LineObj => "Msg" + LineObj)
                .ToArray();

            // If no messages are found during the split process, then we need to return out.
            if (SplitMessageLines.Length == 0)
            {
                MessageProperties = new List<string[]>();
                return "No Messages Found!";
            }

            // Now run each of them thru here.
            MessageProperties = new List<string[]>();
            List<string> OutputMessages = new List<string>();
            foreach (var MsgLineSet in SplitMessageLines)
            {
                // Clean out spaces from the input lines
                string CleanedMsgSet = MsgLineSet
                    .Replace("\r        ", " ")
                    .Replace("\r", string.Empty);
                
                // RegexMatch output here.
                var RegexResultTuples = new List<Tuple<string, string>>();
                bool MatchedContent = MessageContentRegex.Evaluate(CleanedMsgSet, out var MatchedMessageStrings);
                if (!MatchedContent) continue;

                // Make sure the value for Flags is not zero. If it is, then we need to insert a "No Value" object
                var TempList = MatchedMessageStrings.ToList();
                int IndexOfZeroFlags = TempList.FindLastIndex(StringObj =>
                    StringObj.Contains("RxS=00000000") ||
                    StringObj.Contains("TxF=00000000"));
                if (IndexOfZeroFlags != -1) { TempList[IndexOfZeroFlags + 1] = "No Flag Value"; }
                MatchedMessageStrings = TempList.ToArray();

                // Remove any and all whitespace values from our output content here.
                string[] SelectedStrings = MatchedMessageStrings
                    .Skip(1)
                    .Where(StringObj => !string.IsNullOrEmpty(StringObj))
                    .ToArray();

                // Try and replace the double spaced comms in the CarDAQ Log into single spaces
                int LastStringIndex = SelectedStrings.Length - 1;
                SelectedStrings[SelectedStrings.Length - 1] = SelectedStrings[SelectedStrings.Length - 1]
                    .Replace('\r', ' ')
                    .Replace('\n', ' ')
                    .Replace(" ", string.Empty);

                // Fix for when message contents span more than one line.
                SelectedStrings[SelectedStrings.Length - 1] =
                    string.Join(" ", Enumerable.Range(0, SelectedStrings[SelectedStrings.Length - 1].Length / 2)
                        .Select(strIndex => SelectedStrings[SelectedStrings.Length - 1].Substring(strIndex * 2, 2)));

                // Fix for framepad
                if (SelectedStrings[SelectedStrings.Length - 1].Contains("["))
                    SelectedStrings[SelectedStrings.Length - 1] = SelectedStrings[SelectedStrings.Length - 1].Replace(" ", "");

                // Force upper case on the data string values.
                SelectedStrings[SelectedStrings.Length - 1] = SelectedStrings[SelectedStrings.Length - 1].ToUpper();

                // Format our message data content to include a 0x before the data byte and caps lock message bytes.
                string MessageData = SelectedStrings[LastStringIndex];
                string[] SplitMessageData = MessageData.Split(' ');
                string RebuiltMessageData = string.Join(" ", SplitMessageData.Select(StringPart => $"0x{StringPart.Trim().ToUpper()}"));
                SelectedStrings[LastStringIndex] = RebuiltMessageData;

                // Now loop each part of the matched content and add values into our output tuple set.
                RegexResultTuples.AddRange(SelectedStrings
                    .Select((T, StringIndex) => new Tuple<string, string>(ResultStringTable[StringIndex], T)));

                // Build our output table once all our values have been appended in here.
                string RegexValuesOutputString = RegexResultTuples.ToStringTable(
                    new[] { "Message Property", "Message Value" },
                    RegexObj => RegexObj.Item1,
                    RegexObj => RegexObj.Item2
                );

                // Add this string to our list of messages.
                OutputMessages.Add(RegexValuesOutputString);
                MessageProperties.Add(RegexResultTuples.Select(TupleObj => TupleObj.Item2).ToArray());
            }

            // Return built table string object.
            return string.Join("\n", OutputMessages);
        }
        /// <summary>
        /// Pulls out the filter contents of this command as messages and pulls them back. One entry per filter property
        /// If we have a Flow filter it's 3 lines. All others would be 2 line .
        /// </summary>
        /// <param name="FilterProperties">Properties of filter pulled</param>
        /// <returns>Text String table for filter messages.</returns>
        public static string FindFilterContents(this PassThruExpression ExpressionObject, out List<string[]> FilterProperties)
        {
            // Check if we can use this method or not.
            if (ExpressionObject.GetType() != typeof(PassThruStartMessageFilterExpression))
            {
                _expExtLogger.WriteLog("CAN NOT USE THIS METHOD ON A NON PTSTART FILTER COMMAND TYPE!", LogType.ErrorLog);
                FilterProperties = new List<string[]>();
                return string.Empty;
            }

            // Make our value lookup table here and output tuples.
            List<string> ResultStringTable = new List<string>()
            {
                "Message Type",     // Mask Pattern or Flow
                "Message Number",   // Always 0
                "Protocol ID",      // Protocol Of Message
                "Message Size",     // Size of message
                "TX Flags",         // Tx Flags
                "Flag Value",       // String Flag Value
                "Message Content"   // Content of the filter message
            };

            // Split input command lines by the "Msg[x]" identifier and then regex match all of the outputs.
            List<string> CombinedOutputs = new List<string>();
            string[] SplitMessageLines = Regex.Split(ExpressionObject.CommandLines, @"\s+(Mask|Pattern|FlowControl)").Skip(1).ToArray();
            for (int LineIndex = 0; LineIndex < SplitMessageLines.Length; LineIndex++)
            {
                // Append based on line value input here.
                CombinedOutputs.Add(LineIndex + 1 >= SplitMessageLines.Length
                    ? SplitMessageLines[LineIndex]
                    : string.Join(string.Empty, SplitMessageLines.Skip(LineIndex).Take(2)));

                // Check index value.
                if (LineIndex + 1 >= SplitMessageLines.Length) break;
                LineIndex += 1;
            }

            // Check if no values were pulled. If this is the case then dump out.
            if (SplitMessageLines.Length == 0)
            {
                FilterProperties = new List<string[]>();
                return "No Filter Content Found!";
            }

            // Setup Loop constants for parsing operations
            FilterProperties = new List<string[]>();
            List<string> OutputMessages = new List<string>();
            var MessageContentRegex = PassThruExpressionRegex.LoadedExpressions[PassThruExpressionTypes.MessageFilterInfo];

            // Now parse out our content matches. Add a trailing newline to force matches.
            SplitMessageLines = CombinedOutputs.Select(LineSet => LineSet + "\n").ToArray();
            foreach (var MsgLineSet in SplitMessageLines)
            {
                // RegexMatch output here.
                var OutputMessageTuple = new List<Tuple<string, string>>();
                bool MatchedContent = MessageContentRegex.Evaluate(MsgLineSet, out var MatchedMessageStrings);
                if (!MatchedContent)
                {
                    // Check if this is a null flow control instance
                    if (MsgLineSet.Trim() != "FlowControl is NULL")
                        continue;

                    // Add null flow control here.
                    OutputMessageTuple.Add(new Tuple<string, string>(ResultStringTable[1], "FlowControl"));
                    OutputMessageTuple.Add(new Tuple<string, string>(ResultStringTable[2], "-1"));
                    for (int TupleIndex = 3; TupleIndex < ResultStringTable.Count; TupleIndex++)
                        OutputMessageTuple.Add(new Tuple<string, string>(ResultStringTable[TupleIndex], "NULL"));
                }

                // Make sure the value for Flags is not zero. If it is, then we need to insert a "No Value" object
                var TempList = MatchedMessageStrings.ToList();
                int IndexOfZeroFlags = TempList.IndexOf("0x00000000");
                if (IndexOfZeroFlags != -1) { TempList.Insert(IndexOfZeroFlags + 1, "No Value"); }
                MatchedMessageStrings = TempList.ToArray();

                // Knock out any of the whitespace values.
                MatchedMessageStrings = MatchedMessageStrings
                    .Skip(1)
                    .Where(StringObj => !string.IsNullOrEmpty(StringObj))
                    .ToArray();

                // Format our message data content to include a 0x before the data byte and caps lock message bytes.
                int LastStringIndex = MatchedMessageStrings.Length - 1;
                MatchedMessageStrings[LastStringIndex] = string.Join(" ",
                    MatchedMessageStrings[LastStringIndex]
                        .Split(' ')
                        .Where(PartValue => !string.IsNullOrEmpty(PartValue))
                        .Select(StringPart => $"0x{StringPart.Trim().ToUpper()}")
                        .ToArray()
                );

                // Now loop each part of the matched content and add values into our output tuple set.
                OutputMessageTuple.AddRange(MatchedMessageStrings
                    .Select((T, StringIndex) => new Tuple<string, string>(ResultStringTable[StringIndex], T)));

                // Build our output table once all our values have been appended in here.
                string RegexValuesOutputString = OutputMessageTuple.ToStringTable(
                    new[] { "Filter Message Property", "Filter Message Value" },
                    RegexObj => RegexObj.Item1,
                    RegexObj => RegexObj.Item2
                );

                // Add this string to our list of messages.
                OutputMessages.Add(RegexValuesOutputString + "\n");
                FilterProperties.Add(OutputMessageTuple.Select(TupleObj => TupleObj.Item2).ToArray());
            }

            // Return built table string object.
            return string.Join("\n", OutputMessages);
        }
        /// <summary>
        /// Finds all the parameters of the IOCTL command output from the input content
        /// </summary>
        /// <param name="ParameterProperties">Properties to return out.</param>
        /// <returns>The properties of the IOCTL as a string table.</returns>
        public static string FindIoctlParameters(this PassThruExpression ExpressionObject, out Tuple<string, string, string>[] ParameterProperties)
        {
            // Check if we can run this type for the given object.
            if (ExpressionObject.GetType() != typeof(PassThruIoctlExpression))
            {
                _expExtLogger.WriteLog("CAN NOT USE THIS METHOD ON A NON IOCTL COMMAND TYPE!", LogType.ErrorLog);
                ParameterProperties = Array.Empty<Tuple<string, string, string>>();
                return string.Empty;
            }

            // Try and parse out the IOCTL Command objects from the input strings here.
            var IoctlRegex = PassThruExpressionRegex.LoadedExpressions[PassThruExpressionTypes.IoctlParameterInfo];
            bool IoctlResults = IoctlRegex.Evaluate(ExpressionObject.CommandLines, out var IoctlResultStrings);
            if (!IoctlResults)
            {
                ParameterProperties = Array.Empty<Tuple<string, string, string>>();
                return "No Parameters";
            }

            // Now pull out the IOCTL command objects
            ParameterProperties = IoctlResultStrings
                .Last().Split('\r').Select(StringObj =>
                {
                    // Get base values for name and value output.
                    string[] SplitValueAndName = StringObj.Split('=');
                    string[] SplitIdAndNameValue = SplitValueAndName[0].Split(':');

                    // Store values for content here for output tuple set.
                    string NameValue = SplitIdAndNameValue[1].Trim();
                    string ValueString = SplitValueAndName[1].Trim();
                    string IdValue = int.TryParse(SplitIdAndNameValue[0], out int OutputInt) ?
                        $"0x{OutputInt:x8}".Trim() :
                        $"{SplitIdAndNameValue[0]} (ERROR!)".Trim();

                    // Build new output tuple object here and return it.
                    return new Tuple<string, string, string>(IdValue, NameValue, ValueString);
                })
                .ToArray();

            // Build our output table object here.
            string IoctlTableOutput = ParameterProperties.ToStringTable(
                new[] { "Ioctl ID", "Ioctl Name", "Set Value" },
                IoctlPair => IoctlPair.Item1,
                IoctlPair => IoctlPair.Item2
            );

            // Throw new exception since not yet built.
            return IoctlTableOutput;
        }

        // ------------------------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Converts an input Regex command type enum into a type output
        /// </summary>
        /// <param name="InputTypes">Enum Regex Typ</param>
        /// <returns>Type of regex for the class output</returns>
        public static PassThruExpression ToPassThruExpression(this PassThruExpressionTypes InputTypes, string InputLogLines)
        {
            try
            {
                // Start by finding our type value for the given input expression object
                string InputTypeName = InputTypes.ToDescriptionString();
                Type RegexClassType = InputTypes == PassThruExpressionTypes.NONE
                    ? typeof(PassThruExpression)
                    : typeof(PassThruExpression).Assembly.GetTypes()
                        .FirstOrDefault(TypeObj => TypeObj.Name.Contains(InputTypeName));

                // Now build the expression object based on this type value loaded. If types are invalid, then we return an empty expression
                PassThruExpression BuiltExpression = InputTypes == PassThruExpressionTypes.NONE 
                    ? new PassThruExpression(InputLogLines, InputTypes)
                    : (PassThruExpression)Activator.CreateInstance(RegexClassType, InputLogLines);

                // Return the built expression object here
                return BuiltExpression;
            }
            catch (Exception InvokeTypeEx)
            {
                // Catch this exception for debugging use later on
                _expExtLogger.WriteLog($"AN INPUT LOG LINE SET COULD NOT BE PARSED OUT TO AN EXPRESSION TYPE!", LogType.TraceLog);
                _expExtLogger.WriteException("EXCEPTION THROWN DURING CONVERSION ROUTINE IS LOGGED BELOW", InvokeTypeEx, new[] { LogType.TraceLog, LogType.TraceLog });

                // Return null at this point since the log line objects could not be parsed for some reason
                return null;
            }
        }
        /// <summary>
        /// Converts an input Regex command type enum into a type output
        /// </summary>
        /// <param name="InputTypes">Enum Regex Typ</param>
        /// <returns>Type of regex for the class output</returns>
        public static PassThruExpression ToPassThruExpression(this PassThruExpressionTypes InputTypes, string[] InputLogLines)
        {
            // Join the log lines on newline characters and get the type value here
            string JoinedLogLines = string.Join(string.Empty, InputLogLines.Select(LogLine => LogLine.Trim()));
            return ToPassThruExpression(InputTypes, JoinedLogLines);
        }

        // ------------------------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Finds a PTCommand type from the given input line set
        /// </summary>
        /// <param name="InputLines">Lines to find the PTCommand Type for.</param>
        /// <returns>The type of PTCommand regex to search with.</returns>
        public static PassThruExpressionTypes ToPassThruCommandType(this string[] InputLines)
        {
            // Return the result from our joined line output.
            return ToPassThruCommandType(string.Join("\n", InputLines.Select(Input => Input.TrimEnd())));
        }
        /// <summary>
        /// Finds a PTCommand type from the given input line set
        /// </summary>
        /// <param name="InputLines">Lines to find the PTCommand Type for.</param>
        /// <returns>The type of PTCommand regex to search with.</returns>
        public static PassThruExpressionTypes ToPassThruCommandType(this string InputLines)
        {
            // Find the type of command by converting all enums to string array and searching for the type.
            var EnumTypesArray = Enum.GetValues(typeof(PassThruExpressionTypes))
                .Cast<PassThruExpressionTypes>()
                .Select(PtEnumValue => PtEnumValue.ToString())
                .ToArray();

            // Find the return type here based on the first instance of a PTCommand type object on the array.
            var EnumStringSelected = EnumTypesArray.FirstOrDefault(InputLines.Contains);
            return (PassThruExpressionTypes)(string.IsNullOrWhiteSpace(EnumStringSelected)
                ? PassThruExpressionTypes.NONE
                : Enum.Parse(typeof(PassThruExpressionTypes), EnumStringSelected));
        }
    }
}
