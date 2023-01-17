using System;
using System.Collections.Generic;
using System.Linq;

// Static using for Regex lookups and type values
using PassThruRegex = SharpExpressions.PassThruExpressionRegex;

namespace SharpExpressions.PassThruExpressions
{
    /// <summary>
    /// Class object for regex parsing out a PTStopMsgFilter command instance.
    /// </summary>
    public class PassThruStopMessageFilterExpression : PassThruExpression
    {
        // Regex for the stop message filter command (PTStopMsgFilter)
        public readonly PassThruRegex PtStopMsgFilterRegex = PassThruRegex.GetRegexByType(PassThruExpressionType.PTStopMsgFilter);

        // Strings of the command and results from the command output.
        [PassThruProperty("Command Line")] public readonly string PtCommand;
        [PassThruProperty("Channel ID")] public readonly string ChannelID;
        [PassThruProperty("Filter ID")] public readonly string FilterID;

        // ----------------------------------------------------------------------------------------------------

        /// <summary>
        /// Builds a new instance of a stop filter parsing command.
        /// </summary>
        /// <param name="CommandInput"></param>
        public PassThruStopMessageFilterExpression(string CommandInput) : base(CommandInput, PassThruExpressionType.PTStopMsgFilter)
        {
            // Find command issue request values
            var FieldsToSet = this.GetExpressionProperties();
            bool PtStopFilterResult = this.PtStopMsgFilterRegex.Evaluate(CommandInput, out var PassThruFilterStrings);
            if (!PtStopFilterResult) this.ExpressionLogger.WriteLog($"FAILED TO REGEX OPERATE ON ONE OR MORE TYPES FOR EXPRESSION TYPE {this.GetType().Name}!");

            // Find our values to store here and add them to our list of values.
            List<string> StringsToApply = new List<string> { PassThruFilterStrings[0] };
            StringsToApply.AddRange(this.PtStopMsgFilterRegex.ExpressionValueGroups
                .Where(NextIndex => NextIndex <= PassThruFilterStrings.Length)
                .Select(NextIndex => PassThruFilterStrings[NextIndex]));

            // Now apply values using base method and exit out of this routine
            if (!this.SetExpressionProperties(FieldsToSet, StringsToApply.ToArray()))
                throw new InvalidOperationException($"FAILED TO SET CLASS VALUES FOR EXPRESSION OBJECT OF TYPE {this.GetType().Name}!");
        }
    }
}
