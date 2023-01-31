using System;
using System.IO.Pipes;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using SharpLogger.LoggerSupport;

namespace SharpPassThruPipes.J2534Pipes
{
    /// <summary>
    /// Pipe reading instance for our fulcrum server
    /// </summary>
    public sealed class PassThruPipeReader : PassThruPipe
    {
        #region Custom Events

        // Public custom event for when a pipe processes new data
        public event EventHandler<PipeDataEventArgs> PipeDataProcessed;

        #endregion // Custom Events

        #region Fields

        // Singleton configuration to avoid building more than one instance of 
        private static PassThruPipeReader _pipeInstance;
        private static Lazy<PassThruPipeReader> _lazyReader;

        // Pipe and reading state values for this instance
        private readonly NamedPipeClientStream _fulcrumPipe;

        // Default value for pipe processing buffer
        private static int DefaultBufferValue = 10240;
        private static int DefaultReadingTimeout = 100;
        private static int DefaultConnectionTimeout = 10000;

        // Task objects for monitoring background readers
        private CancellationTokenSource _asyncConnectionTokenSource;
        private CancellationTokenSource _asyncReadPipeDataTokenSource;
        private CancellationTokenSource _pipeReadDataTimeoutTokenSource;

        #endregion // Fields

        #region Properties

        // Public facing property that tracks if the pipe instance is connecting/reading or not
        public static bool IsConnecting { get; private set; }
        public static bool IsReading => _lazyReader.Value?._asyncReadPipeDataTokenSource != null;

        #endregion // Properties

        #region Structs and Classes
        #endregion // Structs and Classes

        // -------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Builds a new reading pipe instance for Fulcrum
        /// </summary>
        private PassThruPipeReader() : base(PassThruPipeTypes.ReaderPipe)
        {
            // Build the pipe object here.
            this._fulcrumPipe = new NamedPipeClientStream(
                ".",                    // Name of the pipe host
                base.WriterPipeName,             // Name of the pipe client
                PipeDirection.In,                // Pipe directional configuration
                PipeOptions.None                 // Async operations supported
            );

            // Store our new settings values for the pipe object and apply them
            // DefaultBufferValue = FulcrumSettingsShare.InjectorPipeConfigSettings.GetSettingValue("Reader Pipe Buffer Size", DefaultBufferValue);
            // DefaultReadingTimeout = FulcrumSettingsShare.InjectorPipeConfigSettings.GetSettingValue("Reader Pipe Processing Timeout", DefaultReadingTimeout);
            // DefaultConnectionTimeout = FulcrumSettingsShare.InjectorPipeConfigSettings.GetSettingValue("Reader Pipe Connection Timeout", DefaultConnectionTimeout);
            this.PipeLogger.WriteLog($"STORED NEW DEFAULT BUFFER SIZE VALUE OK! VALUE STORED IS: {DefaultBufferValue}", LogType.WarnLog);
            this.PipeLogger.WriteLog($"STORED NEW CONNECTION TIMEOUT VALUE OK! VALUE STORED IS: {DefaultConnectionTimeout}", LogType.WarnLog);
            this.PipeLogger.WriteLog($"STORED NEW READ OPERATION TIMEOUT VALUE OK! VALUE STORED IS: {DefaultReadingTimeout}", LogType.WarnLog);

            // Build our new pipe instance here and wait for it to update a state value at some point
            this.StartPipeConnectionAsync();
        }
        /// <summary>
        /// Singleton constructor for the PassThruReader pipe type.
        /// </summary>
        /// <param name="ConnectionTask">The async task used to connect to our pipe instance.</param>
        /// <returns>A built pipe reader instance which is being used to connect</returns>
        /// <exception cref="InvalidOperationException">Thrown when the pipe instance built appears to be null</exception>
        public static PassThruPipeReader AllocatePipe(out Task<bool> ConnectionTask)
        {
            // Configure a new lazy reader instance if needed
            if (_lazyReader == null)
            {
                // Build a new lazy reader and store the value of it as our reader instance
                _lazyReader = new(() => new PassThruPipeReader());
                _pipeInstance = _lazyReader.Value;
            }
            if (_pipeInstance == null)
            {
                // Store the pipe type and log the issue out 
                string PipeType = _lazyReader.Value.GetType().Name;
                throw new InvalidOperationException($"Error! Failed to create new pipe instance for type {PipeType}!");
            }

            // Reset Pipe here if needed and invoke a new connection task if needed
            ConnectionTask = null;
            if (_pipeInstance.PipeState != PassThruPipeStates.Connected) { ConnectionTask = _pipeInstance.StartPipeConnectionAsync(); }
            else { _pipeInstance.PipeLogger.WriteLog("WRITER PIPE WAS ALREADY CONNECTED! NOT RECONFIGURING IT!", LogType.WarnLog); }
            return _pipeInstance;
        }

