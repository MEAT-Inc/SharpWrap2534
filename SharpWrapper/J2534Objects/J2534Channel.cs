using System;
using System.Collections.Generic;
using System.Linq;
using SharpWrap2534.PassThruTypes;
using SharpWrap2534.SupportingLogic;

namespace SharpWrap2534.J2534Objects
{
    /// <summary>
    /// J2534 Channel object. Used to control device channels.
    /// THIS IS A SINGLETON CONFIGURED TYPE BASED ON THE DLL VERSION! 
    /// </summary>
    public sealed class J2534Channel
    {
        // -------------------------- SINGLETON CONFIGURATION ----------------------------

        // Singleton schema for this class object. Max channels type can exist.
        private static J2534Channel[][] _j2534Channels;

        /// <summary>
        /// Private Singleton instance builder.
        /// THESE CHANNELS BUILT ARE NOT TRACKED BY THE SINGLETON INSTANCE!
        /// </summary>
        /// <param name="JDevice">Device to use</param>
        /// <param name="ChannelId">ChannelId</param>
        /// <param name="ProtocolId">Protocol Id</param>
        /// <param name="ConnectFlags">Connection flags</param>
        /// <param name="ChannelBaud">BaudRate of the channel.</param>
        private J2534Channel(J2534Device JDevice, J2534Channel PhysicalParent = null)
        {
            // Setup device channel properties.
            _jDevice = JDevice;
            J2534Version = _jDevice.J2534Version;
            ChannelStatus = PTInstanceStatus.INITIALIZED;

            // PTConstants
            var TypeConstants = new PassThruConstants(JDevice.J2534Version);

            // Setup filters and messages.
            JChannelFilters = new J2534Filter[TypeConstants.MaxFilters];
            JChannelPeriodicMessages = new J2534PeriodicMessage[TypeConstants.MaxPeriodicMsgs];

            // Check for logical. If not, add this to our list of channel objects.
            if (PhysicalParent != null)
            {
                // Set logical on and store parent value.
                this.PhysicalParent = PhysicalParent;
                int MaxLogicalChannels = (int)new PassThruConstants(JVersion.V0500).MaxChannelsLogical;

                // Set Logical values. Build new logical array set.
                _logicalChannels = new J2534Channel[MaxLogicalChannels];
                for (int ChannelIndex = 0; ChannelIndex < MaxLogicalChannels; ChannelIndex++)
                    _logicalChannels[ChannelIndex] = new J2534Channel(JDevice, PhysicalParent);
            }
            else
            {
                // Append this to the singleton list object type.
                if (_j2534Channels == null)
                {
                    // Init List of channels here.
                    _j2534Channels = new J2534Channel[TypeConstants.MaxDeviceCount][];
                    for (int InstanceIndex = 0; InstanceIndex < TypeConstants.MaxDeviceCount; InstanceIndex ++)
                        _j2534Channels[InstanceIndex] = new J2534Channel[TypeConstants.MaxChannels];
                }
                
                // Find next open channel object
                ChannelIndex = _j2534Channels[(int)JDevice.DeviceNumber - 1].ToList().IndexOf(null);
                if (ChannelIndex == -1)
                {
                    // Build the open channel on our set of devices.
                    var OpenChannel = _j2534Channels[(int)JDevice.DeviceNumber - 1].FirstOrDefault(ChObj => ChObj.ChannelStatus == PTInstanceStatus.FREED);
                    ChannelIndex = _j2534Channels[(int)JDevice.DeviceNumber - 1].ToList().IndexOf(OpenChannel);
                }
                
                // Set channel index now.
                _j2534Channels[(int)JDevice.DeviceNumber - 1][ChannelIndex] = this;
            }
        }
        /// <summary>
        /// Deconstructs the device object and members
        /// </summary>
        ~J2534Channel()
        {
            // Null out member values for this channel
            try { _j2534Channels[(int)this._jDevice.DeviceNumber - 1][ChannelIndex] = null; }
            catch { } 
        }

