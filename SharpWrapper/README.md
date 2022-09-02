## **Using a New SharpSession**
- Running a new SharpSession is as simple as calling a new instance of a SharpJ2534Session and building it using a JVersion value, and the name of the DLL you wish to pick.
- Building these sessions can be run any number of times as long as you do not go over the max number of device instances allowed as defined in the PassThruConstants. These values are static and defined by the SAE Docs.
- The code snipped below shows how this is done for a DrewTech CarDAQ-Plus 3
  
    ``` csharp
    // Test class for testing a SharpJ2534Session
    public class SharpSessionTest
    {
        // Main class entry point.
        public static void Main(string[] args)
        {
            // Builds a new J2534 Session object using a CarDAQ Plus 3 DLL.
            var SharpSession = new Sharp2534Session(JVersion.V0404, "CarDAQ-Plus 3");

            // Once the instance is built, the device begins in a closed state. 
            // To open it, simply run PTOpen command on the instance.
            SharpSession.PTOpen();

            // Once open, you can call the method ToDetailedString().
            // This call builds a massive output string that contains detailed information on 
            // the DLL and device objects built.
            Console.WriteLine(SharpSession.ToDetailedString());
    
            // Once the Session exists, connecting to a channel is as simple as issuing the 
            // PTConnect call from the session.
            // Once a channel is opened, you can send messages on it by calling the index of it.
            var OpenedChannel = SharpSession.PTConnect(0, ProtocolId.ISO15765, 0x00, 500000);
            OpenedChannel.ClearTxBuffer();
            OpenedChannel.ClearRxBuffer();

            // Then once done with a channel, issue a PTDisconnect on the index provided.
            // When done with the session object, issue a PTClose to clean up the device.
            SharpSession.PTDisconnect(0);
            SharpSession.PTClose();
        }
    }
    ```

- This is a sample output for the SharpSession.ToDetailedString() call on a J2534 V04.04 CarDAQ-Plus 3 device.
- The output will reflect the instance of the Device, DLL, and any open channel values when this is called.

        ------------------------------------------------------------------------------------
        J2534 DLL: CarDAQ-Plus 3 (Version 04.04)
        --> DLL Information:
            \__ Version: Version 04.04
            \__ DLL Vendor: Drew Technologies Inc.
            \__ DLL Long Name: Drew Technologies Inc. - CarDAQ-Plus 3
            \__ DLL Function Library: C:\Program Files (x86)\Drew Technologies, Inc\J2534\CarDAQ Plus 3\cardaqplus3_0404_32.dll
            \__ DLL Supported Protocols: 21
        
        Device: CarDAQ-Plus 3 #011534 (Version 04.04)
        --> Instance Information: 
            \__ Max Devices:    2 Device instances
            \__ Device Id:      2
            \__ Device Name:    CarDAQ-Plus 3 #011534
            \__ Device Version: Version 04.04
            \__ Device Status:  NOT OPEN AND NOT CONNECTED
        --> Device Setup Information:
            \__ DLL Version:    CarDAQ-Plus 3 J2534 Library v1.0.42.0
            \__ FW Version:     CarDAQ-Plus 3 FW:0.1.0.22 BL:0.1.0.7 SN: CMTHI0000011534D
            \__ API Version:    04.04
        --> Device Channel Information:
            \__ Channel Count:  2 Channels
            \__ Logical Chan:   NOT SUPPORTED!
            \__ Logical Count:  0 Logical Channels on each physical channel
            \__ Filter Count:   20 Filters Max across (Evenly Split On All Channels)
            \__ Periodic Count: 20 Periodic Msgs Max (Evenly Split On All Channels)
        ------------------------------------------------------------------------------------
