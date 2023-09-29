using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SharpExpressions;
using SharpExpressions.PassThruExpressions;
using SharpLogging;
using SharpWrapper.J2534Objects;
using SharpWrapper.PassThruTypes;

namespace SharpSimulator.PassThruSimulationSupport
{
    /// <summary>
    /// Collection of extension methods for building simulation channels and populating their values
    /// </summary>
    public static class PassThruSimChannelExtensions
    {
        #region Custom Events
        #endregion //Custom Events

        #region Fields

        // Logger Object for the expressions extension methods. This should again only be run for exceptions
        private static readonly SharpLogger _simExtLogger;

        #endregion //Fields

        #region Properties
        #endregion //Properties

        #region Structs and Classes
        #endregion //Structs and Classes

        // ------------------------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// CTOR for this static class which is used to help configured our logging instance
        /// </summary>
        static PassThruSimChannelExtensions()
        {
            // Spawn in our logger instance now
            _simExtLogger = new SharpLogger(LoggerActions.UniversalLogger);
            _simExtLogger.WriteLog("SIMULATION CHANNEL EXTENSION LOGGER HAS BEEN SPAWNED!", LogType.InfoLog);
        }
        
        // ------------------------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Stores a set of Expressions into messages on the given channel object
        /// </summary>
        /// <param name="ExpressionsToStore">Expressions to extract and store</param>
        /// <returns>The Filters built</returns>
        public static J2534Filter[] StoreMessageFilters(this PassThruSimulationChannel InputChannel, PassThruStartMessageFilterExpression[] ExpressionsToStore)
        {
            // Loop each of these filter objects in parallel and update contents.
            List<J2534Filter> BuiltFilters = new List<J2534Filter>();
            Parallel.ForEach(ExpressionsToStore, (FilterExpression) => BuiltFilters.Add(ConvertFilterExpression(FilterExpression, true)));

            // Return the built filter objects here.
            InputChannel.MessageFilters = BuiltFilters.Where(FilterObj => FilterObj != null).ToArray();
            return BuiltFilters.Where(FilterObj => FilterObj != null).ToArray();
        }
        /// <summary>
        /// Stores a set of PTWrite Message commands into the current sim channel as messages to READ IN
        /// </summary>
        /// <returns>List of messages stored</returns>
        public static PassThruStructs.PassThruMsg[] StoreMessagesWritten(this PassThruSimulationChannel InputChannel, PassThruWriteMessagesExpression[] ExpressionsToStore)
        {
            // Loop each of these filter objects in parallel and update contents.
            List<PassThruStructs.PassThruMsg> BuiltMessages = new List<PassThruStructs.PassThruMsg>();
            Parallel.ForEach(ExpressionsToStore, (MessageExpression) => BuiltMessages.AddRange(ConvertWriteExpression(MessageExpression)));

            // Return the built filter objects here.
            var CombinedMessagesSet = (InputChannel.MessagesSent ?? Array.Empty<PassThruStructs.PassThruMsg>()).ToList();
            CombinedMessagesSet.AddRange(BuiltMessages);
            CombinedMessagesSet = CombinedMessagesSet
                .Distinct()
                .ToList();

            // Return the distinct combinations
            InputChannel.MessagesSent = CombinedMessagesSet.ToArray();
            return CombinedMessagesSet.ToArray();
        }
        /// <summary>
        /// Stores a set of PTWrite Message commands into the current sim channel as messages to READ IN
        /// </summary>
        /// <returns>List of messages stored</returns>
        public static PassThruStructs.PassThruMsg[] StoreMessagesRead(this PassThruSimulationChannel InputChannel, PassThruReadMessagesExpression[] ExpressionsToStore)
        {
            // Loop each of these filter objects in parallel and update contents.
            List<PassThruStructs.PassThruMsg> BuiltMessages = new List<PassThruStructs.PassThruMsg>();
            Parallel.ForEach(ExpressionsToStore, (MessageExpression) => BuiltMessages.AddRange(ConvertReadExpression(MessageExpression)));

            // Return the built filter objects here.
            var CombinedMessagesSet = (InputChannel.MessagesRead ?? Array.Empty<PassThruStructs.PassThruMsg>()).ToList();
            CombinedMessagesSet.AddRange(BuiltMessages.ToList());
            CombinedMessagesSet = CombinedMessagesSet
                .Distinct()
                .ToList();

            // Return the distinct combinations
            InputChannel.MessagesRead = CombinedMessagesSet.ToArray();
            return CombinedMessagesSet.ToArray();
        }
        /// <summary>
        /// Pairs off a set of input Expressions to find their pairings
        /// </summary>
        /// <param name="GroupedExpression">Expressions to search thru</param>
        public static PassThruSimulationChannel.SimulationMessagePair[] StorePassThruPairs(this PassThruSimulationChannel InputChannel, PassThruExpression[] GroupedExpression)
        {
            // Order the input expression objects by time fired off and then pull out our pairing values
            GroupedExpression = GroupedExpression.OrderBy(ExpObj =>
            {
                // Store the seconds and milliseconds values here
                string[] TimeSplit = ExpObj.ExecutionTime.Split('.');

                // Parse the seconds and milliseconds value and return a timespan for them
                int SecondsValue = int.Parse(TimeSplit[0]);
                int MillisValue = int.Parse(TimeSplit[1].Replace("s", string.Empty));

                // Build an output timespan value here and return it for sorting routines
                TimeSpan TimeElapsed = new TimeSpan(0, 0, 0, SecondsValue, MillisValue);
                return TimeElapsed;
            }).ToArray();

            // Build a temporary output list object and loop all of our expressions here
            var MessagesPaired = new List<Tuple<PassThruWriteMessagesExpression, PassThruReadMessagesExpression[]>>();
            foreach (var ExpressionObject in GroupedExpression)
            {
                // Find if the expression is a PTWrite command then find all the next ones that are 
                if (ExpressionObject.TypeOfExpression != PassThruExpressionTypes.PTWriteMsgs) { continue; }

                // Store the next expression
                var IndexOfExpression = GroupedExpression.ToList().IndexOf(ExpressionObject);
                if ((IndexOfExpression + 1) > GroupedExpression.Length) continue;

                // Find the next expression and get all future ones
                IndexOfExpression += 1;
                var ReadExpressions = new List<PassThruReadMessagesExpression>();
                var NextExpression = GroupedExpression[IndexOfExpression];
                while (NextExpression.TypeOfExpression != PassThruExpressionTypes.PTWriteMsgs)
                {
                    // Check if it's a PTRead Messages
                    if (NextExpression.TypeOfExpression != PassThruExpressionTypes.PTReadMsgs)
                    {
                        IndexOfExpression += 1;
                        if ((IndexOfExpression + 1) > GroupedExpression.Length) break;
                        continue;
                    }

                    // Add and check if the value is configured
                    IndexOfExpression += 1;
                    if ((IndexOfExpression + 1) > GroupedExpression.Length) break;
                    ReadExpressions.Add((PassThruReadMessagesExpression)NextExpression);
                    NextExpression = GroupedExpression[IndexOfExpression];
                }

                // Now add it into our list of messages paired with our original write command
                MessagesPaired.Add(new Tuple<PassThruWriteMessagesExpression, PassThruReadMessagesExpression[]>(
                    (PassThruWriteMessagesExpression)ExpressionObject, ReadExpressions.ToArray()
                ));
            }

            // Store onto the class, return built values.
            List<PassThruSimulationChannel.SimulationMessagePair> MessagePairOutput = new List<PassThruSimulationChannel.SimulationMessagePair>();
            foreach (var PairedMessageSet in MessagesPaired)
            {
                // Store basic values for contents here
                PassThruStructs.PassThruMsg SendExpressionAsMessage = ConvertWriteExpression(PairedMessageSet.Item1).FirstOrDefault();
                PassThruStructs.PassThruMsg[][] ReadExpressionsAsMessages = PairedMessageSet.Item2.Select(ConvertReadExpression).ToArray();

                // Append all messages into our list here
                MessagePairOutput.Add(new PassThruSimulationChannel.SimulationMessagePair(SendExpressionAsMessage, ReadExpressionsAsMessages.SelectMany(MsgObj => MsgObj).ToArray()));
            }

            // Store values for the input channel and return output
            InputChannel.MessagePairs = MessagePairOutput.ToArray();
            return InputChannel.MessagePairs;
        }

