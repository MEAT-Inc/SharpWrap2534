using SharpWrapper.PassThruTypes;

namespace SharpAutoId.SharpAutoIdModels
{
    /// <summary>
    /// Class object used to declare types for auto id routines
    /// </summary>
    public class SharpIdConfiguration
    {
        // Class values for pulling in new information about an AutoID routine
        public BaudRate ConnectBaud { get; set; }
        public PassThroughConnect ConnectFlags { get; set; }
        public ProtocolId AutoIdType { get; set; }
        public FilterObject[] RoutineFilters { get; set; }
        public MessageObject[] RoutineCommands { get; set; }
    }
}
