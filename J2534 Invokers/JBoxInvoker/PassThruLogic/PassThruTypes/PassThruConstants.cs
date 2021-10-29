using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using JBoxInvoker.PassThruLogic.SupportingLogic;

namespace JBoxInvoker.PassThruLogic.PassThruTypes
{
    /// <summary>
    /// Contains constants for PassThru types here.
    /// </summary>
    public class PassThruConstants
    {
        // JVersion.
        public JVersion Version { get; private set; }

        // Channel Configurations
        public uint MaxChannels => (uint)(Version == JVersion.V0404 ? 2 : 10);
        public readonly uint MaxFilters = 10;
        public readonly uint MaxPeriodicMsgs = 10;

        // --------------------------------------------------------------------------------------

        /// <summary>
        /// Builds a new set of passthru constants based on the JVersion
        /// </summary>
        /// <param name="J2524Version">Version of the API</param>
        public PassThruConstants(JVersion J2524Version) { this.Version = J2524Version; }
    }
}
