using System.Runtime.CompilerServices;
using SharpWrap2534.SupportingLogic;

namespace SharpWrap2534.PassThruTypes
{
    /// <summary>
    /// Contains constants for PassThru types here.
    /// </summary>
    public class PassThruConstants
    {
        // Registry Keys for PassThruSupport.
        public static readonly string V0404_PASSTHRU_REGISTRY_PATH = "Software\\PassThruSupport.04.04";
        public static readonly string V0404_PASSTHRU_REGISTRY_PATH_6432 = "Software\\Wow6432Node\\PassThruSupport.04.04";

        // Registry Keys for PassThruSupport.
        public static readonly string V0500_PASSTHRU_REGISTRY_PATH = "Software\\PassThruSupport.05.00";
        public static readonly string V0500_PASSTHRU_REGISTRY_PATH_6432 = "Software\\Wow6432Node\\PassThruSupport.05.00";

        // JVersion and Other helper methods
        public JVersion Version { get; private set; }
        public readonly int MaxDeviceCount;

        // Filter Configurations
        internal readonly uint MaxFilters;
        internal readonly uint MaxPeriodicMsgs;

        // Channel Configurations
        internal readonly uint MaxChannels;
        internal readonly uint MaxChannelsLogical;

        // --------------------------------------------------------------------------------------

        /// <summary>
        /// Builds a new set of PassThru constants based on the JVersion
        /// </summary>
        /// <param name="J2524Version">Version of the API</param>
        public PassThruConstants(JVersion J2524Version)
        {
            // Set max device count
            this.MaxDeviceCount = 10;

            // Set filters and periodic messages and version
            this.MaxFilters = 10;
            this.MaxPeriodicMsgs = 10;
            this.Version = J2524Version;

            // Set Channels and Logical Channels
            this.MaxChannels = (uint)(Version == JVersion.V0404 ? 2 : 4);
            this.MaxChannelsLogical = (uint)(Version == JVersion.V0404 ? 0 : 10);
        }

        /// <summary>
        /// Sets a new Version for our instance
        /// </summary>
        /// <param name="J2534Version">Version to set</param>
        public void SetVersion(JVersion J2534Version) { this.Version = J2534Version; }
    }
}