        /// <summary>
        /// Builds a channel Array to use for connecting into.
        /// </summary>
        /// <param name="JDevice">Device to build channels for.</param>
        /// <returns>Array of built J2534 channels.</returns>
        internal static J2534Channel[] BuildDeviceChannels(J2534Device JDevice)
        {
            // Append channels into the device here.
            var JChannelsOut = new J2534Channel[new PassThruConstants(JDevice.J2534Version).MaxChannels];
            for (int ChannelIndex = 0; ChannelIndex < JChannelsOut.Length; ChannelIndex += 1)
                JChannelsOut[ChannelIndex] = new J2534Channel(JDevice);

            // Return built channels and store them onto the device object
            JDevice.DeviceChannels = JChannelsOut;
            return JChannelsOut;
        }

        // -------------------------------- INSTANCE VALUES AND SETUP FOR CHANNEL HERE -----------------------------------

        // Status values.
        public int ChannelIndex { get; }
        public JVersion J2534Version { get; }
        public PTInstanceStatus ChannelStatus { get; }

        // Device information
        private readonly J2534Device _jDevice;
        public uint ChannelId { get; private set; }
        public uint ChannelBaud { get; private set; }
        public uint ConnectFlags { get; private set; }
        public ProtocolId ProtocolId { get; private set; }

        // Logical Channels if possible.
        public J2534Channel PhysicalParent { get; }
        private readonly J2534Channel[] _logicalChannels;
        public bool IsLogicalChannel => this.PhysicalParent != null;
        public J2534Channel[] LogicalChannels => this.J2534Version == JVersion.V0500 ? _logicalChannels : null;

        // Filters and periodic messages.
        public J2534Filter[] JChannelFilters { get; private set; }
        public J2534PeriodicMessage[] JChannelPeriodicMessages { get; private set; }

        // ----------------------------------------- CHANNEL OPEN AND CLOSE METHODS ---------------------------------------

        /// <summary>
        /// Connects/subscribes this channels values with the values given
        /// </summary>
        /// <param name="ChannelId">ID of channel</param>
        /// <param name="ChannelProtocol">Protocol of channel</param>
        /// <param name="ChannelFlags">Flags of channel</param>
        /// <param name="ChannelBaud">Baudrate of channel</param>
        /// <returns></returns>
        internal bool ConnectChannel(uint ChannelId, ProtocolId ChannelProtocol, uint ChannelFlags, uint ChannelBaud)
        {
            // Store channel values.
            this.ChannelId = ChannelId;
            ProtocolId = ChannelProtocol;
            ConnectFlags = ChannelFlags;
            this.ChannelBaud = ChannelBaud;

            // Return stored ok
            return true;
        }
        /// <summary>
        /// Disconnects the provided channel
        /// </summary>
        /// <returns>True if removed. False if not.</returns>
        internal bool DisconnectChannel()
        {
            // Disconnect based on channel Id.
            var ChannelToDisconnect = _j2534Channels[this._jDevice.DeviceNumber - 1].FirstOrDefault(ChannelObj => ChannelObj.ChannelId == ChannelId);
            if (ChannelToDisconnect == null) { throw new InvalidOperationException("Failed to disconnect channel value!"); }

            // Disconnect and reinit here.
            int IndexOfChannel = _j2534Channels[this._jDevice.DeviceNumber - 1].ToList().IndexOf(ChannelToDisconnect);
            _j2534Channels[this._jDevice.DeviceNumber - 1][IndexOfChannel] = null;

            // Return passed.
            return true;
        }

        // ----------------------------------------- STATIC CHANNEL LOCATION METHODS ---------------------------------------

