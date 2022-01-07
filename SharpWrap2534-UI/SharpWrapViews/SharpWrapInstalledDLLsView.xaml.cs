using System.Linq;
using System.Windows;
using System.Windows.Controls;
using SharpLogger;
using SharpLogger.LoggerObjects;
using SharpLogger.LoggerSupport;
using SharpWrap2534.SupportingLogic;
using SharpWrap2534_UI.SharpWrapViewModels;

namespace SharpWrap2534_UI.SharpWrapViews
{
    /// <summary>
    /// Interaction logic for UserControl1.xaml
    /// </summary>
    public partial class SharpWrapInstalledDLLsView : UserControl
    {
        // Logger object.
        private SubServiceLogger ViewLogger => (SubServiceLogger)LogBroker.LoggerQueue.GetLoggers(LoggerActions.SubServiceLogger)
            .FirstOrDefault(LoggerObj => LoggerObj.LoggerName.StartsWith("InstalledDLLsViewLogger")) ?? new SubServiceLogger("InstalledDLLsViewLogger");

        // ViewModel object
        public SharpWrapInstalledDLLsViewModel ViewModel { get; set; }     // ViewModel object to bind onto

        // Values configured for the Versions of DLLs to show.

        // --------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Builds a new instance of a DLL view for our output content
        /// </summary>
        public SharpWrapInstalledDLLsView(JVersion? VersionSet = null)
        {
            // Find our version type based on input value
            InitializeComponent();
            string VersionString = VersionSet != null ? 
                VersionSet.ToString() : 
                "ALL_VERSIONS";

            // Now build a new viewmodel from the given versions
            this.ViewModel = new SharpWrapInstalledDLLsViewModel(VersionString);
        }

        /// <summary>
        /// On loaded, we want to setup our new viewmodel object and populate values
        /// </summary>
        /// <param name="sender">Sending object</param>
        /// <param name="e">Events attached to it.</param>
        private void SharpWrapInstalledDLLsView_OnLoaded(object sender, RoutedEventArgs e)
        {
            // Setup a new ViewModel
            this.ViewModel.SetupViewControl(this);
            this.DataContext = this.ViewModel;

            // Log setup completed correctly
            this.ViewLogger.WriteLog($"CONFIGURED VIEW CONTROL VALUES FOR {this.GetType().Name} OK!", LogType.InfoLog);
        }

        // --------------------------------------------------------------------------------------------------------------------------
    }
}
