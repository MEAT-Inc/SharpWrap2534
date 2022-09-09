﻿using System;
using System.Linq;
using SharpWrapper;
using SharpWrapper.J2534Objects;


namespace SharpSimulator.SimulationEvents
{
    /// <summary>
    /// Fired off when a new session creates a simulation channel object
    /// </summary>
    public class SimChannelEventArgs : EventArgs
    {
        // Event objects for this event
        public readonly Sharp2534Session Session;       // Controlling Session
        public readonly J2534Device SessionDevice;      // Device controlled by the session
        public readonly J2534Channel SessionChannel;    // Channel being controlled for this simulation

        // ------------------------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Pulls in the current session instance and stores values for this event onto our class
        /// </summary>
        /// <param name="InputSession"></param>
        public SimChannelEventArgs(Sharp2534Session InputSession)
        {
            // Store session objects here
            this.Session = InputSession;
            this.SessionDevice = this.Session.JDeviceInstance;
            this.SessionChannel = this.SessionDevice.DeviceChannels.First(ChObj => ChObj.ChannelId != 0);
        }
    }
}
