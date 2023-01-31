using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using SharpLogger.LoggerObjects;
using SharpLogger.LoggerSupport;

namespace SharpPassThruPipes
{
    /// <summary>
    /// Instance object for reading pipe server data from our fulcrum DLL
    /// </summary>
    public class PassThruPipe 
    {
        #region Custom Events

        // Event handler for when a pipe state value is updated
        public event EventHandler<PipeStateEventArgs> PipeStateChanged;

        #endregion // Custom Events

        #region Fields

        // Pipe configuration information which is used to help us configure our pipe instances 
        public readonly string PipeLocation;                // Path of the pipe instance
        internal readonly SubServiceLogger PipeLogger;      // The logger object used to pull in our Pipe values

        // Backing fields for pipe configurations and states
        protected PassThruPipeStates _pipeState;                                        // The state of this pipe instance (backing field)
        public readonly PassThruPipeTypes PipeTypes;                                    // The type of pipe being used in this instance
        private string _readerPipeLocation = "1D16333944F74A928A932417074DD2B3";        // The path location to the reader pipe instance
        private string _writerPipeLocation = "2CC3F0FB08354929BB453151BBAA5A15";        // The path location to the writer pipe instance

        // Backing information about the Injector DLL path to use. Also shows the default debug path and release path
        private readonly string _debugDllPath = "..\\..\\..\\FulcrumShim\\Debug\\FulcrumShim.dll";
        private readonly string _releaseDllPath = "C:\\Program Files (x86)\\MEAT Inc\\FulcrumShim\\FulcrumShim.dll";

        #endregion // Fields

        #region Properties

        // Public facing properties which hold the state and configuration of our different pipe types
        public PassThruPipeStates PipeState
        {
            get => this._pipeState;
            protected set
            {
                // Fire new event state args for the hooked event handlers
                this.PipeStateChanged?.Invoke(this, new PipeStateEventArgs()
                {
                    NewState = value,
                    OldState = this._pipeState,
                    TimeChanged = DateTime.Now
                });

                // Store new value objects and invoke a property changed event
                this._pipeState = value;
                PipeLogger?.WriteLog($"PIPE {this.PipeTypes} STATE IS NOW: {this._pipeState}", LogType.TraceLog);
            }
        }
        public string ReaderPipeName => this._readerPipeLocation;
        public string WriterPipeName => this._writerPipeLocation;
        public string FulcrumShimDLL => Debugger.IsAttached ? _debugDllPath : _releaseDllPath;
        public bool FulcrumShimLoaded 
        {
            get
            {
                try
                {
                    // Find if the file is locked or not. Get path to validate and attempt to load it in as a stream
                    FileStream DllStream = File.Open(this.FulcrumShimDLL, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
                    DllStream.Close();

                    // Return not locked here.
                    return false;
                }
                catch (Exception LoadDLLEx)
                {
                    // If we've got a not found exception, return true
                    if (LoadDLLEx is FileNotFoundException) return true;

                    // If it's not a file missing issue, then return false and log the exception
                    PipeLogger.WriteLog("EXCEPTION THROWN DURING DLL IN USE CHECK!", LogType.ErrorLog);
                    PipeLogger.WriteLog($"DLL FILE PROVIDED AT LOCATION {this.FulcrumShimDLL} COULD NOT BE FOUND!", LoadDLLEx);
                    return false; 
                }
            }
        }

        #endregion // Properties

        #region Structs and Classes

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

        #endregion // Structs and Classes

        // ---------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Builds a new pass thru pipe wrapper class
        /// </summary>
        /// <param name="PipeType">ID Of the pipe in use for this object</param>
        protected PassThruPipe(PassThruPipeTypes PipeType)
        {
            // Configure logger object.
            this.PipeState = PassThruPipeStates.Faulted;
            this.PipeLogger = new SubServiceLogger($"{PipeType}", UseAsync: true);
            this.PipeLogger.WriteLog($"BUILT NEW PIPE LOGGER FOR PIPE TYPE {PipeType} OK!", LogType.InfoLog);

            // Store information about the pipe being configured
            this.PipeTypes = PipeType;
            this.PipeLocation = this.PipeTypes == PassThruPipeTypes.ReaderPipe ? WriterPipeName : ReaderPipeName;
            this.PipeLogger.WriteLog("STORED NEW PIPE DIRECTIONAL INFO AND TYPE ON THIS INSTANCE CORRECTLY!", LogType.InfoLog);
        }
        /// <summary>
        /// Builds and returns a new pipe instance based on the Pipe type value provided
        /// </summary>
        /// <param name="PipeType">The type of pipe we wish to use for this routine</param>
        /// <param name="ConnectionTask">The running async connection routine used to help find when pipes connect</param>
        /// <returns>A newly built PassThruPipe (Reader or writer) based on the type value provided</returns>
        /// <exception cref="InvalidCastException">Thrown when the pipe type provided is impossible</exception>
        public static PassThruPipe AllocatePipe(PassThruPipeTypes PipeType, out Task<bool> ConnectionTask)
        {
            // Build and return out the connection task object based on the pipe helper type
            ConnectionTask = null;
            return PipeType switch
            {
                // See if we're a reader or writer pipe first
                PassThruPipeTypes.ReaderPipe => PassThruPipeReader.AllocatePipe(out ConnectionTask),
                PassThruPipeTypes.WriterPipe => PassThruPipeWriter.AllocatePipe(out ConnectionTask),
                _ => throw new InvalidCastException("Error! Pipe object did not have a defined Reader or Writer type!")
            };
        }

        // ---------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Base method for new pipe init configuration to attempt and pull in new pipe creation tasks
        /// </summary>
        /// <returns>The task object used to build connections to our pipe instances</returns>
        /// <exception cref="InvalidCastException">Throw when this instance is an invalid type for a pipe</exception>
        public virtual Task<bool> StartPipeConnectionAsync()
        {
            // Build and return out the connection task object based on the pipe helper type
            return this switch
            {
                // See if we're a reader or writer pipe first
                PassThruPipeReader ReaderPipe => ReaderPipe.StartPipeConnectionAsync(),
                PassThruPipeWriter WriterPipe => WriterPipe.StartPipeConnectionAsync(),
                _ => throw new InvalidCastException("Error! Pipe object did not have a defined Reader or Writer type!")
            };
        }
        /// <summary>
        /// Base method for pipe connection abort routines.
        /// Should really never be used but it's good to have this defined
        /// </summary>
        /// <returns>True if the pipe is stopped. False if not</returns>
        /// <exception cref="InvalidCastException">Throw when this instance is an invalid type for a pipe</exception>
        public virtual bool AbortAsyncPipeConnection()
        {
            // Build and return out the connection task object based on the pipe helper type
            return this switch
            {
                // See if we're a reader or writer pipe first
                PassThruPipeReader ReaderPipe => ReaderPipe.AbortAsyncPipeConnection(),
                PassThruPipeWriter WriterPipe => WriterPipe.AbortAsyncPipeConnection(),
                _ => throw new InvalidCastException("Error! Pipe object did not have a defined Reader or Writer type!")
            };
        }
    }
}
