using System;
using System.Linq;
using System.Reflection;
using SharpLogging;
using SharpWrapper;
using SharpWrapper.J2534Objects;
using SharpWrapper.PassThruTypes;

namespace SharpAutoId
{
    /// <summary>
    /// Interface base for Auto ID routines which can be used by our connection routine.
    /// This interface lays out a Open, Connect, Read VIN, and Close command.
    /// </summary>
    public abstract class AutoIdHelper
    {
        #region Custom Events

        // Public event handlers used to help configure status changes for the AutoID Helpers
        public EventHandler<double> OnVoltageChanged;
        public EventHandler<string> OnVehicleVinFound;

        #endregion //Custom Events

        #region Fields

        // Logger object for monitoring logger outputs
        protected internal readonly SharpLogger _autoIdLogger;

        // Private backing fields for public properties
        private string _vehicleVIN;
        private double _lastVoltage;

        // Class Values for configuring commands.
        public readonly string DLL;
        public readonly string Device;
        public readonly JVersion Version;
        public readonly ProtocolId AutoIdType;
        public readonly AutoIdConfiguration AutoAutoIdCommands;

        // Runtime Instance Values (private only)
        protected internal uint[] FilterIds;
        protected internal uint ChannelIdOpened;
        protected internal J2534Channel ChannelOpened;
        protected internal Sharp2534Session SessionInstance;

        #endregion //Fields

        #region Properties

        // Result values from our instance. Holds the VIN Number and last read voltage
        public string VehicleVIN
        {
            get => this._vehicleVIN;
            protected set
            {
                // Fire off a new event for VIN found and store it
                this._vehicleVIN = value;
                this.OnVehicleVinFound?.Invoke(this, this._vehicleVIN);
            }
        }
        public double LastVoltage
        {
            get => this._lastVoltage;
            protected set
            {
                // Fire off a new event for voltage changed and store it
                this._lastVoltage = value;
                this.OnVoltageChanged?.Invoke(this, this._lastVoltage);
            }
        }

        #endregion //Properties

        #region Structs and Classes
        #endregion //Structs and Classes

        // ------------------------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Builds a new connection instance for AutoID
        /// </summary>
        protected internal AutoIdHelper(Sharp2534Session SessionInstance, ProtocolId ProtocolValue)
        {
            // Store class values here and build our new logger object.
            this.AutoIdType = ProtocolValue;
            this.DLL = SessionInstance.DllName;
            this.SessionInstance = SessionInstance;
            this.Device = SessionInstance.DeviceName;
            this.Version = SessionInstance.DeviceVersion;

            // If the log broker is not setup, then do so now but make sure we log to a spot where the user will be aware of output
            string LoggerName = $"{ProtocolValue}_AutoIdLogger_{this.Version}_{SessionInstance.DeviceName.Replace(" ", "-")}";
            this._autoIdLogger = new SharpLogger(LoggerActions.UniversalLogger, LoggerName);
            this._autoIdLogger.WriteLog($"BUILT NEW AUTO ID LOGGER FOR PROTOCOL {this.AutoIdType} OK!", LogType.InfoLog);
            this._autoIdLogger.WriteLog($"--> DLL IN USE:    {this.DLL}");
            this._autoIdLogger.WriteLog($"--> DEVICE IN USE: {this.Device}");

            // Build our AutoID routine object from the AppSettings now.
            this._autoIdLogger.WriteLog($"PULLING IN SESSION ROUTINES FOR PROTOCOL TYPE {this.AutoIdType}", LogType.InfoLog);
            if (!AutoIdConfiguration.SupportedProtocols.Contains(this.AutoIdType))
                throw new InvalidOperationException($"CAN NOT USE PROTOCOL TYPE {this.AutoIdType} FOR AUTO ID ROUTINE!");

            // Make sure our instance exists
            this.AutoAutoIdCommands = AutoIdConfiguration.GetRoutine(this.AutoIdType);
            if (this.AutoAutoIdCommands == null)
                throw new NullReferenceException($"FAILED TO FIND AUTO ID ROUTINE COMMANDS FOR PROTOCOL {this.AutoIdType}!");

            // If our session is not null, open it up now
            if (this.SessionInstance != null)
                this.OpenAutoIdSession();
        }
        /// <summary>
        /// Builds a new AutoID Helper object from a given input session and protocol
        /// </summary>
        /// <param name="SessionInstance">Session to build from</param>
        /// <param name="ProtocolValue">Protocol to scan with</param>
        /// <returns></returns>
        public static AutoIdHelper BuildAutoIdHelper(Sharp2534Session SessionInstance, ProtocolId ProtocolValue)
        {
            // Check to make sure the requested protocol is supported first.
            if (!AutoIdConfiguration.SupportedProtocols.Contains(ProtocolValue))
                throw new InvalidOperationException($"CAN NOT USE PROTOCOL {ProtocolValue} SINCE IT IS NOT SUPPORTED!");
            
            // Build auto ID helper and return the object out
            // Get a list of all supported protocols and then pull in all the types of auto ID routines we can use
            var AutoIdType = typeof(AutoIdHelper)
                .Assembly.GetTypes()
                .Where(RoutineType => RoutineType.IsSubclassOf(typeof(AutoIdHelper)) && !RoutineType.IsAbstract)
                .FirstOrDefault(TypeObj => TypeObj.FullName.Contains(ProtocolValue.ToString()));

            // Now build a type of our current autoID Object
            if (AutoIdType == null) throw new TypeAccessException($"CAN NOT USE TYPE FOR PROTOCOL NAMED {ProtocolValue}!");
            AutoIdHelper AutoIdInstance = (AutoIdHelper)Activator.CreateInstance(AutoIdType, SessionInstance);
            
            // Return the AutoID Instance object
            return AutoIdInstance;
        }

