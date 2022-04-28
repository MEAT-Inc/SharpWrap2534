using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using SharpLogger;
using SharpLogger.LoggerObjects;
using SharpLogger.LoggerSupport;
using SharpWrap2534.J2534Objects;
using SharpWrap2534.PassThruImport;
using SharpWrap2534.PassThruTypes;
using SharpWrap2534.SupportingLogic;

namespace SharpWrap2534
{
    /// <summary>
    /// Contains the base information about our J2534 instance objects and types.
    /// </summary>
    public class Sharp2534Session
    {
        // Singleton Configuration array for sessions
        private static Sharp2534Session[] _sharpSessions;

        /// <summary>
        /// Builds a new SharpWrap Session
        /// </summary>
        /// <param name="Version">J2534 Version</param>
        /// <param name="DllNameFilter">DLL Name</param>
        /// <param name="DeviceNameFilter">Device Name</param>
        /// <returns>Built SharpSession if one is made or not.</returns>
        public static Sharp2534Session OpenSession(JVersion Version, string DllNameFilter, string DeviceNameFilter = "")
        {
            // Validate Versions and make sure singleton object exists 
            if (Version == JVersion.ALL_VERSIONS) Version = JVersion.V0404;
            _sharpSessions ??= new Sharp2534Session[new PassThruConstants(Version).MaxDeviceCount];

            // Find next open index. If none found, then fail out.
            int NextOpenSessionIndex = _sharpSessions.ToList().IndexOf(null);
            if (NextOpenSessionIndex == -1) throw new InvalidOperationException($"CAN NOT BUILD A NEW SESSION FOR MORE THAN {_sharpSessions.Length} DEVICES!");

            // Build new session, store it, and return out.
            _sharpSessions[NextOpenSessionIndex] = new Sharp2534Session(Version, DllNameFilter, DeviceNameFilter);
            if (_sharpSessions[NextOpenSessionIndex] == null) throw new NullReferenceException("FAILED TO BUILD NEW SHARP SESSION! THIS IS FATAL!");
            return _sharpSessions[NextOpenSessionIndex];
        }
        /// <summary>
        /// Disposes of our instance object and cleans up resources.
        /// <param name="SessionToClose">Session to close out</param>
        /// </summary>
        public static void CloseSession(Sharp2534Session SessionToClose)
        {
            // Log killing this instance.
            SessionToClose._sessionLogger.WriteLog(SessionToClose._logSupport.SplitLineString(), LogType.TraceLog);
            SessionToClose._sessionLogger.WriteLog("KILLING SHARP SESSION INSTANCE NOW!", LogType.WarnLog);

            // Kill our device instance. This closes it and removes all channels.
            bool KilledOK = SessionToClose.JDeviceInstance.DestroyDevice();
            if (_sharpSessions.Contains(SessionToClose)) _sharpSessions[_sharpSessions.ToList().IndexOf(SessionToClose)] = null;
            if (KilledOK) SessionToClose._sessionLogger.WriteLog("KILLED SESSION WITHOUT ISSUES!", LogType.InfoLog);
            else SessionToClose._sessionLogger.WriteLog("FAILED TO KILL SHARP SESSION! THIS IS FATAL!", LogType.ErrorLog);

            // Split output and return result.
            SessionToClose._sessionLogger?.WriteLog(SessionToClose._logSupport.SplitLineString(), LogType.TraceLog);
        }

