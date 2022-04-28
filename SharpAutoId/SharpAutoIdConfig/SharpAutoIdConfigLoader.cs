using System;
using System.IO;
using System.Linq;
using Newtonsoft.Json.Linq;
using SharpAutoId.SharpAutoIdModels;
using SharpLogger;
using SharpLogger.LoggerObjects;
using SharpLogger.LoggerSupport;
using SharpWrap2534.PassThruTypes;

namespace SharpAutoId.SharpAutoIdConfig
{
    public static class SharpAutoIdConfigLoader
    {
        // Logger object for configuration of an AutoID JSON Reader
        private static SubServiceLogger ConfigLogger => (SubServiceLogger)LogBroker.LoggerQueue.GetLoggers(LoggerActions.SubServiceLogger)
            .FirstOrDefault(LoggerObj => LoggerObj.LoggerName.StartsWith("AutoIdConfigLogger")) ?? new SubServiceLogger("AutoIdConfigLogger");

        // ------------------------------------------------------------------------------------------------------------------------------------------

        // List of all currently supported protocols for Auto ID routines
        public static ProtocolId[] SupportedProtocols => new[] { ProtocolId.ISO15765 };
        public static SharpIdConfiguration[] SupportedCommandRoutines => new[]
        {
            // ISO15765-4 Configuration Routine at 500k BaudRate
            new SharpIdConfiguration()
            {             
                // Generic Connection Flags
                ConnectFlags = 0x00,
                AutoIdType = ProtocolId.ISO15765,
                ConnectBaud = BaudRate.ISO15765_500000,

                // Commands and Filters to Use
                RoutineCommands = new[]
                {
                    // Message for Setup Connection
                    new MessageObject()
                    {
                        MessageProtocol = ProtocolId.ISO15765,
                        MessageFlags = TxFlags.ISO15765_FRAME_PAD,
                        MessageData = "0x00 0x00 0x07 0xDF 0x01 0x00"
                    },

                    // Message for the VIN Number
                    new MessageObject()
                    {
                        MessageProtocol = ProtocolId.ISO15765,
                        MessageFlags = TxFlags.ISO15765_FRAME_PAD,
                        MessageData =  "0x00 0x00 0x07 0xDF 0x09 0x02"
                    }
                },
                RoutineFilters = new []
                {
                    new FilterObject()
                    {
                        FilterProtocol = ProtocolId.ISO15765,
                        FilterFlags = TxFlags.ISO15765_FRAME_PAD,
                        FilterType = FilterDef.FLOW_CONTROL_FILTER,
                        FilterMask = new MessageObject()
                        {
                            MessageData = "0xFF 0xFF 0xFF 0xFF",
                            MessageProtocol = ProtocolId.ISO15765,
                            MessageFlags = TxFlags.ISO15765_FRAME_PAD
                        },
                        FilterPattern = new MessageObject()
                        {
                            MessageData = "0xFF 0xFF 0x07 0xE8",
                            MessageProtocol = ProtocolId.ISO15765,
                            MessageFlags = TxFlags.ISO15765_FRAME_PAD
                        },
                        FilterFlowControl = new MessageObject()
                        {
                            MessageData = "0xFF 0xFF 0x07 0xE0",
                            MessageProtocol = ProtocolId.ISO15765,
                            MessageFlags = TxFlags.ISO15765_FRAME_PAD
                        },
                    },
                    new FilterObject()
                    {
                        FilterProtocol = ProtocolId.ISO15765,
                        FilterFlags = TxFlags.ISO15765_FRAME_PAD,
                        FilterType = FilterDef.FLOW_CONTROL_FILTER,
                        FilterMask = new MessageObject()
                        {
                            MessageData = "0xFF 0xFF 0xFF 0xFF",
                            MessageProtocol = ProtocolId.ISO15765,
                            MessageFlags = TxFlags.ISO15765_FRAME_PAD
                        },
                        FilterPattern = new MessageObject()
                        {
                            MessageData = "0xFF 0xFF 0x07 0xE9",
                            MessageProtocol = ProtocolId.ISO15765,
                            MessageFlags = TxFlags.ISO15765_FRAME_PAD
                        },
                        FilterFlowControl = new MessageObject()
                        {
                            MessageData = "0xFF 0xFF 0x07 0xE1",
                            MessageProtocol = ProtocolId.ISO15765,
                            MessageFlags = TxFlags.ISO15765_FRAME_PAD
                        },
                    },
                    new FilterObject()
                    {
                        FilterProtocol = ProtocolId.ISO15765,
                        FilterFlags = TxFlags.ISO15765_FRAME_PAD,
                        FilterType = FilterDef.FLOW_CONTROL_FILTER,
                        FilterMask = new MessageObject()
                        {
                            MessageData = "0xFF 0xFF 0xFF 0xFF",
                            MessageProtocol = ProtocolId.ISO15765,
                            MessageFlags = TxFlags.ISO15765_FRAME_PAD
                        },
                        FilterPattern = new MessageObject()
                        {
                            MessageData = "0xFF 0xFF 0x07 0xEA",
                            MessageProtocol = ProtocolId.ISO15765,
                            MessageFlags = TxFlags.ISO15765_FRAME_PAD
                        },
                        FilterFlowControl = new MessageObject()
                        {
                            MessageData = "0xFF 0xFF 0x07 0xE2",
                            MessageProtocol = ProtocolId.ISO15765,
                            MessageFlags = TxFlags.ISO15765_FRAME_PAD
                        },
                    },
                    new FilterObject()
                    {
                        FilterProtocol = ProtocolId.ISO15765,
                        FilterFlags = TxFlags.ISO15765_FRAME_PAD,
                        FilterType = FilterDef.FLOW_CONTROL_FILTER,
                        FilterMask = new MessageObject()
                        {
                            MessageData = "0xFF 0xFF 0xFF 0xFF",
                            MessageProtocol = ProtocolId.ISO15765,
                            MessageFlags = TxFlags.ISO15765_FRAME_PAD
                        },
                        FilterPattern = new MessageObject()
                        {
                            MessageData = "0xFF 0xFF 0x07 0xEB",
                            MessageProtocol = ProtocolId.ISO15765,
                            MessageFlags = TxFlags.ISO15765_FRAME_PAD
                        },
                        FilterFlowControl = new MessageObject()
                        {
                            MessageData = "0xFF 0xFF 0x07 0xE3",
                            MessageProtocol = ProtocolId.ISO15765,
                            MessageFlags = TxFlags.ISO15765_FRAME_PAD
                        },
                    },
                    new FilterObject()
                    {
                        FilterProtocol = ProtocolId.ISO15765,
                        FilterFlags = TxFlags.ISO15765_FRAME_PAD,
                        FilterType = FilterDef.FLOW_CONTROL_FILTER,
                        FilterMask = new MessageObject()
                        {
                            MessageData = "0xFF 0xFF 0xFF 0xFF",
                            MessageProtocol = ProtocolId.ISO15765,
                            MessageFlags = TxFlags.ISO15765_FRAME_PAD
                        },
                        FilterPattern = new MessageObject()
                        {
                            MessageData = "0xFF 0xFF 0x07 0xEC",
                            MessageProtocol = ProtocolId.ISO15765,
                            MessageFlags = TxFlags.ISO15765_FRAME_PAD
                        },
                        FilterFlowControl = new MessageObject()
                        {
                            MessageData = "0xFF 0xFF 0x07 0xE4",
                            MessageProtocol = ProtocolId.ISO15765,
                            MessageFlags = TxFlags.ISO15765_FRAME_PAD
                        },
                    },
                    new FilterObject()
                    {
                        FilterProtocol = ProtocolId.ISO15765,
                        FilterFlags = TxFlags.ISO15765_FRAME_PAD,
                        FilterType = FilterDef.FLOW_CONTROL_FILTER,
                        FilterMask = new MessageObject()
                        {
                            MessageData = "0xFF 0xFF 0xFF 0xFF",
                            MessageProtocol = ProtocolId.ISO15765,
                            MessageFlags = TxFlags.ISO15765_FRAME_PAD
                        },
                        FilterPattern = new MessageObject()
                        {
                            MessageData = "0xFF 0xFF 0x07 0xED",
                            MessageProtocol = ProtocolId.ISO15765,
                            MessageFlags = TxFlags.ISO15765_FRAME_PAD
                        },
                        FilterFlowControl = new MessageObject()
                        {
                            MessageData = "0xFF 0xFF 0x07 0xE5",
                            MessageProtocol = ProtocolId.ISO15765,
                            MessageFlags = TxFlags.ISO15765_FRAME_PAD
                        },
                    },
                    new FilterObject()
                    {
                        FilterProtocol = ProtocolId.ISO15765,
                        FilterFlags = TxFlags.ISO15765_FRAME_PAD,
                        FilterType = FilterDef.FLOW_CONTROL_FILTER,
                        FilterMask = new MessageObject()
                        {
                            MessageData = "0xFF 0xFF 0xFF 0xFF",
                            MessageProtocol = ProtocolId.ISO15765,
                            MessageFlags = TxFlags.ISO15765_FRAME_PAD
                        },
                        FilterPattern = new MessageObject()
                        {
                            MessageData = "0xFF 0xFF 0x07 0xEE",
                            MessageProtocol = ProtocolId.ISO15765,
                            MessageFlags = TxFlags.ISO15765_FRAME_PAD
                        },
                        FilterFlowControl = new MessageObject()
                        {
                            MessageData = "0xFF 0xFF 0x07 0xE6",
                            MessageProtocol = ProtocolId.ISO15765,
                            MessageFlags = TxFlags.ISO15765_FRAME_PAD
                        },
                    },
                    new FilterObject()
                    {
                        FilterProtocol = ProtocolId.ISO15765,
                        FilterFlags = TxFlags.ISO15765_FRAME_PAD,
                        FilterType = FilterDef.FLOW_CONTROL_FILTER,
                        FilterMask = new MessageObject()
                        {
                            MessageData = "0xFF 0xFF 0xFF 0xFF",
                            MessageProtocol = ProtocolId.ISO15765,
                            MessageFlags = TxFlags.ISO15765_FRAME_PAD
                        },
                        FilterPattern = new MessageObject()
                        {
                            MessageData = "0xFF 0xFF 0x07 0xEF",
                            MessageProtocol = ProtocolId.ISO15765,
                            MessageFlags = TxFlags.ISO15765_FRAME_PAD
                        },
                        FilterFlowControl = new MessageObject()
                        {
                            MessageData = "0xFF 0xFF 0x07 0xE7",
                            MessageProtocol = ProtocolId.ISO15765,
                            MessageFlags = TxFlags.ISO15765_FRAME_PAD
                        },
                    }
                }
            }
        };

