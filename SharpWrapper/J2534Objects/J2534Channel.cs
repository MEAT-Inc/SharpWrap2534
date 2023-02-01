using System;
using System.Linq;
using SharpWrapper.PassThruTypes;

namespace SharpWrapper.J2534Objects
{
    /// <summary>
    /// J2534 Channel object. Used to control device channels.
    /// THIS IS A SINGLETON CONFIGURED TYPE BASED ON THE DLL VERSION! 
    /// </summary>
    public sealed class J2534Channel
    {
        // -------------------------- SINGLETON CONFIGURATION ----------------------------

        // TODO: REFACTOR OUT THIS SINGLETON CONTENT!
        // Singleton schema for this class object. Max channels type can exist.
        // private static J2534Channel[][] _j2534Channels;

        /// <summary>
        /// Private Singleton instance builder.
        /// THESE CHANNELS BUILT ARE NOT TRACKED BY THE SINGLETON INSTANCE!
        /// </summary>
        /// <param name="JDevice">Device to use</param>
        /// <param name="PhysicalParent">Physical J2534 device parent object</param>
        private J2534Channel(J2534Device JDevice, J2534Channel PhysicalParent = null)
        {
            // Setup device channel properties.
            _jDevice = JDevice;
            J2534Version = _jDevice.J2534Version;
            ChannelStatus = SharpSessionStatus.INITIALIZED;

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
                // Find next open channel object
                ChannelIndex =
                    this._jDevice.DeviceChannels.All(ChannelObj => ChannelObj == null) ? 0 :
                    this._jDevice.DeviceChannels
                        .ToList()
                        .FindIndex(ChannelObj => ChannelObj?.ChannelStatus is SharpSessionStatus.INITIALIZED or SharpSessionStatus.FREED);
                
                // Check index value
                if (ChannelIndex == -1) 
                    throw new InvalidOperationException($"CAN NOT MAKE A NEW CHANNEL ON DEVICE {_jDevice.DeviceName} SINCE NO OPEN CHANNELS WERE FOUND!");

                // Set channel object now and return out
                this._jDevice.DeviceChannels[ChannelIndex] = this;
            }
        }


        /// <summary>
        /// Builds a channel Array to use for connecting into.
        /// </summary>
        /// <param name="JDevice">Device to build channels for.</param>
        /// <returns>Array of built J2534 channels.</returns>
        internal static J2534Channel[] BuildDeviceChannels(J2534Device JDevice)
        {
            // Append channels into the device here.
            JDevice.DeviceChannels = new J2534Channel[new PassThruConstants(JDevice.J2534Version).MaxChannels];
            for (int ChannelIndex = 0; ChannelIndex < JDevice.DeviceChannels.Length; ChannelIndex += 1)
                JDevice.DeviceChannels[ChannelIndex] = new J2534Channel(JDevice);

            // Return built channels and store them onto the device object
            return JDevice.DeviceChannels;
        }
        /// <summary>
        /// Deconstructs the device object and members
        /// </summary>
        /// <returns>True if closed ok. False if not.</returns>
        internal static bool DestroyDeviceChannels(J2534Device JDevice, int ChannelIndex = -1)
        {
            // Null out member values for this channel
            if (ChannelIndex == -1)
            {
                // Close only the desired channel
                var TypeConstants = new PassThruConstants(JDevice.J2534Version);
                try { JDevice.DeviceChannels = new J2534Channel[TypeConstants.MaxChannels]; }
                catch { return false; }

                // Return out passed
                return true;
            }

            // On Device closed routine, close out the whole device instance set
            try { JDevice.DeviceChannels[ChannelIndex] = null; }
            catch { return false; }

            // Return out passed
            return true;
        }

        // -------------------------------- INSTANCE VALUES AND SETUP FOR CHANNEL HERE -----------------------------------

        // Status values.
        public int ChannelIndex { get; }
        public JVersion J2534Version { get; }
        public SharpSessionStatus ChannelStatus { get; }

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
            this.ProtocolId = ChannelProtocol;
            this.ConnectFlags = ChannelFlags;
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
            var ChannelToDisconnect = this._jDevice.DeviceChannels.FirstOrDefault(ChannelObj => ChannelObj.ChannelId == ChannelId);
            if (ChannelToDisconnect == null) { throw new InvalidOperationException("Failed to disconnect channel value!"); }

            // Disconnect and reinit here.
            int IndexOfChannel = this._jDevice.DeviceChannels.ToList().IndexOf(ChannelToDisconnect);
            this._jDevice.DeviceChannels[IndexOfChannel] = new J2534Channel(this._jDevice);