        /// <summary>
        /// Gets all of our filters and pulls one that matches.
        /// </summary>
        /// <param name="FilterId">Find this filter.</param>
        /// <param name="FilterFound">Filter located.</param>
        /// <param name="ChannelId">Use this if you want to only check a specific channel. Set to 0 if not wanted.</param>
        /// <returns>The filter matched and true, or false and nothing.</returns>
        public static bool LocateFilter(uint FilterId, out J2534Filter FilterFound, int ChannelId = -1)
        {
            // Find the new filter value here.
            J2534Channel[] LocatedChannels = _j2534Channels.SelectMany(ChSet => ChSet)
                .Where(ChannelObj => ChannelObj?.JChannelFilters != null)
                .ToArray();
            
            // Check index value
            if (ChannelId != -1) LocatedChannels = new[] { LocatedChannels.FirstOrDefault(ChannelObj => ChannelObj?.ChannelId == ChannelId) };

            // Extract just the filters from these channels
            var AllFilters = LocatedChannels.SelectMany(ChannelObj => ChannelObj?.JChannelFilters).ToArray();
            FilterFound = AllFilters.FirstOrDefault(FilterObj => FilterObj?.FilterId == FilterId) ??
                          new J2534Filter() { FilterStatus = PTInstanceStatus.NULL };

            // Return filter found or not.
            return FilterFound.FilterStatus != PTInstanceStatus.NULL;
        }
        /// <summary>
        /// Gets all of our filters and pulls one that matches.
        /// </summary>
        /// <param name="MessageId">Find this message.</param>
        /// <param name="MessageFound">Message located.</param>
        /// <param name="ChannelId">Use this if you want to only check a specific channel. Set to 0 if not wanted.</param>
        /// <returns>The filter matched and true, or false and nothing.</returns>
        public static bool LocatePeriodicMessage(uint MessageId, out J2534PeriodicMessage MessageFound, int ChannelId = -1)
        {
            // Find the new filter value here.
            J2534Channel[] LocatedChannels = _j2534Channels.SelectMany(ChSet => ChSet)
                .Where(ChannelObj => ChannelObj?.JChannelPeriodicMessages != null)
                .ToArray();
            
            // Check index value
            if (ChannelId != -1) LocatedChannels = new[] { LocatedChannels.FirstOrDefault(ChannelObj => ChannelObj?.ChannelId == ChannelId) };

            // Extract just the filters from these channels
            var AllMessages = LocatedChannels.SelectMany(ChannelObj => ChannelObj?.JChannelPeriodicMessages).ToArray();
            MessageFound = AllMessages.FirstOrDefault(MsgObj => MsgObj.MessageId == MessageId) ??
                           new J2534PeriodicMessage() { MessageStatus = PTInstanceStatus.NULL };

            // Return filter found or not.
            return MessageFound.MessageStatus != PTInstanceStatus.NULL;
        }

        // ---------------------------------------- INSTANCE CHANNEL LOCATION METHODS ---------------------------------------

        /// <summary>
        /// Gets all of our filters and pulls one that matches.
        /// </summary>
        /// <param name="FilterId">Find this filter.</param>
        /// <param name="FilterFound">Filter located.</param>
        /// <returns>The filter matched and true, or false and nothing.</returns>
        public bool LocateFilter(uint FilterId, out J2534Filter FilterFound)
        {
            // Temp return true.
            FilterFound = new J2534Filter();
            return true;
        }
        /// <summary>
        /// Gets all of our filters and pulls one that matches.
        /// </summary>
        /// <param name="MessageId">Find this message.</param>
        /// <param name="MessageFound">Message located.</param>
        /// <returns>The filter matched and true, or false and nothing.</returns>
        public bool LocatePeriodicMessage(uint MessageId, out J2534PeriodicMessage MessageFound)
        {
            // Temp true return.
            MessageFound = new J2534PeriodicMessage();
            return true;
        }

        // ----------------------------------------- INSTANCE CHANNEL CONFIGURATION METHODS -----------------------------------------

