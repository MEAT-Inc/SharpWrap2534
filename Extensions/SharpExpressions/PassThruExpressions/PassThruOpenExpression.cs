using System;
using System.Collections.Generic;
using System.Linq;

// Static using for Regex lookups and type values
using PassThruRegex = SharpExpressions.PassThruExpressionRegex;

namespace SharpExpressions.PassThruExpressions
{
    // Regex Values for Different Command Types.
    public class PassThruOpenExpression : PassThruExpression
    {
        // Regex for the open device command (PTOpen) and the properties of the device processed
        public readonly PassThruRegex PtOpenRegex = PassThruRegex.LoadedExpressions[PassThruExpressionType.PTOpen];
        public readonly PassThruRegex DeviceIdRegex = PassThruRegex.LoadedExpressions[PassThruExpressionType.DeviceID];

        // Strings of the command and results from the command output.
        [PassThruProperty("Command Line")] public readonly string PtCommand;
        [PassThruProperty("Device Name")] public readonly string DeviceName;
        [PassThruProperty("Device Pointer")] public readonly string DevicePointer;
        [PassThruProperty("Device ID", "-1", new[] { "Device Opened", "Invalid Device ID!" }, true)] 
        public readonly string DeviceId;

        // ------------------------------------------------------------------------------------------

        /// <summary>
        /// Builds a new PTOpen Regex command type.
        /// </summary>
        /// <param name="CommandInput">Input expression lines to store.</param>
        public PassThruOpenExpression(string CommandInput) : base(CommandInput, PassThruExpressionType.PTOpen)
        {
            // Find command issue request values
            var FieldsToSet = this.GetExpressionProperties();
            bool PtOpenResult = this.PtOpenRegex.Evaluate(CommandInput, out var PassThruOpenStrings);
            bool DeviceIdResult = this.DeviceIdRegex.Evaluate(CommandInput, out var DeviceIdStrings);
            if (!PtOpenResult || !DeviceIdResult) this.ExpressionLogger.WriteLog($"FAILED TO REGEX OPERATE ON ONE OR MORE TYPES FOR EXPRESSION TYPE {this.GetType().Name}!");

            // Find our values to store here and add them to our list of values.
            List<string> StringsToApply = new List<string> { PassThruOpenStrings[0] };
            StringsToApply.AddRange(this.PtOpenRegex.ExpressionValueGroups
                .Where(NextIndex => NextIndex <= PassThruOpenStrings.Length)
                .Select(NextIndex => PassThruOpenStrings[NextIndex]));
            StringsToApply.AddRange(this.DeviceIdRegex.ExpressionValueGroups
                .Where(NextIndex => NextIndex <= DeviceIdStrings.Length)
                .Select(NextIndex => DeviceIdStrings[NextIndex]));

            // Now apply values using base method and exit out of this routine
            if (!this.SetExpressionProperties(FieldsToSet, StringsToApply.ToArray()))
                throw new InvalidOperationException($"FAILED TO SET CLASS VALUES FOR EXPRESSION OBJECT OF TYPE {this.GetType().Name}!");
        }
    }
}
