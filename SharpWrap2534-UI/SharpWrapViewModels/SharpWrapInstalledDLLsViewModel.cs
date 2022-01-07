using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SharpLogger;
using SharpLogger.LoggerObjects;
using SharpLogger.LoggerSupport;
using SharpWrap2534.J2534Objects;
using SharpWrap2534.PassThruImport;
using SharpWrap2534.SupportingLogic;

namespace SharpWrap2534_UI.SharpWrapViewModels
{
    /// <summary>
    /// View model for a list of SharpWrap DLL instances
    /// </summary>
    public class SharpWrapInstalledDLLsViewModel : SharpWrapViewModel
    {
        // Logger object.
        private static ConsoleLogger ViewModelLogger => (ConsoleLogger)LogBroker.LoggerQueue.GetLoggers(LoggerActions.ConsoleLogger)
            .FirstOrDefault(LoggerObj => LoggerObj.LoggerName.StartsWith("InstalledDLLsViewModelLogger")) ?? new ConsoleLogger("InstalledDLLsViewModelLogger");

        // --------------------------------------------------------------------------------------------------------------------------

        // Private control values
        private J2534Dll[] _locatedJ2534DLLs;
        private J2534Dll[] _selectedJ2534DLLs;
        private readonly JVersion[] _selectedVersions = { JVersion.V0404, JVersion.V0500 };

        // Public values for our view to bind to
        public PassThruImportDLLs DllImportHelper;     // DLL Importing helper object
        public J2534Dll[] LocatedJ2534DLLs { get => _locatedJ2534DLLs; set => OnPropertyChanged(); }        // Set of all DLLs
        public J2534Dll[] SelectedJ2534DLLs { get => _selectedJ2534DLLs; set => OnPropertyChanged(); }      // Set of DLLs for our versions
        
        // --------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Logic for building a new viewmodel when this view binding is built
        /// </summary>
        public SharpWrapInstalledDLLsViewModel(string VersionTypes = "ALL_VERSIONS")
        {
            // Find our version type set first.
            switch (VersionTypes)
            {
                // For BOTH v0.500 and v0.404
                case "ALL_VERSIONS":   
                    _selectedVersions = new[] { JVersion.V0404, JVersion.V0500 };
                    break;

                // For only v0.404
                case "V0404":
                    _selectedVersions = new[] { JVersion.V0404 };
                    break;

                // For only v0.500
                case "V0500":
                    _selectedVersions = new[] { JVersion.V0500 };
                    break;
            }

            // Log information and store values 
            ViewModelLogger.WriteLog($"VIEWMODEL LOGGER FOR VM {this.GetType().Name} HAS BEEN STARTED OK!", LogType.InfoLog);
            ViewModelLogger.WriteLog("SETTING UP DLL VALUE BOUND VALUES NOW...", LogType.WarnLog);
            ViewModelLogger.WriteLog($"USING J2534 VERSIONS: {string.Join(",", _selectedVersions.Select(VerObj => VerObj.ToDescriptionString()))}", LogType.InfoLog);

            // Get a list of all the current DLLs in our system first.
            this.DllImportHelper = new PassThruImportDLLs();
            this.LocatedJ2534DLLs = this.DllImportHelper.LocatedJ2534DLLs;
            string DllVersionString = string.Join(",", _selectedVersions.Select(VerObj => VerObj.ToDescriptionString()));
            this.SelectedJ2534DLLs = this.LocatedJ2534DLLs
                .Where(DllInstance => DllVersionString.Contains(DllInstance.DllVersion.ToString()))
                .ToArray();

            // Log built new DLL List correctly.
            ViewModelLogger.WriteLog("BUILT NEW PASSTHRU DLL IMPORT HELPER INSTANCE AND EXTRACTED DESIRED DLL INSTANCES!", LogType.InfoLog);
            ViewModelLogger.WriteLog("BINDING FOR VALUES HAS BEEN ESTABLISHED CORRECTLY! MOVING ON NOW...", LogType.InfoLog);
        }
    }
}
