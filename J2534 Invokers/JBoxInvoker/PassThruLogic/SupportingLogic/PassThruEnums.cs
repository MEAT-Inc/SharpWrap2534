using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JBoxInvoker.PassThruLogic.SupportingLogic
{
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

    // Enums for J2534 Version
    public enum JVersion
    {
        [Description("Version 0.404")] V0404,
        [Description("Version 0.500")] V0500,
    }

    // Enums to describe which PT Device is in use.
    public enum JDeviceNumber
    {
        [Description("Device #1")] PTDevice1,
        [Description("Device #2")] PTDevice2,
    }

    // Enums to describe the status for the instance.
    public enum PTInstanceStatus
    {
        [Description("Not Configured")] NULL,
        [Description("DLL Loaded")] INITIALIZED,
        [Description("Freed")] FREED,
    }
}
