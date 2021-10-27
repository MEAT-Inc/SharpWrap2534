using System;
using System.Collections.Generic;
using System.ComponentModel;
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

        // Enum types for standard DLL values.
        public enum PassThruPaths
        {
            // CDP3
            [Description(@"C:\Program Files (x86)\Drew Technologies, Inc\J2534\CarDAQ Plus 3\cardaqplus3_0404_32.dll")]
            CarDAQPlus3_0404 = 0x10,
            [Description(@"C:\Program Files (x86)\Drew Technologies, Inc\J2534\CarDAQ Plus 3\0500\cardaqplus3_0500_32.dll")]
            CarDAQPlus3_0500 = 0x11,

            // CDP4
            [Description(@"C:\Program Files (x86)\Drew Technologies, Inc\J2534\CarDAQ Plus 4\cardaqplus4_0404_32.dll")]
            CarDAQPlus4_0404 = 0x21,           
            [Description(@"C:\Program Files (x86)\Drew Technologies, Inc\J2534\CarDAQ Plus 4\0500\cardaqplus4_0500_32.dll")]
            CarDAQPlus4_0500 = 0x22,
        }

        // ---------------------------------------------------------------------------------------------

        /// <summary>
        /// Imports a new JDLL into the project and stores all of its outputs.
        /// </summary>
        /// <param name="DllPath"></param>
        public PassThruImporter(string DllPath)
        {
            // Store the DLL path ehre and import the path as an assy.
            this.JDllPath = DllPath;
            this.JDllAssembly = Assembly.LoadFile(this.JDllPath);
        }
        /// <summary>
        /// Imports a new JDLL into the project and stores all of its outputs.
        /// </summary>
        /// <param name="DllType">Enum type fo the DLL to import.</param>
        public PassThruImporter(PassThruPaths DllType)
        {
            // Store DLL path and import as an assy. 
            this.JDllPath = DllType.ToDescriptionString();
            this.JDllAssembly = Assembly.LoadFile(this.JDllPath);
        }
    }
}
