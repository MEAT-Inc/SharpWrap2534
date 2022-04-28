﻿using System;
using System.Linq;
using System.Reflection;
using SharpLogger;
using SharpLogger.LoggerObjects;
using SharpLogger.LoggerSupport;
using SharpWrap2534;
using SharpWrap2534.PassThruTypes;

namespace SharpAutoId.SharpAutoIdHelpers
{
    /// <summary>
    /// Extensions which provide endpoints for us to hit to build auto ID helper routines
    /// </summary>
    public static class SharpAutoIdExtensions
    {
        /// <summary>
        /// Builds a new AutoID Helper object from a given input session and protocol
        /// </summary>
        /// <param name="SessionInstance">Session to build from</param>
        /// <param name="ProtocolValue">Protocol to scan with</param>
        /// <returns></returns>
        public static SharpAutoId SpawnAutoIdHelper(this Sharp2534Session SessionInstance, ProtocolId ProtocolValue)
        {
            // Check to make sure the requested protocol is supported first.
            if (!SharpAutoIdConfig.SupportedProtocols.Contains(ProtocolValue))
                throw new InvalidOperationException($"CAN NOT USE PROTOCOL {ProtocolValue} SINCE IT IS NOT SUPPORTED!");

            // Get logger object from our session
            PropertyInfo LoggerProp = SessionInstance.GetType().GetProperty("_sessionLogger", BindingFlags.NonPublic | BindingFlags.Instance);
            BaseLogger Logger = (BaseLogger)LoggerProp?.GetValue(SessionInstance) ?? LogBroker.Logger;

            // Build auto ID helper and return the object out
            SharpAutoId AutoIdInstance = new SharpAutoId(SessionInstance, ProtocolValue);
            Logger.WriteLog($"PULLED IN SESSION LOGGER NAMED {Logger.LoggerName}!");
            Logger.WriteLog($"SESSION FOR AUTO ID ROUTINE ON PROTOCOL {ProtocolValue} WAS BUILT OK!", LogType.InfoLog);

            // Return the AutoID Instance object
            return AutoIdInstance;
        }
    }
}