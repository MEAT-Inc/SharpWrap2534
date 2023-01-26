using System;
using System.Collections.Generic;
using System.Linq;
using SharpLogger.LoggerObjects;
using SharpLogger.LoggerSupport;
using SharpSimulator.SimulationObjects;
using SharpWrapper.J2534Objects;
using SharpWrapper.PassThruTypes;

namespace SharpSimulator
{
    /// <summary>
    /// Contains logic for loading in new J2534 Simulations
    /// </summary>
    public class SimulationLoader
    {
        // Logger object
        private readonly SubServiceLogger _simLoaderLogger;

        // All Simulation Channles
        public SimulationChannel[] SimulationChannels { get; private set; }

        // Properties of all channels for the simulation
        public BaudRate[] BaudRates => this.SimulationChannels.Select(SimChannel => SimChannel.ChannelBaudRate).ToArray();
        public PassThroughConnect[] ChannelFlags => this.SimulationChannels.Select(SimChannel => SimChannel.ChannelConnectFlags).ToArray();
        public ProtocolId[] ChannelProtocols => this.SimulationChannels.Select(SimChannel => SimChannel.ChannelProtocol).ToArray();
        public J2534Filter[][] ChannelFilters => this.SimulationChannels.Select(SimChannel => SimChannel.MessageFilters).ToArray();

        // Message objects for configuring output values
        public SimulationMessagePair[][] PairedSimulationMessages => this.SimulationChannels.Select(SimChannel => SimChannel.MessagePairs).ToArray();
        public PassThruStructs.PassThruMsg[] MessagesToRead => (PassThruStructs.PassThruMsg[])PairedSimulationMessages.SelectMany(MsgSet => MsgSet.Select(MsgPair => MsgPair.MessageRead).ToArray());
        public PassThruStructs.PassThruMsg[][] MessagesToWrite => (PassThruStructs.PassThruMsg[][])PairedSimulationMessages.SelectMany(MsgSet => MsgSet.Select(MsgPair => MsgPair.MessageResponses).ToArray());

        // ------------------------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Builds a new Simulation Channel Loader
        /// </summary>
        public SimulationLoader()
        {
            // Setup all our default values for our class lists here.
            this.SimulationChannels = Array.Empty<SimulationChannel>();

            // Build a logger object for this loader
            this._simLoaderLogger = new SubServiceLogger($"SimLoadingLogger");
            this._simLoaderLogger.WriteLog($"BUILT A NEW SIM LOADING LOGGER WITH GUID VALUE: {this._simLoaderLogger.LoggerGuid}");
        }
        

        /// <summary>
        /// Appends a new simulation channel into our loader using an input channel object
        /// </summary>
        /// <param name="ChannelToAdd">Channel to store on our loader</param>
        /// <returns>The index of the channel added</returns>
        public int AddSimChannel(SimulationChannel ChannelToAdd)
        {
            // Store all values of our channel here
            this.SimulationChannels = this.SimulationChannels
                .Append(ChannelToAdd)
                .ToArray();

            // Find new index and return it. Check the min index of the filters and the channels then the messages.
            this._simLoaderLogger.WriteLog($"ADDED NEW VALUES FOR A SIMULATION CHANNEL {ChannelToAdd.ChannelId} WITHOUT ISSUES!", LogType.InfoLog);
            return PairedSimulationMessages.Length - 1;
        }
        /// <summary>
        /// Adds in a new simulation channel object based on the given input values.
        /// </summary>
        /// <param name="Protocol">Protocol of Channel</param>
        /// <param name="Filters">Filters of the channel</param>
        /// <param name="PairedSimulationMessages">Messages To Read and Respond to</param>
        /// <returns>Index of the newest built channel</returns>
        public int AddSimChannel(uint ChannelId, ProtocolId Protocol, PassThroughConnect Flags, BaudRate BaudRate, J2534Filter[] Filters, SimulationMessagePair[] PairedSimulationMessages)
        {
            // Build a temporary simulation channel
            SimulationChannel TempChannel = new SimulationChannel(ChannelId, Protocol, Flags, BaudRate);
            TempChannel.MessagePairs = PairedSimulationMessages.ToArray();

            // Add this channel to our list of all channel objects
            this.SimulationChannels = this.SimulationChannels
                .Append(TempChannel)
                .ToArray();

            // Find new index and return it. Check the min index of the filters and the channels then the messages.
            this._simLoaderLogger.WriteLog($"ADDED NEW VALUES FOR A SIMULATION CHANNEL {TempChannel.ChannelId} WITHOUT ISSUES!", LogType.InfoLog);
            return PairedSimulationMessages.Length - 1;
        }

        /// <summary>
        /// Removes a simulation channel from the list of all channel objects
        /// </summary>
        /// <param name="ChannelToRemove">Channel to pull out of our list of input channels</param>
        /// <returns>True if removed. False if not</returns>
        public bool RemoveSimChannel(SimulationChannel ChannelToRemove)
        {
            // Find the channel to remove and pull it out.
            this._simLoaderLogger.WriteLog($"TRYING TO REMOVE CHANNEL WITH ID {ChannelToRemove.ChannelId}...");
            this.SimulationChannels = this.SimulationChannels
                .Where(SimChannel => SimChannel.ChannelId != ChannelToRemove.ChannelId)
                .ToArray();

            // Check if it exists or not.
            this._simLoaderLogger.WriteLog($"{(this.SimulationChannels.Contains(ChannelToRemove) ? "FAILED TO REMOVE CHANNEL OBJECT!" : "CHANNEL REMOVED OK!")}");
            return !this.SimulationChannels.Contains(ChannelToRemove);
        }
        /// <summary>
        /// Removes a channel by the ID value passed in
        /// </summary>
        /// <param name="ChannelId">ID of the channel to remove</param>
        /// <returns>True if removed. False if not.</returns>
        public bool RemoveSimChannel(int ChannelId)
        {
            // Find the channel to remove and pull it out.
            this._simLoaderLogger.WriteLog($"TRYING TO REMOVE CHANNEL WITH ID {ChannelId}...");
            this.SimulationChannels = this.SimulationChannels
                .Where(SimChannel => SimChannel.ChannelId != ChannelId)
                .ToArray();

            // Check if it exists or not.
            this._simLoaderLogger.WriteLog($"{(this.SimulationChannels.Any(SimChannel => SimChannel.ChannelId == ChannelId) ? "FAILED TO REMOVE CHANNEL OBJECT!" : "CHANNEL REMOVED OK!")}");
            return this.SimulationChannels.All(SimChannel => SimChannel.ChannelId != ChannelId);
        }
    }
}
