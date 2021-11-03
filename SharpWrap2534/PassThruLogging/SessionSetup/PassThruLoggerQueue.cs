using System.Collections.Generic;
using System.Linq;
using SharpWrap2534.PassThruLogging.PassThruLoggerTypes;

namespace SharpWrap2534.PassThruLogging.SessionSetup
{
    /// <summary>
    /// Class which holds all built loggers for this active instace.
    /// </summary>
    internal class SimLoggerQueue
    {
        // List of all logger items in the pool.
        private List<SimSessionLoggerBase> LoggerPool = new List<SimSessionLoggerBase>();

        // ---------------------------------------------------------------------------------------------

        /// <summary>
        /// Adds a logger item to the pool of all loggers.
        /// </summary>
        /// <param name="loggerBaseItem">Item to add to the pool.</param>
        public void AddLoggerToPool(SimSessionLoggerBase loggerBaseItem)
        {
            // Find existing loggers that may have the same name as this logger obj.
            if (LoggerPool.Any(LogObj => LogObj.LoggerGuid == loggerBaseItem.LoggerGuid))
            {
                // Update current.
                int IndexOfExisting = LoggerPool.IndexOf(loggerBaseItem);
                LoggerPool[IndexOfExisting] = loggerBaseItem;
                return;
            }

            // If the logger didnt get added (no dupes) do it not.
            LoggerPool.Add(loggerBaseItem);
        }
        /// <summary>
        /// Removes the logger passed from the logger queue
        /// </summary>
        /// <param name="loggerBaseItem">Logger to yank</param>
        public void RemoveLoggerFromPool(SimSessionLoggerBase loggerBaseItem)
        {
            // Pull out all the dupes.
            var NewLoggers = LoggerPool.Where(LogObj =>
                LogObj.LoggerGuid != loggerBaseItem.LoggerGuid).ToList();

            // Check if new logger is in loggers filtered or not and store it.
            if (NewLoggers.Contains(loggerBaseItem)) NewLoggers.Remove(loggerBaseItem);
            this.LoggerPool = NewLoggers;
        }


        /// <summary>
        /// Gets all loggers that exist currently.
        /// </summary>
        /// <returns></returns>
        public List<SimSessionLoggerBase> GetLoggers()
        {
            // Get them and return.
            return this.LoggerPool;
        }
        /// <summary>
        /// Gets loggers based on a given type of logger.
        /// </summary>
        /// <param name="TypeOfLogger">Type of logger to get.</param>
        /// <returns>List of all loggers for this type.</returns>
        public List<SimSessionLoggerBase> GetLoggers(LoggerActions TypeOfLogger)
        {
            // Logger object to populate
            return this.LoggerPool.Where(LogObj => LogObj.LoggerType == TypeOfLogger).ToList();
        }
    }
}
