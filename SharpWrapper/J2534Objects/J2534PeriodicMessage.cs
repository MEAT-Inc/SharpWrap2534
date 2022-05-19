using System;
using System.Linq;
using System.Runtime.CompilerServices;
using SharpWrap2534.PassThruTypes;
using SharpWrap2534.SupportingLogic;

namespace SharpWrap2534.J2534Objects
{
    /// <summary>
    /// Holds information about a J2534 Periodic Message object.
    /// </summary>
    public class J2534PeriodicMessage : IComparable
    {
        // Message Status.
        public PTInstanceStatus MessageStatus;

        // Message values.
        public uint MessageId;
        public uint SendInterval;
        public PassThruStructs.PassThruMsg Message;

        // ---------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Builds and empty PTPeriodic Message object.
        /// </summary>
        public J2534PeriodicMessage() { MessageStatus = PTInstanceStatus.NULL; }
        /// <summary>
        /// Builds a new message.
        /// </summary>
        /// <param name="Message">Msg to send</param>
        /// <param name="SendInterval">Time between sends</param>
        /// <param name="MessageId">ID of the built message</param>
        internal J2534PeriodicMessage(PassThruStructs.PassThruMsg Message, uint SendInterval, uint MessageId = 0)
        {
            // Set values and status.
            this.Message = Message;
            this.SendInterval = SendInterval;
            this.MessageId = MessageId;

            // Set Status
            MessageStatus = PTInstanceStatus.INITIALIZED;
        }

        // ----------------------------------- OVERRIDES FOR STRING AND COMPARISON ----------------------------------------

        /// <summary>
        /// Builds a string of the filter info.
        /// </summary>
        /// <returns>String built version of the filter.</returns>
        public override string ToString()
        {
            // Build string to convert with.
            string BytesAsData = string.Join(" ", Message.Data.Select(ByteObj => "0x" + ByteObj.ToString("0:x2")));
            string OutputString = $"MessageId: {MessageId} -- Send Interval: {SendInterval}ms -- Message Data: {BytesAsData}";

            // Return string built.
            return OutputString;
        }
        /// <summary>
        /// Builds output string containing just the message values.
        /// </summary>
        /// <returns>String of message data.</returns>
        public string ToMessageDataString()
        {
            // Build and return string.
            return $"MessageData: {string.Join(" ", Message.Data.Select(ByteObj => "0x" + ByteObj.ToString("0:x2")))}";
        }
        /// <summary>
        /// Compares two filter values.
        /// </summary>
        /// <param name="FilterObj">Filter to compare.</param>
        /// <returns></returns>
        public int CompareTo(object FilterObj)
        {
            // Make sure the type is correctly setup
            if (FilterObj.GetType() != typeof(J2534PeriodicMessage))
                throw new InvalidCastException($"Can not convert a type of {FilterObj.GetType().Name} to a J2534 Periodic Message!");

            // Compare here.
            J2534PeriodicMessage CastFilter = (J2534PeriodicMessage)FilterObj;
            return string.Compare(ToString(), CastFilter.ToString());
        }
    }
}
