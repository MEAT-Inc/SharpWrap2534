using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using JBoxInvoker.PassThruLogic.SupportingLogic;

namespace JBoxInvoker.PassThruLogic.PassThruTypes
{
    // Set of structure objects without signed value sets.

    /// <summary>
    /// PassThur message struct
    /// </summary>
    public class PassThruMsg
    {
        public ProtocolId protocolId;
        public uint rxStatus;
        public uint txFlags;
        public uint timestamp;
        public uint dataLength;
        public uint extraDataIndex;
        public byte[] data;

        public PassThruMsg(uint nbytes) { data = new byte[nbytes]; }
    }
    /// <summary>
    /// SConfig list setup
    /// </summary>
    public class SConfigList
    {
        public uint numOfParams;
        public List<SConfig> configList;

        public SConfigList() { configList = new List<SConfig>(); }
    }
    /// <summary>
    /// Single SConfig instance.
    /// </summary>
    public class SConfig
    {
        public ConfigParamId parameter;
        public uint value;

        public SConfig(ConfigParamId param) { parameter = param; }
    }
    /// <summary>
    /// SByte array. Used for sending sets of SConfigs.
    /// </summary>
    public class SByteArray
    {
        public uint numOfBytes;
        public byte[] data;

        public SByteArray(uint numBytes)
        {
            numOfBytes = numBytes;
            data = new byte[numBytes];
        }
    }
    /// <summary>
    /// SDevice instances show all the values of a device when a PTOpen command is run.
    /// </summary>
    public class SDevice
    {
        public string deviceName;
        public uint deviceAvailable;
        public uint deviceDLLFWStatus;
        public uint deviceConnectMedia;
        public uint deviceConnectSpeed;
        public uint deviceSignalQuality;
        public uint deviceSignalStrength;

        // to control what shows up in the combobox
        public override string ToString() { return deviceName; }
    };
    /// <summary>
    /// Resource structs show used controls on a connector instance type for a device.
    /// </summary>
    public class ResourceStruct
    {
        public Connector connector;
        public uint numOfResources;
        public List<int> resourceList = new List<int>();
    }
}
