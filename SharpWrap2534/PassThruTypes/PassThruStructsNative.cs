using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

[assembly: InternalsVisibleTo("JBoxInvokerTests")]
namespace SharpWrap2534.PassThruTypes
{
    /// <summary>
    /// Native structures for Passthru objects.
    /// These need to be marshalled out into the API For real use.
    /// </summary>
    internal class PassThruStructsNative
    {
        [StructLayout(LayoutKind.Sequential, Size = 4152, CharSet = CharSet.Ansi), Serializable]
        public struct PASSTHRU_MSG
        {
            public uint ProtocolID;
            public uint RxStatus;
            public uint TxFlags;
            public uint Timestamp;
            public uint DataSize;
            public uint ExtraDataIndex;

            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4128)]
            public byte[] Data;

            public PASSTHRU_MSG(int dummy)
            {
                ProtocolID = 0;
                RxStatus = 0;
                TxFlags = 0;
                Timestamp = 0;
                DataSize = 0;
                ExtraDataIndex = 0;
                Data = new byte[4128];
            }
        };
        [StructLayout(LayoutKind.Sequential, Size = 4152, CharSet = CharSet.Ansi), Serializable]
        public struct PASSTHRU_RO_MSG
        {
            public uint ProtocolID;
            public uint RxStatus;
            public uint TxFlags;
            public uint Timestamp;
            public uint DataSize;
            public uint ExtraDataIndex;

            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4128)]
            public byte[] Data;
        };
        [Serializable]
        [StructLayout(LayoutKind.Sequential)]
        public struct RESOURCE_STRUCT
        {
            public uint Connector;
            public uint NumOfResources;
            public IntPtr ptrResourceList;
        };
        [Serializable]
        [StructLayout(LayoutKind.Sequential)]
        public struct MPASSTHRU_RO_MSG
        {
            public uint ProtocolID;
            public uint RxStatus;
            public uint TxFlags;
            public uint Timestamp;
            public uint DataSize;
            public uint ExtraDataIndex;

            public IntPtr Data;

            public void Init(byte[] values)
            {
                Data = Marshal.AllocHGlobal(values.Length);
                Marshal.Copy(values, 0, Data, values.Length);
            }

            public void Destroy()
            {
                Marshal.FreeHGlobal(Data);
                Data = IntPtr.Zero;
            }
        }
        [Serializable]
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
        public struct SCONFIG
        {
            public uint Parameter;
            public uint Value;
        };
        [Serializable]
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
        public struct SCONFIG_LIST
        {
            public uint NumOfParams;
            public IntPtr ConfigPtr;
        };
        [Serializable]
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
        public struct SBYTE_ARRAY
        {
            public uint NumOfBytes;
            public IntPtr BytePtr;
        };
        [Serializable]
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
        public struct SPARAM_LIST
        {
            public uint NumOfParameters;
            public IntPtr SParamPtr;
        };
        [Serializable]
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
        public struct SPARAM
        {
            public uint Parameter;
            public uint Value;
            public uint Supported;
        };
        [Serializable]
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
        public struct INT32_WRAPPER
        {
            public uint Number;
        };
        [Serializable]
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
        public struct INT32_ARRAY100_WRAPPER
        {
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 100)]
            public uint[] Data;
        };
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi), Serializable]
        public struct SDEVICE
        {
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 80)]
            public string DeviceName;
            public UInt32 DeviceAvailable;
            public UInt32 DeviceDLLFWStatus;
            public UInt32 DeviceConnectMedia;
            public UInt32 DeviceConnectSpeed;
            public UInt32 DeviceSignalQuality;
            public UInt32 DeviceSignalStrength;
        };
    }
}
