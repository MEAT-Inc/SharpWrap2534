using System;
using System.IO;
using System.Linq;
using System.Reflection;
using SharpLogger;
using SharpLogger.LoggerObjects;
using SharpLogger.LoggerSupport;
using SharpWrapper;
using SharpWrapper.PassThruTypes;

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
        public static SharpAutoIdHelper SpawnAutoIdHelper(this Sharp2534Session SessionInstance, ProtocolId ProtocolValue)
        {
            // Make Sure logging is configured
            if (LogBroker.BaseOutputPath == null)
            {
                // Configure output locations
                LogBroker.ConfigureLoggingSession(
                    Assembly.GetExecutingAssembly().FullName,
                    Path.Combine(Directory.GetCurrentDirectory(), "SharpLogging"));
                
                // Populate Broker Pool
                LogBroker.BrokerInstance.FillBrokerPool();
            }

            // Check to make sure the requested protocol is supported first.
            if (!SharpAutoIdConfig.SupportedProtocols.Contains(ProtocolValue))
                throw new InvalidOperationException($"CAN NOT USE PROTOCOL {ProtocolValue} SINCE IT IS NOT SUPPORTED!");

            // Get logger object from our session
            PropertyInfo LoggerProp = SessionInstance.GetType().GetProperty("_sessionLogger", BindingFlags.NonPublic | BindingFlags.Instance);
            BaseLogger Logger = (BaseLogger)LoggerProp?.GetValue(SessionInstance) ?? LogBroker.Logger;
            Logger.WriteLog($"PULLED IN SESSION LOGGER NAMED {Logger.LoggerName}!");

            // Build auto ID helper and return the object out
            // Get a list of all supported protocols and then pull in all the types of auto ID routines we can use
            var AutoIdType = typeof(SharpAutoIdHelper)
                .Assembly.GetTypes()
                .Where(RoutineType => RoutineType.IsSubclassOf(typeof(SharpAutoIdHelper)) && !RoutineType.IsAbstract)
                .FirstOrDefault(TypeObj => TypeObj.FullName.Contains(ProtocolValue.ToString()));

            // Now build a type of our current autoID Object
            if (AutoIdType == null) throw new TypeAccessException($"CAN NOT USE TYPE FOR PROTOCOL NAMED {ProtocolValue}!");
            SharpAutoIdHelper AutoIdInstance = (SharpAutoIdHelper)Activator.CreateInstance(AutoIdType, SessionInstance);
            Logger.WriteLog($"SESSION FOR AUTO ID ROUTINE ON PROTOCOL {ProtocolValue} WAS BUILT OK!", LogType.InfoLog);

            // Return the AutoID Instance object
            return AutoIdInstance;
        }
    }
}
