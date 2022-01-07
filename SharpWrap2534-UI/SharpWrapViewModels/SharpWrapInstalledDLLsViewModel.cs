using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SharpLogger;
using SharpLogger.LoggerObjects;
using SharpLogger.LoggerSupport;
using SharpWrap2534.PassThruImport;
using SharpWrap2534.SupportingLogic;

namespace SharpWrap2534_UI.SharpWrapViewModels
{
    /// <summary>
    /// View model for a list of SharpWrap DLL instances
    /// </summary>
    public class SharpWrapInstalledDLLsViewModel : ViewModelControlBase
    {
        // Logger object.
        private static ConsoleLogger ViewModelLogger => (ConsoleLogger)LogBroker.LoggerQueue.GetLoggers(LoggerActions.ConsoleLogger)
            .FirstOrDefault(LoggerObj => LoggerObj.LoggerName.StartsWith("InstalledDLLsViewModelLogger")) ?? new ConsoleLogger("InstalledDLLsViewModelLogger");

        // --------------------------------------------------------------------------------------------------------------------------

        // Private control values
        private JVersion[] _selectedVersions = new JVersion[] { JVersion.V0404, JVersion.V0500 };

        // Public values for our view to bind to
        public PassThruImportDLLs ImportHelper;

        // --------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Logic for building a new viewmodel when this view binding is built
        /// </summary>
        public SharpWrapInstalledDLLsViewModel(string VersionTypes = "ALL_VERSIONS")
        {
            // Find our version type set first.
            switch (VersionTypes)
            {
                case "ALL_VERSIONS":
                    _selectedVersions = new[] { JVersion.V0404, JVersion.V0500 };
                    break;

                case "V0404":
                    _selectedVersions = new[] { JVersion.V0404 };
                    break;

                case "V0500":
                    _selectedVersions = new[] { JVersion.V0500 };
                    break;
            }

            // Log information and store values 
            ViewModelLogger.WriteLog($"VIEWMODEL LOGGER FOR VM {this.GetType().Name} HAS BEEN STARTED OK!", LogType.InfoLog);
            ViewModelLogger.WriteLog("SETTING UP DLL VALUE BOUND VALUES NOW...", LogType.WarnLog);
            ViewModelLogger.WriteLog($"USING J2534 VERSIONS: {string.Join(",", _selectedVersions.Select(VerObj => VerObj.ToDescriptionString()))}", LogType.InfoLog);

            // Get a list of all the current DLLs in our system first.
            this.ImportHelper = new PassThruImportDLLs();
            ViewModelLogger.WriteLog("BUILDING NEW PASSTHRU DLL IMPORT HELPER INSTANCE NOW...", LogType.InfoLog);
        }
    }
}
