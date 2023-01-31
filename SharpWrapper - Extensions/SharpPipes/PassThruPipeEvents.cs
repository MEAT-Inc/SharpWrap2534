using System;

namespace SharpPassThruPipes
{
    /// <summary>
    /// Args for when the state of a pipe is modified
    /// </summary>
    public class PipeStateEventArgs : EventArgs
    {
        // Values for our new event args
        public DateTime TimeChanged;
        public PassThruPipeStates OldState;
        public PassThruPipeStates NewState;

        /// <summary>
        /// Builds our new instance of the event args
        /// </summary>
        public PipeStateEventArgs()
        {
            // Store time of event changed
            this.TimeChanged = DateTime.Now;
        }
    }
    /// <summary>
    /// Event object fired when there's new data processed onto our pipe reader
    /// </summary>
    public class PipeDataEventArgs : EventArgs
    {
        // Properties of the pipe data we processed in.
        public readonly DateTime TimeProcessed;
        public uint ByteDataLength;
        public byte[] PipeByteData;
        public string PipeDataString;
        public uint PipeDataStringLength;

        /// <summary>
        /// Builds new event arguments for a pipe reader processing state
        /// </summary>
        public PipeDataEventArgs()
        {
            // Store time of pipe data processed
            this.TimeProcessed = DateTime.Now; ;
        }
    }
}