        /// <summary>
        /// Builds a new J2534 Filter for the given channel input.
        /// </summary>
        /// <param name="FilterType">Filter to store</param>
        /// <param name="MaskString">Mask value</param>
        /// <param name="PatternString">Pattern Value</param>
        /// <param name="FlowControl">Flow Ctl Value</param>
        /// <param name="FilterFlags">Flags for the filter</param>
        /// <param name="ForcedIndex">Forces a filter to be applied to a given index.</param>
        public J2534Filter StartMessageFilter(FilterDef FilterType, string MaskString, string PatternString, string FlowControl, uint FilterFlags = 0, ProtocolId FilterProtocol = default, int ForcedIndex = -1)
        {
            // Make sure filter array exists and check for the filters being null or if one exists identical to this desired filter.
            if (ForcedIndex >= 10) { throw new ArgumentOutOfRangeException("Unable to set filter for index over 9!"); }
            if (JChannelFilters == null) { JChannelFilters = new J2534Filter[new PassThruConstants(J2534Version).MaxFilters]; }

            // Build messages from filter strings.
            FilterProtocol = FilterProtocol == default ? ProtocolId : FilterProtocol;
            PassThruStructs.PassThruMsg PtMaskMsg = J2534Device.CreatePTMsgFromString(FilterProtocol, FilterFlags, MaskString);
            PassThruStructs.PassThruMsg PtPatternMsg = J2534Device.CreatePTMsgFromString(FilterProtocol, FilterFlags, PatternString);
            PassThruStructs.PassThruMsg PtFlowCtlMsg = J2534Device.CreatePTMsgFromString(FilterProtocol, FilterFlags, FlowControl);

            // Check if we need to override/replace a filter.
            if (ForcedIndex != -1 && JChannelFilters[ForcedIndex] != null)
            {
                // Remove old filter.
                uint OldFilterId = JChannelFilters[ForcedIndex].FilterId;
                _jDevice.ApiMarshall.PassThruStopMsgFilter(ChannelId, OldFilterId);
            }
            else
            {
                // Check if any of the filters are identical so far.
                if (JChannelFilters.FirstOrDefault(FilterObj => FilterObj.ToString()
                    .Contains($"MessageData: {MaskString ?? "NO_MASK"},{PatternString ?? "NO_PATTERN"},{FlowControl ?? "NO_FLOW"}")) != null)
                    throw new PassThruException("Can not apply an already existing filter!", J2534Err.ERR_INVALID_FILTER_ID);
            }

            // Build new filter here.
            int NextIndex = ForcedIndex == -1 ? JChannelFilters.ToList().IndexOf(null) : ForcedIndex;
            if (NextIndex == -1) throw new PassThruException("Failed to add new filter since there are no open slots!", J2534Err.ERR_INVALID_FILTER_ID);

            // Issue the new filter here and store onto filter list.
            _jDevice.ApiMarshall.PassThruStartMsgFilter(ChannelId, FilterType, PtMaskMsg, PtPatternMsg, PtFlowCtlMsg, out uint FilterId);
            if (FlowControl == null || FilterType != FilterDef.FLOW_CONTROL_FILTER)
                JChannelFilters[NextIndex] = new J2534Filter(FilterType.ToString(), MaskString, PatternString, FilterFlags, FilterId);
            else JChannelFilters[NextIndex] = new J2534Filter(FilterType.ToString(), MaskString, PatternString, FlowControl, FilterFlags, FilterId);

            // Return the new filter.
            return JChannelFilters[NextIndex];
        }
        /// <summary>
        /// Sets a new J2534 Filter for this channel using the values provided for it.
        /// </summary>
        /// <param name="FilterToSet">Filter to apply.</param>
        /// <param name="ForcedIndex">Forces index for the filter</param>
        public J2534Filter StartMessageFilter(J2534Filter FilterToSet, int ForcedIndex = -1)
        {
            // Set the filter using the above method. Set it up using the stings from our filter.
            FilterDef FilterType = (FilterDef)Enum.Parse(typeof(FilterDef), FilterToSet.FilterType);
            return StartMessageFilter(FilterType, FilterToSet.FilterMask, FilterToSet.FilterPattern, FilterToSet.FilterFlowCtl, FilterToSet.FilterFlags, ForcedIndex: ForcedIndex);
        }

