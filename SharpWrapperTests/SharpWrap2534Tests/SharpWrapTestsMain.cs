using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using SharpWrap2534;
using SharpWrap2534.PassThruTypes;

namespace SharpWrap2534Tests
{
    /// <summary>
    /// Main testing class for our SharpWrapper project
    /// </summary>
    public class SharpWrapTestsMain
    {
        /// <summary>
        /// Main entry point for testing
        /// </summary>
        /// <param name="args"></param>
        public static void Main(string[] args)
        {
            // Test our execution routines here
            if (!JsonConvertTests.ExecuteTests()) 
                throw new InvalidOperationException("JSON CONVERSION ROUTINES FAILED!");
        }
    }
}
