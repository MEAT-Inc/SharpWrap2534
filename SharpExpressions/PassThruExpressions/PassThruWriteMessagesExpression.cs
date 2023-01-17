using System;
using System.Collections.Generic;
using System.Linq;
using SharpExpressions.PassThruExtensions;
using SharpLogger.LoggerSupport;

// Static using for Regex lookups and type values
using PassThruRegex = SharpExpressions.PassThruExpressionRegex;

namespace SharpExpressions.PassThruExpressions
{
    /// <summary>
    /// Class object used for our PTWrite Message command parsing output
    /// </summary>
    public class PassThruWriteMessagesExpression : PassThruExpression
    {
        // Regex for the write messages command (PTWriteMsgs) and the number of messages written by the command
        public readonly PassThruRegex PtWriteMessagesRegex = PassThruRegex.GetRegexByType(PassThruExpressionType.PTWriteMsgs);
        public readonly PassThruRegex MessagesWrittenRegex = PassThruRegex.GetRegexByType(PassThruExpressionType.MessageCount);
        
        // Strings of the command and results from the command output.
        [PassThruProperty("Command Line")] public readonly string PtCommand;
        [PassThruProperty("Channel ID")] public readonly string ChannelId;
        [PassThruProperty("Channel Pointer")] public readonly string ChannelPointer;
        [PassThruProperty("Message Pointer")] public readonly string MessagePointer;
        [PassThruProperty("Timeout")] public readonly string TimeoutTime;
        [PassThruProperty("Sent Count")] public readonly string MessageCountSent;
        [PassThruProperty("Expected Count")] public readonly string MessageCountTotal;

        // Contents of message objects located. Shown as a set of tuples and values.
        // The output Array contains a list of tuples paired "Property, Value" 
        // When we complete the expression sets and need to parse these objects into command models, we can Just loop the arrays
        // and pull out the values one by one.
        //
        // So a Sample would be
        //      Message 0 { 0,  ISO15765 }
        //      Message 1 { 0,  ISO15765 }
        //
        // Then from those values, we can build out a PTMessage object.
        public readonly List<string[]> MessageProperties;

        // ----------------------------------------------------------------------------------------------------

        /// <summary>
        /// Builds a new PTWrite Messages Command instance.
        /// </summary>
        /// <param name="CommandInput">Input Command Lines</param>
        public PassThruWriteMessagesExpression(string CommandInput) : base(CommandInput, PassThruExpressionType.PTWriteMsgs)
        { 
            // Find command issue request values
            var FieldsToSet = this.GetExpressionProperties();
            bool PtWriteMsgsResult = this.PtWriteMessagesRegex.Evaluate(CommandInput, out var PassThruWriteMsgsStrings);
            bool MessagesWrittenResult = this.MessagesWrittenRegex.Evaluate(CommandInput, out var MessagesSentStrings);
            if (!PtWriteMsgsResult || !MessagesWrittenResult) 
                this.ExpressionLogger.WriteLog($"FAILED TO REGEX OPERATE ON ONE OR MORE TYPES FOR EXPRESSION TYPE {this.GetType().Name}!");

            // Find our values to store here and add them to our list of values.
            List<string> StringsToApply = new List<string> { PassThruWriteMsgsStrings[0] };
            StringsToApply.AddRange(this.PtWriteMessagesRegex.ExpressionValueGroups
                .Where(NextIndex => NextIndex <= PassThruWriteMsgsStrings.Length)
                .Select(NextIndex => PassThruWriteMsgsStrings[NextIndex]));
            StringsToApply.AddRange(this.MessagesWrittenRegex.ExpressionValueGroups
                .Where(NextIndex => NextIndex <= MessagesSentStrings.Length)
                .Select(NextIndex => MessagesSentStrings[NextIndex]));

            // Find our message content values here.
            string MessageTable = this.FindMessageContents(out this.MessageProperties);
            if (MessageTable is "" or "No Messages Found!") 
                this.ExpressionLogger.WriteLog($"WARNING! NO MESSAGES FOUND FOR EXPRESSION TYPE {this.GetType().Name}!", LogType.WarnLog);

            // Now apply values using base method and exit out of this routine
            if (!this.SetExpressionProperties(FieldsToSet, StringsToApply.ToArray()))
                throw new InvalidOperationException($"FAILED TO SET CLASS VALUES FOR EXPRESSION OBJECT OF TYPE {this.GetType().Name}!");
        }
    }
}
