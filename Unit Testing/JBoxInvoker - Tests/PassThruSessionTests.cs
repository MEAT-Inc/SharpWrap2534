using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NUnit;
using NUnit.Framework;

namespace JBoxInvokerTests
{
    [TestClass]
    public class PassThruSessionTests
    {
        [TestMethod]
        public void BuildNewJ2534Session()
        {
            // Builds a new J2534 Session object using a CarDAQ Plus 3 DLL.
            var JSessionInstance = new J25334Session();
        }
    }
}
