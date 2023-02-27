using System;
using System.ComponentModel;
using System.Linq;
using System.Runtime.Serialization;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace SharpExpressions
{
    /// <summary>
    /// Extension class for pulling description attributes from the enums
    /// </summary>
    internal static class EnumLibExtensions
    {
        /// <summary>
        /// Gets a descriptor string for the enum type provided.
        /// </summary>
        /// <param name="EnumValue">Enum to convert/get description on</param>
        /// <returns>Enum description</returns>
        public static string ToDescriptionString<TEnumType>(this TEnumType EnumValue)
        {
            // Get the descriptor from the enum attributes pulled.
            DescriptionAttribute[] EnumAtribs = (DescriptionAttribute[])EnumValue
               .GetType()
               .GetField(EnumValue.ToString())
               .GetCustomAttributes(typeof(DescriptionAttribute), false);
            return EnumAtribs.Length > 0 ? EnumAtribs[0].Description : string.Empty;
        }
        /// <summary>
        /// Converts an input string (enum descriptor) into an output enum object.
        /// </summary>
        /// <param name="EnumDescription">The enum object output we wish to use.</param>
        /// <returns>A parsed enum if passed. Otherwise an invalid arg exception is thrown</returns>
        public static TEnumType FromDescriptionString<TEnumType>(this string EnumDescription)
        {
            // Find the types first, then pull the potential file value types.
            foreach (var EnumFieldObj in typeof(TEnumType).GetFields())
            {
                // Check the attributes here. If one matches the type provided and the description is correct, return it.
                if (Attribute.GetCustomAttribute(EnumFieldObj, typeof(DescriptionAttribute)) is DescriptionAttribute EnumAtrib)
                    if (EnumAtrib.Description == EnumDescription) return (TEnumType)EnumFieldObj.GetValue(null);
                    else { if (EnumFieldObj.Name == EnumDescription) return (TEnumType)EnumFieldObj.GetValue(null); }
            }

            // Throw invalid description type 
            throw new ArgumentException($"Unable to convert the input type {EnumDescription} to a valid MessengerHubTypes enum", nameof(EnumDescription));
        }
    }

    /// <summary>
    /// The names of the command types.
    /// Matches a type for the PT Command to a regex class type.
    /// </summary>
    [JsonConverter(typeof(StringEnumConverter))]
    public enum PassThruExpressionTypes
    {
        // Expression types for the different base PassThruCommands supported for parsing
        [EnumMember(Value = "NONE")][Description("PassThruExpresssion")] NONE,
        [EnumMember(Value = "PTOpen")][Description("PassThruOpenExpression")] PTOpen,
        [EnumMember(Value = "PTClose")][Description("PassThruCloseExpression")] PTClose,
        [EnumMember(Value = "PTIoctl")][Description("PassThruIoctlExpression")] PTIoctl,
        [EnumMember(Value = "PTConnect")][Description("PassThruConnectExpression")] PTConnect,
        [EnumMember(Value = "PTDisconnect")][Description("PassThruDisconnectExpression")] PTDisconnect,
        [EnumMember(Value = "PTReadMsgs")][Description("PassThruReadMessagesExpression")] PTReadMsgs,
        [EnumMember(Value = "PTWriteMsgs")][Description("PassThruWriteMessagesExpression")] PTWriteMsgs,
        [EnumMember(Value = "PTStartMsgFilter")][Description("PassThruStartMessageFilterExpression")] PTStartMsgFilter,
        [EnumMember(Value = "PTStartMsgFilter")][Description("PassThruStopMessageFilterExpression")] PTStopMsgFilter,
        // TODO: Write PassThruStartPeriodicMessage (Not Needed for Sims)
        // TODO: Write PassThruStopPeriodicMessage (Not Needed for Sims)
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

}