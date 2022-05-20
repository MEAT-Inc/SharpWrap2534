using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SharpWrap2534;
using SharpWrap2534.J2534Objects;
using SharpWrap2534.PassThruTypes;

namespace SharpSimulator
{
    /// <summary>
    /// Event args for a new simulation message being processed and responded to
    /// </summary>
    public class SimMessageEventArgs : EventArgs
    {
        // Event objects for this event
        public readonly Sharp2534Session Session;       // Controlling Session
        public readonly J2534Device SessionDevice;      // Device controlled by the session
        public readonly J2534Channel SessionChannel;    // Channel being controlled for this simulation

        // Messages processed by our sim event
        public readonly bool ResponsePassed;
        public readonly PassThruStructs.PassThruMsg ReadMessage;
        public readonly PassThruStructs.PassThruMsg[] Responses;

        // ------------------------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Builds a new event argument helper for this session
        /// </summary>
        /// <param name="InputSession">Session to process</param>
        /// <param name="MessageRead">Message read</param>
        /// <param name="MessagesReplied">Responses sent out</param>
        public SimMessageEventArgs(Sharp2534Session InputSession, bool ResponseSent, PassThruStructs.PassThruMsg MessageRead, PassThruStructs.PassThruMsg[] MessagesReplied)
        {
            // Store session objects here
            this.Session = InputSession;
            this.SessionDevice = this.Session.JDeviceInstance;
            this.SessionChannel = this.SessionDevice.DeviceChannels.First(ChObj => ChObj.ChannelId != 0);

            // Store Messages here
            this.ResponsePassed = ResponseSent;
            this.ReadMessage = MessageRead;
            this.Responses = MessagesReplied;
        }
    }
}