        /// <summary>
        /// Removes a J2534 Filter object from the channel set.
        /// </summary>
        /// <param name="FilterIndex">Index of the filter to remove.</param>
        public void StopMessageFilter(int FilterIndex)
        {
            // Remove filter and update.
            _jDevice.ApiMarshall.PassThruStopMsgFilter(ChannelId, JChannelFilters[FilterIndex].FilterId);
            JChannelFilters[FilterIndex] = null;
        }
        /// <summary>
        /// Removes a J2534 Filter object from the channel set.
        /// </summary>
        /// <param name="FilterIndex">Index of the filter to remove.</param>
        public void StopMessageFilter(J2534Filter FilterToRemove)
        {
            // Remove filter and update.
            int FilterIndex = JChannelFilters.ToList().IndexOf(FilterToRemove);
            if (FilterIndex == -1)
            {
                // Try finding via the string values.
                string[] CastFilterStrings = JChannelFilters.Select(FilterObj =>
                {
                    // Check if null.
                    if (FilterObj == null) { return "NULL"; }
                    return FilterObj.ToString();
                }).ToArray();

                // Check if the string of the data is inside here.
                if (!CastFilterStrings.Any(FilterString => FilterString.Contains(FilterToRemove.ToMessageDataString())))
                    throw new PassThruException("Failed to find a matching filter to remove!", J2534Err.ERR_INVALID_FILTER_ID);

                // Store index for filter.
                var MatchedFilter = CastFilterStrings.FirstOrDefault(FilterObj => FilterObj.Contains(FilterToRemove.ToMessageDataString()));
                FilterIndex = CastFilterStrings.ToList().IndexOf(MatchedFilter);
            }

            // Remove the filter and setup values for new filter.
            _jDevice.ApiMarshall.PassThruStopMsgFilter(ChannelId, JChannelFilters[FilterIndex].FilterId);
            JChannelFilters[FilterIndex] = null;
        }

        // ---------------------------------------- INSTANCE CHANNEL PERIODIC METHODS ----------------------------------------

        /// <summary>
        /// Starts a new periodic message for the given value and send time.
        /// </summary>
        /// <param name="MessageToWrite">Message to send.</param>
        /// <param name="SendInterval">Delay between sends.</param>
        /// <param name="ForcedIndex">Index of the filter forced to be set.</param>
        public J2534PeriodicMessage StartPeriodicMessage(PassThruStructs.PassThruMsg MessageToWrite, uint SendInterval, int ForcedIndex = -1)
        {
            // Make sure filter array exists and check for the filters being null or if one exists identical to this desired filter.
            if (ForcedIndex >= 10) { throw new ArgumentOutOfRangeException("Unable to set filter for index over 9!"); }
            if (JChannelPeriodicMessages == null) { JChannelPeriodicMessages = new J2534PeriodicMessage[new PassThruConstants(J2534Version).MaxPeriodicMsgs]; }

            // Check if we need to override/replace a filter.
            if (ForcedIndex != -1 && JChannelPeriodicMessages[ForcedIndex] != null)
            {
                // Remove old filter.
                uint OldFilterId = JChannelPeriodicMessages[ForcedIndex].MessageId;
                _jDevice.ApiMarshall.PassThruStopMsgFilter(ChannelId, OldFilterId);
            }
            else
            {
                // Check if any of the filters are identical so far.
                if (JChannelPeriodicMessages.FirstOrDefault(FilterObj => FilterObj.ToString()
                    .Contains($"{string.Join(" ", MessageToWrite.Data.Select(ByteObj => "0x" + ByteObj.ToString("0:x2")))}")) != null)
                    throw new PassThruException("Can not apply an already existing filter!", J2534Err.ERR_INVALID_FILTER_ID);
            }

            // Build new filter here.
            int NextIndex = ForcedIndex == -1 ? JChannelPeriodicMessages.ToList().IndexOf(null) : ForcedIndex;
            if (NextIndex == -1) throw new PassThruException("Failed to add new filter since there are no open slots!", J2534Err.ERR_INVALID_FILTER_ID);

            // Issue the message here.
            _jDevice.ApiMarshall.PassThruStartPeriodicMsg(ChannelId, MessageToWrite, out var MessageId, SendInterval);
            JChannelPeriodicMessages[NextIndex] = new J2534PeriodicMessage(MessageToWrite, SendInterval, MessageId);
            return JChannelPeriodicMessages[NextIndex];
        }
        /// <summary>
        /// Issues a new PTStart periodic command for the message string given and the time send.
        /// </summary>
        /// <param name="MessageValue"></param>
        /// <param name="MessageFlags"></param>
        /// <param name="SendInterval"></param>
        /// <param name="ForcedIndex"></param>
        public J2534PeriodicMessage StartPeriodicMessage(string MessageValue, uint MessageFlags, uint SendInterval, int ForcedIndex = -1)
        {
            // Build a passthru message then issue it out to our device.
            PassThruStructs.PassThruMsg NewMessage = J2534Device.CreatePTMsgFromString(ProtocolId, MessageFlags, MessageValue);
            return StartPeriodicMessage(NewMessage, SendInterval, ForcedIndex);
        }