        /// <summary>
        /// PRIVATE CONSTRUCTOR! THIS IS ONLY USED FOR BUILDING INTERNALLY!
        /// Builds a new J2534 Session instance object using the DLL Name provided.
        /// </summary>
        /// <param name="DllNameFilter">Dll to use</param>
        /// <param name="DeviceNameFilter">Name of the device To use.</param>
        /// <param name="Version">Version of the API</param>
        private Sharp2534Session(JVersion Version, string DllNameFilter, string DeviceNameFilter = "")
        {
            // Build new J2534 DLL For the version and DLL name provided first.
            if (PassThruImportDLLs.FindDllByName(DllNameFilter, Version, out J2534Dll BuiltJDll)) this.JDeviceDll = BuiltJDll;
            else { throw new NullReferenceException($"No J2534 DLLs with the name filter '{DllNameFilter}' were located matching the version given!"); }

            // Now build our new device object. Find a possible device based on the filter given.
            var LocatedDevicesForDLL = JDeviceDll.FindConnectedDeviceNames();
            if (LocatedDevicesForDLL.Count == 0) throw new NullReferenceException("No devices for the DLL specified exist on the system at this time!");
            if (DeviceNameFilter != "" && LocatedDevicesForDLL.FirstOrDefault(NameValue => NameValue.Contains(DeviceNameFilter)) == null)
                throw new NullReferenceException($"No devices were found matching the name filter of '{DeviceNameFilter}' provided!");

            // Build device now using the name value desired.
            string NewDeviceName = DeviceNameFilter == "" ?
                LocatedDevicesForDLL.FirstOrDefault(DeviceObj => !DeviceObj.ToUpper().Contains("IN USE")) :
                LocatedDevicesForDLL.FirstOrDefault(DeviceName => DeviceName.Contains(DeviceNameFilter));

            // Try to build the new session object inside try/catch for when it naturally fails out for some reason.
            try { JDeviceInstance = J2534Device.BuildJ2534Device(JDeviceDll, NewDeviceName); }
            catch (Exception InitJ2534FailureEx)
            {
                // Build new compound init Exception and throw it.
                Exception FailedInitException = new InvalidOperationException(
                    "Failed to build new Device Session for the provided device and DLL configuration!",
                    InitJ2534FailureEx
                );

                // Throw the exception built.
                throw FailedInitException;
            }
            
            // Make Sure logging is configured
            if (LogBroker.BaseOutputPath == null)
                LogBroker.ConfigureLoggingSession(
                    Assembly.GetExecutingAssembly().FullName,
                    Path.Combine(Directory.GetCurrentDirectory(), "SharpLogging")
                );

            // Build logging support
            this.SessionGuid = Guid.NewGuid();
            this._sessionLogger = new SubServiceLogger($"SharpWrapSession_{this.SessionGuid}_SessionLogger");
            this._logSupport = new LoggingSupport(this.DeviceName, this._sessionLogger);
            this._sessionLogger.WriteLog(this._logSupport.SplitLineString(), LogType.TraceLog);
            this._sessionLogger.WriteLog("SHARPWRAP J2534 SESSION BUILT CORRECTLY! SESSION STATE IS BEING PRINTED OUT BELOW", LogType.InfoLog);
            this._sessionLogger.WriteLog($"\n{this.ToDetailedString()}");
            this._sessionLogger.WriteLog(this._logSupport.SplitLineString(), LogType.TraceLog);
        }

        // ------------------------------------------------------------------------------------------------------------------------------------------

        // Logger object for a session instance and helper methods
        private readonly LoggingSupport _logSupport;
        private readonly SubServiceLogger _sessionLogger;

        // ------------------------------------------------------------------------------------------------------------------------------------------

        // DLL and Device Instance for our J2534 Box.       
        public J2534Dll JDeviceDll { get; private set; }                 // The DLL Instance in use.
        public J2534Device JDeviceInstance { get; private set; }         // The Device instance in use.

        // ------------------------------------------------------------------------------------------------------------------------------------------

        // Status of this session instance, device, and DLL objects
        public PTInstanceStatus SessionStatus =>
            DllStatus == PTInstanceStatus.INITIALIZED && DeviceStatus == PTInstanceStatus.INITIALIZED ?
                PTInstanceStatus.INITIALIZED :
                PTInstanceStatus.NULL;
        public PTInstanceStatus DllStatus => JDeviceDll.JDllStatus;
        public PTInstanceStatus DeviceStatus => JDeviceInstance.DeviceStatus;

        // ------------------------------------------------------------------------------------------------------------------------------------------

        // Session GUID value built
        public readonly Guid SessionGuid;

