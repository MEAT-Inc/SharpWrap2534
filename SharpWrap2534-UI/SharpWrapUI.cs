using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using ControlzEx.Theming;
using NLog.Fluent;
using SharpLogger;
using SharpLogger.LoggerObjects;
using SharpLogger.LoggerSupport;

namespace SharpWrap2534_UI
{
    /// <summary>
    /// Static setup class which pulls information in from the current working directories for style information and logging configurations
    /// </summary>
    public static class SharpWrapUi
    {
        // Logger object for setup methods. Only logs to console
        private static BaseLogger SetupLogger
        {
            get
            {
                // If the main log file is configured, file out a file logger
                if (LogBroker.MainLogFileName != null)
                    return (SubServiceLogger)LogBroker.LoggerQueue.GetLoggers(LoggerActions.SubServiceLogger)
                        .FirstOrDefault(LoggerObj => LoggerObj.LoggerName.StartsWith("SharpWrapUI_SetupLogger")) 
                           ?? new SubServiceLogger("SharpWrapUI_SetupLogger");
                
                // If the main log file is not built, then don't return a file logger. This should only go to file if it's ready
                return (ConsoleLogger)LogBroker.LoggerQueue.GetLoggers(LoggerActions.ConsoleLogger)
                           .FirstOrDefault(LoggerObj => LoggerObj.LoggerName.StartsWith("SharpWrapUI_SetupLogger"))
                       ?? new ConsoleLogger("SharpWrapUI_SetupLogger");
            }
        }

        // Public values for setting up this library instance
        public static ResourceDictionary AppThemeResources { get; private set; }     // Application theme styles
        public static Tuple<UserControl, SharpWrapViewModel>[] ActiveUserControls { get; internal set; }   // Set of all controls active

        // --------------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Pulls in a new logging configuration and style sheet configuration if one is given
        /// If provided, these values are applied into our output view and logs. If no logging is done, then
        /// the logging output is kept off
        /// </summary>
        /// <returns>True if configuration passed. False if not.</returns>
        public static bool RegisterApplicationThemes(ResourceDictionary ThemeDictionary)
        {
            // Set the resource back here.
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
        public static void RegisterContentView(UserControl UserContent, SharpWrapViewModel ViewModel)
        {
            // Log the type information being added in here.
            SetupLogger.WriteLog($"APPENDING NEW USER CONTROL TYPE OF {UserContent.GetType().Name} TO LIST OF CONTENT NOW...", LogType.TraceLog);
            SetupLogger.WriteLog($"VIEWMODEL TYPE WAS SEEN TO BE {ViewModel.GetType().Name}", LogType.TraceLog);

            // Now find if any of the existing viewModels match the one being passed in.
            var TempTuple = new Tuple<UserControl, SharpWrapViewModel>(UserContent, ViewModel);
            if (ActiveUserControls.Any(ObjSet => ObjSet.Item1 == TempTuple.Item1 || ObjSet.Item2 == TempTuple.Item2))
            {
                // Remove the existing object here.
                var ControlsAsList = ActiveUserControls.ToList();
                int RemovalIndex = ControlsAsList.IndexOf(TempTuple);
                SetupLogger.WriteLog($"REMOVING EXISTING INDEX PAIR VALUES AT INDEX {RemovalIndex}", LogType.WarnLog);
               
                // Remove old and insert new
                ControlsAsList.RemoveAt(RemovalIndex); 
                ControlsAsList.Insert(RemovalIndex, TempTuple);

                // Store new value set onto instance
                ActiveUserControls = ControlsAsList.ToArray();
                SetupLogger.WriteLog($"APPENDED NEW VALUE INTO TUPLE ARRAY AT INDEX {RemovalIndex} WITHOUT ISSUES", LogType.TraceLog);
                return;
            }

            // Append new values into our list here.
            SetupLogger.WriteLog("NO MATCH FOUND!", LogType.TraceLog);
            ActiveUserControls = ActiveUserControls.Append(TempTuple).ToArray();
            SetupLogger.WriteLog("APPENDING NEW CONTROL OBJECT ONTO THE END OF THE STORE ARRAY CORRECTLY!", LogType.InfoLog);
        }
    }
}
