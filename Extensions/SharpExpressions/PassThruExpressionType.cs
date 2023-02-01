using System;
using System.ComponentModel;
using System.Linq;
using System.Runtime.Serialization;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace SharpExpressions
{
    /// <summary>
    /// The names of the command types.
    /// Matches a type for the PT Command to a regex class type.
    /// </summary>
    [JsonConverter(typeof(StringEnumConverter))]
    public enum PassThruExpressionType
    {
        // Expression types for the different base PassThruCommands supported for parsing
        [EnumMember(Value = "NONE")] [Description("PassThruExpresssion")] NONE,
        [EnumMember(Value = "PTOpen")] [Description("PassThruOpenExpression")] PTOpen,
        [EnumMember(Value = "PTClose")] [Description("PassThruCloseExpression")] PTClose,
        [EnumMember(Value = "PTIoctl")] [Description("PassThruIoctlExpression")] PTIoctl,
        [EnumMember(Value = "PTConnect")] [Description("PassThruConnectExpression")] PTConnect,
        [EnumMember(Value = "PTDisconnect")] [Description("PassThruDisconnectExpression")] PTDisconnect,
        [EnumMember(Value = "PTReadMsgs")] [Description("PassThruReadMessagesExpression")] PTReadMsgs,
        [EnumMember(Value = "PTWriteMsgs")] [Description("PassThruWriteMessagesExpression")] PTWriteMsgs,
        [EnumMember(Value = "PTStartMsgFilter")] [Description("PassThruStartMessageFilterExpression")] PTStartMsgFilter,
        [EnumMember(Value = "PTStartMsgFilter")] [Description("PassThruStopMessageFilterExpression")] PTStopMsgFilter,
        // TODO: Write PTStartPeriodic (May be needed for Sims)
        // TODO: Write PTStopPeriodic (May be needed for Sims)
        // TODO: Write PassThruSetProgrammingVoltage (Not Needed for Sims)
        // TODO: Write PTReadVersion (Not Needed for Sims)
        // TODO: Write PassThruGetLastError (Not needed for Sims)

        // Expression types for the different supporting Regex objects used to pull command values
        [EnumMember(Value = "Filter ID")] FilterID,                              // Supporting regex for filter ID values returned
        [EnumMember(Value = "Device ID")] DeviceID,                              // Supporting regex for Device ID values returned
        [EnumMember(Value = "Channel ID")] ChannelID,                            // Supporting regex for channel ID values returned
        [EnumMember(Value = "Message Data")] MessageData,                        // Supporting regex for message data read or sent
        [EnumMember(Value = "Command Time")] CommandTime,                        // Supporting regex for time commands were issued
        [EnumMember(Value = "Message Count")] MessageCount,                      // Supporting regex for message counts
        [EnumMember(Value = "Command Status")] CommandStatus,                    // Supporting regex for command status values
        [EnumMember(Value = "Message Sent Info")] MessageSentInfo,               // Supporting regex for sent message objects
        [EnumMember(Value = "Message Read Info")] MessageReadInfo,               // Supporting regex for read message objects
        [EnumMember(Value = "Message Filter Info")] MessageFilterInfo,           // Supporting regex for message filter objects
        [EnumMember(Value = "Ioctl Parameter Info")] IoctlParameterInfo,         // Supporting regex for PTIoctl parameter objects
        [EnumMember(Value = "Command Parameter Info")] CommandParameterInfo,     // Supporting regex for the parameters of a command

        // Expression types used for importing existing expressions files
        [EnumMember(Value = "Import Expressions Split")] ImportExpressionsSplit,        // Supporting regex for splitting input expression file content
        [EnumMember(Value = "Import Expressions Replace")] ImportExpressionsReplace,    // Supporting regex for replacing content in imported expressions
    }

    // ----------------------------------------------------------------------------------------------------------------------------------------------

    /// <summary>
    /// Set of static helper methods used to pull in the PTCommand Type as an extension class.
    /// </summary>
    public static class PassThruExpressionTypeExtensions
    {
        /// <summary>
        /// Finds a PTCommand type from the given input line set
        /// </summary>
        /// <param name="InputLines">Lines to find the PTCommand Type for.</param>
        /// <returns>The type of PTCommand regex to search with.</returns>
        public static PassThruExpressionType ToPassThruCommandType(this string[] InputLines)
        {
            // Return the result from our joined line output.
            return ToPassThruCommandType(string.Join("\n", InputLines.Select(Input => Input.TrimEnd())));
        }
        /// <summary>
        /// Finds a PTCommand type from the given input line set
        /// </summary>
        /// <param name="InputLines">Lines to find the PTCommand Type for.</param>
        /// <returns>The type of PTCommand regex to search with.</returns>
        public static PassThruExpressionType ToPassThruCommandType(this string InputLines)
        {
            // Find the type of command by converting all enums to string array and searching for the type.
            var EnumTypesArray = Enum.GetValues(typeof(PassThruExpressionType))
                .Cast<PassThruExpressionType>()
                .Select(PtEnumValue => PtEnumValue.ToString())
                .ToArray();

            // Find the return type here based on the first instance of a PTCommand type object on the array.
            var EnumStringSelected = EnumTypesArray.FirstOrDefault(InputLines.Contains);
            return (PassThruExpressionType)(string.IsNullOrWhiteSpace(EnumStringSelected) 
                ? PassThruExpressionType.NONE 
                : Enum.Parse(typeof(PassThruExpressionType), EnumStringSelected));
        }
    }
}