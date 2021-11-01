using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using JBoxInvoker;
using JBoxInvoker.PassThruLogic.PassThruImport;
using JBoxInvoker.PassThruLogic.SupportingLogic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NUnit;
using NUnit.Framework;
using Assert = Microsoft.VisualStudio.TestTools.UnitTesting.Assert;

namespace JBoxInvokerTests
{
    /// <summary>
    /// Test class to configure DLL importing
    /// </summary>
    [TestClass]
    [TestCategory("J2534 Logic")]
    public class PassThruImportTests
    {
        // Split output string value.
        private static readonly string SepString = "------------------------------------------------------";

        [TestMethod]
        [TestCategory("J2534 DLL Import")]
        public void ImportUsingRegPath()
        {
            // Build new J2534 DLL from the import path.
            Console.WriteLine(SepString + "\nTests Running...\n");

            // Build DLL importing list.
            var DLLImporter = new PassThruImportDLLs();
            var ListOfDLLs = DLLImporter.LocatedJ2534DLLs;
            Assert.IsTrue(ListOfDLLs.Length != 0, "No DLLs were found on the system!");

            // Print infos out.
            Console.WriteLine("--> Built DLL List OK!");
            Console.WriteLine($"--> Found a total of {ListOfDLLs.Length} DLLs\n");

            // Print names of DLLs found.
            Console.WriteLine($"DLLs located are listed below");
            Console.WriteLine(string.Join("\n", ListOfDLLs.Select(DLlObj =>
            {
                // Build info string output for the DLL
                string DllString = $"   --> DLL #{ListOfDLLs.ToList().IndexOf(DLlObj) + 1}: " + DLlObj.Name;
                DllString += $" (Version { DLlObj.DllVersion.ToDescriptionString()})";
                return DllString;
            })));
            
            // Print the infos for the base ones.
            List<bool> ResultsList = new List<bool>();
            var PathsToLoop = Enum.GetValues(typeof(PassThruPaths));
            Console.WriteLine($"\n{SepString}\nLooping Basic DLLs now...\n");
            foreach (PassThruPaths PTPath in PathsToLoop)
            {
                // Find the DLL object for the current DLL
                Console.WriteLine($"Testing Path: {PTPath.ToDescriptionString()}");
                ResultsList.Add(PassThruImportDLLs.FindDllFromPath(PTPath, out var NextDLL));

                // Check to see if passed or not.
                if (!ResultsList.Last())
                {
                    Console.WriteLine("--> Failed to import DLL!");
                    Console.WriteLine("--> No Dll was returned from the import call!");

                    // Check if our file is real or not.
                    if (!File.Exists(PTPath.ToDescriptionString())) 
                        Console.WriteLine("--> The file specified at the path value given could not be found!");

                    // Print newline.
                    Console.WriteLine("");
                    continue;
                }

                // Print the DLL infos.
                Console.WriteLine("--> DLL Located OK!");
                Console.WriteLine("--> DLL Values generated are below");
                Console.WriteLine(NextDLL.ToDetailedString().Replace("J2534 DLL:", "    J2534 DLL:").Replace("\n", "\n    --> "));
                Console.WriteLine("");
            }

            // Write infos out to console
            Console.WriteLine(SepString);
            Console.WriteLine("\nTests completed without fatal exceptions!\n");

            // Print split line and check if passed.
            Console.WriteLine(SepString);
            Assert.IsTrue(ResultsList.TrueForAll(ResultSet => ResultSet));
        }

        [TestMethod]
        [TestCategory("J2534 DLL Import")]
        public void LoadJ2534ApisTest()
        {
            // Results for loading DLLs.
            List<(bool, Exception)> ResultsList = new List<(bool, Exception)>();
            var PathsToLoop = Enum.GetValues(typeof(PassThruPaths));

            // Loop all the PTPath Values.
            Console.WriteLine(SepString + "\nTests Running...\n");
            foreach (PassThruPaths PTPath in PathsToLoop)
            {
                try
                {
                    // Loads in the CDP3 DLL and returns.
                    Console.WriteLine($"Loading DLL for type: {PTPath}...");
                    var JDllImporter = new PassThruApiImporter(PTPath.ToDescriptionString());

                    // Log Results.
                    Console.WriteLine($"    --> DLL Path: {JDllImporter.JDllPath}");
                    Console.WriteLine($"    --> DLL Pointer: {JDllImporter.ModulePointer}");
                    Console.WriteLine($"    --> Setup new DLL Loader OK!");
                    
                    // Add into list of bools.
                    ResultsList.Add(new Tuple<bool, Exception>(true, null));
                }
                catch (Exception LoadEx)
                {
                    // Add failure results.
                    Console.WriteLine($"    --> FAILED TO LOAD DLL: {PTPath}!");
                    ResultsList.Add(new Tuple<bool, Exception>(false, LoadEx));
                }
            }

            // Loop all the results and print their outputs here.
            Console.WriteLine("\n" + SepString + "\nTest Results\n");
            for (int PathIndex = 0; PathIndex < PathsToLoop.Length; PathIndex++)
            {
                // Console Output.
                string DllPath = PathsToLoop.GetValue(PathIndex).ToString();
                var ResultSet = ResultsList[PathIndex];

                // Write infos out to console
                Console.Write($"--> DLL {DllPath}: ");
                Console.WriteLine(ResultSet.Item1
                    ? "Imported without issues!"
                    : $"Import Failed! Exception: {ResultSet.Item2.Message}");
            }

            // Write sep string.
            Console.WriteLine("\n" + SepString);
            Assert.IsTrue(ResultsList.TrueForAll(ResultSet => ResultSet.Item1));
        }
    }
}
