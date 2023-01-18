using System;
using System.Collections.Generic;
using System.Linq;

// Static using for Regex lookups and type values
using PassThruRegex = SharpExpressions.PassThruExpressionRegex;

namespace SharpExpressions.PassThruExpressions
{
    /// <summary>
    /// PTClose Command Regex Operations
    /// </summary>
    public class PassThruCloseExpression : PassThruExpression
    {
        // Regex for the connect close device command (PTClose)
        public readonly PassThruRegex PtCloseRegex = PassThruRegex.ExpressionsLoaded[PassThruExpressionType.PTClose];

        // -----------------------------------------------------------------------------------------

        // Strings of the command and results from the command output.
        [PassThruProperty("Command Line")] public readonly string PtCommand;
        [PassThruProperty("Device ID", "-1", new[] { "Device Closed", "Device Invalid!" }, true)] 
        public readonly string DeviceId;    

        // ------------------------------------------------------------------------------------------

        /// <summary>
        /// Builds a new instance of a PTClose Regex type output.
        /// </summary>
        /// <param name="CommandInput">InputLines for the command object strings.</param>
        public PassThruCloseExpression(string CommandInput) : base(CommandInput, PassThruExpressionType.PTClose)
        {
            // Find the PTClose Command Results.
            var FieldsToSet = this.GetExpressionProperties();
            bool PtCloseResult = this.PtCloseRegex.Evaluate(CommandInput, out var PassThruCloseStrings);
            if (!PtCloseResult) this.ExpressionLogger.WriteLog($"FAILED TO REGEX OPERATE ON ONE OR MORE TYPES FOR EXPRESSION TYPE {this.GetType().Name}!");

            // Find our values to store here and add them to our list of values.
            List<string> StringsToApply = new List<string> { PassThruCloseStrings[0] };
            StringsToApply.AddRange(this.PtCloseRegex.ExpressionValueGroups
                .Where(NextIndex => NextIndex <= PassThruCloseStrings.Length)
                .Select(NextIndex => PassThruCloseStrings[NextIndex]));

            // Now apply values using base method and exit out of this routine
            if (!this.SetExpressionProperties(FieldsToSet, StringsToApply.ToArray()))
                throw new InvalidOperationException($"FAILED TO SET CLASS VALUES FOR EXPRESSION OBJECT OF TYPE {this.GetType().Name}!");
        }
    }
}
