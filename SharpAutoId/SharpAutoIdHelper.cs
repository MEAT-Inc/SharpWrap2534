using System;
using System.Linq;
using SharpAutoId.SharpAutoIdHelpers;
using SharpAutoId.SharpAutoIdModels;
using SharpLogger;
using SharpLogger.LoggerObjects;
using SharpLogger.LoggerSupport;
using SharpWrapper;
using SharpWrapper.PassThruTypes;
using SharpWrapper.SupportingLogic;

namespace SharpAutoId
{
    /// <summary>
    /// Interface base for Auto ID routines which can be used by our connection routine.
    /// This interface lays out a Open, Connect, Read VIN, and Close command.
    /// </summary>
    public abstract class SharpAutoIdHelper
    {
        // Logger object for monitoring logger outputs
        protected internal readonly SubServiceLogger AutoIdLogger;

        // Class Values for configuring commands.
        public readonly string DLL;
        public readonly string Device;
        public readonly JVersion Version;
        public readonly ProtocolId AutoIdType;
        public readonly SharpIdConfiguration AutoIdCommands;

        // Runtime Instance Values (private only)
        protected internal uint[] FilterIds;
        protected internal uint ChannelIdOpened;
        protected internal Sharp2534Session SessionInstance;

        // Result values from our instance.
        public string VinNumberLocated { get; protected set; }

        // ------------------------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Builds a new connection instance for AutoID
        /// </summary>
        protected internal SharpAutoIdHelper(Sharp2534Session SessionInstance, ProtocolId ProtocolValue)
        {
            // Store class values here and build our new logger object.
            this.AutoIdType = ProtocolValue;
            this.DLL = SessionInstance.DllName;
            this.SessionInstance = SessionInstance;
            this.Device = SessionInstance.DeviceName;
            this.Version = SessionInstance.DeviceVersion;

            // Build our new logger object
            string LoggerName = $"{ProtocolValue}_AutoIdLogger_{this.Version}_{SessionInstance.DeviceName.Replace(" ", "-")}";
            this.AutoIdLogger = (SubServiceLogger)LoggerQueue.SpawnLogger(LoggerName, LoggerActions.SubServiceLogger);

            // Log built new auto ID routine without issues.
            this.AutoIdLogger.WriteLog($"BUILT NEW AUTO ID LOGGER FOR PROTOCOL {this.AutoIdType} OK!", LogType.InfoLog);
            this.AutoIdLogger.WriteLog($"--> DLL IN USE:    {this.DLL}");
            this.AutoIdLogger.WriteLog($"--> DEVICE IN USE: {this.Device}");

            // Build our AutoID routine object from the AppSettings now.
            this.AutoIdLogger.WriteLog($"PULLING IN SESSION ROUTINES FOR PROTOCOL TYPE {this.AutoIdType}", LogType.InfoLog);
            if (!SharpAutoIdConfig.SupportedProtocols.Contains(this.AutoIdType))
                throw new InvalidOperationException($"CAN NOT USE PROTOCOL TYPE {this.AutoIdType} FOR AUTO ID ROUTINE!");

            // Make sure our instance exists
            this.AutoIdCommands = SharpAutoIdConfig.GetRoutine(this.AutoIdType);
            if (this.AutoIdCommands == null)
                throw new NullReferenceException($"FAILED TO FIND AUTO ID ROUTINE COMMANDS FOR PROTOCOL {this.AutoIdType}!");
        }

        // ------------------------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Opens a new session for J2534 sessions.
        /// </summary>
        /// <param name="InputSession">Instance built</param>
        /// <returns>True if the session is built ok. False if it is not.</returns>
        public bool OpenAutoIdSession(Sharp2534Session InputSession)
        {
            try
            {
                // Store our instance session
                this.SessionInstance = InputSession;
                this.AutoIdLogger.WriteLog("STORED INSTANCE SESSION OK! READY TO BEGIN AN AUTO ID ROUTINE WITH IT NOW...");

                // Open our session object and begin connecting
                this.SessionInstance.PTOpen();
                this.AutoIdLogger.WriteLog("BUILT NEW SHARP SESSION FOR ROUTINE OK! SHOWING RESULTS BELOW", LogType.InfoLog);
                
                // Log the instance information output
                this.AutoIdLogger.WriteLog(this.SessionInstance.ToDetailedString());
                return true;
            }
            catch (Exception SessionEx)
            {
                // Log our exception and throw failures.
                this.AutoIdLogger.WriteLog($"FAILED TO BUILD AUTO ID ROUTINE SESSION FOR PROTOCOL TYPE {this.AutoIdType}!", LogType.ErrorLog);
                this.AutoIdLogger.WriteLog("EXCEPTION THROWN DURING SESSION CONFIGURATION METHOD", SessionEx);
                return false;
            }
        }
        /// <summary>
        /// Closes our session for building an AutoID routine.
        /// </summary>
        /// <returns>True if the session was closed ok. False if not.</returns>
        public bool CloseAutoIdSession()
        {
            try
            {
                // Start by issuing a PTClose method.
                this.SessionInstance.PTDisconnect(0);
                this.SessionInstance.PTClose();
                this.AutoIdLogger.WriteLog("CLOSED SESSION INSTANCE OK!", LogType.InfoLog);
                return true;
            }
            catch (Exception SessionEx)
            {
                // Log our exception and throw failures.
                this.AutoIdLogger.WriteLog($"FAILED TO CLOSE AN AUTO ID ROUTINE SESSION FOR PROTOCOL TYPE {this.AutoIdType}!", LogType.ErrorLog);
                this.AutoIdLogger.WriteLog("EXCEPTION THROWN DURING SESSION SHUTDOWN METHOD", SessionEx);
                return false;
            }
        }

        // ------------------------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Connects to a given channel instance using the protocol value given in the class type and the 
        /// </summary>
        /// <param name="ChannelId">Channel ID Opened</param>
        /// <returns>True if the channel is opened, false if it is not.</returns>
        public abstract bool ConnectChannel(out uint ChannelId);
        /// <summary>
        /// Finds the VIN of the currently connected vehicle
        /// </summary>
        /// <param name="VinNumber">VIN Number pulled</param>
        /// <returns>True if a VIN is pulled, false if it isn't</returns>
        public abstract bool RetrieveVinNumber(out string VinNumber);
    }
}
