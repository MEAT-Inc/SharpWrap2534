using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using SharpExpressions.PassThruExpressions;
using SharpLogger;
using SharpLogger.LoggerObjects;
using SharpLogger.LoggerSupport;
using SharpWrapper.J2534Objects;
using SharpWrapper.PassThruTypes;

namespace SharpExpressions.PassThruExtensions
{
    /// <summary>
    /// Extensions for parsing out commands into new types of output for PT Regex Classes
    /// </summary>
    public static class PassThruExpressionExtensions
    {
        // Logger Object for the expressions extension methods. This should again only be run for exceptions
        private static SubServiceLogger _expExtLogger => (SubServiceLogger)LoggerQueue.SpawnLogger($"ExpressionsExtLogger", LoggerActions.SubServiceLogger);

        // ------------------------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Pulls out all of our message content values and stores them into a list with details.
        /// </summary>
        public static string FindMessageContents(this PassThruExpression ExpressionObject, out List<string[]> MessageProperties)
        {
            // Check if not read or write types. 
            if (ExpressionObject.GetType() != typeof(PassThruReadMessagesExpression) && ExpressionObject.GetType() != typeof(PassThruWriteMessagesExpression))
            {
                ExpressionObject.ExpressionLogger.WriteLog("CAN NOT USE THIS METHOD ON A NON READ OR WRITE COMMAND TYPE!", LogType.ErrorLog);
                MessageProperties = new List<string[]>();
                return string.Empty;
            }

            // Pull the object, find our matches based on our type object value.
            var MessageContentRegex = ExpressionObject.GetType() == typeof(PassThruReadMessagesExpression)
                ? PassThruExpressionRegex.LoadedExpressions[PassThruExpressionType.MessageReadInfo]
                : PassThruExpressionRegex.LoadedExpressions[PassThruExpressionType.MessageSentInfo];

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
                // RegexMatch output here.
                var RegexResultTuples = new List<Tuple<string, string>>();
                bool MatchedContent = MessageContentRegex.Evaluate(MsgLineSet, out var MatchedMessageStrings);
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
                ExpressionObject.ExpressionLogger.WriteLog("CAN NOT USE THIS METHOD ON A NON PTSTART FILTER COMMAND TYPE!", LogType.ErrorLog);
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
            var MessageContentRegex = PassThruExpressionRegex.LoadedExpressions[PassThruExpressionType.MessageFilterInfo];

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
                ExpressionObject.ExpressionLogger.WriteLog("CAN NOT USE THIS METHOD ON A NON IOCTL COMMAND TYPE!", LogType.ErrorLog);
                ParameterProperties = Array.Empty<Tuple<string, string, string>>();
                return string.Empty;
            }

            // Try and parse out the IOCTL Command objects from the input strings here.
            var IoctlRegex = PassThruExpressionRegex.LoadedExpressions[PassThruExpressionType.IoctlParamInfo];
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
        /// <param name="InputType">Enum Regex Typ</param>
        /// <returns>Type of regex for the class output</returns>
        public static PassThruExpression ToPassThruExpression(this PassThruExpressionType InputType, string InputLogLines)
        {
            try
            {
                // Pull the description string and get type of regex class.
                string InputTypeName = InputType.ToDescriptionString();
                string ClassNamespace = typeof(PassThruExpression).Namespace;
                string ClassType = $"{ClassNamespace}.{InputTypeName}";

                // Build a new PassThru Expression object here based on the type found for our expression
                Type RegexClassType = Type.GetType(ClassType);
                PassThruExpression BuiltExpression = RegexClassType == null
                    ? new PassThruExpression(InputLogLines, InputType)
                    : (PassThruExpression)Activator.CreateInstance(RegexClassType, InputLogLines);

                // Return the new Expression object here and move on
                return BuiltExpression;
            }
            catch (Exception InvokeTypeEx)
            {
                // Catch this exception for debugging use later on
                _expExtLogger.WriteLog($"AN INPUT LOG LINE SET COULD NOT BE PARSED OUT TO AN EXPRESSION TYPE!", LogType.TraceLog);
                _expExtLogger.WriteLog("EXCEPTION THROWN DURING CONVERSION ROUTINE IS LOGGED BELOW", InvokeTypeEx, new[] { LogType.TraceLog, LogType.TraceLog });

                // Return null at this point since the log line objects could not be parsed for some reason
                return null;
            }
        }
        /// <summary>
        /// Converts an input Regex command type enum into a type output
        /// </summary>
        /// <param name="InputType">Enum Regex Typ</param>
        /// <returns>Type of regex for the class output</returns>
        public static PassThruExpression ToPassThruExpression(this PassThruExpressionType InputType, string[] InputLogLines)
        {
            // Join the log lines on newline characters and get the type value here
            string JoinedLogLines = string.Join(string.Empty, InputLogLines.Select(LogLine => LogLine.Trim()));
            return ToPassThruExpression(InputType, JoinedLogLines);
        }

