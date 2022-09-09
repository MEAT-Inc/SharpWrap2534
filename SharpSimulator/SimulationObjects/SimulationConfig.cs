using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SharpWrapper.J2534Objects;
using SharpWrapper.PassThruTypes;

namespace SharpSimulator.SimulationObjects
{
    /// <summary>
    /// Default simulation configuration layout
    /// </summary>
    public class SimulationConfig
    {
        // Reader default configurations
        public uint ReaderTimeout;
        public uint ReaderMsgCount;
        public uint ResponseTimeout;

        // Basic Channel Configurations
        public BaudRate ReaderBaudRate;
        public ProtocolId ReaderProtocol;
        public PassThroughConnect ReaderChannelFlags;

        // Reader configuration filters and IOCTLs
        public J2534Filter[] ReaderFilters;
        public PassThruStructs.SConfigList ReaderConfigs;

        // ------------------------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Builds a new configuration object and sets defaults to null/empty
        /// </summary>
        public SimulationConfig(ProtocolId ProtocolInUse, BaudRate BaudRate)
        {
            // Store protocol and BaudRate
            this.ReaderBaudRate = BaudRate;
            this.ReaderProtocol = ProtocolInUse;

            // Store basic values here
            this.ReaderMsgCount = 1;
            this.ReaderTimeout = 100;
            this.ResponseTimeout = 500;
            this.ReaderChannelFlags = 0x00;

            // Setup basic empty array for filters with a max count of 10
            this.ReaderFilters = new J2534Filter[10];
            this.ReaderConfigs = new PassThruStructs.SConfigList(0);
        }
    }
}