        /// <summary>
        /// Stops a Periodic message on the given index.
        /// </summary>
        /// <param name="MessageIndex"></param>
        public void StopPeriodicMessage(int MessageIndex)
        {
            // Remove message and update.
            _jDevice.ApiMarshall.PassThruStopPeriodicMsg(ChannelId, JChannelPeriodicMessages[MessageIndex].MessageId);
            JChannelFilters[MessageIndex] = null;
        }
        /// <summary>
        /// Stops a periodic message based on the message object passed in.
        /// </summary>
        /// <param name="MessageToStop"></param>
        public void StopPeriodicMessage(J2534PeriodicMessage MessageToStop)
        {
            // Remove filter and update.
            int MessageIndex = JChannelPeriodicMessages.ToList().IndexOf(MessageToStop);
            if (MessageIndex == -1)
            {
                // Try finding via the string values.
                string[] CastMessageStrings = JChannelPeriodicMessages
                    .Select(MsgObj => MsgObj == null ? "NULL" : MsgObj.ToString())
                    .ToArray();

                // Check if the string of the data is inside here.
                if (!CastMessageStrings.Any(MsgString => MsgString.Contains(MessageToStop.ToMessageDataString())))
                    throw new PassThruException("Failed to find a matching message to remove!", J2534Err.ERR_INVALID_MSG_ID);

                // Store index for filter.
                var MatchedFilter = CastMessageStrings.FirstOrDefault(MsgString => MsgString.Contains(MessageToStop.ToMessageDataString()));
                MessageIndex = CastMessageStrings.ToList().IndexOf(MatchedFilter);
            }

            // Remove the filter and setup values for new filter.
            _jDevice.ApiMarshall.PassThruStopMsgFilter(ChannelId, JChannelPeriodicMessages[MessageIndex].MessageId);
            JChannelPeriodicMessages[MessageIndex] = null;
        }

        // ---------------------------------------- CHANNEL COMMANDS FOR WRITE-READ -------------------------------------------

        /// <summary>
        /// Runs a standard PTRead
        /// </summary>
        /// <param name="MessagesToRead">Reads this number of messages and stores them</param>
        /// <param name="ReadTimeout">Timeout on the read operations</param>
        /// <returns>Messages read</returns>
        public PassThruStructs.PassThruMsg[] PTReadMessages(ref uint MessagesToRead, uint ReadTimeout)
        {
            // Run the read command here and return them
            _jDevice.ApiMarshall.PassThruReadMsgs(ChannelId, out var ProcessedMessages, ref MessagesToRead, ReadTimeout);
            return ProcessedMessages;
        }
        /// <summary>
        /// Writes messages to the device from this channel
        /// </summary>
        /// <param name="MessagesToWrite">Sends these values</param>
        /// <param name="SendTimeout">Times out after waiting this period</param>
        /// <returns>Number of messages send out.</returns>
        public uint PTWriteMessages(PassThruStructs.PassThruMsg[] MessagesToWrite, uint SendTimeout)
        {
            // Get a ref uint for the number of values being sent.
            uint MessageCount = (uint)MessagesToWrite.Length;

            // Send out the messages here.
            _jDevice.ApiMarshall.PassThruWriteMsgs(ChannelId, MessagesToWrite, ref MessageCount, SendTimeout);
            return MessageCount;
        }
        /// <summary>
        /// Sends a single PTMessage object.
        /// </summary>
        /// <param name="MessageToSend"></param>
        /// <param name="SendTimeout"></param>
        /// <returns>Number of messages actually send out.</returns>
        public uint PTWriteMessages(PassThruStructs.PassThruMsg MessageToSend, uint SendTimeout)
        {
            // Should ALWAYS be one for single message.
            uint MessageCount = 1;

            // Send out the message value here.
            _jDevice.ApiMarshall.PassThruWriteMsgs(ChannelId, MessageToSend, ref MessageCount, SendTimeout);
            return MessageCount;
        }

