using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using SharpWrap2534.PassThruLogging.PassThruLoggerTypes;

namespace SharpWrap2534.PassThruLogging.SessionSetup
{
    /// <summary>
    /// Session broker for sim logging objects
    /// </summary>
    internal sealed class SimLoggingBroker
    {
        // Singleton instance configuration from the broker.
        private static SimLoggingBroker _brokerInstance;
        public static SimLoggingBroker BrokerInstance => _brokerInstance ?? (_brokerInstance = new SimLoggingBroker());

        // Logging infos.
        public static string MainLogFileName;
        public static string AppInstanceName;
        public static string BaseOutputPath;
        public static SimSessionLoggerBase LoggerBase;
        public static SimLoggerQueue LoggerQueue = new SimLoggerQueue();

        // Init Done or not.
        public static LogType MinLevel;
        public static LogType MaxLevel;

        // ----------------------------------------- PRIVATE SINGLETON CTOR METHODS ----------------------------------

        /// <summary>
        /// Builds a new ERS Object and generates the logger output object.
        /// </summary>
        /// <param name="LoggerName"></param>
        private SimLoggingBroker()
        {
            // Setup App constants here.
            if (AppInstanceName == null)
            {
                // Try and Set Process name. If Null, get the name of the called app
                var ProcessModule = Process.GetCurrentProcess().MainModule;
                AppInstanceName = ProcessModule != null
                    ? new FileInfo(ProcessModule.FileName).Name
                    : new FileInfo(Environment.GetCommandLineArgs()[0]).Name;
            }

            // Path to output and base file name.
            if (BaseOutputPath == null)
            {
                // Setup Outputs in the docs folder.
                string DocsFolder = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
                BaseOutputPath = Path.Combine(DocsFolder, AppInstanceName + "_Logs");
            }

            // Get Root logger and build queue.
            if (MinLevel == default) MinLevel = LogType.TraceLog;
            if (MaxLevel == default) MaxLevel = LogType.FatalLog;
        }

        // -----------------------------------------------------------------------------------------------------

        /// <summary>
        /// Stores the broker initial object values before calling the CTOR so the values provided may be used for configuration
        /// </summary>
        /// <param name="InstanceName">Name of the app being run.</param>
        /// <param name="BaseLogPath">Path to write output to.</param>
        public static void ConfigureLoggingSession(string InstanceName = "", string BaseLogPath = "", LogType MinLogLevel = LogType.TraceLog, LogType MaxLogLevel = LogType.FatalLog)
        {
            // Store logging level values
            MinLevel = MinLogLevel;
            MaxLevel = MaxLogLevel;

            // Use the LogFile base if given, or use the name of the exe running.
            AppInstanceName = string.IsNullOrWhiteSpace(InstanceName) ?
                Assembly.GetExecutingAssembly().GetName().Name :
                InstanceName;

            // Set path value.
            BaseOutputPath = string.IsNullOrWhiteSpace(BaseLogPath) ?
                Path.Combine(Directory.GetCurrentDirectory(), AppInstanceName + "_Logs") :
                BaseLogPath;

            // Setup logging session now.
            MainLogFileName = Path.Combine(BaseOutputPath, $"{AppInstanceName}_Logging_{DateTime.Now.ToString("MMddyyy-HHmmss")}.log");
            BrokerInstance.FillBrokerPool();
        }

        /// <summary>
        /// Actually spins up a new logger object once the broker is initialized.
        /// </summary>
        public void FillBrokerPool()
        {
            // DO NOT RUN THIS MORE THAN ONCE!
            if (LoggerBase != null) { return; }

            // Make a new NLogger Config
            if (LogManager.Configuration == null) LogManager.Configuration = new LoggingConfiguration();
            LoggerBase = new SimSessionLoggerBase(
                $"{AppInstanceName}",
                MainLogFileName,
                (int)MinLevel,
                (int)MaxLevel
            );

            // Log output info for the current DLL Assy
            string AssyVersion = Assembly.GetExecutingAssembly().GetName().Version.ToString();
            LoggerBase.WriteLog("LOGGER BROKER BUILT AND SESSION MAIN LOGGER HAS BEEN BOOTED CORRECTLY!", LogType.WarnLog);
            LoggerBase.WriteLog($"--> TIME OF DLL INIT: {DateTime.Now.ToString("g")}", LogType.InfoLog);
            LoggerBase.WriteLog($"--> DLL ASSEMBLY VER: {AssyVersion}", LogType.InfoLog);
            LoggerBase.WriteLog($"--> HAPPY LOGGING. LETS HOPE EVERYTHING GOES WELL...", LogType.InfoLog);
        }
    }
}
