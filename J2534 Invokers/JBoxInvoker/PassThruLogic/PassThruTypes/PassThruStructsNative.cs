using System;
using System.Runtime.InteropServices;

namespace JBoxInvoker.PassThruLogic.PassThruImport
{
    /// <summary>
    /// Native structures for Passthru objects.
    /// These need to be marshalled out into the API For real use.
    /// </summary>
    public class PassThruStructs_Native
    {
        [StructLayout(LayoutKind.Sequential, Size = 4152, CharSet = CharSet.Ansi), Serializable]
        public struct PASSTHRU_MSG
        {
            public UInt32 ProtocolID;
            public UInt32 RxStatus;
            public UInt32 TxFlags;
            public UInt32 Timestamp;
            public UInt32 DataSize;
            public UInt32 ExtraDataIndex;

            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4128)]
            public Byte[] Data;

            public PASSTHRU_MSG(int dummy)
            {
                ProtocolID = 0;
                RxStatus = 0;
                TxFlags = 0;
                Timestamp = 0;
                DataSize = 0;
                ExtraDataIndex = 0;
                Data = new Byte[4128];
            }
        };
        [StructLayout(LayoutKind.Sequential, Size = 4152, CharSet = CharSet.Ansi), Serializable]
        public struct PASSTHRU_RO_MSG
        {
            public UInt32 ProtocolID;
            public UInt32 RxStatus;
            public UInt32 TxFlags;
            public UInt32 Timestamp;
            public UInt32 DataSize;
            public UInt32 ExtraDataIndex;

            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4128)]
            public Byte[] Data;
        };
        [Serializable]
        [StructLayout(LayoutKind.Sequential)]
        public struct RESOURCE_STRUCT
        {
            public UInt32 Connector;
            public UInt32 NumOfResources;
            public IntPtr ptrResourceList;
        };
        [Serializable]
        [StructLayout(LayoutKind.Sequential)]
        public struct MPASSTHRU_RO_MSG
        {
            public UInt32 ProtocolID;
            public UInt32 RxStatus;
            public UInt32 TxFlags;
            public UInt32 Timestamp;
            public UInt32 DataSize;
            public UInt32 ExtraDataIndex;

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
            public UInt32 Parameter;
            public UInt32 Value;
        };
        [Serializable]
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
        public struct SCONFIG_LIST
        {
            public UInt32 NumOfParams;
            public IntPtr ConfigPtr;
        };
        [Serializable]
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
        public struct SBYTE_ARRAY
        {
            public UInt32 NumOfBytes;
            public IntPtr BytePtr;
        };
        [Serializable]
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
        public struct SPARAM_LIST
        {
            public UInt32 NumOfParameters;
            public IntPtr SParamPtr;
        };
        [Serializable]
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
        public struct SPARAM
        {
            public UInt32 Parameter;
            public UInt32 Value;
            public UInt32 Supported;
        };
        [Serializable]
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
        public struct INT32_WRAPPER
        {
            public UInt32 Number;
        };
        [Serializable]
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
        public struct INT32_ARRAY100_WRAPPER
        {
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 100)]
            public UInt32[] Data;
        };
    }
}