        // ----------------------------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Async connects to our client on the reader side of operations
        /// </summary>
        /// <returns>True if the connecting routine has been started, false if it has not</returns>
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

                // Exit this method
                return default;
            }

            // Build task token objects
            this._asyncConnectionTokenSource = new CancellationTokenSource();
            this.PipeLogger.WriteLog("CONFIGURED NEW ASYNC CONNECTION TASK TOKENS OK!", LogType.InfoLog);
            // DefaultConnectionTimeout = FulcrumSettingsShare.InjectorPipeConfigSettings.GetSettingValue("Reader Pipe Connection Timeout", DefaultConnectionTimeout);

            // Set connection building bool value and update view if possible
            IsConnecting = true;
            this.PipeState = PassThruPipeStates.Open;
            this.PipeLogger.WriteLog("STARTING READER PIPE CONNECTION ROUTINE NOW...", LogType.WarnLog); 
            return Task.Run(() =>
            {
                // Log ready for connection and send it.
                this.PipeLogger.WriteLog("PIPE CLIENT STREAM HAS BEEN CONFIGURED! ATTEMPTING CONNECTION ON IT NOW...", LogType.WarnLog);
                this.PipeLogger.WriteLog($"WAITING FOR {DefaultConnectionTimeout} MILLISECONDS BEFORE THE PIPES WILL TIMEOUT DURING THE CONNECTION ROUTINE", LogType.TraceLog);

                // Build pipe reading stream object
                bool IsLogged = false;
                while (!this._fulcrumPipe.IsConnected)
                {
                    try { this._fulcrumPipe.Connect(DefaultConnectionTimeout); }
                    catch (Exception PipeConnectionEx)
                    {
                        // Connecting to false
                        IsConnecting = false;
                        if (PipeConnectionEx is not TimeoutException)
                        {
                            // Throw exception and return out assuming window content has been built now
                            this.PipeState = PassThruPipeStates.Faulted;
                            this.PipeLogger.WriteLog($"FAILED TO CONNECT TO OUR PIPE INSTANCE FOR PIPE ID {this.PipeTypes}!", LogType.ErrorLog);
                            this.PipeLogger.WriteLog("EXCEPTION THROWN DURING CONNECTION OR STREAM OPERATIONS FOR THIS PIPE CONFIGURATION!", LogType.ErrorLog);
                            this.PipeLogger.WriteLog("EXCEPTION THROWN IS BEING LOGGED BELOW", PipeConnectionEx);
                        }

                        // Set the state to disconnected if it's not currently disconnected and log out this state value if needed
                        if (this.PipeState != PassThruPipeStates.Disconnected) this.PipeState = PassThruPipeStates.Disconnected;
                        if (!IsLogged)
                        {
                            // Set the logged state value to true so we stop logging this message [FULC-134]
                            IsLogged = true;
                            this.PipeLogger.WriteLog("FAILED TO CONNECT TO HOST PIPE SERVER AFTER GIVEN TIMEOUT VALUE!", LogType.WarnLog);
                        }

                        // Continue on to the next iteration
                        continue;
                    }
                   
                    // If we're connected, log that information and break out
                    IsConnecting = false;
                    this.PipeState = PassThruPipeStates.Connected;
                    this.PipeLogger.WriteLog("CONNECTED NEW SERVER INSTANCE TO OUR READER!", LogType.WarnLog);
                    this.PipeLogger.WriteLog($"PIPE SERVER CONNECTED TO FULCRUM PIPER {this.PipeTypes} OK!", LogType.InfoLog);

                    // Now boot the reader process.
                    this.StartReadPipeDataAsync();
                }

                // Return passed once done
                return true;
            }, this._asyncConnectionTokenSource.Token);
        }
        /// <summary>
        /// Kills our reading pipe connection process if the process is currently running
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

        /// <summary>
        /// This boots up a new background thread which reads our pipes over and over as long as we want
        /// </summary>
        public Task<bool> StartReadPipeDataAsync()
        {
            // Start by building a task object if we need to. If already built, just exit out
            if (this._asyncReadPipeDataTokenSource is { IsCancellationRequested: false }) return default;
            if (!this._fulcrumPipe.IsConnected)
            {
                // Log information and throw if auto connection is false
                this.PipeLogger.WriteLog("WARNING! A READ ROUTINE WAS CALLED WITHOUT ENSURING A PIPE CONNECTION WAS BUILT!", LogType.WarnLog);
                this.PipeLogger.WriteLog("CALLING A CONNECTION METHOD BEFORE INVOKING THIS READING ROUTINE TO ENSURE DATA WILL BE READ...", LogType.WarnLog);
                throw new InvalidOperationException("FAILED TO CONNECT TO OUR PIPE SERVER BEFORE READING OUTPUT FROM SHIM DLL!");
            }

            // Build token, build source and then log information
            this._asyncReadPipeDataTokenSource = new CancellationTokenSource();
            this.PipeLogger.WriteLog("BUILT NEW TASK CONTROL OBJECTS FOR READING PROCESS OK!", LogType.InfoLog);

            // Now read forever. Log a warning if no event is hooked onto our reading application event
            if (this.PipeDataProcessed == null) this.PipeLogger.WriteLog("WARNING! READER EVENT IS NOT CONFIGURED! THIS DATA MAY GO TO WASTE!", LogType.WarnLog);
            return Task.Run(() =>
            {
                // Log booting process now and run
                this.PipeLogger.WriteLog("PREPARING TO READ INPUT PIPE DATA ON REPEAT NOW...", LogType.InfoLog);
                while (!this._asyncReadPipeDataTokenSource.IsCancellationRequested)
                {
                    // Now read the information needed after a connection has been established ok
                    if (this.ReadPipeData(out string NextPipeData)) continue;

                    // If failed, then break this loop here
                    this.PipeLogger.WriteLog("FAILED TO READ NEW DATA DUE TO A FATAL ERROR OF SOME TYPE!", LogType.ErrorLog);
                    this.PipeLogger.WriteLog($"EXCEPTION GENERATED FROM READER: {NextPipeData}", LogType.ErrorLog);

                    // Try running a reconnect method here.
                    this.PipeLogger.WriteLog("TRYING TO RECONNECT TO OUR PIPE HOST NOW...", LogType.WarnLog);
                    this.PipeLogger.WriteLog("TRYING CONNECTION A TOTAL OF 5 TIMES BEFORE FAILING THIS ROUTINE!", LogType.WarnLog);
                    if (this.StartPipeConnectionAsync().Wait(DefaultConnectionTimeout * 5)) continue;

                    // Stop the reading operation if we can't get back into our host
                    this.StartReadPipeDataAsync();
                    this.PipeLogger.WriteLog("FAILED TO LOCATE OUR PIPE HOST AGAIN! STOPPING READING PROCESS AND EXITING THIS LOOP", LogType.ErrorLog);
                    break;
                }

                // Return the state of the pipe from this method
                return this._fulcrumPipe.IsConnected;
            }, this._asyncReadPipeDataTokenSource.Token);
        }
        /// <summary>
        /// Kills our reading process if the process is currently running
        /// </summary>
        /// <returns></returns>
        public bool AbortAsyncReadPipeData()
        {
            // Check if the source or token are null
            if (this._asyncReadPipeDataTokenSource == null)
            {
                // IF the token source was null, then we've got something sideways going on
                this.PipeLogger.WriteLog("TOKENS AND SOURCES WERE NOT YET CONFIGURED WHICH MEANS READING WAS NOT STARTED!", LogType.WarnLog);
                return false;
            }

            // Cancel here and return
            this.PipeLogger.WriteLog("CANCELING ACTIVE READING TASK NOW...", LogType.InfoLog);
            this._asyncReadPipeDataTokenSource.Cancel(false);
            this.PipeLogger.WriteLog("CANCELED BACKGROUND ACTIVITY OK!", LogType.WarnLog);
            return true;
        }

        /// <summary>
        /// Attempts to read data from our pipe server instance.
        /// </summary>
        /// <param name="ReadDataContents">Data processed</param>
        /// <returns>True if content comes back. False if not.</returns>
        public bool ReadPipeData(out string ReadDataContents)
        {
            // Store our new settings for the pipe buffer and timeout
            // DefaultBufferValue = FulcrumSettingsShare.InjectorPipeConfigSettings.GetSettingValue("Reader Pipe Buffer Size", DefaultBufferValue);
            // DefaultReadingTimeout = FulcrumSettingsShare.InjectorPipeConfigSettings.GetSettingValue("Reader Pipe Processing Timeout", DefaultReadingTimeout);

            // Build new timeout token values for reading operation and read now
            byte[] OutputBuffer = new byte[DefaultBufferValue];
            this._pipeReadDataTimeoutTokenSource = new CancellationTokenSource(DefaultReadingTimeout);

            try
            {
                // Read input content and check how many bytes we've pulled in
                var ReadingTask = this._fulcrumPipe.ReadAsync(OutputBuffer, 0, OutputBuffer.Length);
                try { ReadingTask.Wait(this._pipeReadDataTimeoutTokenSource.Token); }
                catch (Exception AbortEx) { if (AbortEx is not OperationCanceledException) throw AbortEx; }

                // Now convert our bytes into a string object, and print them to our log files.
                int BytesRead = ReadingTask.Result;
                if (BytesRead != OutputBuffer.Length) OutputBuffer = OutputBuffer.Take(BytesRead).ToArray();
                ReadDataContents = Encoding.Default.GetString(OutputBuffer, 0, OutputBuffer.Length).TrimEnd();

                // Now fire off a pipe data read event if possible. Otherwise return
                this.PipeDataProcessed?.Invoke(this, new PipeDataEventArgs()
                {
                    // Store byte values
                    PipeByteData = OutputBuffer,
                    ByteDataLength = (uint)OutputBuffer.Length,

                    // Store string values
                    PipeDataString = ReadDataContents,
                    PipeDataStringLength = (uint)ReadDataContents.Length
                });

                // Return passed and build output string values
                return true;
            }
            catch (Exception ReadEx)
            {
                // Log our failures and return failed output
                this.PipeLogger.WriteLog("FAILED TO READ NEW PIPE INPUT DATA!", LogType.ErrorLog);
                this.PipeLogger.WriteLog("EXCEPTION THROWN DURING READING OPERATIONS OF OUR INPUT PIPE DATA PROCESSING!", LogType.ErrorLog);
                this.PipeLogger.WriteLog("EXCEPTION THROWN IS LOGGED BELOW", ReadEx);

                // Return failed
                ReadDataContents = $"FAILED_PIPE_READ__{ReadEx.GetType().Name.ToUpper()}";
                return false;
            }
        }
    }
}