        // ------------------------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Converts a J2534 Filter expression into a filter object
        /// </summary>
        /// <param name="FilterExpression"></param>
        /// <returns></returns>
        public static J2534Filter ConvertFilterExpression(PassThruStartMessageFilterExpression FilterExpression, bool Inverted = false)
        {
            // Store the Pattern, Mask, and Flow Ctl objects if they exist.
            FilterExpression.FindFilterContents(out List<string[]> FilterContent);
            if (FilterContent.Count == 0)
            {
                FilterExpression.ExpressionLogger.WriteLog("FILTER CONTENTS WERE NOT ABLE TO BE EXTRACTED!", LogType.ErrorLog);
                FilterExpression.ExpressionLogger.WriteLog($"FILTER COMMAND LINES ARE SHOWN BELOW:\n{FilterExpression.CommandLines}", LogType.TraceLog);
                return null;
            }

            // Build filter output contents
            // BUG: NOT ALL EXTRACTED REGEX OUTPUT IS THE SAME! THIS RESULTS IN SOME POOR INDEXING ROUTINES
            try
            {
                var FilterType = FilterExpression.FilterType.Split(':')[1];
                var FilterFlags = (TxFlags)uint.Parse(FilterContent[0][4].Replace("TxF=", string.Empty), NumberStyles.HexNumber);
                var FilterProtocol = (ProtocolId)uint.Parse(FilterContent[0][2].Split(':')[0]);
                var FilterPatten = FilterContent
                    .FirstOrDefault(FilterSet => FilterSet.Any(FilterString => FilterString.Contains("Pattern")))
                    .Last()
                    .Replace("0x", string.Empty)
                    .Replace("  ", " ");
                var FilterMask = FilterContent
                    .FirstOrDefault(FilterSet => FilterSet.Any(FilterString => FilterString.Contains("Mask")))
                    .Last()
                    .Replace("0x", string.Empty)
                    .Replace("  ", " ");
                var FilterFlow =
                    FilterContent.Count != 3 ? "" :
                    FilterContent.FirstOrDefault(FilterSet => FilterSet.Any(FilterString => FilterString.Contains("Flow")))
                        .Last()
                        .Replace("0x", string.Empty)
                        .Replace("  ", " ");

                // Now convert our information into string values.
                FilterDef FilterTypeCast = (FilterDef)Enum.Parse(typeof(FilterDef), FilterType);
                J2534Filter OutputFilter = new J2534Filter()
                {
                    FilterFlags = FilterFlags,
                    FilterProtocol = FilterProtocol,
                    FilterType = FilterTypeCast,
                    FilterStatus = SharpSessionStatus.INITIALIZED
                };

                // Now store the values for the message itself.
                if (FilterTypeCast == FilterDef.FLOW_CONTROL_FILTER)
                {
                    // Store a mask, pattern, and flow control value here
                    OutputFilter.FilterMask = FilterMask;
                    OutputFilter.FilterPattern = Inverted ? FilterFlow : FilterPatten;
                    OutputFilter.FilterFlowCtl = Inverted ? FilterPatten : FilterFlow;
                }
                else
                {
                    // Store ONLY a mask and a pattern here
                    OutputFilter.FilterMask = Inverted ? FilterPatten : FilterMask;
                    OutputFilter.FilterPattern = Inverted ? FilterMask : FilterPatten;
                    OutputFilter.FilterFlowCtl = string.Empty;
                }

                // Return the built J2534 filter object
                return OutputFilter;
            }
            catch (Exception ConversionEx)
            {
                // TODO: FIND OUT WHY THIS ROUTINE CAN FAIL SOMETIMES!
                FilterExpression.ExpressionLogger.WriteLog("FAILED TO CONVERT THIS EXPRESSION INTO A USABLE FILTER! EXCEPTION IS BEING SHOWN BELOW", LogType.WarnLog);
                FilterExpression.ExpressionLogger.WriteLog($"FILTER EXPRESSION: {FilterExpression.CommandLines}", LogType.TraceLog);
                FilterExpression.ExpressionLogger.WriteLog("EXCEPTION THROWN", ConversionEx);
                return null;
            }
        }
        /// <summary>
        /// Converts a PTWrite Message Expression into a PTMessage
        /// Some additional info about this conversion routine
        ///
        /// This method starts by pulling out the data of our message and finding out how many parts we need to split it into
        /// Sample Input Data:
        ///      0x00 0x00 0x07 0xE8 0x49 0x02 0x01 0x31 0x47 0x31 0x46 0x42 0x33 0x44 0x53 0x33 0x4B 0x30 0x31 0x31 0x37 0x32 0x32 0x38
        ///
        /// Using the input message size (24 in this case) we need to built N Number of messages with a max size of 12 bytes for each one
        /// We also need to include the changes needed to make sure the message count bytes are included
        /// So with that in mind, our 24 bytes of data gets 3 bytes removed and included in our first message out
        /// First message value would be as follows
        ///      00 00 07 E8 10 14 49 02 01 31 47 31
        ///          00 00 07 E8 -> Response Address
        ///          10 14 -> Indicates a multiple part message and the number of bytes left to read
        ///          49 02 -> 09 + 40 means positive response and 02 is the command issues
        ///          01 -> Indicates data begins
        ///          31 47 31 -> First three bytes of data
        /// 
        /// Then all following messages follow the format of 00 00 0X XX TC DD DD DD DD DD DD DD
        ///      00 00 0X -> Padding plus the address start byte
        ///      XX -> Address byte two
        ///      TC -> T - Total messages and C - Current Message number
        ///      DD DD DD DD DD DD DD -> Data of the message value
        /// 
        /// We also need to include a frame pad indicator output. This message is just 00 00 07 and  the input address byte two 
        /// minus 8. So for 00 00 07 E8, we would get 00 00 07 E0
        ///
        /// This means for our input message value in this block of text, our output must look like the following
        ///      00 00 07 E8 10 14 49 02 01 31 47 31     --> Start of message and first three bytes
        ///      00 00 07 E0 00 00 00 00 00 00 00 00     --> Frame pad message
        ///      00 00 07 E8 21 46 42 33 44 53 33 4B     --> Bytes 4-10
        ///      00 00 07 E8 22 30 31 31 37 32 32 38     --> Bytes 11-17
        /// </summary>
        /// <param name="MessageExpression"></param>
        /// <returns></returns>
        public static PassThruStructs.PassThruMsg[] ConvertWriteExpression(PassThruWriteMessagesExpression MessageExpression)
        {
            // Store the Message Data and the values of the message params.
            MessageExpression.FindMessageContents(out List<string[]> MessageContents);
            if (MessageContents.Count == 0)
            {
                // Return an empty array of output objects
                MessageExpression.ExpressionLogger.WriteLog("MESSAGE CONTENTS WERE NOT ABLE TO BE EXTRACTED!", LogType.ErrorLog);
                MessageExpression.ExpressionLogger.WriteLog($"MESSAGE COMMAND LINES ARE SHOWN BELOW:\n{MessageExpression.CommandLines}", LogType.TraceLog);
                return Array.Empty<PassThruStructs.PassThruMsg>();
            }

            // Loop all the message values located and append them into the list of output
            PassThruStructs.PassThruMsg[] MessagesBuilt = Array.Empty<PassThruStructs.PassThruMsg>();
            foreach (var MessageSet in MessageContents)
            {
                // Wrap inside a try catch to ensure we get something back
                // TODO: FORMAT THIS CODE TO WORK FOR DIFFERENT PROTOCOL VALUE TYPES!
                try
                {
                    // Store message values here.
                    var MessageFlags = uint.Parse(MessageSet[3].Replace("TxF=", string.Empty), NumberStyles.HexNumber);
                    var ProtocolId = (ProtocolId)Enum.Parse(typeof(ProtocolId), MessageSet[1].Split(':')[0]);

                    // ISO15765 11 Bit
                    var MessageData = MessageSet.Last();
                    if (MessageData.StartsWith("0x00 0x00") || MessageData.StartsWith("00 00"))
                    {
                        // 11 Bit messages need to be converted according to this format
                        // 00 00 07 DF 01 00 -- 00 00 07 DF 02 01 00 00 00 00 00 00
                        // Take first 4 bytes 
                        //      00 00 07 DF
                        // Count the number of bytes left
                        //      01 00 -- 2 bytes
                        // Insert the number of bytes (02) and the data sent
                        //      00 00 07 DF 02 01 00
                        // Take the number of bytes now and append 0s till it's 12 long
                        //      00 00 07 DF 02 01 00 00 00 00 00 00

                        // Build a message and then return it.
                        string[] MessageDataSplit = MessageData.Split(' ').ToArray();
                        string[] FormattedData = MessageDataSplit.Take(4).ToArray();                           // 00 00 07 DF   --   Padding + Send Address
                        string[] DataSentOnly = MessageDataSplit.Skip(4).ToArray();                            // 01 00         --   Actual data transmission
                        string[] FinalData = FormattedData
                            .Concat(new string[] { "0x" + DataSentOnly.Length.ToString("X2") })
                            .Concat(DataSentOnly)
                            .ToArray();                                                                        // 00 00 07 DF 02 01 00   --   Padding + Send Addr + Size + Data                                                         
                        string[] TrailingZeros = Enumerable.Repeat("0x00", 12 - FinalData.Length).ToArray();
                        FinalData = FinalData.Concat(TrailingZeros).ToArray();                                 // 00 00 07 DF 02 01 00 00 00 00 00 -- Finalized output message

                        // Convert back into a string value and format
                        MessageData = string.Join(" ", FinalData);
                    }

                    // ISO15765 29 Bit
                    else if (MessageData.StartsWith("0x18 0xdb") || MessageData.StartsWith("18 db"))
                    {
                        // TODO: BUILD FORMATTING ROUTINE FOR 29 BIT CAN!
                    }

                    // Build our final output message.
                    MessageData = MessageData.Replace("0x", string.Empty).Replace("  ", " ");
                    var NextMessage = J2534Device.CreatePTMsgFromString(ProtocolId, MessageFlags, MessageData);
                    MessagesBuilt = MessagesBuilt.Append(NextMessage).ToArray();
                }
                catch (Exception ConversionEx)
                {
                    // TODO: FIND OUT WHY THIS ROUTINE CAN FAIL SOMETIMES!
                    MessageExpression.ExpressionLogger.WriteLog("FAILED TO CONVERT THIS EXPRESSION INTO A USABLE MESSAGE! EXCEPTION IS BEING SHOWN BELOW", LogType.WarnLog);
                    MessageExpression.ExpressionLogger.WriteLog($"MESSAGE EXPRESSION: {MessageExpression.CommandLines}", LogType.TraceLog);
                    MessageExpression.ExpressionLogger.WriteLog("EXCEPTION THROWN", ConversionEx);
                    return default;
                }
            }

            // Return the built message
            return MessagesBuilt;
        }
        /// <summary>
        /// Converts an input PTRead Expression to a PTMessage
        /// </summary>
        /// <param name="MessageExpression"></param>
        /// <returns></returns>
        public static PassThruStructs.PassThruMsg[] ConvertReadExpression(PassThruReadMessagesExpression MessageExpression)
        {
            // Store the Message Data and the values of the message params.
            MessageExpression.FindMessageContents(out List<string[]> MessageContents);
            if (MessageContents.Count == 0)
            {
                // Return an empty array of output objects
                MessageExpression.ExpressionLogger.WriteLog("MESSAGE CONTENTS WERE NOT ABLE TO BE EXTRACTED!", LogType.ErrorLog);
                MessageExpression.ExpressionLogger.WriteLog($"MESSAGE COMMAND LINES ARE SHOWN BELOW:\n{MessageExpression.CommandLines}", LogType.TraceLog);
                return Array.Empty<PassThruStructs.PassThruMsg>();
            }

            // Loop all the message values located and append them into the list of output
            PassThruStructs.PassThruMsg[] MessagesBuilt = Array.Empty<PassThruStructs.PassThruMsg>();
            foreach (var MessageSet in MessageContents)
            {
                // Wrap this in a try catch
                try
                {
                    // Store message values here.
                    var MessageData = MessageSet.Last();
                    if (MessageData.Contains("[") || MessageData.Contains("]"))
                    {
                        // Format for framepad output
                        MessageData = MessageData.Replace("0x", string.Empty);
                        string[] SplitData = MessageData
                            .Split(']')
                            .Select(SplitPart => SplitPart.Replace("[", string.Empty))
                            .Where(SplitPart => SplitPart.Length != 0)
                            .ToArray();

                        // Now restore message values
                        MessageData = string.Join(" ", SplitData);
                    }

                    // If it's not a frame pad message, add to our simulation
                    var RxStatus = uint.Parse(MessageSet[4].Replace("RxS=", string.Empty), NumberStyles.HexNumber);
                    var ProtocolId = (ProtocolId)Enum.Parse(typeof(ProtocolId), MessageSet[2].Split(':')[0]);

                    // Build a message and then return it.
                    MessageData = MessageData.Replace("0x", string.Empty).Replace("  ", " ");
                    var NextMessage = J2534Device.CreatePTMsgFromString(ProtocolId, 0x00, MessageData);
                    NextMessage.RxStatus = (RxStatus)RxStatus;
                    MessagesBuilt = MessagesBuilt.Append(NextMessage).ToArray();
                }
                catch (Exception ConversionEx)
                {
                    // TODO: FIND OUT WHY THIS ROUTINE CAN FAIL SOMETIMES!
                    MessageExpression.ExpressionLogger.WriteLog("FAILED TO CONVERT THIS EXPRESSION INTO A USABLE MESSAGE! EXCEPTION IS BEING SHOWN BELOW", LogType.WarnLog);
                    MessageExpression.ExpressionLogger.WriteLog($"MESSAGE EXPRESSION: {MessageExpression.CommandLines}", LogType.TraceLog);
                    MessageExpression.ExpressionLogger.WriteLog("EXCEPTION THROWN", ConversionEx);
                    return default;
                }
            }

            // Return the message
            return MessagesBuilt;
        }
    }
}
