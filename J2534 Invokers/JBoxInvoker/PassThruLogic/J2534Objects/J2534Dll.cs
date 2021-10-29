using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using JBoxInvoker.PassThruLogic.PassThruTypes;
using JBoxInvoker.PassThruLogic.SupportingLogic;

// For comparing name values
using static System.String;

namespace JBoxInvoker.PassThruLogic.J2534Objects
{
    public class J2534Dll : IComparable
    {
        // DLL Version.
        public JVersion DllVersion { get; private set; }

        // DLL Class values.
        public string Name { get; private set; }
        public string LongName { get; private set; }
        public string FunctionLibrary { get; private set; }
        public string Vendor { get; private set; }
        public List<ProtocolId> SupportedProtocols = new List<ProtocolId>();

        // --------------------- DLL OBJECT CTOR AND OVERLOAD VALUES -------------------

        /// <summary>
        /// Builds a new instance of a J2534 DLL
        /// </summary>
        /// <param name="NameOfDLL"></param>
        public J2534Dll(string NameOfDLL) { LongName = NameOfDLL; }
        public override string ToString() { return Name; }
        /// <summary>
        /// Useful for comparing DLL Types in a combobox/array
        /// </summary>
        public int CompareTo(object DLLAsObject)
        {
            J2534Dll DllObj = (J2534Dll)DLLAsObject;
            return CompareOrdinal(this.Name, DllObj.Name);
        }
    }
}
