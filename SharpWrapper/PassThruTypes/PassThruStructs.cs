using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.Remoting.Metadata.W3cXsd2001;

namespace SharpWrap2534.PassThruTypes
{
    /// <summary>
    /// Set of structure objects without signed value sets.
    /// </summary>
    public class PassThruStructs
    {
        /// <summary>
        /// PassThur message struct
        /// </summary>
        public struct PassThruMsg
        {
            public ProtocolId ProtocolID;
            public uint RxStatus;
            public uint TxFlags;
            public uint Timestamp;
            public uint DataSize;
            public uint ExtraDataIndex;
            public byte[] Data;

            /// <summary>
            /// Builds a new PassThru message from managed types.
            /// </summary>
            /// <param name="ByteCount"></param>
            public PassThruMsg(uint ByteCount)
            {
                ProtocolID = 0;
                RxStatus = 0;
                TxFlags = 0;
                Timestamp = 0;
                DataSize = 0;
                ExtraDataIndex = 0;
                Data = new byte[ByteCount];
            }
        }
        /// <summary>
        /// SConfig list setup
        /// </summary>
        public struct SConfigList
        {
            public uint NumberOfParams;
            public List<SConfig> ConfigList;

            /// <summary>
            /// Builds a new managed SConfig List
            /// </summary>
            public SConfigList(uint ParamCount = 0)
            {
                ConfigList = new List<SConfig>((int)ParamCount);
                NumberOfParams = (uint)ConfigList.Count;
            }
        }
        /// <summary>
        /// Single SConfig instance.
        /// </summary>
        public struct SConfig
        {
            public uint SConfigValue;
            public ConfigParamId SConfigParamId;

            /// <summary>
            /// Builds a new Sconfig Param
            /// </summary>
            /// <param name="ConfigParam"></param>
            public SConfig(ConfigParamId ConfigParam)
            {
                SConfigParamId = ConfigParam;
                SConfigValue = (uint)ConfigParam;
            }
        }
        /// <summary>
        /// SByte array. Used for sending sets of SConfigs.
        /// </summary>
        public struct SByteArray
        {
            public uint NumberOfBytes;
            public byte[] Data;

            /// <summary>
            /// Builds a new SByte Array
            /// </summary>
            /// <param name="ByteCount"></param>
            public SByteArray(uint ByteCount)
            {
                NumberOfBytes = ByteCount;
                Data = new byte[ByteCount];
            }
        }
        /// <summary>
        /// SDevice instances show all the values of a device when a PTOpen command is run.
        /// </summary>
        public struct SDevice
        {
            public string DeviceName;
            public uint DeviceAvailable;
            public uint DeviceDllFWStatus;
            public uint DeviceConnectMedia;
            public uint DeviceConnectSpeed;
            public uint DeviceSignalQuality;
            public uint DeviceSignalStrength;

            // to control what shows up in the combobox
            public override string ToString() { return DeviceName; }
        };
        /// <summary>
        /// Resource structs show used controls on a connector instance type for a device.
        /// </summary>
        public struct ResourceStruct
        {
            public Connector ConnectorType;
            public uint ResourceCount;
            public List<int> ResourceList;

            /// <summary>
            /// Builds a new resource struct object
            /// </summary>
            /// <param name="NumResources"></param>
            public ResourceStruct(int NumResources = 0)
            {
                ResourceCount = (uint)NumResources;
                ResourceList = new List<int>(NumResources);
                ConnectorType = default;
            }
        }

        // ---------------------------------------------- VERSION 0500 USE CASES ONLY! ----------------------------------------

        /// <summary>
        /// ISO15765 Channel Descriptor for Logical commands
        /// </summary>
        public struct ISO15765ChannelDescriptor
        {
            public uint LocalTxFlags;
            public uint RemoteTxFlags;
            public byte[] LocalAddress;
            public byte[] RemoteAddress;

            /// <summary>
            /// Builds a new Channel descriptor
            /// </summary>
            public ISO15765ChannelDescriptor(byte[] LocalAddress, byte[] RemoteAddress, uint LocalFlags, uint RemoteFlags)
            {
                // Store the values here.
                this.LocalAddress = LocalAddress;
                this.RemoteAddress = RemoteAddress;
                this.LocalTxFlags = LocalFlags;
                this.RemoteTxFlags = RemoteFlags;
            }
        }
        /// <summary>
        /// SChannel set object for logical operations
        /// </summary>
        public struct SChannelSet
        {
            public uint ChannelCount;
            public uint ChannelThreshold;
            public List<int> ChannelList;

            /// <summary>
            /// Builds a new Channel set object
            /// </summary>
            /// <param name="ChannelCount"></param>
            public SChannelSet(uint ChannelCount, uint ChannelThreshold)
            {
                // Store the values here.
                this.ChannelList = new List<int>();
                this.ChannelCount = ChannelCount;
                this.ChannelThreshold = ChannelThreshold;
            }
        }
    }
}