        // DLL and Device Versions
        public JVersion DllVersion => JDeviceDll.DllVersion;
        public JVersion DeviceVersion => JDeviceInstance.J2534Version;

        // DLL and Device Names/IDs
        public string DllName => JDeviceDll.LongName;
        public uint DeviceId => JDeviceInstance.DeviceId;
        public string DeviceName => JDeviceInstance.DeviceName;

        // ------------------------------------------------------------------------------------------------------------------------------------------

        // The last opened J2534 Channel object. This is our fallback when we don't get an input channel ID
        private J2534Channel _defaultChannel;

        // Device Channel Information, filters, and periodic messages.
        public J2534Channel[] DeviceChannels => JDeviceInstance.DeviceChannels;
        public J2534Channel[][] DeviceLogicalChannels => JDeviceInstance.DeviceChannels.Select(ChObj => ChObj.LogicalChannels).ToArray();
        public J2534Filter[][] ChannelFilters => JDeviceInstance.DeviceChannels.Select(ChObj => ChObj.JChannelFilters).ToArray();
        public J2534PeriodicMessage[][] ChannelPeriodicMsgs => JDeviceInstance.DeviceChannels.Select(ChObj => ChObj.JChannelPeriodicMessages).ToArray();

        // ------------------------------------------------------------------------------------------------------------------------------------------

        // The ToString override will return a combination of the following configuration setups.
        public string DeviceDllInfoString => JDeviceDll.ToDetailedString();
        public string DeviceInfoString => JDeviceInstance.ToDetailedString();

        /// <summary>
        /// ToString override which contains detailed information about this instance object.
        /// </summary>
        /// <returns>String of the instance session</returns>
        public override string ToString()
        {
            // Build output string.
            return
                $"J2534 DLL:    {JDeviceDll.LongName} ({JDeviceDll.DllVersion.ToDescriptionString()})\n" +
                $"J2534 Device: {DeviceName} ({JDeviceInstance.J2534Version.ToDescriptionString()})";
        }
        /// <summary>
        /// Builds detailed output info string. 
        /// Contains names, versions, the DLL path, the Device FW, API, and Dll Version, and other info.
        /// </summary>
        /// <returns></returns>
        public string ToDetailedString()
        {
            // Builds combo string of detailed output information about the DLL now.
            return DeviceDllInfoString + "\n\n" + DeviceInfoString;
        }

        // ------------------------------------------------- PassThru Command Routines/Methods ------------------------------------------------------

        #region PassThruOpen - PassThruClose
        /// <summary>
        /// PTOpen command passed thru
        /// </summary>
        public bool PTOpen()
        {
            // Log and open our JBox instance.
            this.JDeviceInstance.PTOpen(this.DeviceName);
            this._logSupport.WriteCommandLog("OPENED NEW J2534 INSTANCE OK!", LogType.InfoLog);
            this._logSupport.WriteCommandLog($"DEVICE NAME AND ID: {this.DeviceName} - {this.DeviceId}", LogType.InfoLog);
            this._logSupport.WriteCommandLog($"DEVICE OPEN: {this.JDeviceInstance.IsOpen}");

            // Return if the device is open
            return this.JDeviceInstance.IsOpen;
        }
        /// <summary>
        /// PTClose command passed thr
        /// </summary>
        public bool PTClose()
        {
            // Log and close our JBox instance.
            this.JDeviceInstance.PTClose();
            this._logSupport.WriteCommandLog("CLOSED OUR J2534 INSTANCE OK!", LogType.InfoLog);
            this._logSupport.WriteCommandLog($"DEVICE OPEN: {this.JDeviceInstance.IsOpen}");

            // Return if the the device is closed
            return !this.JDeviceInstance.IsOpen;
        }
        #endregion

