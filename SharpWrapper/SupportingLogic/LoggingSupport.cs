using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using SharpLogger.LoggerObjects;
using SharpLogger.LoggerSupport;

namespace SharpWrap2534.SupportingLogic
{
    /// <summary>
    /// Logging helpers for session logging configuration
    /// </summary>
    internal class LoggingSupport
    {
        // Logging Helper Object
        private readonly string DeviceName;
        private readonly SubServiceLogger SessionLogger;

        /// <summary>
        /// Logger support constructor object
        /// </summary>
        /// <param name="DeviceName">Name of device in use</param>
        /// <param name="Logger">Logger object to consume.</param>
        public LoggingSupport(string DeviceName, SubServiceLogger Logger)
        {
            // Logging object and device name storage
            this.DeviceName = DeviceName;
            this.SessionLogger = Logger;
            this.SessionLogger.WriteLog("BUILT NEW SESSION LOGGING HELPER FOR PT OUTPUT OK!", LogType.InfoLog);
        }

        // -------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Builds an output splitting command line value.
        /// </summary>
        /// <param name="SplitChar">Split char value</param>
        /// <param name="LineSize">Size of line</param>
        /// <returns>Built splitting string</returns>
        public string SplitLineString(string SplitChar = "=", int LineSize = 150)
        {
            // Build output string by combining the input values as many chars long as specified
            return string.Join(string.Empty, Enumerable.Repeat(
                SplitChar == "" ? "=" : SplitChar,
                LineSize <= 50 ? 50 : LineSize)
            );
        }
        /// <summary>
        /// Writes a basic log output value and includes the name of the PT Command being sent out.
        /// </summary>
        /// <param name="LoggerObject">Logger to write with</param>
        /// <param name="Message">Message to write</param>
        /// <param name="Level">Level to write</param>
        public void WriteCommandLog(string Message, LogType Level = LogType.DebugLog, [CallerMemberName] string MemberName = "PT COMMAND")
        {
            // Find the command type being issued. If none found, then just write normal output.
            if (!MemberName.StartsWith("PT")) {
                this.SessionLogger?.WriteLog($"[{MemberName}] ::: {Message}", LogType.InfoLog);
                return;
            }

            // Now write our output contents.
            string FinalMessage = $"[{this.DeviceName}][{MemberName}] ::: {Message}";
            SessionLogger?.WriteLog(FinalMessage, Level);
        }
    }
}
