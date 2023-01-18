using System;
using System.Collections.Generic;
using System.Linq;
using SharpExpressions.PassThruExtensions;

// Static using for Regex lookups and type values
using PassThruRegex = SharpExpressions.PassThruExpressionRegex;

namespace SharpExpressions.PassThruExpressions
{
    /// <summary>
    /// Class object to help parse out the PTIoctl Control values from a PTIoctl command.
    /// </summary>
    public class PassThruIoctlExpression : PassThruExpression
    {
        // Regex for the IO Control command (PTIoctl)
        public readonly PassThruRegex PtIoctlRegex = PassThruRegex.LoadedExpressions[PassThruExpressionType.PTIoctl];

        // Strings of the command and results from the command output.
        [PassThruProperty("Command Line")] public readonly string PtCommand;
        [PassThruProperty("Channel ID")] public readonly string ChannelID;
        [PassThruProperty("IOCTL Type")] public readonly string IoctlType;
        [PassThruProperty("IOCTL Input")] public readonly string IoctlInputStruct;
        [PassThruProperty("IOCTL Output")] public readonly string IoctlOutputStruct;
        [PassThruProperty("Parameter Count")] public readonly string ParameterCount;

        // Number of Parameters and values for the IOCTL command.
        public readonly Tuple<string, string, string>[] ParameterValues;

        // ------------------------------------------------------------------------------------------

        /// <summary>
        /// Builds a new instance of a PTIoctl expression for parsing a PTIoctl command.
        /// </summary>
        /// <param name="CommandInput">Input command lines.</param>
        public PassThruIoctlExpression(string CommandInput) : base(CommandInput, PassThruExpressionType.PTIoctl)
        {
            // Find command issue request values
            var FieldsToSet = this.GetExpressionProperties();
            bool PtIoctlResult = this.PtIoctlRegex.Evaluate(CommandInput, out var PassThruIoctlStrings);
            if (!PtIoctlResult) this.ExpressionLogger.WriteLog($"FAILED TO REGEX OPERATE ON ONE OR MORE TYPES FOR EXPRESSION TYPE {this.GetType().Name}!");

            // Find our values to store here and add them to our list of values.
            List<string> StringsToApply = new List<string> { PassThruIoctlStrings[0] };
            StringsToApply.AddRange(this.PtIoctlRegex.ExpressionValueGroups
                .Where(NextIndex => NextIndex <= PassThruIoctlStrings.Length)
                .Select(NextIndex => PassThruIoctlStrings[NextIndex]));

            // Now build the Ioctl Parameters from the input content if any exist.
            this.FindIoctlParameters(out this.ParameterValues);
            this.ParameterCount = this.ParameterValues.Length == 0 ? "No Parameters" : $"{this.ParameterValues.Length} Parameters";
            
            // Now apply values using base method and exit out of this routine
            StringsToApply.Add(this.ParameterCount);
            if (!this.SetExpressionProperties(FieldsToSet, StringsToApply.ToArray()))
                throw new InvalidOperationException($"FAILED TO SET CLASS VALUES FOR EXPRESSION OBJECT OF TYPE {this.GetType().Name}!");
        }
    }
}

