﻿using SharpWrap2534.J2534Objects;
using SharpWrap2534.PassThruTypes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SharpSimulator;

namespace SharpSimulatorTests
{
    /// <summary>
    /// Test data for static routines during Loading
    /// </summary>
    public static class SimLoadingTestData
    {
        // Start by building a new simulation channel object to load in.
        public static readonly uint BaudRate = 500000;
        public static readonly uint ChannelFlags = 0x00;
        public static readonly ProtocolId Protocol = ProtocolId.ISO15765;
        public static readonly J2534Filter[] BuiltFilters = new[] { new J2534Filter()
        {
            FilterFlags = 0x40,
            FilterMask = "00 00 FF FF",
            FilterPattern = "00 00 07 E0",
            FilterFlowCtl = "00 00 07 E8",
            FilterProtocol = ProtocolId.ISO15765,
            FilterType = FilterDef.FLOW_CONTROL_FILTER
        }};

        // Paired Message Commands
        public static readonly SimulationMessagePair[] PairedMessages = new[]
        {
            // Calibration ID messages
            new SimulationMessagePair
            {
                // Message to get calibration ID
                MessageRead = new PassThruStructs.PassThruMsg()
                {
                    DataSize = 12,
                    Data = new byte[] { 0x00, 0x00, 0x07, 0xDF, 0x02, 0x01, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 },
                },

                // Responses with the calibration ID
                MessageResponses = new []
                {
                    new PassThruStructs.PassThruMsg()
                    {
                        DataSize = 10,
                        ProtocolID = ProtocolId.ISO15765,
                        TxFlags = (uint)TxFlags.ISO15765_FRAME_PAD,
                        Data = new byte[] { 0x00, 0x00, 0x07, 0xE8, 0x41, 0x00, 0xBF, 0xFF, 0xB9, 0x93 },
                    },
                }
            },

            // VIN Number messages
            new SimulationMessagePair
            {
                // Message to get our VIN Number
                MessageRead = new PassThruStructs.PassThruMsg()
                {
                    DataSize = 12,
                    Data = new byte[] { 0x00, 0x00, 0x07, 0xDF, 0x02, 0x09, 0x02, 0x00, 0x00, 0x00, 0x00, 0x00 },
                },

                // Message containing the VIN Number
                MessageResponses = new []
                {
                    new PassThruStructs.PassThruMsg()
                    {
                        DataSize = 24,
                        ProtocolID = ProtocolId.ISO15765,
                        TxFlags = (uint)TxFlags.ISO15765_FRAME_PAD,
                        Data = new byte[] { 0x00, 0x00, 0x07, 0xE8, 0x49, 0x02, 0x01, 0x31, 0x47, 0x31, 0x46, 0x42, 0x33, 0x44, 0x53, 0x33, 0x4B, 0x30, 0x31, 0x31, 0x37, 0x32, 0x32, 0x38 },
                    },
                }
            }
        };
    }
}
