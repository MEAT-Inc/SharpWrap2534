using System;
using System.Threading;

namespace SharpWrapperTests.TestHelpers
{
    /// <summary>
    /// Helper class which is used to invoke a window instance during a unit test
    /// </summary>
    /// <typeparam name="TWindowType">The type of window being passed into this helper class</typeparam>
    internal class WindowInvoker<TWindowType> where TWindowType : Window
    {
        #region Custom Events

        /// <summary>
        /// Event handler to run when the test window instance is closed out
        /// </summary>
        /// <param name="SendingObject">The window sending this request</param>
        /// <param name="EventArgs">Event args associated with this request</param>
        /// <exception cref="InvalidOperationException">Thrown when thread aborting fails for the window</exception>
        private void _testWindowOnClosed(object SendingObject, EventArgs EventArgs)
        {
            // Invoke the shutdown routine on the sending window
            if (SendingObject is not Window SendingWindow)
            {
                // Store the window type and throw an exception 
                string SendingType = SendingObject.GetType().Name;
                string ExceptionMessage = $"Error! Sending object was of type {SendingType} but should have been a Window!";

                // Throw the built exception for this failure
                throw new TypeAccessException(ExceptionMessage);
            }

            // Invoke the shutdown routine for our sending object/window and exit out
            this._testWindow = null;
            SendingWindow.Dispatcher.InvokeShutdown();
        }

        #endregion //Custom Events

        #region Fields

        // Private backing fields for the test window helper
        private TWindowType _testWindow;              // The window object being shown with this helper class
        private Thread _testWindowThread;             // The thread which owns the window being shown
        private readonly object[] _testWindowArgs;    // The arguments used to spawn a new instance of our window

        #endregion //Fields

        #region Properties

        // Public facing properties for our test window helper
        public bool IsWindowOpen => this._testWindow != null;
        public bool IsTestRunning => this.IsWindowOpen || this._testWindowThread?.ThreadState == ThreadState.Running;     

        #endregion //Properties

        #region Structs and Classes
        #endregion //Structs and Classes

        // ------------------------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Builds a new Test window helper which is used to show windows during a unit test
        /// </summary>
        /// <param name="ShowTestWindow">When true, the window is built and shown once opened</param>
        /// <param name="WindowCtorArgs">Arguments needed to build this new window type for a testing routine</param>
        public WindowInvoker(bool ShowTestWindow, params object[] WindowCtorArgs)
        {
            // Store window CTOR args and show it if we want to now. Otherwise exit out 
            this._testWindowArgs = WindowCtorArgs;
            if (ShowTestWindow) this.ShowTestWindow();
        }

        // ------------------------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Builds a new thread to show our window as window instance which blocks further operation until it is closed
        /// </summary>
        /// <param name="ShowAsDialog">When true, the window is shown as a blocking dialog instance</param>
        /// <returns>True if the window is opened, false if it is not</returns>
        public bool ShowTestWindow(bool ShowAsDialog = true)
        {
            // If tests are running, abort them and build a new input window
            if (this.IsTestRunning) this._testWindow.Close();
            this._testWindowThread = new Thread(() =>
            {
                // Spawn in a new instance of the requested window here to show on our UI
                this._testWindow = (TWindowType)Activator.CreateInstance(
                    typeof(TWindowType), 
                    this._testWindowArgs
                );

                // Open the window as a dialog or as a background window based on input parameters
                this._testWindow.Closed += this._testWindowOnClosed;
                if (ShowAsDialog) this._testWindow.ShowDialog();
                else this._testWindow.Show();
                
                // Enable message pumping on the dispatcher so we can run this window on the main thread
                if (!Dispatcher.CurrentDispatcher.HasShutdownFinished) Dispatcher.Run();
            });

            // Configure the window thread to allow us to show it from a test class
            this._testWindowThread.SetApartmentState(ApartmentState.STA);
            this._testWindowThread.Start(); this._testWindowThread.Join();

            // Once the window is shown, return out of this routine
            return ShowAsDialog || this._testWindowThread.IsAlive;
        }
        /// <summary>
        /// Invokes a new action on a window and returns the value of the method invoked
        /// </summary>
        /// <typeparam name="TWindowType">The type of window being invoked for this method</typeparam>
        /// <param name="WindowAction">The action body being run on the window instance</param>
        /// <returns>True if this action is invoked correctly, false if it is not</returns>
        public bool InvokeOnWindow(Action<TWindowType> WindowAction)
        {
            try
            {
                // If our window is not open at this point, force a new one to open up and invoke this method on it
                if (this.IsTestRunning) this._testWindow.Close();
                this._testWindowThread = new Thread(() =>
                {
                    // Spawn in a new instance of the requested window here to show on our UI
                    this._testWindow = (TWindowType)Activator.CreateInstance(
                        typeof(TWindowType),
                        this._testWindowArgs
                    );

                    // Open the window as a dialog or as a background window based on input parameters
                    this._testWindow.Closed += this._testWindowOnClosed;
                    WindowAction.Invoke(this._testWindow);
                    this._testWindow.ShowDialog();

                    // Enable message pumping on the dispatcher so we can run this window on the main thread
                    Dispatcher.Run();
                });

                // Configure the window thread to allow us to show it from a test class
                this._testWindowThread.SetApartmentState(ApartmentState.STA);
                this._testWindowThread.Start(); this._testWindowThread.Join();
                return true;
            }
            catch (Exception InvokeMethodEx)
            {
                // Catch the failure thrown during this routine and log it out.
                InvokeMethodEx.Data.Add("IsWindowOpen", this.IsWindowOpen);
                InvokeMethodEx.Data.Add("IsTestRunning", this.IsTestRunning);

                // Throw the exception with some extra data in it now
                throw InvokeMethodEx;
            }
        }
    }
}
