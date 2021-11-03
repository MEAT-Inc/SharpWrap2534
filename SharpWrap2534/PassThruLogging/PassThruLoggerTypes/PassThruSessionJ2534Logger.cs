using System;
using System.IO;
using System.Runtime.CompilerServices;
using SharpWrap2534.PassThruLogging.SessionSetup;

namespace SharpWrap2534.PassThruLogging.PassThruLoggerTypes
{
    /// <summary>
    /// Logger object used to log data from a J2534 object.
    /// </summary>
    internal class SimSessionJ2534Logger : SimSessionLoggerBase
    {
        // File Paths for logger
        public string LoggerFile;           // Path of the logger file.
        public string OutputPath;           // Base output path.

        // -----------------------------------------------------------------------------------------------------

        /// <summary>
        /// Builds a new J2534 Simulator logging helper.
        /// </summary>
        /// <param name="LoggerType"></param>
        /// <param name="LoggerName"></param>
        /// <param name="MinLevel"></param>
        /// <param name="MaxLevel"></param>
        internal SimSessionJ2534Logger([CallerMemberName] string LoggerName = "", string LogFileName = "", int MinLevel = 0, int MaxLevel = 5) : base(LoggerActions.J2534Logger, LoggerName, MinLevel, MaxLevel)
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

            // Store class values.
            this.LoggerFile = LogFileName;
            this.OutputPath = new FileInfo(LogFileName).DirectoryName;

            // Build Logger object now.
            this.LoggingConfig = LogManager.Configuration;
            this.LoggingConfig.AddRule(
                LogLevel.FromOrdinal(MinLevel),
                LogLevel.FromOrdinal(MaxLevel),
                SimLoggerFactory.GenerateFileLogger(LoggerName, LogFileName),$"*{LoggerName}*");
            this.LoggingConfig.AddRule(
                LogLevel.FromOrdinal(MinLevel),
                LogLevel.FromOrdinal(MaxLevel),
                SimLoggerFactory.GenerateConsoleLogger(LoggerName), $"*{LoggerName}*");

            // Store configuration
            LogManager.Configuration = this.LoggingConfig;
            this.NLogger = LogManager.GetCurrentClassLogger();
            this.PrintLoggerInfos();
        }
    }
}
