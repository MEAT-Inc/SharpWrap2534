using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace JBoxInvoker
{
    /// <summary>
    /// Impors a provided DLL file and maps functions out for the PassThru calls for it. 
    /// This can take any standard V0404 J2534 DLL input and provides basic interfacing for all the 
    /// DLLs native calls.
    /// </summary>
    public class PassThruImporter
    {
        // Class values for the DLL to import.
        public string JDllPath;
        public Assembly JDllAssembly;

        // -----------------------------------------------------------------------------

        /// <summary>
        /// Imports a new JDLL into the project and stores all of its outputs.
        /// </summary>
        /// <param name="DllPath"></param>
        public PassThruImporter(string DllPath)
        {

        }
    }
}
