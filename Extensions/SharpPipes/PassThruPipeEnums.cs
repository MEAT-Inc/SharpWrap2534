namespace SharpPipes
{
    /// <summary>
    /// Enums for pipe types
    /// </summary>
    public enum PassThruPipeTypes
    {
        ReaderPipe,      // Pipe number 1 (Input)
        WriterPipe,      // Pipe number 2 (Output)
    }
    /// <summary>
    /// Possible states for our pipe objects.
    /// </summary>
    public enum PassThruPipeStates
    {
        Faulted,            // Failed to build
        Open,               // Open and not connected
        Connected,          // Connected
        Disconnected,       // Disconnected
        Closed,             // Open but closed manually
    }
}
