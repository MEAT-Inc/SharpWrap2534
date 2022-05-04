using System.ComponentModel;
using System.Runtime.Serialization;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace SharpExpressions.ExpressionTypes
{
    /// <summary>
    /// The names of the command types.
    /// Matches a type for the PT Command to a regex class type.
    /// </summary>
    [JsonConverter(typeof(StringEnumConverter))]
    public enum PassThruCommandType
    {
        // Command Types for PassThru Regex. Pulled values from settings parse into here.
        [EnumMember(Value = "NONE")] [Description("PassThruExpresssion")] NONE,
        [EnumMember(Value = "PTOpen")] [Description("PassThruOpenExpression")] PTOpen,
        [EnumMember(Value = "PTClose")] [Description("PassThruCloseExpression")] PTClose,
        [EnumMember(Value = "PTConnect")] [Description("PassThruConnectExpression")] PTConnect,
        [EnumMember(Value = "PTDisconnect")] [Description("PassThruDisconnectExpression")] PTDisconnect,
        [EnumMember(Value = "PTReadMsgs")] [Description("PassThruReadMessagesExpression")] PTReadMsgs,
        [EnumMember(Value = "PTWriteMsgs")] [Description("PassThruWriteMessagesExpression")] PTWriteMsgs,
        // TODO: Write PTStartPeriodic (May be needed for Sims)
        // TODO: Write PTStopPeriodic (May be needed for Sims)
        [EnumMember(Value = "PTStartMsgFilter")] [Description("PassThruStartMessageFilterExpression")] PTStartMsgFilter,
        [EnumMember(Value = "PTStartMsgFilter")] [Description("PassThruStopMessageFilterExpression")] PTStopMsgFilter,
        // TODO: Write PassThruSetProgrammingVoltage (Not Needed for Sims)
        // TODO: Write PTReadVersion (Not Needed for Sims)
        [EnumMember(Value = "PTIoctl")] [Description("PassThruIoctlExpression")] PTIoctl,
        // TODO: Write PassThruGetLastError (Not needed for Sims)
    }

    // -------------------------------------------------------------------------------------------------------------------
}