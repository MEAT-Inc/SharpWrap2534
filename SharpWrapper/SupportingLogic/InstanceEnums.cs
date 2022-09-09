using System.ComponentModel;

namespace SharpWrapper.SupportingLogic
{
    // Enum types for standard DLL values.
    public enum PassThruPaths
    {
        // CDP3
        [Description("C:\\Program Files (x86)\\Drew Technologies, Inc\\J2534\\CarDAQ Plus 3\\cardaqplus3_0404_32.dll")]
        CarDAQPlus3_0404 = 0x10,
        [Description("C:\\Program Files (x86)\\Drew Technologies, Inc\\J2534\\CarDAQ Plus 3\\0500\\cardaqplus3_0500_32.dll")]
        CarDAQPlus3_0500 = 0x11,

        // CDP4
        [Description("C:\\Program Files (x86)\\Drew Technologies, Inc\\J2534\\CarDAQ Plus 4\\cardaqplus4_0404_32.dll")]
        CarDAQPlus4_0404 = 0x10,
        [Description("C:\\Program Files (x86)\\Drew Technologies, Inc\\J2534\\CarDAQ Plus 4\\0500\\cardaqplus4_0500_32.dll")]
        CarDAQPlus4_0500 = 0x11,
    }

    // Enums for J2534 Version
    public enum JVersion
    {
        [Description("Version 04.04")] V0404,
        [Description("Version 05.00")] V0500,
        [Description("Any Version")] ALL_VERSIONS,      // Added 2/21/22 - Used for universal searching
    }

    // Enums to describe the status for the instance.
    public enum PTInstanceStatus
    {
        [Description("Not Configured")] NULL,
        [Description("Instance Loaded")] INITIALIZED,
        [Description("Freed")] FREED,
    }
}
