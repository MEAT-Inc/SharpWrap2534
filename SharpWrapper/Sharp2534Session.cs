using System;
using System.IO;
using System.Linq;
using SharpLogging;
using SharpSupport;
using SharpWrapper.J2534Objects;
using SharpWrapper.PassThruImport;
using SharpWrapper.PassThruTypes;

namespace SharpWrapper
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
            if (SharpLogBroker.LogFileFolder == null)
            {
                // Configure a new logging session but keep logging disabled since nothing else requested we create it
                SharpLogBroker.InitializeLogging(new SharpLogBroker.BrokerConfiguration()
                {
                    LogBrokerName = "SharpWrap2534",
                    LogFilePath = Path.Combine(Directory.GetCurrentDirectory(), "SharpLogging"),
                    MinLogLevel = LogType.NoLogging,
                    MaxLogLevel = LogType.NoLogging
                });
            }

            // Build logging support
            this.SessionGuid = Guid.NewGuid();
            this._logSupport = new LoggingSupport(this.DeviceName, this._sessionLogger);
            this._sessionLogger = new SharpLogger(LoggerActions.UniversalLogger, $"SharpSession_{this.SessionGuid.ToString("D".ToUpper())}");
            this._sessionLogger.WriteLog(this._logSupport.SplitLineString(), LogType.TraceLog);
            this._sessionLogger.WriteLog("SHARPWRAP J2534 SESSION BUILT CORRECTLY! SESSION STATE IS BEING PRINTED OUT BELOW", LogType.InfoLog);
            this._sessionLogger.WriteLog($"\n{this.ToDetailedString()}");
            this._sessionLogger.WriteLog(this._logSupport.SplitLineString(), LogType.TraceLog);
        }

        // ------------------------------------------------------------------------------------------------------------------------------------------

        // Logger object for a session instance and helper methods
        private readonly LoggingSupport _logSupport;
        private readonly SharpLogger _sessionLogger;

        // ------------------------------------------------------------------------------------------------------------------------------------------

        // DLL and Device Instance for our J2534 Box.       
        public J2534Dll JDeviceDll { get; private set; }                 // The DLL Instance in use.
        public J2534Device JDeviceInstance { get; private set; }         // The Device instance in use.

        // ------------------------------------------------------------------------------------------------------------------------------------------

        // Status of this session instance, device, and DLL objects
        public SharpSessionStatus SessionStatus =>
            DllStatus == SharpSessionStatus.INITIALIZED && DeviceStatus == SharpSessionStatus.INITIALIZED ?
                SharpSessionStatus.INITIALIZED :
                SharpSessionStatus.NULL;
        public SharpSessionStatus DllStatus => JDeviceDll.JDllStatus;
        public SharpSessionStatus DeviceStatus => JDeviceInstance.DeviceStatus;

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

        // ------------------------------------------------- Universal PassThru Command Routines/Methods ------------------------------------------------------

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
        public J2534Channel PTConnect(int ChannelIndex, ProtocolId Protocol, PassThroughConnect Flags, BaudRate ChannelBaud, out uint ChannelId)
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
       
        #region PassThruLogicalConnect - PassThruLogicalDisconnect
        /// <summary>
        /// Builds a new logical channel on the given physical channel for our current instance
        /// If no physical channels are found, then one is built if possible.
        /// </summary>
        /// <param name="Protocol">Logical channel protocol</param>
        /// <param name="Flags">Logical channel flags</param>
        /// <param name="ChannelDescriptor">Connection configuration</param>
        /// <param name="LogicalChannelId">ID of the built logical channel</param>
        /// <returns>The logical channel built when this method executes</returns>
        public J2534Channel PTLogicalConnect(ProtocolId Protocol, PassThroughConnect Flags, PassThruStructs.ISO15765ChannelDescriptor ChannelDescriptor, out uint LogicalChannelId)
        {
            // Start by finding a physical channel to connect onto.
            if (this.DeviceChannels.All(ChannelObj => ChannelObj == null)) {
                this._logSupport.WriteCommandLog("CAN NOT ISSUE LOGICAL COMMANDS ON A DEVICE WITH NO OPENED CHANNELS!", LogType.ErrorLog);
                LogicalChannelId = 0; return null;
            }

            // Find our ISO15765 channel to build a logical connection on
            var ParentPhysicalChannel = this.DeviceChannels.FirstOrDefault(ChannelObj => ChannelObj.J2534Version == JVersion.V0500 && ChannelObj.ProtocolId == ProtocolId.ISO15765);
            if (ParentPhysicalChannel == null) {
                this._logSupport.WriteCommandLog("NO PHYSICAL CHANNEL OBJECTS WERE FOUND TO ISSUE LOGICAL COMMANDS ONTO!", LogType.ErrorLog);
                LogicalChannelId = 0; return null;
            }

            // Now issue the logical connect command using the channel command
            J2534Channel BuiltLogicalChannel = ParentPhysicalChannel.PTLogicalConnect(Protocol, (uint)Flags, ChannelDescriptor); 
            this._logSupport.WriteCommandLog($"ISSUED A PTLOGICAL CONNECT COMMAND FOR PHYSICAL CHANNEL {ParentPhysicalChannel.ChannelId} WITHOUT ISSUES!", LogType.InfoLog);
            if (BuiltLogicalChannel == null) {
                this._logSupport.WriteCommandLog("FAILED TO BUILD A NEW LOGICAL CHANNEL! THE OUTPUT OBJECT WAS NULL!", LogType.ErrorLog);
                LogicalChannelId = 0; return null;
            }

            // Store the channel output ID and return the channel
            LogicalChannelId = BuiltLogicalChannel.ChannelId;
            return BuiltLogicalChannel;
        }
        /// <summary>
        /// Disconnects a Logical J2534 channel from a physical channel parent
        /// </summary>
        /// <param name="ChannelId"></param>
        public void PTLogicalDisconnect(uint ChannelId)
        {
            // Start by finding a physical channel to connect onto.
            if (this.DeviceChannels.All(ChannelObj => ChannelObj == null)) {
                this._logSupport.WriteCommandLog("CAN NOT ISSUE LOGICAL COMMANDS ON A DEVICE WITH NO OPENED CHANNELS!", LogType.ErrorLog);
                return;
            }

            // Find our ISO15765 channel to build a logical connection on
            var ParentPhysicalChannel = this.DeviceChannels.FirstOrDefault(ChannelObj => ChannelObj.J2534Version == JVersion.V0500 && ChannelObj.ProtocolId == ProtocolId.ISO15765);
            if (ParentPhysicalChannel == null) {
                this._logSupport.WriteCommandLog("NO PHYSICAL CHANNEL OBJECTS WERE FOUND TO ISSUE LOGICAL COMMANDS ONTO!", LogType.ErrorLog);
                return;
            }

            // Now issue our disconnect routine
            ParentPhysicalChannel.PTLogicalDisconnect(ChannelId);
            this._logSupport.WriteCommandLog($"ISSUED A PTLOGICAL DISCONNECT COMMAND FOR PHYSICAL CHANNEL {ParentPhysicalChannel.ChannelId} WITHOUT ISSUES!", LogType.InfoLog);
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

        #region PTIoctl (Set Pins, Get Config, Set Config)
        /// <summary>
        /// Issues a Set pins command routine on our selected channel object
        /// </summary>
        /// <param name="PinsToSet">Pins to select</param>
        /// <param name="ChannelId">ID of the channel to force this command onto</param>
        public void PTSetPins(int PinsToSet, int ChannelId = -1)
        {
            // Check for all null channels
            if (this.DeviceChannels.All(ChannelObj => ChannelObj == null)) {
                this._logSupport.WriteCommandLog("CAN NOT ISSUE IOCTL COMMANDS ON A DEVICE WITH NO OPENED CHANNELS!", LogType.ErrorLog);
                return;
            }

            // Log Clearing RX buffer, clear it and return 
            J2534Channel ChannelInUse = this._defaultChannel;
            this._logSupport.WriteCommandLog($"SETTING PINS ON CHANNEL FOR DEVICE {this.DeviceName} NOW...", LogType.InfoLog);
            if (ChannelId != -1)
            {
                // Check our Device Channel ID
                ChannelInUse = DeviceChannels?.FirstOrDefault(ChannelObj => ChannelObj != null);
                if (ChannelInUse != null)
                {
                    // Log can't operate on a null channel and exit method
                    this._logSupport.WriteCommandLog("CAN NOT SET PINS ON NULL CHANNELS!", LogType.ErrorLog);
                    return;
                }
            }

            // Issue the Set Pins command here
            this._logSupport.WriteCommandLog($"USING DEVICE INSTANCE {this.DeviceName} FOR BUFFER OPERATIONS", LogType.InfoLog);
            this._logSupport.WriteCommandLog($"SETTING PINS TO {PinsToSet} FOR CHANNEL ID: {ChannelInUse.ChannelId}!", LogType.WarnLog);
            ChannelInUse.SetPins((uint)PinsToSet);
        }
        /// <summary>
        /// Runs a PTGetConfig command on the desired channel object
        /// </summary>
        /// <param name="ConfigParam">Configuration to get</param>
        /// <param name="ChannelId">Force ID of the channel to use</param>
        /// <param name="ConfigValue">Value located from the configuration object</param>
        public bool PTGetConfig(ConfigParamId ConfigParam, out uint ConfigValue, int ChannelId = -1)
        {
            // Check for all null channels
            if (this.DeviceChannels.All(ChannelObj => ChannelObj == null)) {
                this._logSupport.WriteCommandLog("CAN NOT ISSUE IOCTL COMMANDS ON A DEVICE WITH NO OPENED CHANNELS!", LogType.ErrorLog);
                ConfigValue = 0; return false;
            }

            // Log Clearing RX buffer, clear it and return 
            J2534Channel ChannelInUse = this._defaultChannel;
            this._logSupport.WriteCommandLog($"GETTING CONFIGURATION FROM CHANNEL CHANNEL FOR DEVICE {this.DeviceName} NOW...", LogType.InfoLog);
            if (ChannelId != -1)
            {
                // Check our Device Channel ID
                ChannelInUse = DeviceChannels?.FirstOrDefault(ChannelObj => ChannelObj != null);
                if (ChannelInUse != null)
                {
                    // Log can't operate on a null channel and exit method
                    this._logSupport.WriteCommandLog("CAN NOT GET CONFIGURATIONS ON NULL CHANNELS!", LogType.ErrorLog);
                    ConfigValue = 0; return false;
                }
            }

            // Issue the Set Pins command here
            this._logSupport.WriteCommandLog($"USING DEVICE INSTANCE {this.DeviceName} FOR BUFFER OPERATIONS", LogType.InfoLog);
            this._logSupport.WriteCommandLog($"GETTING CONFIGURATION {ConfigParam} FOR CHANNEL ID: {ChannelInUse.ChannelId}!", LogType.WarnLog); 
            ConfigValue = ChannelInUse.GetConfig(ConfigParam);
            return true;
        }
        /// <summary>
        /// Issues a Set configuration routine on the channel desired with the config param and value wanted
        /// </summary>
        /// <param name="ConfigParam">Configuration param to set</param>
        /// <param name="ConfigValue">Value to set on the configuration</param>
        /// <param name="ChannelId">Forced ID of the channel to use</param>
        /// <returns></returns>
        public bool PTSetConfig(ConfigParamId ConfigParam, uint ConfigValue, int ChannelId = -1)
        { 
            // Check for all null channels
            if (this.DeviceChannels.All(ChannelObj => ChannelObj == null)) {
                this._logSupport.WriteCommandLog("CAN NOT ISSUE IOCTL COMMANDS ON A DEVICE WITH NO OPENED CHANNELS!", LogType.ErrorLog);
                return false;
            }

            // Log Clearing RX buffer, clear it and return 
            J2534Channel ChannelInUse = this._defaultChannel;
            this._logSupport.WriteCommandLog($"SETTING CONFIGURATION FROM CHANNEL CHANNEL FOR DEVICE {this.DeviceName} NOW...", LogType.InfoLog);
            if (ChannelId != -1)
            {
                // Check our Device Channel ID
                ChannelInUse = DeviceChannels?.FirstOrDefault(ChannelObj => ChannelObj != null);
                if (ChannelInUse != null)
                {
                    // Log can't operate on a null channel and exit method
                    this._logSupport.WriteCommandLog("CAN NOT SET CONFIGURATIONS ON NULL CHANNELS!", LogType.ErrorLog);
                    return false;
                }
            }

            // Now issue the Set Configuration command on the channel in use
            this._logSupport.WriteCommandLog($"USING DEVICE INSTANCE {this.DeviceName} FOR BUFFER OPERATIONS", LogType.InfoLog);
            this._logSupport.WriteCommandLog($"SETTINGS CONFIGURATION {ConfigParam} WITH VALUE {ConfigValue} FOR CHANNEL ID: {ChannelInUse.ChannelId}!", LogType.WarnLog);
            ChannelInUse.SetConfig(ConfigParam, ConfigValue);
            return true;
        }
        #endregion

        #region PTIoctl (Five Baud Init, Fast Init)
        /// <summary>
        /// Issues a new FiveBaudInit routine with the given connection byte
        /// </summary>
        /// <param name="InputByte">Byte to issue to the bus</param>
        /// <param name="ChannelId">Forced ID of the channel to use</param>
        /// <param name="ResponseBytes">Response byte from the bus</param>
        /// <returns>True if execution passes, false if not.</returns>
        public bool PTFiveBaudInit(byte InputByte, out byte[] ResponseBytes, int ChannelId = -1)
        {
            // Check for all null channels
            if (this.DeviceChannels.All(ChannelObj => ChannelObj == null)) {
                this._logSupport.WriteCommandLog("CAN NOT ISSUE IOCTL COMMANDS ON A DEVICE WITH NO OPENED CHANNELS!", LogType.ErrorLog);
                ResponseBytes = null; return false;
            }

            // Log Clearing RX buffer, clear it and return 
            J2534Channel ChannelInUse = this._defaultChannel;
            this._logSupport.WriteCommandLog($"ISSUING FIVE BAUD INIT FOR DEVICE {this.DeviceName} NOW...", LogType.InfoLog);
            if (ChannelId != -1)
            {
                // Check our Device Channel ID
                ChannelInUse = DeviceChannels?.FirstOrDefault(ChannelObj => ChannelObj != null);
                if (ChannelInUse != null)
                {
                    // Log can't operate on a null channel and exit method
                    this._logSupport.WriteCommandLog("CAN NOT RUN FIVE BAUD INIT ON NULL CHANNELS!", LogType.ErrorLog);
                    ResponseBytes = null; return false;
                }
            }

            // Issue the 5 Baud Init Command here
            this._logSupport.WriteCommandLog($"USING DEVICE INSTANCE {this.DeviceName} FOR BUFFER OPERATIONS", LogType.InfoLog);
            this._logSupport.WriteCommandLog($"SENDING CONFIGURATION BYTE {InputByte.ToString("X")} FOR CHANNEL ID: {ChannelInUse.ChannelId}!", LogType.WarnLog); 
            ResponseBytes = ChannelInUse.FiveBaudInit(InputByte);
            return true;
        }
        /// <summary>
        /// Issues a Fast init command to the desired channel object and returns the responses
        /// </summary>
        /// <param name="InputBytes">Bytes to send to the bus</param>
        /// <param name="RequiresResponse">Response required or not.</param>
        /// <param name="ResponseBytes">Bytes returned from the bus</param>
        /// <param name="ChannelId">Forced ID of the channel to issue this command on</param>
        /// <returns>True if execution passes, false if not.</returns>
        public bool PTFastInit(byte[] InputBytes, bool RequiresResponse, out byte[] ResponseBytes, int ChannelId = -1)
        {
            // Check for all null channels
            if (this.DeviceChannels.All(ChannelObj => ChannelObj == null)) {
                this._logSupport.WriteCommandLog("CAN NOT ISSUE IOCTL COMMANDS ON A DEVICE WITH NO OPENED CHANNELS!", LogType.ErrorLog);
                ResponseBytes = null; return false;
            }

            // Log Clearing RX buffer, clear it and return 
            J2534Channel ChannelInUse = this._defaultChannel;
            this._logSupport.WriteCommandLog($"ISSUING FAST INIT FOR DEVICE {this.DeviceName} NOW...", LogType.InfoLog);
            if (ChannelId != -1)
            {
                // Check our Device Channel ID
                ChannelInUse = DeviceChannels?.FirstOrDefault(ChannelObj => ChannelObj != null);
                if (ChannelInUse != null)
                {
                    // Log can't operate on a null channel and exit method
                    this._logSupport.WriteCommandLog("CAN NOT FAST INIT ON NULL CHANNELS!", LogType.ErrorLog);
                    ResponseBytes = null; return false;
                }
            }

            // Issue the Fast Init Command here
            string InputByteString = string.Join(" ", InputBytes.Select(ByteObj => "0x" + ByteObj.ToString("X")));
            this._logSupport.WriteCommandLog($"USING DEVICE INSTANCE {this.DeviceName} FOR BUFFER OPERATIONS", LogType.InfoLog);
            this._logSupport.WriteCommandLog($"SENDING CONFIGURATION BYTES {InputByteString} FOR CHANNEL ID: {ChannelInUse.ChannelId}!", LogType.WarnLog);
            ResponseBytes = ChannelInUse.FastInit(InputBytes, RequiresResponse);
            return true;
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
            this._logSupport.WriteCommandLog($"ISSUING PASSTHRU WRITE COMMAND WITH TIMEOUT AND MESSAGE: {SendTimeout}ms - 1 MESSAGES", LogType.InfoLog);

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
            foreach (var MsgObject in MessageToSend) { this._logSupport.WriteCommandLog($"\tISSUING MESSAGE: {BitConverter.ToString(MsgObject.Data)}", LogType.TraceLog); }

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
            try
            {
                // Build logging output table for field information
                var BuiltStrings = J2534Device.PTMessageToTableString(ReadMessages);
                this._logSupport.WriteCommandLog(BuiltStrings);
            } 
            catch
            {
                // Log them out normally here
                this._logSupport.WriteCommandLog("MESSAGES READ WITHOUT ISSUES! PRINTING THEM OUT BELOW IN HEX FORMAT NOW...", LogType.InfoLog);
                this._logSupport.WriteCommandLog(string.Join("\t-->", ReadMessages.Select(MsgObj => BitConverter.ToString(MsgObj.Data))));
            }
            if (MessagesToRead != ReadMessages.Length) this._logSupport.WriteCommandLog("WARNING! READ MISMATCH ON MESSAGE COUNT!", LogType.WarnLog);
            return ReadMessages;
        }
        #endregion

        #region PassThruSelect - PassThruQueueMessages
        /// <summary>
        /// Issues a PTSelect command on the given channel index
        /// </summary>
        /// <param name="ChannelId">ID of the PARENT channel to issue our PTSelect routine on</param>
        /// <param name="SelectedChannelSet">Selected channel objects found and returned</param>
        /// <returns>True if select routine passes, false if not.</returns>
        public bool PTSelect(out PassThruStructs.SChannelSet SelectedChannelSet, int ChannelId = -1)
        {
            // Start by finding a physical channel to connect onto.
            if (this.DeviceChannels.All(ChannelObj => ChannelObj == null)) {
                this._logSupport.WriteCommandLog("CAN NOT ISSUE LOGICAL COMMANDS ON A DEVICE WITH NO OPENED CHANNELS!", LogType.ErrorLog);
                SelectedChannelSet = new PassThruStructs.SChannelSet(0,0); return false;
            }

            // Find our ISO15765 channel to build a logical connection on
            var ParentPhysicalChannel = ChannelId == -1 ?
                this.DeviceChannels.FirstOrDefault(ChannelObj => ChannelObj.J2534Version == JVersion.V0500 && ChannelObj.ProtocolId == ProtocolId.ISO15765) :
                this.DeviceChannels.FirstOrDefault(ChannelObj => ChannelObj.ChannelId == ChannelId);
            if (ParentPhysicalChannel == null) {
                this._logSupport.WriteCommandLog("NO PHYSICAL CHANNEL OBJECTS WERE FOUND TO ISSUE LOGICAL COMMANDS ONTO!", LogType.ErrorLog);
                SelectedChannelSet = new PassThruStructs.SChannelSet(0, 0); return false;
            }
            
            // Issue the PTSelect command here
            SelectedChannelSet = (PassThruStructs.SChannelSet)ParentPhysicalChannel.PTSelect();
            this._logSupport.WriteCommandLog($"ISSUED A PTSELECT FOR PHYSICAL CHANNEL {ParentPhysicalChannel.ChannelId} WITHOUT ISSUES!", LogType.InfoLog);
            return true;
        }
        /// <summary>
        /// Issues a PTQueue messages command on a desired logical channel.
        /// </summary>
        /// <param name="LogicalChannelId">ID of the channel to issue the commands on</param>
        /// <param name="MessageToWrite">Message to queue</param>
        /// <param name="PhysicalChannelId">Forced ID of the parent physical channel</param>
        /// <returns>True if the queue routine passes, false if not.</returns>
        public bool PTQueueMessages(uint LogicalChannelId, PassThruStructs.PassThruMsg MessageToWrite, int PhysicalChannelId = -1)
        {
             // Start by finding a physical channel to connect onto.
            if (this.DeviceChannels.All(ChannelObj => ChannelObj == null)) {
                this._logSupport.WriteCommandLog("CAN NOT ISSUE LOGICAL COMMANDS ON A DEVICE WITH NO OPENED CHANNELS!", LogType.ErrorLog);
                return false;
            }

            // Find our ISO15765 channel to build a logical connection on
            var ParentPhysicalChannel = PhysicalChannelId == -1 ?
                this.DeviceChannels.FirstOrDefault(ChannelObj => ChannelObj.J2534Version == JVersion.V0500 && ChannelObj.ProtocolId == ProtocolId.ISO15765) :
                this.DeviceChannels.FirstOrDefault(ChannelObj => ChannelObj.ChannelId == PhysicalChannelId);
            if (ParentPhysicalChannel == null) {
                this._logSupport.WriteCommandLog("NO PHYSICAL CHANNEL OBJECTS WERE FOUND TO ISSUE LOGICAL COMMANDS ONTO!", LogType.ErrorLog);
                return false;
            }

            // Now find the logical channel to use for the send command
            var LogicalChildChannel = ParentPhysicalChannel.LogicalChannels.FirstOrDefault(LogicalChannelObj => LogicalChannelObj.ChannelId == LogicalChannelId);
            if (LogicalChildChannel == null) {
                this._logSupport.WriteCommandLog($"NO LOGICAL COMMAND WITH ID {LogicalChannelId} COULD BE FOUND!", LogType.ErrorLog);
                return false;
            }

            // Finally, use our logical channel to issue the PTQueue command
            uint QueuedMessageCount = 0;
            LogicalChildChannel.PTQueueMessages(MessageToWrite, ref QueuedMessageCount);
            this._logSupport.WriteCommandLog($"ISSUED A PTQUEUE MESSAGES COMMAND TO PHYSICAL CHANNEL {ParentPhysicalChannel.ChannelId} AT LOGICAL CHANNEL {LogicalChildChannel.ChannelId} OK!", LogType.InfoLog);
            return true;
        }
        /// <summary>
        /// Issues a PTQueue messages command on a desired logical channel.
        /// </summary>
        /// <param name="LogicalChannelId">ID of the channel to issue the commands on</param>
        /// <param name="MessagesToWrite">Message to queue</param>
        /// <param name="PhysicalChannelId">Forced ID of the parent physical channel</param>
        /// <returns>True if the queue routine passes, false if not.</returns>
        public bool PTQueueMessages(uint LogicalChannelId, PassThruStructs.PassThruMsg[] MessagesToWrite, int PhysicalChannelId = -1)
        {
            // Start by finding a physical channel to connect onto.
            if (this.DeviceChannels.All(ChannelObj => ChannelObj == null))
            {
                this._logSupport.WriteCommandLog("CAN NOT ISSUE LOGICAL COMMANDS ON A DEVICE WITH NO OPENED CHANNELS!", LogType.ErrorLog);
                return false;
            }

            // Find our ISO15765 channel to build a logical connection on
            var ParentPhysicalChannel = PhysicalChannelId == -1 ?
                this.DeviceChannels.FirstOrDefault(ChannelObj => ChannelObj.J2534Version == JVersion.V0500 && ChannelObj.ProtocolId == ProtocolId.ISO15765) :
                this.DeviceChannels.FirstOrDefault(ChannelObj => ChannelObj.ChannelId == PhysicalChannelId);
            if (ParentPhysicalChannel == null)
            {
                this._logSupport.WriteCommandLog("NO PHYSICAL CHANNEL OBJECTS WERE FOUND TO ISSUE LOGICAL COMMANDS ONTO!", LogType.ErrorLog);
                return false;
            }

            // Now find the logical channel to use for the send command
            var LogicalChildChannel = ParentPhysicalChannel.LogicalChannels.FirstOrDefault(LogicalChannelObj => LogicalChannelObj.ChannelId == LogicalChannelId);
            if (LogicalChildChannel == null)
            {
                this._logSupport.WriteCommandLog($"NO LOGICAL COMMAND WITH ID {LogicalChannelId} COULD BE FOUND!", LogType.ErrorLog);
                return false;
            }

            // Finally, use our logical channel to issue the PTQueue command
            uint QueuedMessageCount = 0;
            LogicalChildChannel.PTQueueMessages(MessagesToWrite, ref QueuedMessageCount);
            this._logSupport.WriteCommandLog($"ISSUED A PTQUEUE MESSAGES COMMAND TO PHYSICAL CHANNEL {ParentPhysicalChannel.ChannelId} AT LOGICAL CHANNEL {LogicalChildChannel.ChannelId} OK!", LogType.InfoLog);
            return true;
        }

        #endregion

        #region PassThruStartMsgFilter - PassThruStopMessageFilter
        /// <summary>
        /// Builds a new Message filter from a set of input data and returns it. Passed out the Id of the filter built.
        /// </summary>
        /// <returns>Filter object built from this command.</returns>
        public J2534Filter PTStartMessageFilter(ProtocolId FilterProtocol, FilterDef FilterType, string Mask, string Pattern, string FlowControl = null, TxFlags FilterFlags = 0x00, int ChannelId = -1)
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
            J2534Filter OutputFilter = ChannelInUse.StartMessageFilter(FilterProtocol, FilterType, Mask, Pattern, FlowControl, FilterFlags);
            if (OutputFilter != null) this._logSupport.WriteCommandLog($"STARTED NEW FILTER CORRECTLY! FILTER ID: {OutputFilter.FilterId}", LogType.InfoLog);
            this._logSupport.WriteCommandLog("FILTER OBJECT HAS BEEN STORED! RETURNING OUTPUT CONTENTS NOW");
            return OutputFilter;
        }
        /// <summary>
        /// Builds a new Message filter from a set of input data and returns it. Passed out the Id of the filter built.
        /// </summary>
        /// <returns>Filter object built from this command.</returns>
        public J2534Filter PTStartMessageFilter(J2534Filter FilterToStart, int ChannelId = -1)
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
            this._logSupport.WriteCommandLog($"ISSUING A PASSTHRU FILTER ({FilterToStart.FilterType}) COMMAND NOW", LogType.InfoLog);
            this._logSupport.WriteCommandLog($"STARTING FILTER ON CHANNEL WITH ID: {ChannelInUse.ChannelId}", LogType.InfoLog);

            // Issue command, log output and return.
            J2534Filter OutputFilter = ChannelInUse.StartMessageFilter(FilterToStart);
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

        #region PassThruStartPeriodicMsg - PassThruStopPeriodicMsg
        /// <summary>
        /// Builds a new J2534 Periodic message using the given message and the send interval provided
        /// </summary>
        /// <param name="MessageToWrite">Message to send our device</param>
        /// <param name="SendInterval">Delay between send commands </param>
        /// <param name="ChannelId">Forced channel ID to use</param>
        /// <returns></returns>
        public J2534PeriodicMessage PTStartPeriodicMessage(PassThruStructs.PassThruMsg MessageToWrite, uint SendInterval, int ChannelId = -1)
        {
            // Log information. If all channels are null, then exit.
            if (this.DeviceChannels.All(ChannelObj => ChannelObj == null)) {
                this._logSupport.WriteCommandLog("CAN NOT ISSUE PERIODIC COMMANDS ON A DEVICE WITH NO OPENED CHANNELS!", LogType.ErrorLog);
                return null;
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
                    return null;
                }
            }

            // Log information out and prepare to send our periodic message
            this._logSupport.WriteCommandLog($"SENDING PERIODIC MESSAGES ON CHANNEL WITH ID: {ChannelInUse.ChannelId}", LogType.InfoLog);
            this._logSupport.WriteCommandLog($"ISSUING PASSTHRU START PERIODIC COMMAND WITH TIMEOUT AND MESSAGE: {SendInterval}ms - 1 MESSAGES", LogType.InfoLog);

            // Issue command, log output and return.
            J2534PeriodicMessage MessageBuilt = ChannelInUse.StartPeriodicMessage(MessageToWrite, SendInterval);
            this._logSupport.WriteCommandLog($"ISSUED A PTSTARTPERIODIC MESSAGE COMMAND TO OUR API INSTANCE", LogType.WarnLog);
            if (MessageBuilt == null) { this._logSupport.WriteCommandLog("ERROR! FAILED TO SEND OUT THE REQUESTED PT MESSAGE!", LogType.ErrorLog); }
            return MessageBuilt;
        }
        /// <summary>
        /// Stops a Periodic Message based on the ID of the message
        /// </summary>
        /// <param name="MessageId">ID of the message to stop sending</param>
        /// <returns>True if removed, false if it fails</returns>
        public bool PTStopPeriodicMessage(uint MessageId)
        {
            // Log information, build our filter object, and issue command to stop it.
            if (this.DeviceChannels.All(ChannelObj => ChannelObj == null)) {
                this._logSupport.WriteCommandLog("CAN NOT ISSUE PERIODIC COMMANDS ON A DEVICE WITH NO OPENED CHANNELS!", LogType.ErrorLog);
                return false;
            }

            // Find the message object here and store value for it.
            var LocatedPeriodicMsg = this.ChannelPeriodicMsgs
                .SelectMany(MsgSet => MsgSet)
                .FirstOrDefault(MessageObject => MessageObject.MessageId == MessageId);

            // Ensure message object is not null and continue.
            if (LocatedPeriodicMsg == null) {
                this._logSupport.WriteCommandLog($"ERROR! NO MESSAGES FOUND FOR THE GIVEN MESSAGE ID OF {MessageId}", LogType.ErrorLog);
                return false;
            }

            // Issue the stop command here.
            for (int ChannelIndex = 0; ChannelIndex < this.DeviceChannels.Length; ChannelIndex++)
            {
                if (!this.ChannelPeriodicMsgs[ChannelIndex].Contains(LocatedPeriodicMsg)) continue;
                this._logSupport.WriteCommandLog($"STOPPING PERIODIC MESSAGE WITH ID {MessageId} ON CHANNEL {ChannelIndex} (ID: {this.DeviceChannels[ChannelIndex].ChannelId}) NOW!", LogType.InfoLog);
                this.DeviceChannels[ChannelIndex].StopPeriodicMessage(LocatedPeriodicMsg);
                return true;
            }

            // If we get here, something is wrong.
            this._logSupport.WriteCommandLog("ERROR! COULD NOT FIND A CHANNEL WITH THE GIVEN PERIODIC MESSAGE TO STOP ON! THIS IS WEIRD!", LogType.ErrorLog);
            return false;
        }
        /// <summary>
        /// Stops a Periodic Message based on the ID of the message
        /// </summary>
        /// <param name="MessageInstance">The built message object object to stop</param>
        /// <returns>True if removed, false if it fails</returns>
        public bool PTStopPeriodicMessage(J2534PeriodicMessage MessageInstance)
        {
            // Log information, build our filter object, and issue command to stop it.
            if (this.DeviceChannels.All(ChannelObj => ChannelObj == null)) {
                this._logSupport.WriteCommandLog("CAN NOT ISSUE PERIODIC COMMANDS ON A DEVICE WITH NO OPENED CHANNELS!", LogType.ErrorLog);
                return false;
            }

            // Issue the stop command here.
            for (int ChannelIndex = 0; ChannelIndex < this.DeviceChannels.Length; ChannelIndex++)
            {
                if (!this.ChannelPeriodicMsgs[ChannelIndex].Contains(MessageInstance)) continue;
                this._logSupport.WriteCommandLog($"STOPPING PERIODIC MESSAGE WITH ON CHANNEL {ChannelIndex} (ID: {this.DeviceChannels[ChannelIndex].ChannelId}) NOW!", LogType.InfoLog);
                this.DeviceChannels[ChannelIndex].StopPeriodicMessage(MessageInstance);
                return true;
            }

            // If we get here, something is wrong.
            this._logSupport.WriteCommandLog("ERROR! COULD NOT FIND A CHANNEL WITH THE GIVEN PERIODIC MESSAGE TO STOP ON! THIS IS WEIRD!", LogType.ErrorLog);
            return false;
        }
        #endregion
    }
}