        // All Loaded AutoID routines for this instance
        public static Tuple<ProtocolId, SharpIdConfiguration>[] LoadedRoutines
        {
            get
            {
                // Store our AutoID Protocols and Routines here
                var Protocols = SupportedProtocols
                    .OrderBy(ProcObj => ProcObj)
                    .ToArray();
                var Routines = SupportedCommandRoutines
                    .OrderBy(ConfigObj => ConfigObj.AutoIdType)
                    .ToArray();

                // Now build our tuple object.
                var ZippedRoutines = Protocols
                    .Zip(Routines, (ProcObj, RoutineObj) =>
                        new Tuple<ProtocolId, SharpIdConfiguration>(ProcObj, RoutineObj))
                    .ToArray();

                // Return the build list of routines here
                ConfigLogger.WriteLog("ZIPPED PROTOCOLS AND ROUTINES OK! RETURNING THEM NOW...", LogType.InfoLog);
                return ZippedRoutines;
            }
        }

        // ------------------------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Gets an auto ID routine for the given protocol value.
        /// </summary>
        /// <param name="ProtocolToUse">Protocol To use for the AutoID</param>
        /// <returns>Routine matching the given protocol or null</returns>
        internal static SharpIdConfiguration GetRoutine(ProtocolId ProtocolToUse)
        {
            // Find our routine.
            var RoutineLocated = SupportedCommandRoutines.FirstOrDefault(RoutineObj => RoutineObj.AutoIdType == ProtocolToUse);
            ConfigLogger.WriteLog(
                RoutineLocated == null ? "NO ROUTINE WAS FOUND! RETURNING NULL!" : $"RETURNING ROUTINE FOR PROTOCOL {ProtocolToUse} NOW...",
                RoutineLocated == null ? LogType.ErrorLog : LogType.InfoLog
            );

            // Return the located routine here
            return RoutineLocated;
        }
    }
}