        #region PassThruConnect - PassThruDisconnect
        /// <summary>
        /// PassThru Connect passed thru.
        /// </summary>
        /// <param name="ChannelIndex">Index of channel</param>
        /// <param name="Protocol">Protocol to use</param>
        /// <param name="Flags">Flags to use</param>
        /// <param name="ChannelBaud">Baudrate</param>
        public J2534Channel PTConnect(int ChannelIndex, ProtocolId Protocol, uint Flags, uint ChannelBaud, out uint ChannelId)
        {
            // Log command being built out
            this._logSupport.WriteCommandLog("SENDING OUT PASSTHRU CONNECT METHOD NOW.", LogType.InfoLog);
            this._logSupport.WriteCommandLog($"CONNECT PARAMETERS: {ChannelIndex}, {Protocol}, {ChannelBaud}", LogType.TraceLog);

            // Run our connect routine here.
            this.JDeviceInstance.PTConnect(ChannelIndex, Protocol, Flags, ChannelBaud);
            ChannelId = this.JDeviceInstance.DeviceChannels[ChannelIndex].ChannelId;

            // Log information and return output.
            this._logSupport.WriteCommandLog($"PULLED OUT CHANNEL ID: {ChannelId}", LogType.InfoLog);
            this._logSupport.WriteCommandLog("STORING NEWEST CHANNEL AS OUR FALLBACK CHANNEL NOW...", LogType.InfoLog);
            this._defaultChannel = this.JDeviceInstance.DeviceChannels[ChannelIndex];
            return this._defaultChannel;
        }
        /// <summary>
        /// Runs a PTDisconnect
        /// </summary>
        /// <param name="ChannelIndex">Index to disconnect</param>
        public void PTDisconnect(int ChannelIndex)
        {
            // Log information and issue disconnect.
            this._logSupport.WriteCommandLog($"DISCONNECTING CHANNEL INDEX: {ChannelIndex}", LogType.WarnLog);
            this.JDeviceInstance.PTDisconnect(ChannelIndex);
        }
        #endregion

        #region PTIoctl (Voltage, TX, RX Buffers)
        /// <summary>
        /// Clears out the RX buffer on a current device instance
        /// </summary>
        public void PTClearRxBuffer(int ChannelId = -1)
        {
            // Check for all null channels
            if (this.DeviceChannels.All(ChannelObj => ChannelObj == null)) {
                this._logSupport.WriteCommandLog("CAN NOT ISSUE IOCTL COMMANDS ON A DEVICE WITH NO OPENED CHANNELS!", LogType.ErrorLog);
                return;
            }

            // Log Clearing RX buffer, clear it and return 
            J2534Channel ChannelInUse = this._defaultChannel;
            this._logSupport.WriteCommandLog($"CLEARING RX BUFFER FROM DEVICE {this.DeviceName} NOW...", LogType.InfoLog);
            if (ChannelId != -1)
            {
                // Check our Device Channel ID
                ChannelInUse = DeviceChannels?.FirstOrDefault(ChannelObj => ChannelObj != null);
                if (ChannelInUse != null)
                {
                    // Log can't operate on a null channel and exit method
                    this._logSupport.WriteCommandLog("CAN NOT CLEAR RX BUFFER FROM NULL CHANNELS!", LogType.ErrorLog); 
                    return;
                }
            }

            // Clear out the channel RX Buffer by the ID here
            this._logSupport.WriteCommandLog($"USING DEVICE INSTANCE {this.DeviceName} FOR BUFFER OPERATIONS", LogType.InfoLog);
            this._logSupport.WriteCommandLog($"CLEARING RX BUFFER FROM CHANNEL ID: {ChannelInUse.ChannelId}!", LogType.WarnLog);
            ChannelInUse.ClearRxBuffer();
        }
        /// <summary>
        /// Clears out the TX buffer on a current device instance
        /// </summary>
        public void PTClearTxBuffer(int ChannelId = -1)
        {            
            // Check for all null channels
            if (this.DeviceChannels.All(ChannelObj => ChannelObj == null)) {
                this._logSupport.WriteCommandLog("CAN NOT ISSUE IOCTL COMMANDS ON A DEVICE WITH NO OPENED CHANNELS!", LogType.ErrorLog);
                return;
            }

            // Log Clearing RX buffer, clear it and return 
            J2534Channel ChannelInUse = this._defaultChannel;
            if (ChannelId != -1)
            {
                // Check our Device Channel ID
                ChannelInUse = DeviceChannels?.FirstOrDefault(ChannelObj => ChannelObj != null);
                if (ChannelInUse == null)
                {
                    // Log can't operate on a null channel and exit method
                    this._logSupport.WriteCommandLog("CAN NOT CLEAR TX BUFFER FROM NULL CHANNELS!", LogType.ErrorLog);
                    return;
                }
            }

            // Clear out the channel RX Buffer by the ID here
            this._logSupport.WriteCommandLog($"USING DEVICE INSTANCE {this.DeviceName} FOR BUFFER OPERATIONS", LogType.InfoLog);
            this._logSupport.WriteCommandLog($"CLEARING TX BUFFER FROM CHANNEL ID: {ChannelInUse.ChannelId}!", LogType.WarnLog);
            ChannelInUse.ClearTxBuffer();
        }
        /// <summary>
        /// Reads our voltage value from a PTDevice instance connected via a channel.
        /// </summary>
        /// <param name="VoltageRead">Value of voltage pulled</param>
        /// <param name="ChannelId">ID Of channel to issue from</param>
        /// <param name="SilentRead">Sets if we need to silent pull or not. Useful for when running in a loop</param>
        public void PTReadVoltage(out double VoltageRead, bool SilentRead = false, int ChannelId = -1)
        {
            // Log Pulling Voltage, find channel ID, and return it.
            if (!SilentRead) this._logSupport.WriteCommandLog($"READING VOLTAGE FROM DEVICE {this.DeviceName} NOW...", LogType.InfoLog);

            // Pull voltage value and check for -1
            var VoltageInt = JDeviceInstance.PTReadVBattery();
            if (VoltageInt == -1) {
                this._logSupport.WriteCommandLog("VOLTAGE VALUE WAS -1 THIS IS DUE TO A FAILED IOCTL!", LogType.ErrorLog);
                VoltageRead = -1.0;
                return;
            }

            // Store our new voltage value here and return it.
            VoltageRead = ((double)VoltageInt / (double)1000);
            if (!SilentRead) this._logSupport.WriteCommandLog($"PULLED VOLTAGE VALUE OF {VoltageRead:F2} OK!", LogType.InfoLog);
        }
        #endregion

