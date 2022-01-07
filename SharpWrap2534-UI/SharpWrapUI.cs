using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using SharpLogger;
using SharpLogger.LoggerObjects;
using SharpLogger.LoggerSupport;

namespace SharpWrap2534_UI
{
    /// <summary>
    /// Static setup class which pulls information in from the current working directories for style information and logging configurations
    /// </summary>
    public static class SharpWrapUI
    {
        // Logger object for setup methods. Only logs to console
        private static ConsoleLogger SetupLogger => (ConsoleLogger)LogBroker.LoggerQueue.GetLoggers(LoggerActions.ConsoleLogger)
            .FirstOrDefault(LoggerObj => LoggerObj.LoggerName.StartsWith("SharpWrapUI_SetupLogger")) ?? new ConsoleLogger("SharpWrapUI_SetupLogger");

        // Public values for setting up this library instance
        public static ResourceDictionary AppThemeResources;             // The main theme object for our users application being configured.

        // Set of all controls active at this time
        public static Tuple<UserControl, ViewModelControlBase>[] ActiveUserControls { get; internal set; }
        private static UserControl[] RegisteredUserControls => ActiveUserControls.Select(ObjSet => ObjSet.Item1).ToArray();
        private static ViewModelControlBase[] RegisteredViewModels => ActiveUserControls.Select(ObjSet => ObjSet.Item2).ToArray();

        // ----------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Pulls in a new logging configuration and style sheet configuration if one is given
        /// If provided, these values are applied into our output view and logs. If no logging is done, then
        /// the logging output is kept off
        /// </summary>
        /// <returns>True if configuration passed. False if not.</returns>
        public static bool ConfigureUserControls(ResourceDictionary ThemeDictionary)
        {
            // Pull in logging configuration from a passed in broker object.
            AppThemeResources = ThemeDictionary;
            SetupLogger.WriteLog("SETUP NEW INSTANCE VALUES FOR OUR LOG BROKER AND THEME RESOURCES WITHOUT ISSUES!", LogType.InfoLog);
            SetupLogger.WriteLog("FROM THIS POINT FORWARD, ALL THEME CONFIGURATION WILL BE LINKED BACK INTO THIS THEME OBJECT DICTIONARY!", LogType.InfoLog);
            return true;
        }

        /// <summary>
        /// Assigns a new usercontrol object into a list of current instances.
        /// </summary>
        /// <param name="UserContent"></param>
        /// <param name="ViewModel"></param>
        /// <returns>True if added. False if it already exists</returns>
        public static bool RegisterContentView(UserControl UserContent, ViewModelControlBase ViewModel)
        {
            // Log the type information being added in here.
            SetupLogger.WriteLog($"APPENDING NEW USER CONTROL TYPE OF {UserContent.GetType().Name} TO LIST OF CONTENT NOW...");
            SetupLogger.WriteLog($"VIEWMODEL TYPE WAS SEEN TO BE {ViewModel.GetType().Name}");

            // Now find if any of the existing viewModels match the one being passed in.
            var TempTuple = new Tuple<UserControl, ViewModelControlBase>(UserContent, ViewModel);
            if (ActiveUserControls.Any(ObjSet => ObjSet.Item1 == TempTuple.Item1 || ObjSet.Item2 == TempTuple.Item2))
                return false;

            // Append new values into our list here.
            SetupLogger.WriteLog("APPENDING NEW CONTROL OBJECT INTO OUR CONTROL ARRAYS NOW", LogType.InfoLog);
            ActiveUserControls = ActiveUserControls.Append(TempTuple).ToArray();
            return true;
        }
    }
}
