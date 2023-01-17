using System;
using System.Collections.Generic;
using System.Linq;

// Static using for Regex lookups and type values
using PassThruRegex = SharpExpressions.PassThruExpressionRegex;

namespace SharpExpressions.PassThruExpressions
{
    /// <summary>
    /// Set of Regular Expressions for the PTConnect Command
    /// </summary>
    public class PassThruConnectExpression : PassThruExpression
    {
        // Regex for the connect channel command (PTConnect) and the channel ID returned
        public readonly PassThruRegex PtConnectRegex = PassThruRegex.GetRegexByType(PassThruExpressionType.PTConnect);
        public readonly PassThruRegex ChannelIdRegex = PassThruRegex.GetRegexByType(PassThruExpressionType.ChannelId);

        // Strings of the command and results from the command output.
        [PassThruProperty("Command Line")] public readonly string PtCommand;
        [PassThruProperty("Device ID")] public readonly string DeviceId;
        [PassThruProperty("Protocol ID")] public readonly string ProtocolId;
        [PassThruProperty("Connect Flags")] public readonly string ConnectFlags;
        [PassThruProperty("BaudRate")] public readonly string BaudRate;
        [PassThruProperty("Channel Pointer")] public readonly string ChannelPointer;
        [PassThruProperty("Channel ID", "-1", new[] { "Channel Opened", "Invalid Channel!"}, true)] 
        public readonly string ChannelId;

        // ----------------------------------------------------------------------------------------------------

        /// <summary>
        /// Builds a new Regex helper to search for our PTConnect Command
        /// </summary>
        /// <param name="CommandInput">Input text for the command to find.</param>
        public PassThruConnectExpression(string CommandInput) : base(CommandInput, PassThruExpressionType.PTConnect)
        {
            // Find command issue request values
            var FieldsToSet = this.GetExpressionProperties();
            bool PtConnectResult = this.PtConnectRegex.Evaluate(CommandInput, out var PassThruConnectStrings);
            bool ChannelIdResult = this.ChannelIdRegex.Evaluate(CommandInput, out var ChannelIdStrings);
            if (!PtConnectResult || !ChannelIdResult) this.ExpressionLogger.WriteLog($"FAILED TO REGEX OPERATE ON ONE OR MORE TYPES FOR EXPRESSION TYPE {this.GetType().Name}!");

            // Find our values to store here and add them to our list of values.
            List<string> StringsToApply = new List<string> { PassThruConnectStrings[0] };
            StringsToApply.AddRange(this.PtConnectRegex.ExpressionValueGroups
                .Where(NextIndex => NextIndex <= PassThruConnectStrings.Length)
                .Select(NextIndex => PassThruConnectStrings[NextIndex]));
            StringsToApply.AddRange(this.ChannelIdRegex.ExpressionValueGroups
                .Where(NextIndex => NextIndex <= ChannelIdStrings.Length)
                .Select(NextIndex => ChannelIdStrings[NextIndex]));
          
            // Now apply values using base method and exit out of this routine
            if (!this.SetExpressionProperties(FieldsToSet, StringsToApply.ToArray()))
                throw new InvalidOperationException($"FAILED TO SET CLASS VALUES FOR EXPRESSION OBJECT OF TYPE {this.GetType().Name}!");
        }
    }
}