        #region PassThruWriteMessages - PassThruReadMessages
        /// <summary>
        /// Sends a message on the first possible channel found.
        /// </summary>
        /// <param name="MessageToSend">Message to send out</param>
        /// <param name="SendTimeout">Timeout for send operation</param>
        public bool PTWriteMessages(PassThruStructs.PassThruMsg MessageToSend, uint SendTimeout = 100, int ChannelId = -1)
        {
            // Log information. If all channels are null, then exit.
            if (this.DeviceChannels.All(ChannelObj => ChannelObj == null)) {
                this._logSupport.WriteCommandLog("CAN NOT ISSUE WRITE COMMANDS ON A DEVICE WITH NO OPENED CHANNELS!", LogType.ErrorLog);
                return false;
            }

            // Find the channel to use and send out the command.
            J2534Channel ChannelInUse = this._defaultChannel;
            if (ChannelId != -1)
            {
                // Check our Device Channel ID
                ChannelInUse = DeviceChannels?.FirstOrDefault(ChannelObj => ChannelObj != null);
                if (ChannelInUse == null)
                {
                    // Log can't operate on a null channel and exit method
                    this._logSupport.WriteCommandLog("CAN NOT CLEAR WRITE MESSAGES TO NULL CHANNELS!", LogType.ErrorLog);
                    return false;
                }
            }

            // Log information out and prepare to wrtie
            this._logSupport.WriteCommandLog($"SENDING MESSAGES ON CHANNEL WITH ID: {ChannelInUse.ChannelId}", LogType.InfoLog);
            this._logSupport.WriteCommandLog($"ISSUING PASSTHRU WRITE COMMAND WITH TIMEOUT AND MESSAGE: {SendTimeout}ms - {MessageToSend} MESSAGES", LogType.InfoLog);

            // Issue command, log output and return.
            uint MessagesSent = ChannelInUse.PTWriteMessages(MessageToSend, SendTimeout);
            this._logSupport.WriteCommandLog($"SENT A TOTAL OF {MessagesSent} OUT OF AN EXPECTED 1 MESSAGE!", LogType.WarnLog);
            if (MessagesSent != 1) { this._logSupport.WriteCommandLog("ERROR! FAILED TO SEND OUT THE REQUESTED PT MESSAGE!", LogType.ErrorLog); }
            return MessagesSent == 1;
        }
        /// <summary>
        /// Sends a message on the first possible channel found.
        /// </summary>
        /// <param name="MessageToSend">Message to send out</param>
        /// <param name="SendTimeout">Timeout for send operation</param>
        public bool PTWriteMessages(PassThruStructs.PassThruMsg[] MessageToSend, uint SendTimeout = 100, int ChannelId = -1)
        {
            // Log information. If all channels are null, then exit.
            if (this.DeviceChannels.All(ChannelObj => ChannelObj == null)) {
                this._logSupport.WriteCommandLog("CAN NOT ISSUE WRITE COMMANDS ON A DEVICE WITH NO OPENED CHANNELS!", LogType.ErrorLog);
                return false;
            }

            // Log information. If all channels are null, then exit.
            J2534Channel ChannelInUse = this._defaultChannel;
            this._logSupport.WriteCommandLog($"ISSUING PASSTHRU WRITE COMMAND FOR {MessageToSend.Length} MESSAGES WITH TIMEOUT", LogType.InfoLog);
            if (ChannelId != -1)
            {
                // Check our Device Channel ID
                ChannelInUse = DeviceChannels?.FirstOrDefault(ChannelObj => ChannelObj != null);
                if (ChannelInUse == null) 
                {
                    // Log can't operate on a null channel and exit method
                    this._logSupport.WriteCommandLog("CAN NOT CLEAR WRITE MESSAGES TO NULL CHANNELS!", LogType.ErrorLog);
                    return false;
                }
            }

            // Log information and send out our messages
            this._logSupport.WriteCommandLog($"SENDING MESSAGES ON CHANNEL WITH ID: {ChannelInUse.ChannelId}", LogType.InfoLog);
            foreach (var MsgObject in MessageToSend) { this._logSupport.WriteCommandLog($"\tISSUING MESSAGE: {MsgObject}", LogType.TraceLog); }

            // Issue command, log output and return.
            uint MessagesSent = ChannelInUse.PTWriteMessages(MessageToSend, SendTimeout);
            this._logSupport.WriteCommandLog($"SENT A TOTAL OF {MessagesSent} OUT OF AN EXPECTED {MessageToSend.Length} MESSAGES!", LogType.WarnLog);
            if (MessagesSent != MessageToSend.Length) { this._logSupport.WriteCommandLog("ERROR! FAILED TO SEND OUT THE REQUESTED PT MESSAGE!", LogType.ErrorLog); }
            return MessagesSent == MessageToSend.Length;
        }
        /// <summary>
        /// Reads a given number of messages from the first open channel found with the supplied timeout.
        /// </summary>
        /// <param name="MessagesToRead"></param>
        /// <param name="ReadTimeout"></param>
        /// <returns></returns>
        public PassThruStructs.PassThruMsg[] PTReadMessages(uint MessagesToRead = 1, uint ReadTimeout = 250, int ChannelId = -1)
        {
            // Log information. If all channels are null, then exit.
            if (this.DeviceChannels.All(ChannelObj => ChannelObj == null)) {
                this._logSupport.WriteCommandLog("CAN NOT ISSUE WRITE COMMANDS ON A DEVICE WITH NO OPENED CHANNELS!", LogType.ErrorLog);
                return Array.Empty<PassThruStructs.PassThruMsg>();
            }

            // Log information and issue the command. Find the channel to use here.
            J2534Channel ChannelInUse = this._defaultChannel;
            this._logSupport.WriteCommandLog($"ISSUING A PTREAD MESSAGES COMMAND FOR A TOTAL OF {MessagesToRead} MESSAGES WITH A TIMEOUT OF {ReadTimeout}", LogType.InfoLog);
            if (ChannelId != -1)
            {
                // Check our Device Channel ID
                ChannelInUse = DeviceChannels?.FirstOrDefault(ChannelObj => ChannelObj != null);
                if (ChannelInUse == null) 
                {
                    // Log can't operate on a null channel and exit method
                    this._logSupport.WriteCommandLog("CAN NOT CLEAR WRITE MESSAGES TO NULL CHANNELS!", LogType.ErrorLog);
                    return Array.Empty<PassThruStructs.PassThruMsg>();
                }
            }

            // Find the channel to use and send out the command.
            var ChannelInstance = this.DeviceChannels.FirstOrDefault(ChannelObj => ChannelObj != null);
            this._logSupport.WriteCommandLog($"READING MESSAGES ON CHANNEL WITH ID: {ChannelInstance.ChannelId}", LogType.InfoLog);
            var ReadMessages = ChannelInstance.PTReadMessages(ref MessagesToRead, ReadTimeout);

            // If no messages found, log an error and drop back out.
            if (ReadMessages.Length == 0) {
                this._logSupport.WriteCommandLog("ERROR! NO MESSAGES PROCESSED FROM OUR PTREAD COMMAND!", LogType.ErrorLog);
                return Array.Empty<PassThruStructs.PassThruMsg>();
            }

            // Print our messages out and return them.
            this._logSupport.WriteCommandLog("RETURNING OUT CONTENTS FOR MESSAGES PULLED IN NOW!", LogType.InfoLog);
            this._logSupport.WriteCommandLog($"READ A TOTAL OF {ReadMessages.Length} OUT OF {MessagesToRead} EXPECTED MESSAGES", LogType.InfoLog);
            this._logSupport.WriteCommandLog(J2534Device.PTMessageToTableString(ReadMessages));
            if (MessagesToRead != ReadMessages.Length) this._logSupport.WriteCommandLog("WARNING! READ MISMATCH ON MESSAGE COUNT!", LogType.WarnLog);
            return ReadMessages;
        }
        #endregion

