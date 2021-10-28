using System;
using System.Collections.Generic;
using System.IO;
using JBoxInvoker;
using JBoxInvoker.PassThruLogic.J2534Api;
using JBoxInvoker.PassThruLogic.SupportingLogic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NUnit;
using NUnit.Framework;
using Assert = Microsoft.VisualStudio.TestTools.UnitTesting.Assert;

namespace JBoxInvoker___Tests
{
    [TestClass]
    public class PassThruImportTests
    {
        // Split output string value.
        private static readonly string SepString = "------------------------------------------------------";

        [TestMethod]
        [TestCategory("J2534 Instance")]
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
                    var JDllImporter = new PassThruImporter(PTPath.ToDescriptionString());

                    // Log Results.
                    Console.WriteLine($"    --> DLL Path: {JDllImporter.JDllPath}");
                    Console.WriteLine($"    --> DLL Pointer: {JDllImporter.ModulePointer}");
                    Console.WriteLine($"    --> Setup new DLL Loader OK!");

                    // Add into list of bools.
                    ResultsList.Add(new (true, null));
                }
                catch (Exception LoadEx)
                {
                    // Add failure results.
                    Console.WriteLine($"    --> FAILED TO LOAD DLL: {PTPath}!");
                    ResultsList.Add(new (false, LoadEx));
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
