using System;
using System.Collections.Generic;

namespace SharpExpressions
{
    /// <summary>
    /// Regex object class for a PTReadMessages command
    /// </summary>
    public class PassThruReadMessagesExpression : PassThruExpression
    {
        // Command for the open command it self
        public readonly PassThruRegexModel MessagesReadRegex = PassThruRegexModelShare.NumberOfMessages;
        public readonly PassThruRegexModel PtReadMessagesRegex = PassThruRegexModelShare.PassThruReadMessages;

        // Strings of the command and results from the command output.
        [PassThruExpression("Command Line")] public readonly string PtCommand;
        [PassThruExpression("Channel ID")] public readonly string ChannelId;
        [PassThruExpression("Channel Pointer")] public readonly string ChannelPointer;
        [PassThruExpression("Message Pointer")] public readonly string MessagePointer;
        [PassThruExpression("Timeout")] public readonly string TimeoutTime;
        [PassThruExpression("Read Count")] public readonly string MessageCountRead;
        [PassThruExpression("Expected Count")] public readonly string MessageCountTotal;

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
        /// Builds a new Regex helper to search for our PTRead Messages Command
        /// </summary>
        /// <param name="CommandInput">Input text for the command to find.</param>
        public PassThruReadMessagesExpression(string CommandInput) : base(CommandInput, PassThruCommandType.PTReadMsgs)
        {
            // Find command issue request values
            var FieldsToSet = this.GetExpressionProperties();
            bool PtReadMsgsResult = this.PtReadMessagesRegex.Evaluate(CommandInput, out var PassThruReadMsgsStrings);
            bool MessagesReadResult = this.MessagesReadRegex.Evaluate(CommandInput, out var MessagesReadStrings);

            // If we failed to pull our read count just send out ? and ?. If it's a complete read count, then we know we're passed so just do 0/0
            if (!PtReadMsgsResult) this.ExpressionLogger.WriteLog($"FAILED TO REGEX OPERATE ON ONE OR MORE TYPES FOR EXPRESSION TYPE {this.GetType().Name}!");
            if (!MessagesReadResult) MessagesReadStrings = CommandInput.Contains("PTReadMsgs() complete") || CommandInput.Contains("Zero messages received")
                ? new[] { "Read 0/0", "0", "0" }
                : new[] { "Read ? of ? messages", "?", "?" };

            // Find our values to store here and add them to our list of values.
            List<string> StringsToApply = new List<string> { PassThruReadMsgsStrings[0] };
            StringsToApply.AddRange(from NextIndex in this.PtReadMessagesRegex.ExpressionValueGroups where NextIndex <= PassThruReadMsgsStrings.Length select PassThruReadMsgsStrings[NextIndex]);
            StringsToApply.AddRange(from NextIndex in this.MessagesReadRegex.ExpressionValueGroups where NextIndex <= MessagesReadStrings.Length select MessagesReadStrings[NextIndex]);

            // Now apply values using base method and exit out of this routine
            this.FindMessageContents(out this.MessageProperties);
            if (!this.SetExpressionProperties(FieldsToSet, StringsToApply.ToArray()))
                throw new InvalidOperationException($"FAILED TO SET CLASS VALUES FOR EXPRESSION OBJECT OF TYPE {this.GetType().Name}!");
        }
    }
}
