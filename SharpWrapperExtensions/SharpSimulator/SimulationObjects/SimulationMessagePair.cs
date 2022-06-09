using SharpWrap2534.PassThruTypes;

namespace SharpSimulator.SimulationObjects
{
    /// <summary>
    /// Structure for grouping message objects on simulation channels in a more cleaned up manner
    /// </summary>
    public struct SimulationMessagePair
    {
        // Message read in and the responses to it.
        public PassThruStructs.PassThruMsg MessageRead;
        public PassThruStructs.PassThruMsg[] MessageResponses;

        // ------------------------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Builds a new message pairing for our simulation objects
        /// </summary>
        /// <param name="ReadMessage"></param>
        /// <param name="ResponseMessages"></param>
        public SimulationMessagePair(PassThruStructs.PassThruMsg ReadMessage, PassThruStructs.PassThruMsg[] ResponseMessages)
        {
            // Store values here
            this.MessageRead = ReadMessage;
            this.MessageResponses = ResponseMessages;
        }
    }
}
