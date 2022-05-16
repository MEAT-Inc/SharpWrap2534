using SharpWrap2534.J2534Objects;
using SharpWrap2534.PassThruTypes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SharpSimulatorTests
{
    /// <summary>
    /// Test data for static routines during Loading
    /// </summary>
    public static class SimLoadingTestData
    {
        // Start by building a new simulation channel object to load in.
        public static readonly ProtocolId Protocol = ProtocolId.ISO15765;
        public static readonly J2534Filter[] BuiltFilters = new[] { new J2534Filter()
        {
            FilterFlags = 0x40,
            FilterMask = "00 00 FF FF",
            FilterPattern = "00 00 07 E8",
            FilterFlowCtl = "00 00 07 E0"
        }};
        public static readonly PassThruStructs.PassThruMsg[] MessagesToWrite = new[] { new PassThruStructs.PassThruMsg()
        {
            DataSize = 6,
            ProtocolID = ProtocolId.ISO15765,
            TxFlags = (uint)TxFlags.ISO15765_FRAME_PAD,
            Data = new byte[] { 0x00, 0x00, 0x07, 0xE8, 0x09, 0x42 },
        }};
        public static readonly PassThruStructs.PassThruMsg[] MessagesToRead = new[] { new PassThruStructs.PassThruMsg()
        {
            DataSize = 6,
            ProtocolID = ProtocolId.ISO15765,
            TxFlags = (uint)TxFlags.ISO15765_FRAME_PAD,
            Data = new byte[] { 0x00, 0x00, 0x07, 0xDF, 0x09, 0x02 },
        }};
    }
}
