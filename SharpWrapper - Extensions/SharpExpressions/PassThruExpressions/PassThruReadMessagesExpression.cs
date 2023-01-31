using System;
using System.Collections.Generic;
using System.Linq;

// Static using for Regex lookups and type values
using PassThruRegex = SharpExpressions.PassThruExpressionRegex;

namespace SharpExpressions.PassThruExpressions
{
    /// <summary>
    /// Regex object class for a PTReadMessages command
    /// </summary>
    public class PassThruReadMessagesExpression : PassThruExpression
    {
        // Regex for the read messages command (PTReadMsgs) and the number of messages processed 
        public readonly PassThruRegex PtReadMessagesRegex = PassThruRegex.LoadedExpressions[PassThruExpressionType.PTReadMsgs];
        public readonly PassThruRegex MessagesReadRegex = PassThruRegex.LoadedExpressions[PassThruExpressionType.MessageCount];
        
        // Strings of the command and results from the command output.
        [PassThruProperty("Command Line")] public readonly string PtCommand;
        [PassThruProperty("Channel ID")] public readonly string ChannelId;
        [PassThruProperty("Channel Pointer")] public readonly string ChannelPointer;
        [PassThruProperty("Message Pointer")] public readonly string MessagePointer;
        [PassThruProperty("Timeout")] public readonly string TimeoutTime;
        [PassThruProperty("Read Count")] public readonly string MessageCountRead;
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
        /// Builds a new Regex helper to search for our PTRead Messages Command
        /// </summary>
        /// <param name="CommandInput">Input text for the command to find.</param>
        public PassThruReadMessagesExpression(string CommandInput) : base(CommandInput, PassThruExpressionType.PTReadMsgs)
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
            StringsToApply.AddRange(this.PtReadMessagesRegex.ExpressionValueGroups
                .Where(NextIndex => NextIndex <= PassThruReadMsgsStrings.Length)
                .Select(NextIndex => PassThruReadMsgsStrings[NextIndex]));
            StringsToApply.AddRange(this.MessagesReadRegex.ExpressionValueGroups
                .Where(NextIndex => NextIndex <= MessagesReadStrings.Length)
                .Select(NextIndex => MessagesReadStrings[NextIndex]));

            // Now apply values using base method and exit out of this routine
            this.FindMessageContents(out this.MessageProperties);
            if (!this.SetExpressionProperties(FieldsToSet, StringsToApply.ToArray()))
                throw new InvalidOperationException($"FAILED TO SET CLASS VALUES FOR EXPRESSION OBJECT OF TYPE {this.GetType().Name}!");
        }
    }
}