        // ------------------------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Converts a J2534 Filter expression into a filter object
        /// </summary>
        /// <param name="FilterExpression"></param>
        /// <returns></returns>
        public static J2534Filter ConvertFilterExpression(this PassThruStartMessageFilterExpression FilterExpression, bool Inverted = false)
        {
            // Store the Pattern, Mask, and Flow Ctl objects if they exist.
            FilterExpression.FindFilterContents(out List<string[]> FilterContent);
            if (FilterContent.Count == 0)
            {
                // Log that we failed to find contents for this filter and exit out
                _simExtLogger.WriteLog($"FILTER {FilterExpression.FilterID} FOR CHANNEL {FilterExpression.ChannelID} COULD NOT BE EXTRACTED!", LogType.ErrorLog);
                _simExtLogger.WriteLog($"FILTER COMMAND LINES ARE SHOWN BELOW:\n{FilterExpression.CommandLines}", LogType.TraceLog);
                return null;
            }

            // Build filter output contents
            // BUG: NOT ALL EXTRACTED REGEX OUTPUT IS THE SAME! THIS RESULTS IN SOME POOR INDEXING ROUTINES
            try
            {
                // Store types and values for the filter being built here
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
                _simExtLogger.WriteLog("FAILED TO CONVERT THIS EXPRESSION INTO A USABLE FILTER! EXCEPTION IS BEING SHOWN BELOW", LogType.WarnLog);
                _simExtLogger.WriteLog($"FILTER EXPRESSION: {FilterExpression.CommandLines}", LogType.TraceLog);
                _simExtLogger.WriteException("EXCEPTION THROWN", ConversionEx);
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
        /// <param name="MessageExpression">The expressions to convert</param>
        /// <returns>A collection of built PassThru messages based on the read expressions</returns>
        public static PassThruStructs.PassThruMsg[] ConvertWriteExpression(this PassThruWriteMessagesExpression MessageExpression)
        {
            // Store the Message Data and the values of the message params.
            MessageExpression.FindMessageContents(out List<string[]> MessageContents);
            if (MessageContents.Count == 0)
            {
                // Return an empty array of output objects
                _simExtLogger.WriteLog("MESSAGE CONTENTS WERE NOT ABLE TO BE EXTRACTED!", LogType.ErrorLog);
                _simExtLogger.WriteLog($"MESSAGE COMMAND LINES ARE SHOWN BELOW:\n{MessageExpression.CommandLines}", LogType.TraceLog);
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
                    _simExtLogger.WriteLog("FAILED TO CONVERT THIS EXPRESSION INTO A USABLE MESSAGE! EXCEPTION IS BEING SHOWN BELOW", LogType.WarnLog);
                    _simExtLogger.WriteLog($"MESSAGE EXPRESSION: {MessageExpression.CommandLines}", LogType.TraceLog);
                    _simExtLogger.WriteException("EXCEPTION THROWN", ConversionEx);
                    return default;
                }
            }

            // Return the built message
            return MessagesBuilt;
        }
        /// <summary>
        /// Converts an input PTRead Expression to a PTMessage
        /// </summary>
        /// <param name="MessageExpression">The expressions to convert</param>
        /// <returns>A collection of built PassThru messages based on the read expressions</returns>
        public static PassThruStructs.PassThruMsg[] ConvertReadExpression(this PassThruReadMessagesExpression MessageExpression)
        {
            // Store the Message Data and the values of the message params.
            MessageExpression.FindMessageContents(out List<string[]> MessageContents);
            if (MessageContents.Count == 0)
            {
                // Return an empty array of output objects
                _simExtLogger.WriteLog("MESSAGE CONTENTS WERE NOT ABLE TO BE EXTRACTED!", LogType.ErrorLog);
                _simExtLogger.WriteLog($"MESSAGE COMMAND LINES ARE SHOWN BELOW:\n{MessageExpression.CommandLines}", LogType.TraceLog);
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
                    
                    // Pull out the protocol, message data content, and parse the Rx Status value
                    var ProtocolId = (ProtocolId)Enum.Parse(typeof(ProtocolId), MessageSet[2].Split(':')[0]);
                    MessageData = MessageData.Replace("0x", string.Empty).Replace("  ", " ");
                    RxStatus MsgRxStatus = (RxStatus)uint.Parse(MessageSet[4].Replace("RxS=", string.Empty), NumberStyles.HexNumber);

                    // Configure message TxFlags here
                    TxFlags MsgTxFlags = TxFlags.NO_TX_FLAGS;
                    if (ProtocolId == ProtocolId.ISO15765)
                    {
                        // Check the length of the message here
                        int DataSize = MessageData.Split(' ').Length;
                        if (DataSize <= 12) MsgTxFlags = TxFlags.ISO15765_FRAME_PAD;
                    }

                    // If this message was read in as part of a flow control operation, skip it
                    if (MsgRxStatus.HasFlag(RxStatus.TX_MSG_TYPE)) continue;
                    if (MsgRxStatus.HasFlag(RxStatus.START_OF_MESSAGE)) continue;

                    // Build a message and then return it. Store the needed RxStatus values for it if needed
                    var NextMessage = J2534Device.CreatePTMsgFromString(ProtocolId, (uint)MsgTxFlags, (uint)MsgRxStatus, MessageData);
                    MessagesBuilt = MessagesBuilt.Append(NextMessage).ToArray();
                }
                catch (Exception ConversionEx)
                {
                    // TODO: FIND OUT WHY THIS ROUTINE CAN FAIL SOMETIMES!
                    _simExtLogger.WriteLog("FAILED TO CONVERT THIS EXPRESSION INTO A USABLE MESSAGE! EXCEPTION IS BEING SHOWN BELOW", LogType.WarnLog);
                    _simExtLogger.WriteLog($"MESSAGE EXPRESSION: {MessageExpression.CommandLines}", LogType.TraceLog);
                    _simExtLogger.WriteException("EXCEPTION THROWN", ConversionEx);
                    return default;
                }
            }

            // Return the message
            return MessagesBuilt;
        }
    }
}
