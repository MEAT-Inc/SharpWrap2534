using System;
using LogLevel = NLog.LogLevel;

namespace SharpWrap2534.PassThruLogging
{
    /// <summary>
    /// Wrapped log level type so NLOG isn't a required ref for anything that uses this.
    /// </summary>
    internal enum LogType : int
    {
        // Basic logging level values
        TraceLog,   // Compares to LogLevel.Trac
        DebugLog,   // Compares to LogLevel.Debug
        InfoLog,    // Compares to LogLevel.Info
        WarnLog,    // Compares to LogLevel.Warn
        ErrorLog,   // Compares to LogLevel.Error
        FatalLog,   // Compares to LogLevel.Fatal
        NoLogging   // Compares to LogLevel.Off
    }

    /// <summary>
    /// Custom type of logger being used.
    /// </summary>
    internal enum LoggerActions : int
    {
        BaseSimLogger,        // Base logging object.
        SessionLogger,        // Logger built for file output
        J2534Logger,          // J2534 Logging object
    }


    /// <summary>
    /// Extensions for log type objects
    /// </summary>
    internal static class LogTypeExtensions
    {
        // Default logging levels.
        private static LogLevel NLevelDefault = LogLevel.Trace;
        private static LogType TypeLevelDefault = LogType.TraceLog;

        /// <summary>
        /// Set the min logging level for this class when conversion fails
        /// </summary>
        /// <param name="Level"></param>
        public static void SetDefaultLevel(LogType Level) { TypeLevelDefault = Level; NLevelDefault = Level.ToNLevel(); }
        /// <summary>
        /// Set the min logging level for this class when conversion fails
        /// </summary>
        /// <param name="Level"></param>
        public static void SetDefaultLevel(LogLevel Level) { NLevelDefault = Level; TypeLevelDefault = Level.ToLogType(); }


        /// <summary>
        /// Converts a LogType into a LogLevel for NLOG use.
        /// </summary>
        /// <param name="Level"></param>
        /// <returns>LogLevel Pulled out of here.</returns>
        public static LogLevel ToNLevel(this LogType Level) { return (int)Level > 6 ? NLevelDefault : LogLevel.FromOrdinal((int)Level); }
        /// <summary>
        /// Converts a given NLogLevel into a LogType
        /// </summary>
        /// <param name="Level">Level to check</param>
        /// <returns>Gives back a default log type.</returns>
        public static LogType ToLogType(this LogLevel Level) { return Level.Ordinal > 6 ? TypeLevelDefault : (LogType)Level.Ordinal; }
    }
}
