# **SharpWrap2534 - The Ultimate J2534 Wrapper**

## **What is SharpWrap2534?**
SharpWrap2534 is an approach at trying to build and deploy and easily consumable, user friendly, J2534 API wrapper in C#. While there's a good number of these out there, SharpWrap stands out from the rest for a number of reasons. Some of the key features of SharpWrap are:
-  Both Version 04.04 and Version 05.00 APIs are supported completely!
-  All J2534 calls are setup in the background leaving the user with minimal configuration needed to consume this library
-  Setting up a new instance of a J2534 device can be done in **ONE LINE OF CODE**. 
   -  All configuration of the J2534 Device and DLL objects are done when a new SharpSession is started. 
   -  When a SharpSession is built, a DLL is built based on the name provided and then the next possible device is automatically loaded up as well.
- Any exceptions thrown inside the J2534 instances are always returned out to the user for easy debugging.
-  A total of (2) two device instances can be generated at one time. By forcing this design pattern, it's nearly impossible to try and build instances of devices which are impossible. 
   -  Like the device instances, only a set number of J2534 Channels can exist at a time to prevent losing track of them. 
   -  If Logical channels are needed, then they are built as well.
-  Supports the hidden DrewTech API for device locating and initalization processes 
   -   These methods include the PTGetNextDevice() PTGetNextCarDAQ() PTScanForDevice() and more.
   -   Each of these methods are mapped using their pointer delegates for unmanaged access and when called, are passed through an API Marshall to convert them into managed types.
  
---

## **Installing SharpWrap**
- SharpWrap can either be manually imported into a project using the DLLs (and debugging symbols for best results) under the releases page. Or, the package is published on Nuget for easy updates and other support.

    ### Install from PackageManager CLI
  - From the Nuget PackageManager CLI or other Nuget interface, run the following command.  The package itself does not have any third party depenednecies so version conflicts should't be an issue any any point.
    
    ` dotnet add PROJECT package SharpWrap2534 --version 1.1.3.104 `

--- 

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

---

### **Development Setup**
- If you're looking to help develop this project, you'll need to add the NuGet server for the MEAT Inc workspace into your nuget configuration. 
- To do so, navigate to your AppData\Roaming folder (You can do this by opening windows explorer and clicking the top path bar and typing %appdata%)
- Now find the folder named NuGet and open the file named NuGet.config
- Inside this file, under packageSources, you need to add a new source. Insert the following line into here 
     ```XML 
      <add key="MEAT-Inc" value="https://nuget.pkg.github.com/MEAT-Inc/index.json/" protocolVersion="3" />
    ```
- Once added in, scroll down to packageSourceCredentials (if it's not there, just make a new section for it)
- Inside this section, put the following block of code into it.
   ```XML
    <MEAT-Inc>
       <add key="Username" value="meatincreporting" />
       <add key="ClearTextPassword" value="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" />
    </MEAT-Inc>
    ```
 - Once added in, save this file and close it out. 
 - Your NuGet.config should look something like this. This will allow you to access the packages inside the MEAT Inc repo/workspaces to be able to build the solution.
    ```XML
      <?xml version="1.0" encoding="utf-8"?>
          <configuration>
              <packageSources>
                  <add key="nuget.org" value="https://api.nuget.org/v3/index.json" protocolVersion="3" />
                  <add key="MEAT-Inc" value="https://nuget.pkg.github.com/MEAT-Inc/index.json/" protocolVersion="3" />
              </packageSources>
              <packageSourceCredentials>
                  <MEAT-Inc>
                      <add key="Username" value="meatincreporting" />
                      <add key="ClearTextPassword" value="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" />
                  </MEAT-Inc>
              </packageSourceCredentials>
              <packageRestore>
                  <add key="enabled" value="True" />
                  <add key="automatic" value="True" />
              </packageRestore>
              <bindingRedirects>
                  <add key="skip" value="False" />
              </bindingRedirects>
              <packageManagement>
                  <add key="format" value="1" />
                  <add key="disabled" value="True" />
              </packageManagement>
          </configuration> 

---

### Questions, Comments, Concerns? 
- I don't wanna hear it...
- But feel free to send an email to zack.walsh@meatinc.autos. He might feel like being generous sometimes...
- Or if you're feeling like a good little nerd, make an issue on this repo's project and I'll take a peek at it.