        // --------------------------------------- CHANNEL CONFIGURATION METHODS FOR SETUP --------------------------------------

        /// <summary>
        /// Reads the voltage of the pin number given and returns it as a uint value natively.
        /// </summary>
        /// <param name="PinNumber">Number of the pin to pull the value from</param>
        /// <returns>The Uint value of the voltage on the given pin number in milivolts</returns>
        public uint ReadPinVoltage(int PinNumber = 16)
        {
            // Build our control struct for pulling out the voltage of our device
            PassThruStructs.ResourceStruct PinStruct = new PassThruStructs.ResourceStruct(1)
            {
                ConnectorType = Connector.ENTIRE_DEVICE,        // Connector Type
                ResourceList = new List<int>() { PinNumber }    // Pin to check 
            };

            // Read the voltage off of our ApiMarshall.
            _jDevice.ApiMarshall.PassThruIoctl(ChannelId, IoctlId.READ_PIN_VOLTAGE, PinStruct, out uint VoltageRead);
            return VoltageRead;
        }
        /// <summary>
        /// Clears out the RX Buffer on the channel
        /// </summary>
        public void ClearRxBuffer() { _jDevice.ApiInstance.PassThruIoctl(ChannelId, IoctlId.CLEAR_RX_BUFFER, IntPtr.Zero, IntPtr.Zero); }
        /// <summary>
        /// Clears out the TX Buffer on the channel
        /// </summary>
        public void ClearTxBuffer() { _jDevice.ApiInstance.PassThruIoctl(ChannelId, IoctlId.CLEAR_TX_BUFFER, IntPtr.Zero, IntPtr.Zero); }
        
        /*  NOTE! PLEASE READ THIS BEFORE THINKING THIS WRAPPER IS MISSING SHIT!        
            TODO: INCLUDE THESE METHODS INTO THIS CHANNEL WRAPPER AT SOME POINT IN THE FUTURE!       
        
            ----------------------------------------------------------------------------------            
        
            The following methods are NOT included in this wrapper yet since I don't need them.
            \__ FiveBaudInit
            \__ FastInit
            \__ SetPins (uint)
            \__ GetConfig (ConfigParamId)
            \__ SetConfig (ConfigParamId, uint)
        */

        public void SetPins(uint PinsToSet) { throw new NotImplementedException("SetPins is not yet built into this wrapper!"); }
        public uint GetConfig(ConfigParamId configParam) { throw new NotImplementedException("GetConfig is not yet built into this wrapper!"); }
        public void SetConfig(ConfigParamId configParam, uint val) { throw new NotImplementedException("SetConfig is not yet built into this wrapper!"); }
        public byte[] FiveBaudInit(byte byteIn) { throw new NotImplementedException("FiveBaudInit is not yet built into this wrapper!"); }
        public byte[] FastInit(byte[] bytesIn, bool responseRequired) { throw new NotImplementedException("FastInit is not yet built into this wrapper!"); }

        // ----------------------------------------- VERSION 0500 CHANNEL SPECIFIC METHOD SET HERE --------------------------------

        /// <summary>
        /// Runs a logical select on the channel
        /// </summary>
        /// <returns>Logical channels on this physical channel</returns>
        public PassThruStructs.SChannelSet? PTSelect()
        {
            // Check if this can be done. Must be 0500 and must be logical
            if (this.J2534Version == JVersion.V0404)
                throw new InvalidOperationException("Can not issue a PTSelect on a version 0404 object!");
            if (!this.IsLogicalChannel)
                throw new InvalidOperationException("Can not issue a PTSelect on a non logical channel!");

            // Build channel set struct
            PassThruStructs.SChannelSet ResultingChannels = new PassThruStructs.SChannelSet();
            ResultingChannels.ChannelThreshold = 0;
            ResultingChannels.ChannelList.Add((int)this.ChannelId);
            ResultingChannels.ChannelCount = (uint)ResultingChannels.ChannelList.Count;

            // Issue command to the API and return the struct output.
            _jDevice.ApiMarshall.PassThruSelect(ref ResultingChannels, 0);
            return ResultingChannels;
        }
    }
}
