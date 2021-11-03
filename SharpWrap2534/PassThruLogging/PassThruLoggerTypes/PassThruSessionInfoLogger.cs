using System;
using System.IO;
using System.Runtime.CompilerServices;
using SharpWrap2534.PassThruLogging.SessionSetup;

namespace SharpWrap2534.PassThruLogging.PassThruLoggerTypes
{
    /// <summary>
    /// Base file logging logger object.
    /// </summary>
    internal class SimSessionInfoLogger : SimSessionLoggerBase
    {
        // -----------------------------------------------------------------------------------------------------

        /// <summary>
        /// Builds a new Session information logger.
        /// Used mainly to write diagnostic information out.
        /// </summary>
        /// <param name="LoggerType"></param>
        /// <param name="LoggerName"></param>
        /// <param name="MinLevel"></param>
        /// <param name="MaxLevel"></param>
        internal SimSessionInfoLogger([CallerMemberName] string LoggerName = "", string LogFileName = "", int MinLevel = 0, int MaxLevel = 5) : base(LoggerActions.SessionLogger, LoggerName, MinLevel, MaxLevel)
        {
            // Check file name.
            if (string.IsNullOrEmpty(LogFileName))
            {
                // Check for broker file.
                LogFileName = SimLoggingBroker.MainLogFileName != null
                    ? SimLoggingBroker.MainLogFileName
                    : Path.Combine(
                        SimLoggingBroker.BaseOutputPath,
                        $"{this.LoggerName}_Logging_{DateTime.Now.ToString("ddMMyyy-hhmmss")}.log"
                    );
            }

            // Build Master Logging Configuration.
            this.LoggingConfig = LogManager.Configuration;
            this.LoggingConfig.AddRule(
                LogLevel.FromOrdinal(MinLevel),
                LogLevel.FromOrdinal(MaxLevel),
                SimLoggerFactory.GenerateFileLogger(LoggerName, LogFileName), $"*{LoggerName}*");
            this.LoggingConfig.AddRule(
                LogLevel.FromOrdinal(MinLevel),
                LogLevel.FromOrdinal(MaxLevel),
                SimLoggerFactory.GenerateConsoleLogger(LoggerName), $"*{LoggerName}*");

            // Store config for the NLog object now.
            LogManager.Configuration = this.LoggingConfig;
            this.NLogger = LogManager.GetCurrentClassLogger();
            this.PrintLoggerInfos();
        }
    }
}