        // ------------------------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Opens a new session for J2534 sessions.
        /// </summary>
        /// <returns>True if the session is built ok. False if it is not.</returns>
        public bool OpenAutoIdSession()
        {
            try
            {
                // Open our session object and begin connecting
                this.SessionInstance.PTOpen();
                this._autoIdLogger.WriteLog("BUILT NEW SHARP SESSION FOR ROUTINE OK! SHOWING RESULTS BELOW", LogType.InfoLog);
                
                // Log the instance information output
                this._autoIdLogger.WriteLog(this.SessionInstance.ToDetailedString());
                return true;
            }
            catch (Exception SessionEx)
            {
                // Log our exception and throw failures.
                this._autoIdLogger.WriteLog($"FAILED TO BUILD AUTO ID ROUTINE SESSION FOR PROTOCOL TYPE {this.AutoIdType}!", LogType.ErrorLog);
                this._autoIdLogger.WriteException("EXCEPTION THROWN DURING SESSION CONFIGURATION METHOD", SessionEx);
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
                this._autoIdLogger.WriteLog("CLOSED SESSION INSTANCE OK!", LogType.InfoLog);
                return true;
            }
            catch (Exception SessionEx)
            {
                // Log our exception and throw failures.
                this._autoIdLogger.WriteLog($"FAILED TO CLOSE AN AUTO ID ROUTINE SESSION FOR PROTOCOL TYPE {this.AutoIdType}!", LogType.ErrorLog);
                this._autoIdLogger.WriteException("EXCEPTION THROWN DURING SESSION SHUTDOWN METHOD", SessionEx);
                return false;
            }
        }

        /// <summary>
        /// Reads in the current voltage for the connected device
        /// </summary>
        /// <returns>The voltage value read from the car if the routine passed</returns>
        public double ReadVehicleVoltage()
        {
            // Read the voltage value from our session and return it out
            this.SessionInstance.PTReadVoltage(out double ReadVoltage);
            this.LastVoltage = ReadVoltage;
            return this.LastVoltage;
        }

        /// <summary>
        /// Connects to a given channel instance using the protocol value given in the class type and the 
        /// </summary>
        /// <param name="ChannelId">Channel ID Opened</param>
        /// <returns>True if the channel is opened, false if it is not.</returns>
        public abstract bool ConnectAutoIdChannel(out uint ChannelId);
        /// <summary>
        /// Finds the VIN of the currently connected vehicle
        /// </summary>
        /// <param name="VinNumber">VIN Number pulled</param>
        /// <returns>True if a VIN is pulled, false if it isn't</returns>
        public abstract bool RetrieveVehicleVIN(out string VinNumber);
    }
}
