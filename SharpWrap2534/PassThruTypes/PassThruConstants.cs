using System.Runtime.CompilerServices;
using SharpWrap2534.SupportingLogic;

[assembly: InternalsVisibleTo("JBoxInvokerTests")]
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

        // JVersion.
        public JVersion Version { get; private set; }

        // Max Device Instances
        public int MaxDeviceCount => 2;

        // Channel Configurations
        public uint MaxChannels => (uint)(Version == JVersion.V0404 ? 2 : 4);
        public uint MaxChannelsLogical => (uint)(Version == JVersion.V0404 ? 0 : 10); 
        public readonly uint MaxFilters = 10;
        public readonly uint MaxPeriodicMsgs = 10;

        // --------------------------------------------------------------------------------------

        /// <summary>
        /// Builds a new set of PassThru constants based on the JVersion
        /// </summary>
        /// <param name="J2524Version">Version of the API</param>
        public PassThruConstants(JVersion J2524Version) { Version = J2524Version; }
    }
}
