using System;
using System.Linq;
using System.Runtime.CompilerServices;
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
        // Logger object for a session instance and helper methods
        private readonly SubServiceLogger SessionLogger;

        /// <summary>
        /// Builds an output splitting command line value.
        /// </summary>
        /// <param name="SplitChar">Split char value</param>
        /// <param name="LineSize">Size of line</param>
        /// <returns>Built splitting string</returns>
        private string SplitLineString(string SplitChar = "=", int LineSize = 150)
        {
            // Build output string by combining the input values as many chars long as specified
            return string.Join(string.Empty, Enumerable.Repeat(
                SplitChar == "" ? "=" : SplitChar,
                LineSize <= 50 ? 50 : LineSize)
            );
        }
        /// <summary>
        /// Writes a basic log output value and includes the name of the PT Command being sent out.
        /// </summary>
        /// <param name="LoggerObject">Logger to write with</param>
        /// <param name="Message">Message to write</param>
        /// <param name="Level">Level to write</param>
        private void WriteCommandLog(string Message, LogType Level = LogType.DebugLog, [CallerMemberName] string MemberName = "PT COMMAND")
        {
            // Find the command type being issued. If none found, then just write normal output.
            if (!MemberName.StartsWith("PT"))
            {
                this.SessionLogger?.WriteLog($"[{MemberName}] ::: {Message}", LogType.InfoLog);
                return;
            }

            // Now write our output contents.
            string SessionName = $"{this.DeviceName} - {this.DllName}";
            string FinalMessage = $"[{SessionName}][{MemberName}] ::: {Message}";
            SessionLogger?.WriteLog(FinalMessage, Level);
        }

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

        // DLL and Device Versions
        public JVersion DllVersion => JDeviceDll.DllVersion;
        public JVersion DeviceVersion => JDeviceInstance.J2534Version;

        // DLL and Device Names/IDs
        public string DllName => JDeviceDll.LongName;
        public uint DeviceId => JDeviceInstance.DeviceId;
        public string DeviceName => JDeviceInstance.DeviceName;

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

        // ------------------------------------------------------------------------------------------------------------------------------------------

        // Device Channel Information, filters, and periodic messages.
        public J2534Channel[] DeviceChannels => JDeviceInstance.DeviceChannels;
        public J2534Channel[][] DeviceLogicalChannels => JDeviceInstance.DeviceChannels.Select(ChObj => ChObj.LogicalChannels).ToArray();
        public J2534Filter[][] ChannelFilters => JDeviceInstance.DeviceChannels.Select(ChObj => ChObj.JChannelFilters).ToArray();
        public J2534PeriodicMessage[][] ChannelPeriodicMsgs => JDeviceInstance.DeviceChannels.Select(ChObj => ChObj.JChannelPeriodicMessages).ToArray();

        // ------------------------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Builds a new J2534 Session instance object using the DLL Name provided.
        /// </summary>
        /// <param name="DllNameFilter">Dll to use</param>
        /// <param name="DeviceNameFilter">Name of the device To use.</param>
        /// <param name="Version">Version of the API</param>
        public Sharp2534Session(JVersion Version, string DllNameFilter, string DeviceNameFilter = "")
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

            // Build a new session logger object here to use for logging commands and output.
            this.SessionLogger = new SubServiceLogger($"{this.DllName}_{this.DeviceName}_SessionLogger");
            this.SessionLogger.WriteLog(this.SplitLineString(), LogType.TraceLog);
            this.SessionLogger.WriteLog("SHARPWRAP J2534 SESSION BUILT CORRECTLY! SESSION STATE IS BEING PRINTED OUT BELOW", LogType.InfoLog);
            this.SessionLogger.WriteLog(this.ToDetailedString());
            this.SessionLogger.WriteLog(this.SplitLineString(), LogType.TraceLog);
        }
        /// <summary>
        /// Releases an instance of the J2534 Session objects.
        /// </summary>
        ~Sharp2534Session()
        {
            // Log killing this instance.
            this.SessionLogger.WriteLog(this.SplitLineString(), LogType.TraceLog);
            this.SessionLogger.WriteLog("KILLING SHARPWARP SESSION INSTANCE!", LogType.WarnLog);
            this.SessionLogger.WriteLog($"SESSION WAS LOCKED ONTO DEVICE AND DLL {this.DeviceName} - {this.DllName}", LogType.InfoLog);
            this.SessionLogger.WriteLog(this.SplitLineString(), LogType.TraceLog);

            // Begin with device, then the DLL.
            this.JDeviceDll = null;
            this.JDeviceInstance = null;
        }

        // ------------------------------------------------- Object Location Routines/Methods ------------------------------------------------------

        // TODO: WRITE IN LOGIC FOR CONTROLLING EXISTING LOGICAL OBJECTS FOR PULLING IN FILTERS/MESSAGES CONSTRUCTED ON OUR CHANNELS!

        // ------------------------------------------------- PassThru Command Routines/Methods ------------------------------------------------------

        #region PassThruOpen - PassThruClose
        /// <summary>
        /// PTOpen command passed thru
        /// </summary>
        public bool PTOpen()
        {
            // Log and open our JBox instance.
            this.JDeviceInstance.PTOpen();
            this.WriteCommandLog("OPENED NEW J2534 INSTANCE OK!", LogType.InfoLog);
            this.WriteCommandLog($"DEVICE NAME AND ID: {this.DeviceName} - {this.DeviceId}", LogType.InfoLog);
            this.WriteCommandLog($"DEVICE OPEN: {this.JDeviceInstance.IsOpen}");

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
            this.WriteCommandLog("CLOSED OUR J2534 INSTANCE OK!", LogType.InfoLog);
            this.WriteCommandLog($"DEVICE OPEN: {this.JDeviceInstance.IsOpen}");

            // Return if the the device is closed
            return this.JDeviceInstance.IsOpen == false;
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
            this.WriteCommandLog("SENDING OUT PASSTHRU CONNECT METHOD NOW.", LogType.InfoLog);
            this.WriteCommandLog($"CONNECT PARAMETERS: {ChannelIndex}, {Protocol}, {ChannelBaud}", LogType.TraceLog);

            // Run our connect routine here.
            this.JDeviceInstance.PTConnect(ChannelIndex, Protocol, Flags, ChannelBaud);
            ChannelId = this.JDeviceInstance.DeviceChannels[ChannelIndex].ChannelId;

            // Log information and return output.
            this.WriteCommandLog($"PULLED OUT CHANNEL ID: {ChannelId}", LogType.InfoLog);
            return this.JDeviceInstance.DeviceChannels[ChannelIndex];
        }
        /// <summary>
        /// Runs a PTDisconnect
        /// </summary>
        /// <param name="ChannelIndex">Index to disconnect</param>
        public void PTDisconnect(int ChannelIndex)
        {
            // Log information and issue disconnect.
            this.WriteCommandLog($"DISCONNECTING CHANNEL INDEX: {ChannelIndex}", LogType.WarnLog);
            this.JDeviceInstance.PTDisconnect(ChannelIndex);
        }
        #endregion

        #region PassThruWriteMessages - PassThruReadMessages
        /// <summary>
        /// Sends a message on the first possible channel found.
        /// </summary>
        /// <param name="MessageToSend">Message to send out</param>
        /// <param name="SendTimeout">Timeout for send operation</param>
        public bool PTWriteMessages(PassThruStructs.PassThruMsg MessageToSend, uint SendTimeout = 100)
        {
            // Log information. If all channels are null, then exit.
            this.WriteCommandLog($"ISSUING PASSTHRU WRITE COMMAND WITH TIMEOUT AND MESSAGE: {SendTimeout} - {MessageToSend}", LogType.InfoLog);
            if (this.DeviceChannels.All(ChannelObj => ChannelObj == null))
            {
                this.WriteCommandLog("CAN NOT ISSUE WRITE COMMANDS ON A DEVICE WITH NO OPENED CHANNELS!", LogType.ErrorLog);
            }

            // Find the channel to use and send out the command.
            var ChannelInstance = this.DeviceChannels.FirstOrDefault(ChannelObj => ChannelObj != null);
            this.WriteCommandLog($"SENDING MESSAGES ON CHANNEL WITH ID: {ChannelInstance.ChannelId}", LogType.InfoLog);

            // Issue command, log output and return.
            uint MessagesSent = ChannelInstance.PTWriteMessages(MessageToSend, SendTimeout);
            this.WriteCommandLog($"SENT A TOTAL OF {MessagesSent} OUT OF AN EXPECTED 1 MESSAGE!", LogType.WarnLog);
            if (MessagesSent != 1) { this.WriteCommandLog("ERROR! FAILED TO SEND OUT THE REQUESTED PT MESSAGE!", LogType.ErrorLog); }
            return MessagesSent == 1;
        }
        /// <summary>
        /// Sends a message on the first possible channel found.
        /// </summary>
        /// <param name="MessageToSend">Message to send out</param>
        /// <param name="SendTimeout">Timeout for send operation</param>
        public bool PTWriteMessages(PassThruStructs.PassThruMsg[] MessageToSend, uint SendTimeout = 100)
        {
            // Log information. If all channels are null, then exit.
            this.WriteCommandLog($"ISSUING PASSTHRU WRITE COMMAND FOR {MessageToSend.Length} MESSAGES WITH TIMEOUT", LogType.InfoLog);
            foreach (var MsgObject in MessageToSend) { this.WriteCommandLog($"\tISSUING MESSAGE: {MsgObject}", LogType.TraceLog); }
            if (this.DeviceChannels.All(ChannelObj => ChannelObj == null))
            {
                this.WriteCommandLog("CAN NOT ISSUE WRITE COMMANDS ON A DEVICE WITH NO OPENED CHANNELS!", LogType.ErrorLog);
            }

            // Find the channel to use and send out the command.
            var ChannelInstance = this.DeviceChannels.FirstOrDefault(ChannelObj => ChannelObj != null);
            this.WriteCommandLog($"SENDING MESSAGES ON CHANNEL WITH ID: {ChannelInstance.ChannelId}", LogType.InfoLog);

            // Issue command, log output and return.
            uint MessagesSent = ChannelInstance.PTWriteMessages(MessageToSend, SendTimeout);
            this.WriteCommandLog($"SENT A TOTAL OF {MessagesSent} OUT OF AN EXPECTED {MessageToSend.Length} MESSAGES!", LogType.WarnLog);
            if (MessagesSent != MessageToSend.Length) { this.WriteCommandLog("ERROR! FAILED TO SEND OUT THE REQUESTED PT MESSAGE!", LogType.ErrorLog); }
            return MessagesSent == MessageToSend.Length;
        }
        /// <summary>
        /// Sends a message on the channel index provided.
        /// </summary>
        /// <param name="MessageToSend">Message to send out</param>
        /// <param name="SendTimeout">Timeout for send operation</param>
        public bool PTWriteMessages(int ChannelIndex, PassThruStructs.PassThruMsg MessageToSend, uint SendTimeout = 100)
        {
            // Log information. If all channels are null, then exit.
            this.WriteCommandLog($"ISSUING PASSTHRU WRITE COMMAND WITH TIMEOUT AND MESSAGE: {SendTimeout} - {MessageToSend}", LogType.InfoLog);
            if (this.DeviceChannels.All(ChannelObj => ChannelObj == null))
            {
                this.WriteCommandLog("CAN NOT ISSUE WRITE COMMANDS ON A DEVICE WITH NO OPENED CHANNELS!", LogType.ErrorLog);
            }

            // Validate input for index of pulled channel.
            if (ChannelIndex > this.DeviceChannels.Length)
            {
                this.WriteCommandLog("ERROR! CHANNEL INDEX WAS OUTSIDE RANGE OF POSSIBLE CHANNEL IDS!", LogType.ErrorLog);
                return false;
            }

            // Now write the output messages.
            var ChannelInstance = this.DeviceChannels[ChannelIndex];
            this.WriteCommandLog($"SENDING MESSAGES ON CHANNEL WITH ID: {ChannelInstance.ChannelId}", LogType.InfoLog);

            // Issue command, log output and return.
            uint MessagesSent = ChannelInstance.PTWriteMessages(MessageToSend, SendTimeout);
            this.WriteCommandLog($"SENT A TOTAL OF {MessagesSent} OUT OF AN EXPECTED 1 MESSAGE!", LogType.WarnLog);
            if (MessagesSent != 1) { this.WriteCommandLog("ERROR! FAILED TO SEND OUT THE REQUESTED PT MESSAGE!", LogType.ErrorLog); }
            return MessagesSent == 1;
        }
        /// <summary>
        /// Sends a message on the channel index provided.
        /// </summary>
        /// <param name="MessageToSend">Message to send out</param>
        /// <param name="SendTimeout">Timeout for send operation</param>
        public bool PTWriteMessages(int ChannelIndex, PassThruStructs.PassThruMsg[] MessageToSend, uint SendTimeout = 100)
        {
            // Log information. If all channels are null, then exit.
            this.WriteCommandLog($"ISSUING PASSTHRU WRITE COMMAND FOR {MessageToSend.Length} MESSAGES WITH TIMEOUT", LogType.InfoLog);
            foreach (var MsgObject in MessageToSend) { this.WriteCommandLog($"\tISSUING MESSAGE: {MsgObject}", LogType.TraceLog); }
            if (this.DeviceChannels.All(ChannelObj => ChannelObj == null))
            {
                this.WriteCommandLog("CAN NOT ISSUE WRITE COMMANDS ON A DEVICE WITH NO OPENED CHANNELS!", LogType.ErrorLog);
            }

            // Validate input for index of pulled channel.
            if (ChannelIndex > this.DeviceChannels.Length)
            {
                this.WriteCommandLog("ERROR! CHANNEL INDEX WAS OUTSIDE RANGE OF POSSIBLE CHANNEL IDS!", LogType.ErrorLog);
                return false;
            }

            // Now write the output messages.
            var ChannelInstance = this.DeviceChannels[ChannelIndex];
            this.WriteCommandLog($"SENDING MESSAGES ON CHANNEL WITH ID: {ChannelInstance.ChannelId}", LogType.InfoLog);

            // Issue command, log output and return.
            uint MessagesSent = ChannelInstance.PTWriteMessages(MessageToSend, SendTimeout);
            this.WriteCommandLog($"SENT A TOTAL OF {MessagesSent} OUT OF AN EXPECTED {MessageToSend.Length} MESSAGES!", LogType.WarnLog);
            if (MessagesSent != MessageToSend.Length) { this.WriteCommandLog("ERROR! FAILED TO SEND OUT THE REQUESTED PT MESSAGE!", LogType.ErrorLog); }
            return MessagesSent == MessageToSend.Length;
        }
        /// <summary>
        /// Reads a given number of messages from the first open channel found with the supplied timeout.
        /// </summary>
        /// <param name="MessagesToRead"></param>
        /// <param name="ReadTimeout"></param>
        /// <returns></returns>
        public PassThruStructs.PassThruMsg[] PTReadMessages(uint MessagesToRead = 1, uint ReadTimeout = 250)
        {
            // Log information and issue the command. Find the channel to use here.
            this.WriteCommandLog($"ISSUING A PTREAD MESSAGES COMMAND FOR A TOTAL OF {MessagesToRead} MESSAGES WITH A TIMEOUT OF {ReadTimeout}", LogType.InfoLog);
            if (this.DeviceChannels.All(ChannelObj => ChannelObj == null))
            {
                this.WriteCommandLog("CAN NOT ISSUE READ COMMANDS ON A DEVICE WITH NO OPENED CHANNELS!", LogType.ErrorLog);
            }

            // Find the channel to use and send out the command.
            var ChannelInstance = this.DeviceChannels.FirstOrDefault(ChannelObj => ChannelObj != null);
            this.WriteCommandLog($"READING MESSAGES ON CHANNEL WITH ID: {ChannelInstance.ChannelId}", LogType.InfoLog);
            var ReadMessages = ChannelInstance.PTReadMessages(ref MessagesToRead, ReadTimeout);

            // If no messages found, log an error and drop back out.
            if (ReadMessages.Length == 0)
            {
                this.WriteCommandLog("ERROR! NO MESSAGES PROCESSED FROM OUR PTREAD COMMAND!", LogType.ErrorLog);
                return Array.Empty<PassThruStructs.PassThruMsg>();
            }

            // Print our messages out and return them.
            this.WriteCommandLog("RETURNING OUT CONTENTS FOR MESSAGES PULLED IN NOW!", LogType.InfoLog);
            this.WriteCommandLog($"READ A TOTAL OF {ReadMessages.Length} OUT OF {MessagesToRead} EXPECTED MESSAGES", LogType.InfoLog);
            this.WriteCommandLog(J2534Device.PTMessageToTableString(ReadMessages));
            if (MessagesToRead != ReadMessages.Length) this.WriteCommandLog("WARNING! READ MISMATCH ON MESSAGE COUNT!", LogType.WarnLog);
            return ReadMessages;
        }
        /// <summary>
        /// Reads a given number of messages from the first open channel found with the supplied timeout.
        /// </summary>
        /// <param name="MessagesToRead"></param>
        /// <param name="ReadTimeout"></param>
        /// <returns></returns>
        public PassThruStructs.PassThruMsg[] PTReadMessages(int ChannelIndex, uint MessagesToRead = 1, uint ReadTimeout = 250)
        {
            // Log information and issue the command. Find the channel to use here.
            this.WriteCommandLog($"ISSUING A PTREAD MESSAGES COMMAND FOR A TOTAL OF {MessagesToRead} MESSAGES WITH A TIMEOUT OF {ReadTimeout}", LogType.InfoLog);
            if (this.DeviceChannels.All(ChannelObj => ChannelObj == null))
            {
                this.WriteCommandLog("CAN NOT ISSUE READ COMMANDS ON A DEVICE WITH NO OPENED CHANNELS!", LogType.ErrorLog);
                return Array.Empty<PassThruStructs.PassThruMsg>();
            }

            // Validate input for index of pulled channel.
            if (ChannelIndex > this.DeviceChannels.Length)
            {
                this.WriteCommandLog("ERROR! CHANNEL INDEX WAS OUTSIDE RANGE OF POSSIBLE CHANNEL IDS!", LogType.ErrorLog);
                return Array.Empty<PassThruStructs.PassThruMsg>();
            }

            // Find the channel to use and send out the command.
            var ChannelInstance = this.DeviceChannels[ChannelIndex];
            this.WriteCommandLog($"READING MESSAGES ON CHANNEL WITH ID: {ChannelInstance.ChannelId}", LogType.InfoLog);
            var ReadMessages = ChannelInstance.PTReadMessages(ref MessagesToRead, ReadTimeout);

            // If no messages found, log an error and drop back out.
            if (ReadMessages.Length == 0)
            {
                this.WriteCommandLog("ERROR! NO MESSAGES PROCESSED FROM OUR PTREAD COMMAND!", LogType.ErrorLog);
                return Array.Empty<PassThruStructs.PassThruMsg>();
            }

            // Print our messages out and return them.
            this.WriteCommandLog("RETURNING OUT CONTENTS FOR MESSAGES PULLED IN NOW!", LogType.InfoLog);
            this.WriteCommandLog($"READ A TOTAL OF {ReadMessages.Length} OUT OF {MessagesToRead} EXPECTED MESSAGES", LogType.InfoLog);
            this.WriteCommandLog(J2534Device.PTMessageToTableString(ReadMessages));
            if (MessagesToRead != ReadMessages.Length) this.WriteCommandLog("WARNING! READ MISMATCH ON MESSAGE COUNT!", LogType.WarnLog);
            return ReadMessages;
        }
        #endregion

        #region PassThruStartMsgFilter - PassThruStopMessageFilter
        /// <summary>
        /// Builds a new Message filter from a set of input data and returns it. Passed out the Id of the filter built.
        /// </summary>
        /// <returns>Filter object built from this command.</returns>
        public J2534Filter PTStartMessageFilter(FilterDef FilterType, string Mask, string Pattern, string FlowControl = null, uint FilterFlags = 0x00)
        {
            // Log information, build our filter object, and issue command to start it.
            this.WriteCommandLog($"ISSUING A PASSTHRU FILTER ({FilterType}) COMMAND NOW", LogType.InfoLog);
            if (this.DeviceChannels.All(ChannelObj => ChannelObj == null))
            {
                this.WriteCommandLog("CAN NOT ISSUE FILTER COMMANDS ON A DEVICE WITH NO OPENED CHANNELS!", LogType.ErrorLog);
            }

            // Find the channel to use and send out the command.
            var ChannelInstance = this.DeviceChannels.FirstOrDefault(ChannelObj => ChannelObj != null);
            this.WriteCommandLog($"STARTING FILTER ON CHANNEL WITH ID: {ChannelInstance.ChannelId}", LogType.InfoLog);

            // Issue command, log output and return.
            J2534Filter OutputFilter = ChannelInstance.StartMessageFilter(FilterType, Mask, Pattern, FlowControl);
            if (OutputFilter != null) this.WriteCommandLog($"STARTED NEW FILTER CORRECTLY! FILTER ID: {OutputFilter.FilterId}", LogType.InfoLog);
            this.WriteCommandLog("FILTER OBJECT HAS BEEN STORED! RETURNING OUTPUT CONTENTS NOW");
            return OutputFilter;
        }
        /// <summary>
        /// Builds a new Message filter from a set of input data and returns it. Passed out the Id of the filter built.
        /// </summary>
        /// <returns>Filter object built from this command.</returns>
        public J2534Filter PTStartMessageFilter(int ChannelIndex, FilterDef FilterType, string Mask, string Pattern, string FlowControl = null, uint FilterFlags = 0x00)
        {
            // Log information, build our filter object, and issue command to start it.
            this.WriteCommandLog($"ISSUING A PASSTHRU FILTER ({FilterType}) COMMAND NOW", LogType.InfoLog);
            if (this.DeviceChannels.All(ChannelObj => ChannelObj == null))
            {
                this.WriteCommandLog("CAN NOT ISSUE FILTER COMMANDS ON A DEVICE WITH NO OPENED CHANNELS!", LogType.ErrorLog);
            }

            // Validate input for index of pulled channel.
            if (ChannelIndex > this.DeviceChannels.Length)
            {
                this.WriteCommandLog("ERROR! CHANNEL INDEX WAS OUTSIDE RANGE OF POSSIBLE CHANNEL IDS!", LogType.ErrorLog);
                return null;
            }

            // Find the channel to use and send out the command.
            var ChannelInstance = this.DeviceChannels.FirstOrDefault(ChannelObj => ChannelObj != null);
            this.WriteCommandLog($"STARTING FILTER ON CHANNEL WITH ID: {ChannelInstance.ChannelId}", LogType.InfoLog);

            // Issue command, log output and return.
            J2534Filter OutputFilter = ChannelInstance.StartMessageFilter(FilterType, Mask, Pattern, FlowControl);
            if (OutputFilter != null) this.WriteCommandLog($"STARTED NEW FILTER CORRECTLY! FILTER ID: {OutputFilter.FilterId}", LogType.InfoLog);
            this.WriteCommandLog("FILTER OBJECT HAS BEEN STORED! RETURNING OUTPUT CONTENTS NOW");
            return OutputFilter;
        }
        /// <summary>
        /// Stops a filter by the ID of it provided.
        /// </summary>
        /// <param name="FilterId">Stops the filter matching this ID</param>
        /// <returns>True if stopped. False if not.</returns>
        public bool PTStopMessageFilter(uint FilterId)
        {
            // Log information, find the filter and stop it.
            this.WriteCommandLog($"ISSUING A PASSTHRU STOP MESSAGE FILTER COMMAND NOW FOR FILTER {FilterId}", LogType.InfoLog);
            if (this.DeviceChannels.All(ChannelObj => ChannelObj == null))
            {
                this.WriteCommandLog("CAN NOT ISSUE FILTER COMMANDS ON A DEVICE WITH NO OPENED CHANNELS!", LogType.ErrorLog);
            }

            // Find the filter object here and store value for it.
            var LocatedFilter = this.ChannelFilters
                .SelectMany(FilterSet => FilterSet)
                .FirstOrDefault(FilterObj => FilterObj.FilterId == FilterId);

            // Ensure filter is not null and continue.
            if (LocatedFilter == null)
            {
                this.WriteCommandLog($"ERROR! NO FILTERS FOUND FOR THE GIVEN FILTER ID OF {FilterId}", LogType.ErrorLog);
                return false;
            }

            // Issue the stop command here.
            for (int ChannelIndex = 0; ChannelIndex < this.DeviceChannels.Length; ChannelIndex++)
            {
                if (!this.ChannelFilters[ChannelIndex].Contains(LocatedFilter)) continue;
                this.WriteCommandLog($"STOPPING FILTER ID {FilterId} ON CHANNEL {ChannelIndex} NOW!", LogType.InfoLog);
                this.DeviceChannels[ChannelIndex].StopMessageFilter(LocatedFilter);
                return true;
            }

            // If we get here, something is wrong.
            this.WriteCommandLog("ERROR! COULD NOT FIND A CHANNEL WITH THE GIVEN FILTER TO STOP ON! THIS IS WEIRD!", LogType.ErrorLog);
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
            this.WriteCommandLog($"ISSUING A PASSTHRU STOP MESSAGE FILTER COMMAND NOW FOR FILTER {FilterInstance.FilterId}", LogType.InfoLog);
            if (this.DeviceChannels.All(ChannelObj => ChannelObj == null))
            {
                this.WriteCommandLog("CAN NOT ISSUE FILTER COMMANDS ON A DEVICE WITH NO OPENED CHANNELS!", LogType.ErrorLog);
            }

            // Find the index of the filter.
            var ChannelFilterSet = this.ChannelFilters.FirstOrDefault(FilterSet => FilterSet.Contains(FilterInstance));
            if (ChannelFilterSet == null)
            {
                this.WriteCommandLog("ERROR! COULD NOT FIND FILTER OBJECT TO REMOVE FROM OUR INSTANCE!", LogType.ErrorLog);
                return false;
            }

            // Issue the command here.
            int IndexOfChannel = this.ChannelFilters.ToList().IndexOf(ChannelFilterSet);
            this.WriteCommandLog($"STOPPING FILTER ID {FilterInstance.FilterId} ON CHANNEL {IndexOfChannel} NOW!", LogType.InfoLog);
            this.DeviceChannels[IndexOfChannel].StopMessageFilter(FilterInstance);
            return true;
        }
        #endregion

        #region PassThruClearRX - PassThruClearTX
        /// <summary>
        /// Clears out the RX buffer on a current device instance
        /// </summary>
        public void PassThruClearRxBuffer(int ChannelId = -1)
        {
            // Log Clearing RX buffer, clear it and return 
            this.WriteCommandLog($"CLEARING RX BUFFER FROM DEVICE {this.DeviceName} NOW...", LogType.InfoLog);
            if (ChannelId == -1)
            {
                // Check our Device Channel ID
                var ChannelFound = DeviceChannels?.FirstOrDefault(ChannelObj => ChannelObj != null);
                if (ChannelFound == null) { this.WriteCommandLog("CAN NOT CLEAR RX BUFFER FROM NULL CHANNELS!", LogType.ErrorLog); return; }
                ChannelId = (int)ChannelFound.ChannelId;
            }

            // Clear out the channel RX Buffer by the ID here
            if (ChannelId == -1) { this.WriteCommandLog("CHANNEL ID WAS -1! CAN NOT CLEAR!", LogType.ErrorLog); return; }
            this.WriteCommandLog($"CLEARING RX BUFFER FROM CHANNEL ID: {ChannelId}!", LogType.WarnLog);
            this.JDeviceInstance.ApiInstance.PassThruIoctl((uint)ChannelId, IoctlId.CLEAR_RX_BUFFER, IntPtr.Zero, IntPtr.Zero);
        }

        /// <summary>
        /// Clears out the TX buffer on a current device instance
        /// </summary>
        public void PassThruClearTxBuffer()
        {

        }

        #endregion
    }
}