        #region PassThruStartMsgFilter - PassThruStopMessageFilter
        /// <summary>
        /// Builds a new Message filter from a set of input data and returns it. Passed out the Id of the filter built.
        /// </summary>
        /// <returns>Filter object built from this command.</returns>
        public J2534Filter PTStartMessageFilter(FilterDef FilterType, string Mask, string Pattern, string FlowControl = null, uint FilterFlags = 0x00, uint FilterProtocol = 0x00, int ChannelId = -1)
        {
            // Log information, build our filter object, and issue command to start it.
            if (this.DeviceChannels.All(ChannelObj => ChannelObj == null)) {
                this._logSupport.WriteCommandLog("CAN NOT ISSUE FILTER COMMANDS ON A DEVICE WITH NO OPENED CHANNELS!", LogType.ErrorLog);
                return null;
            }

            // Find our channel object to use here
            J2534Channel ChannelInUse = this._defaultChannel;
            if (ChannelId != -1)
            {
                // Check our Device Channel ID
                ChannelInUse = DeviceChannels?.FirstOrDefault(ChannelObj => ChannelObj != null);
                if (ChannelInUse == null)
                {
                    // Log can't operate on a null channel and exit method
                    this._logSupport.WriteCommandLog("CAN NOT CLEAR WRITE MESSAGES TO NULL CHANNELS!", LogType.ErrorLog);
                    return null;
                }
            }

            // Find the channel to use and send out the command.
            this._logSupport.WriteCommandLog($"ISSUING A PASSTHRU FILTER ({FilterType}) COMMAND NOW", LogType.InfoLog);
            this._logSupport.WriteCommandLog($"STARTING FILTER ON CHANNEL WITH ID: {ChannelInUse.ChannelId}", LogType.InfoLog);

            // Issue command, log output and return.
            J2534Filter OutputFilter = ChannelInUse.StartMessageFilter(FilterType, Mask, Pattern, FlowControl, FilterFlags, (ProtocolId)FilterProtocol);
            if (OutputFilter != null) this._logSupport.WriteCommandLog($"STARTED NEW FILTER CORRECTLY! FILTER ID: {OutputFilter.FilterId}", LogType.InfoLog);
            this._logSupport.WriteCommandLog("FILTER OBJECT HAS BEEN STORED! RETURNING OUTPUT CONTENTS NOW");
            return OutputFilter;
        } 
        /// <summary>
        /// Stops a filter by the ID of it provided.
        /// </summary>
        /// <param name="FilterId">Stops the filter matching this ID</param>
        /// <returns>True if stopped. False if not.</returns>
        public bool PTStopMessageFilter(uint FilterId)
        {
            // Log information, build our filter object, and issue command to stop it.
            if (this.DeviceChannels.All(ChannelObj => ChannelObj == null)) {
                this._logSupport.WriteCommandLog("CAN NOT ISSUE FILTER COMMANDS ON A DEVICE WITH NO OPENED CHANNELS!", LogType.ErrorLog);
                return false;
            }

            // Find the filter object here and store value for it.
            var LocatedFilter = this.ChannelFilters
                .SelectMany(FilterSet => FilterSet)
                .FirstOrDefault(FilterObj => FilterObj.FilterId == FilterId);

            // Ensure filter is not null and continue.
            if (LocatedFilter == null) {
                this._logSupport.WriteCommandLog($"ERROR! NO FILTERS FOUND FOR THE GIVEN FILTER ID OF {FilterId}", LogType.ErrorLog);
                return false;
            }

            // Issue the stop command here.
            for (int ChannelIndex = 0; ChannelIndex < this.DeviceChannels.Length; ChannelIndex++)
            {
                if (!this.ChannelFilters[ChannelIndex].Contains(LocatedFilter)) continue;
                this._logSupport.WriteCommandLog($"STOPPING FILTER ID {FilterId} ON CHANNEL {ChannelIndex} (ID: {this.DeviceChannels[ChannelIndex].ChannelId}) NOW!", LogType.InfoLog);
                this.DeviceChannels[ChannelIndex].StopMessageFilter(LocatedFilter);
                return true;
            }

            // If we get here, something is wrong.
            this._logSupport.WriteCommandLog("ERROR! COULD NOT FIND A CHANNEL WITH THE GIVEN FILTER TO STOP ON! THIS IS WEIRD!", LogType.ErrorLog);
            return false;
        }
        /// <summary>
        /// Stops a filter by the instance of it provided
        /// </summary>
        /// <param name="FilterInstance">Stops the filter matching this object provided</param>
        /// <returns>True if stopped. False if not.</returns>
        public bool PTStopMessageFilter(J2534Filter FilterInstance)
        {
            // Log information, find the filter and stop it.
            if (this.DeviceChannels.All(ChannelObj => ChannelObj == null)) {
                this._logSupport.WriteCommandLog("CAN NOT ISSUE FILTER COMMANDS ON A DEVICE WITH NO OPENED CHANNELS!", LogType.ErrorLog);
                return false;
            }

            // Find the index of the filter.
            var ChannelFilterSet = this.ChannelFilters.FirstOrDefault(FilterSet => FilterSet.Contains(FilterInstance));
            if (ChannelFilterSet == null) {
                this._logSupport.WriteCommandLog("ERROR! COULD NOT FIND FILTER OBJECT TO REMOVE FROM OUR INSTANCE!", LogType.ErrorLog);
                return false;
            }

            // Issue the command here.
            int ChannelIndex = this.ChannelFilters.ToList().IndexOf(ChannelFilterSet);
            this._logSupport.WriteCommandLog($"STOPPING FILTER ID {FilterInstance.FilterId} ON CHANNEL {ChannelIndex} (ID: {this.DeviceChannels[ChannelIndex].ChannelId}) NOW!", LogType.InfoLog);
            this.DeviceChannels[ChannelIndex].StopMessageFilter(FilterInstance);
            return true;
        }
        #endregion

    }
}
