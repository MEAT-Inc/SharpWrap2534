using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

[assembly: InternalsVisibleTo("JBoxInvokerTests")]
namespace JBoxInvoker.PassThruTypes
{
    /// <summary>
    /// Exception object from a J2534 Call
    /// </summary>
    internal class PassThruException : Exception
    {
        // Error Code and string value.
        public J2534Err J2534ErrorCode;
        public string LastErrorString = "";

        // ------------------------------------- CLASS CREATION OBJECTS AND METHODS -------------------------------------

        /// <summary>
        /// Builds a new J2534 CTOR object.
        /// </summary>
        public PassThruException() { }
        /// <summary>
        /// Builds a new JException based on an error code.
        /// </summary>
        /// <param name="code"></param>
        public PassThruException(J2534Err JErrorCode) { J2534ErrorCode = JErrorCode; }
        /// <summary>
        /// Builds a new JException based on an error code and string.
        /// </summary>
        /// <param name="JErrorCode">Error fro the method</param>
        /// <param name="LastErrorCode">Last error thrown from the Exception</param>
        public PassThruException(J2534Err JErrorCode, StringBuilder LastErrorCode)
        {
            J2534ErrorCode = JErrorCode;
            LastErrorString = LastErrorCode.ToString();
        }
        /// <summary>
        /// Builds a new Exception from a message and error code.
        /// </summary>
        /// <param name="ErrorMessage">Error thrown message</param>
        /// <param name="JErrorCode">J2534 Exception code</param>
        public PassThruException(string ErrorMessage, J2534Err JErrorCode) : base(ErrorMessage) { J2534ErrorCode = JErrorCode; }
        /// <summary>
        /// Builds a new Exception from a message and error code.
        /// </summary>
        /// <param name="ErrorMessage">Error thrown message</param>
        /// <param name="JErrorCode">J2534 Exception code</param>
        /// <param name="InnerException">Inner exception thrown running</param>
        public PassThruException(string ErrorMessage, Exception InnerException, J2534Err JErrorCode) : base(ErrorMessage, InnerException)
        {
            // Store code value.
            J2534ErrorCode = JErrorCode;
        }

        // ------------------------------------- STRING CONVERSIONS FOR ERROR OBJECTS ----------------------------------

        /// <summary>
        /// Override the exception to string.
        /// </summary>
        /// <returns>String version of the exception</returns>
        public override string ToString()
        {
            // Return string value output here.
            return base.ToString();
        }
        /// <summary>
        /// Gets a simple output string for this error object.
        /// </summary>
        /// <returns>Simple description</returns>
        public string SimpleDescription() { return TargetSite.Name + " " + J2534ErrorCode; }
    }
}