            // Return passed.
            return true;
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
            // Find our channel objects and then search them
            var ChannelObjects = this._jDevice.DeviceChannels;
            FilterFound = ChannelObjects
                .SelectMany(ChObj => ChObj.JChannelFilters)
                .FirstOrDefault(FilterObj => FilterObj.FilterId == FilterId);

            // Check if the filter is null or not.
            return FilterFound != null;
        }
        /// <summary>
        /// Gets all of our filters and pulls one that matches.
        /// </summary>
        /// <param name="MessageId">Find this message.</param>
        /// <param name="MessageFound">Message located.</param>
        /// <returns>The filter matched and true, or false and nothing.</returns>
        public bool LocatePeriodicMessage(uint MessageId, out J2534PeriodicMessage MessageFound)
        {
            // Find our channel objects and then search them
            var ChannelObjects = this._jDevice.DeviceChannels;
            MessageFound = ChannelObjects
                .SelectMany(ChObj => ChObj.JChannelPeriodicMessages)
                .FirstOrDefault(MessageObj => MessageObj.MessageId == MessageId);

            // Check if the filter is null or not.
            return MessageFound != null;
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
        public J2534Filter StartMessageFilter(ProtocolId FilterProtocol, FilterDef FilterType, string MaskString, string PatternString, string FlowControl, TxFlags FilterFlags = 0, int ForcedIndex = -1)
        {
            // Make sure filter array exists and check for the filters being null or if one exists identical to this desired filter.
            if (ForcedIndex >= 10) { throw new ArgumentOutOfRangeException("UNABLE TO SET A FILTER INDEX OVER 9!"); }
            if (JChannelFilters == null) { JChannelFilters = new J2534Filter[new PassThruConstants(J2534Version).MaxFilters]; }

            // Build messages from filter strings.
            PassThruStructs.PassThruMsg PtMaskMsg = J2534Device.CreatePTMsgFromString(FilterProtocol, (uint)FilterFlags, MaskString);
            PassThruStructs.PassThruMsg PtPatternMsg = J2534Device.CreatePTMsgFromString(FilterProtocol, (uint)FilterFlags, PatternString);
            PassThruStructs.PassThruMsg PtFlowCtlMsg = J2534Device.CreatePTMsgFromString(FilterProtocol, (uint)FilterFlags, FlowControl);

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
                if (JChannelFilters.FirstOrDefault(FilterObj => FilterObj != null && FilterObj.ToString()
                    .Contains($"MessageData: {MaskString ?? "NO_MASK"},{PatternString ?? "NO_PATTERN"},{FlowControl ?? "NO_FLOW"}")) != null)
                    throw new PassThruException("Can not apply an already existing filter!", J2534Err.ERR_INVALID_FILTER_ID);
            }

            // Build new filter here.
            int NextIndex = ForcedIndex == -1 ? JChannelFilters.ToList().IndexOf(null) : ForcedIndex;
            if (NextIndex == -1) throw new PassThruException("Failed to add new filter since there are no open slots!", J2534Err.ERR_INVALID_FILTER_ID);

            // Issue the new filter here and store onto filter list.
            _jDevice.ApiMarshall.PassThruStartMsgFilter(ChannelId, FilterType, PtMaskMsg, PtPatternMsg, PtFlowCtlMsg, out uint FilterId);
            if (FlowControl == null || FilterType != FilterDef.FLOW_CONTROL_FILTER)
                JChannelFilters[NextIndex] = new J2534Filter(FilterProtocol, FilterType, MaskString, PatternString, FilterFlags, FilterId);
            else JChannelFilters[NextIndex] = new J2534Filter(FilterProtocol, FilterType, MaskString, PatternString, FlowControl, FilterFlags, FilterId);

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
            return StartMessageFilter(
                FilterToSet.FilterProtocol,
                FilterToSet.FilterType,
                FilterToSet.FilterMask,
                FilterToSet.FilterPattern,
                FilterToSet.FilterFlowCtl, 
                FilterToSet.FilterFlags,
                ForcedIndex: ForcedIndex
            );
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
        /// Clears out the RX Buffer on the channel
        /// </summary>
        public void ClearRxBuffer() { _jDevice.ApiInstance.PassThruIoctl(ChannelId, IoctlId.CLEAR_RX_BUFFER, IntPtr.Zero, IntPtr.Zero); }
        /// <summary>
        /// Clears out the TX Buffer on the channel
        /// </summary>
        public void ClearTxBuffer() { _jDevice.ApiInstance.PassThruIoctl(ChannelId, IoctlId.CLEAR_TX_BUFFER, IntPtr.Zero, IntPtr.Zero); }
        /// <summary>
        /// Issues an IOCTL for setting pins on the current channel object 
        /// </summary>
        /// <param name="PinsToSet">Pin to set</param>
        public void SetPins(uint PinsToSet)
        {
            // Build a config list, then new param object
            PassThruStructs.SConfigList SetConfigList = new PassThruStructs.SConfigList();
            PassThruStructs.SConfig ParamOne = new PassThruStructs.SConfig(ConfigParamId.J1962_PINS);

            // Store values onto the newly built config object
            ParamOne.SConfigValue = PinsToSet;
            SetConfigList.ConfigList.Add(ParamOne);
            SetConfigList.NumberOfParams = 1;

            // Marshall out the command to our IOCTL
            this._jDevice.ApiMarshall.PassThruIoctl(this.ChannelId, IoctlId.SET_CONFIG, ref SetConfigList);
        }
        /// <summary>
        /// Reads our current configuration for a given SConfig value
        /// </summary>
        /// <param name="ConfigParam">Config value to locate</param>
        /// <returns>Uint built output value</returns>
        public uint GetConfig(ConfigParamId ConfigParam)
        {
            // Build structures for config pulling
            PassThruStructs.SConfigList GetConfigList = new PassThruStructs.SConfigList(1);
            PassThruStructs.SConfig SConfigToPull = new PassThruStructs.SConfig(ConfigParam);

            // Add values into our list of configurations and marshall it out
            GetConfigList.ConfigList.Add(SConfigToPull);
            this._jDevice.ApiMarshall.PassThruIoctl(this.ChannelId, IoctlId.GET_CONFIG, ref GetConfigList);

            // Return the located value output
            return GetConfigList.ConfigList[0].SConfigValue;
        }
        /// <summary>
        /// Issues a SetConfig command which is used to allow us to configure 
        /// </summary>
        /// <param name="ConfigParam">Config value ot set</param>
        /// <param name="Value">Value of the config param</param>
        public void SetConfig(ConfigParamId ConfigParam, uint Value)
        {
            // Build values for the structs to use for setting configuration
            PassThruStructs.SConfigList SetConfigList = new PassThruStructs.SConfigList(1);
            PassThruStructs.SConfig SetConfig = new PassThruStructs.SConfig(ConfigParam) { SConfigValue = Value };
            
            // Add to the configuration list and marshall out the values.
            SetConfigList.ConfigList.Add(SetConfig);
            this._jDevice.ApiMarshall.PassThruIoctl(this.ChannelId, IoctlId.SET_CONFIG, ref SetConfigList);
        }
        /// <summary>
        /// Issues a FiveBaudInit routine for some ISO protocols.
        /// </summary>
        /// <param name="ByteIn">Byte read in</param>
        /// <returns>The Byte array for the syncup needed</returns>
        public byte[] FiveBaudInit(byte ByteIn)
        {
            // Built input and output SByte Arrays
            PassThruStructs.SByteArray SByteArrayIn = new PassThruStructs.SByteArray(1) { Data = { [0] = ByteIn } };
            PassThruStructs.SByteArray SByteArrayOut = new PassThruStructs.SByteArray(64);

            // Marshall out the command and store the output value
            this._jDevice.ApiMarshall.PassThruIoctl(this.ChannelId, IoctlId.FIVE_BAUD_INIT, SByteArrayIn, ref SByteArrayOut);
            byte[] OutputBytes = J2534Device.CreateByteArrayFromSByteArray(SByteArrayOut);

            // Return the built output values
            return OutputBytes; 
        }
        /// <summary>
        /// Issues a fast init used for some ISO commands
        /// </summary>
        /// <param name="BytesIn">Input byte for sync</param>
        /// <param name="ResponseRequired">Indicates if we need to keep the response or not.</param>
        /// <returns>Response from this command if one is given and asked for</returns>
        public byte[] FastInit(byte[] BytesIn, bool ResponseRequired)
        {
            // Build a message to hook into for input and output
            PassThruStructs.PassThruMsg InputMessage = default;
            PassThruStructs.PassThruMsg OutputMessage = default;
            
            // Hook and populate values.
            if (BytesIn != null) InputMessage = J2534Device.CreatePTMsgFromDataBytes(this.ProtocolId, 0, BytesIn);
            if (ResponseRequired) OutputMessage = new PassThruStructs.PassThruMsg(64);

            // Marshall out the content values and then return if required
            this._jDevice.ApiMarshall.PassThruIoctl(this.ChannelId, IoctlId.FAST_INIT, InputMessage, ref OutputMessage);
            return ResponseRequired ? OutputMessage.Data : null;
        }

        // ----------------------------------------- VERSION 0500 CHANNEL SPECIFIC METHOD SET HERE --------------------------------
        
        /// <summary>
        /// Issues a new PTLogical connect routine on this channel
        /// </summary>
        /// <param name="Protocol">Protocol to connect with</param>
        /// <param name="Flags">Flags to connect using</param>
        /// <param name="ChannelDescriptor">Connection configuration object</param>
        /// <returns>The logical channel built when this method executes</returns>
        public J2534Channel PTLogicalConnect(ProtocolId Protocol, uint Flags, PassThruStructs.ISO15765ChannelDescriptor ChannelDescriptor)
        {
            // Check if this can be done. Must be 0500 and must be physical
            if (this.J2534Version == JVersion.V0404)
                throw new InvalidOperationException("Can not issue a PTLogicalConnect on a version 0404 object!");
            if (this.IsLogicalChannel)
                throw new InvalidOperationException("Can not issue a PTLogicalConnect on a non physical channel!");

            // Issue the connection command and build a new Logical channel.
            this._jDevice.ApiMarshall.PassThruLogicalConnect(this.ChannelId, Protocol, Flags, ChannelDescriptor, out uint LogicalId);
            J2534Channel LogicalChannelBuilt = new J2534Channel(this._jDevice, this);

            // Return the built logical channel object
            return LogicalChannelBuilt;
        }
        /// <summary>
        /// Issues a PTLogicalDisconnect routine on this channel instance
        /// </summary>
        /// <param name="ChannelId">ID Of the channel to disconnect</param>
        public void PTLogicalDisconnect(uint ChannelId)
        {
            // Check if this can be done. Must be 0500 and must be physical
            if (this.J2534Version == JVersion.V0404)
                throw new InvalidOperationException("Can not issue a PTLogicalConnect on a version 0404 object!");
            if (this.IsLogicalChannel)
                throw new InvalidOperationException("Can not issue a PTLogicalConnect on a non physical channel!");

            // Issue the connection command and build a new Logical channel.
            this._jDevice.ApiMarshall.PassThruLogicalDisconnect(ChannelId);
        }

        /// <summary>
        /// Runs a logical select on the channel
        /// </summary>
        /// <returns>Logical channels on this physical channel</returns>
        public PassThruStructs.SChannelSet? PTSelect()
        {
            // Check if this can be done. Must be 0500 and must be logical
            if (this.J2534Version == JVersion.V0404)
                throw new InvalidOperationException("Can not issue a PTSelect on a version 0404 object!");
            if (this.IsLogicalChannel)
                throw new InvalidOperationException("Can not issue a PTSelect on a logical channel!");

            // Build channel set struct
            PassThruStructs.SChannelSet ResultingChannels = new PassThruStructs.SChannelSet();
            ResultingChannels.ChannelThreshold = 0;
            ResultingChannels.ChannelList.Add((int)this.ChannelId);
            ResultingChannels.ChannelCount = (uint)ResultingChannels.ChannelList.Count;

            // Issue command to the API and return the struct output.
            _jDevice.ApiMarshall.PassThruSelect(ref ResultingChannels, 0);
            return ResultingChannels;
        }

        /// <summary>
        /// Queues messages onto a given logical channel
        /// </summary>
        /// <param name="MessageToQueue">Messages to queue on the send operation channel</param>
        /// <param name="MessageCount">Number of messages to be queued</param>
        public void PTQueueMessages(PassThruStructs.PassThruMsg MessageToQueue, ref uint MessageCount)
        {
            // Check if this can be done. Must be 0500 and must be physical
            if (this.J2534Version == JVersion.V0404)
                throw new InvalidOperationException("Can not issue a PTLogicalConnect on a version 0404 object!");
            if (!this.IsLogicalChannel)
                throw new InvalidOperationException("Can not issue a PTLogicalConnect on a non physical channel!");

            // Issue the command with our device API Marshall
            _jDevice.ApiMarshall.PassThruQueueMsgs(this.ChannelId, MessageToQueue, ref MessageCount);
        }
        /// <summary>
        /// Queues messages onto a given logical channel
        /// </summary>
        /// <param name="MessagesToQueue">Messages to queue on the send operation channel</param>
        /// <param name="MessageCount">Number of messages to be queued</param>
        public void PTQueueMessages(PassThruStructs.PassThruMsg[] MessagesToQueue, ref uint MessageCount)
        {
            // Check if this can be done. Must be 0500 and must be physical
            if (this.J2534Version == JVersion.V0404)
                throw new InvalidOperationException("Can not issue a PTLogicalConnect on a version 0404 object!");
            if (!this.IsLogicalChannel)
                throw new InvalidOperationException("Can not issue a PTLogicalConnect on a non physical channel!");

            // Issue the command with our device API Marshall
            _jDevice.ApiMarshall.PassThruQueueMsgs(this.ChannelId, MessagesToQueue, ref MessageCount);
        }
    }
}
