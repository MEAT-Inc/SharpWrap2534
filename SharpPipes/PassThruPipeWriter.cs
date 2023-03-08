using System;
using System.Diagnostics;
using System.IO.Pipes;
using System.Threading;
using System.Threading.Tasks;
using SharpLogging;

namespace SharpPipes
{
    /// <summary>
    /// Fulcrum pipe writing class. Sends data out to our DLLs
    /// </summary>
    public sealed class PassThruPipeWriter : PassThruPipe
    {
        #region Custom Events
        #endregion // Custom Events

        #region Fields

        // Pipe writer and token sources for aborting routines
        private readonly NamedPipeServerStream _fulcrumPipe;
        private CancellationTokenSource _asyncConnectionTokenSource;

        // Singleton configuration to avoid building more than one instance of 
        private static PassThruPipeWriter _pipeInstance;
        private static Lazy<PassThruPipeWriter> _lazyReader;

        #endregion // Fields

        #region Properties

        // Sets if we can run a new connection or not
        public static bool IsConnecting { get; private set; }
        
        #endregion // Properties

        #region Structs and Classes
        #endregion // Structs and Classes

        // ------------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Builds a new outbound pipe sender
        /// </summary>
        private PassThruPipeWriter() : base(PassThruPipeTypes.WriterPipe)
        {
            // Build the pipe object here.
            this._fulcrumPipe = new NamedPipeServerStream(
                base.ReaderPipeName,      // Name of the pipe host
                PipeDirection.Out         // Direction of the pipe host      
            );

            // Build event helper for state changed.
            this.PipeStateChanged += (PipeObj, SendingArgs) =>
            {
                // Check if currently connecting or not.
                if (IsConnecting) return;
                if (IsConnecting || SendingArgs.NewState != PassThruPipeStates.Open) return;
                
                // Now run the connection routine and wait for results
                this.PipeLogger.WriteLog("DETECTED A NEW STATE OF OPEN FOR OUR PIPE WRITER! TRYING TO CONNECT IT NOW...", LogType.WarnLog);
                this.StartPipeConnectionAsync();
            };

            // Build our new pipe instance here and wait for it to update a state value at some point
            this.StartPipeConnectionAsync();
        }
        /// <summary>
        /// Singleton constructor for the PassThrWriter pipe type.
        /// </summary>
        /// <returns>A built pipe reader instance which is being used to connect</returns>
        /// <exception cref="InvalidOperationException">Thrown when the pipe instance built appears to be null</exception>
        public static PassThruPipeWriter AllocatePipe()
        {
            // Configure a new lazy reader instance if needed
            if (_lazyReader == null)
            {
                // Build a new lazy reader and store the value of it as our reader instance
                _lazyReader = new(() => new PassThruPipeWriter());
                _pipeInstance = _lazyReader.Value;
            }
            if (_pipeInstance == null)
            {
                // Store the pipe type and log the issue out 
                string PipeType = _lazyReader.Value.GetType().Name;
                throw new InvalidOperationException($"Error! Failed to create new pipe instance for type {PipeType}!");
            }

            // Return out the newly build pipe instance here
            return _pipeInstance;
        }

        // ------------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Async connects to our client on the reader side of operations
        /// </summary>
        /// <returns>The task being used to issue a new connection routine</returns>
        public override Task<bool> StartPipeConnectionAsync()
        {
            // Check if connected already or not
            if (this._fulcrumPipe.IsConnected || IsConnecting)
            {
                // Check what condition we hit
                this.PipeLogger.WriteLog(
                    IsConnecting
                        ? "CAN NOT FORCE A NEW CONNECTION ATTEMPT WHILE A PREVIOUS ONE IS ACTIVE!"
                        : "PIPE WAS ALREADY CONNECTED! RETURNING OUT NOW...", LogType.WarnLog);

                // Exit this method and return an empty task for our connection routine
                return new Task<bool>(() => false);
            }

            // Apply it based on values pulled and try to open a new client
            Stopwatch ConnectionTimeStopwatch = new Stopwatch();
            this.PipeLogger.WriteLog("STARTING WRITER PIPE CONNECTION ROUTINE NOW...", LogType.WarnLog);
            this.PipeLogger.WriteLog("PIPE HOST WRITER STREAM HAS BEEN CONFIGURED! ATTEMPTING TO FIND CLIENTS FOR IT NOW...", LogType.WarnLog);
            this.PipeLogger.WriteLog($"WAITING FOR NEW CLIENT ENDLESSLY BEFORE BREAKING OUT OF SETUP METHODS!", LogType.WarnLog);

            // Build a new task and start it up to get our pipe connections
            return Task.Run(() =>
            {
                try
                {
                    // Run a task while the connected value is false
                    IsConnecting = true;
                    ConnectionTimeStopwatch.Start();
                    this._fulcrumPipe.WaitForConnectionAsync();

                    // If we're connected, log that information and break out
                    this.PipeState = PassThruPipeStates.Connected;
                    this.PipeLogger.WriteLog("CONNECTED NEW CLIENT INSTANCE!", LogType.WarnLog);
                    this.PipeLogger.WriteLog($"PIPE CLIENT CONNECTED TO FULCRUM PIPER {this.PipeTypes} OK!", LogType.InfoLog);
                    this.PipeLogger.WriteLog($"ESTIMATED {ConnectionTimeStopwatch.ElapsedMilliseconds} MILLISECONDS ELAPSED FOR CLIENT CONNECTION!", LogType.WarnLog);

                    // Toggle the IsConnecting state and exit out with a passed result
                    IsConnecting = false;
                    return true;
                }
                catch (Exception PipeConnectionEx)
                {
                    // Log the exception and reset our connection state values
                    IsConnecting = false;
                    this.PipeState = PassThruPipeStates.Faulted;
                    this.PipeLogger.WriteLog($"FAILED TO CONNECT TO OUR PIPE INSTANCE FOR PIPE ID {this.PipeTypes}!", LogType.ErrorLog);
                    this.PipeLogger.WriteLog("EXCEPTION THROWN DURING CONNECTION OR STREAM OPERATIONS FOR THIS PIPE CONFIGURATION!", LogType.ErrorLog);
                    this.PipeLogger.WriteException("EXCEPTION THROWN IS BEING LOGGED BELOW", PipeConnectionEx);

                    // Return out failed from this routine
                    return false;
                }
            });
        }
        /// <summary>
        /// Kills our writing pipe connection process if the process is currently running
        /// </summary>
        /// <returns>True if the pipe connection attempt is stopped. False if it is not</returns>
        public override bool AbortAsyncPipeConnection()
        {
            // Check if the source or token are null
            if (this._asyncConnectionTokenSource == null)
            {
                this.PipeLogger.WriteLog("TOKENS AND SOURCES WERE NOT YET CONFIGURED WHICH MEANS CONNECTING WAS NOT STARTED!", LogType.WarnLog);
                return false;
            }

            // Cancel here and return
            this.PipeLogger.WriteLog("CANCELING ACTIVE CONNECTION TASK NOW...", LogType.InfoLog);
            this._asyncConnectionTokenSource.Cancel(false);
            this.PipeLogger.WriteLog("CANCELED BACKGROUND ACTIVITY OK!", LogType.WarnLog);
            return true;
        }
    }
}
