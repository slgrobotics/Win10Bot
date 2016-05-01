/*
this is a Universal Windows compliant port of the original Element (Serializer 3.0) .NET library
see:
    http://www.robotmarketplace.com/products/CM-ELEMENT.html
    http://www.robotmarketplace.com/products/images/CM-ELEMENT_guide.pdf
    http://www.roboticstomorrow.com/content.php?post_type=1791
    http://www.amazon.com/Element-GEN2-Robot-Controller/dp/B00EUB6XYM

This port is done to make a specific robot operate under Windows IoT platform, 
and serves as an example of particular Hardware connector implementation.

Original Element (Serializer 3.0) .NET library code is publically available, and is in public domain.
This port is assuming Apache License, Version 2.0 (the "License");
You may obtain a copy of the License at 
    http://www.apache.org/licenses/LICENSE-2.0

Ported by Sergei Grichine, 2015.
Distributed on an "AS IS" BASIS, * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
*/

using System;
using System.Collections.Generic;
using System.Diagnostics;
//using System.IO.Ports;
using System.Threading;
using cmRobot.Element.Components;
using cmRobot.Element.Internal;
using cmRobot.Element.Ids;

using cmRobot.Element.Sensors;

using slg.RobotExceptions;
using slg.RobotAbstraction;
using slg.RobotAbstraction.Ids;
using slg.RobotAbstraction.Events;
using slg.RobotAbstraction.Sensors;
using slg.RobotAbstraction.Drive;
using System.Threading.Tasks;
using cmRobot.Element.Controllers;

namespace cmRobot.Element
{
    // see https://github.com/dotMorten/NmeaParser/wiki/Using-in-a-Windows-Universal-App-(SerialPort) 

    /// <summary>
    /// Manages communication between <c>ElementComponent</c>s and 
    /// a cmRobot Element board.
    /// </summary>
    /// <include file='Docs\remarks.xml' path='/remarks/remarks[@name="Element"]'/>
    public class Element : IAbstractRobotHardware, IHardwareComponent
    {

		#region Constants

		/// <summary>
		/// The default value for the <c>AsyncCallbacksEnabled</c> property.
		/// </summary>
		public const bool AsyncCallbacksEnabledDefault = false;

		/// <summary>
		/// The default value for the <c>PortName</c> property.
		/// </summary>
		public const string PortNameDefault = "COM1";

		/// <summary>
		/// The default value for the <c>BaudRate</c> property.
		/// </summary>
		public const int BaudRateDefault = 19200;

        /// <summary>
        /// The default value for the <c>Units</c> property.
        /// </summary>
        public const Units UnitsDefault = Units.English;

        #endregion // Constants


        #region Ctors

        /// <summary>
        /// Initializes a new instance of the <c>Element</c> class.
        /// </summary>
        /// <seealso cref="ElementComponent"/>
        public Element()
		{
			Initialize();
		}

        #endregion // Ctors


        #region Public Properties

        /// <summary>
        /// Enables asynchronous event callbacks.  This allows events to be invoked
        /// on ThreadPool Threads.  Otherwise they are queued internally until
        /// PumpEvents() is invoked directly or implicitly when using the Run() loop.
        /// </summary>
        public bool AsyncCallbacksEnabled
		{
			get { return asyncCallbacks; }
			set { asyncCallbacks = value; }
		}

		/// <summary>
        /// The name of the serial port to use to communicate with the Element.
		/// </summary>
		public string PortName
		{
			get { return portName; }

			set 
			{
				if (communicating)
				{
					throw new InvalidOperationException(
                        "Cannot change PortName while communicating with the Element.");
				}

				portName = value; 
			}
		}

		/// <summary>
        /// The baud rate of the serial port to use to communicate with the Element.
		/// </summary>
		public int BaudRate
		{
			get { return baudRate; }

			set 
			{
				if (communicating)
				{
					throw new InvalidOperationException(
                        "Cannot change BaudRate while communicating with the Element.");
				}

				baudRate = value; 
			}
		}

        public CommunicationPortType PortType { get; set; }

        /// <summary>
        /// The units used to convert sonar, infrared, and thermopile sensor readings coming from the Element.
        /// </summary>
        public Units Units
        {
            get { return units; }
            set 
            {
                string cmd = String.Format("cfg units {0}", (int)value);
                commTask.EnqueueCommJob(Priority.High, cmd);
                units = value;
            }
        }

        #endregion // Public Properties


        #region Public Events

        /// <summary>
        /// Occurs when communication with the Element board has been established.
        /// </summary>
        public event HardwareComponentEventHandler CommunicationStarted;

        /// <summary>
        /// Occurs after the comm channel has been opened, but before communication
        /// is established with the Element board.  This allows you to initialize
        /// any serial devices, which require initialization strings to stream data in/out,
        /// such as some bluetooth devices (e.g. bluesmurf).
        /// </summary>
        public event CommunicationChannelEventHandler CommunicationsStarting;

		/// <summary>
        /// Occurs when communication with the Element board has been shutdown.
		/// </summary>
		public event HardwareComponentEventHandler CommunicationStopped;

        /// <summary>
        /// Occurs when communication with the Element board has been shutdown,
        /// but before the comm channel is closed.  So, you still have a chance to
        /// read/write bytes out, in case you need to do cleanup of a bluetooth
        /// device, etc.
        /// </summary>
        public event CommunicationChannelEventHandler CommunicationsStopping;

        #endregion // Public Events


        #region Public Methods

        /// <summary>
        /// Starts communication with the Element board.  All Element componenets
        /// will now begin performing their tasks.
        /// </summary>
        public async Task StartCommunication(CancellationTokenSource cts)
		{
			if (!communicating)
			{
                try { 
                    Debug.WriteLine("Element: Trying port: " + portName);
				    await commTask.Start(portName, baudRate, cts);
				    communicating = true;

                    //this.Units = Units.English;    // distance from sensors delivered in inches
                    this.Units = Units.Metric;       // distance from sensors delivered in centimeters
                    //this.BlinkLED(LedId.Led2, 20);

                    SignalCommunicationStarted();
                }
                catch (AggregateException aexc)
                {
                    throw new CommunicationException("Could not start communication");
                }
                catch (Exception exc)
                {
                    throw new CommunicationException("Could not start communication");
                }
            }
        }

        internal void StartingCommunication(ICommunicationChannel sp)
        {
            if (!communicating)
            {
                SignalCommunicationsStarting(sp);
            }
        }

        internal void StoppingCommunication(ICommunicationChannel sp)
        {
            if (communicating)
            {
                SignalCommunicationsStopping(sp);
            }
        }

		/// <summary>
        /// Stops all communication with the Element board. All Element componenets
		/// will now stop performing their tasks.
		/// </summary>
		public async Task StopCommunication()
		{
			if (communicating)
			{
				await commTask.Stop();
				communicating = false;

				SignalCommunicationStopped();
			}
		}

		/// <summary>
		/// Invokes all outstanding event callbacks on this thread.  Does nothing if
		/// AsyncCallbacksEnabled is set to true.  
		/// </summary>
		/// <remarks>
		/// This is handled automatically if the Run() method is used to establish a 
		/// run loop.  If Run() cannot be used (perhaps because the System.Windows.Forms 
		/// run loop is used instead), then PumpEvents() must be called manually.  In the 
		/// System.Windows.Forms example, invoking this from a System.Windows.Forms.Timer 
		/// callback is a good solution. 
		/// </remarks>
		public void PumpEvents()
		{
			CallbackInfo ci;

			while (true)
			{
				lock (callbacks)
				{
					if (callbacks.Count == 0)
					{
						break;
					}
					ci = callbacks.Dequeue();
				}

                ci.callback?.Invoke(ci.component);
            }
		}

		/// <summary>
        /// Performs a Element run loop for the calling thread.  Communication
        /// with the Element board is started and this thread is blocked until
		/// ShutDown() is called (presumably in an event handler).
		/// </summary>
		/// <include file='Docs\examples.xml' path='examples/example[@name="CommunicationStarted"]'/>
		//public void Run()
		//{
		//	Task taskComm = StartCommunication();
  //          taskComm.Start();
  //          taskComm.Wait();

  //          while (true)
		//	{
		//		eventsReady.WaitOne();
		//		if (shutdown)
		//		{
		//			break;
		//		}

		//		PumpEvents();
		//	}
		//}

		/// <summary>
        /// Stop communication with the Element board and cause the 
		/// the run loop to be exited.
		/// </summary>
		//public void ShutDown()
		//{
		//	StopCommunication();
		//	shutdown = true;
		//	eventsReady.Set();
		//}

        public void Close()
        {
            shutdown = true;
            eventsReady.Set();
        }


        /// <summary>
        /// Retrieve the firmware version string from the Element board.
        /// </summary>
        /// <returns>The firmware version string.</returns>
        public string GetFirmwareVersion()
		{
			return commTask.EnqueueCommJobAndWait(Priority.Low, "fw");
		}

        /// <summary>
        /// Blink one of the two leds on the Element.  
        /// Note: Element versions 3.0 and thereafter only have one user programmable LED.
        /// Therefore, led id '1' is the only valid LED on version 3.0 and greater boards.
        /// </summary>
        /// <param name="led">Led Id</param>
        /// <param name="rate">Blink Rate (0-127)</param>
        public void BlinkLED (LedId led, int rate)
        {
			string cmd = String.Format("blink {0}:{1}", (int)led, rate);
            commTask.EnqueueCommJob(Priority.High, cmd);
        }

        /// <summary>
        /// Resets the Element by performing a soft-boot
        /// </summary>
        public void Reset()
        {
            commTask.EnqueueCommJob(Priority.High, "reset");
        }

        /// <summary>
        /// Restores the Element to the default factory state.
        /// </summary>
        public void Restore()
        {
            commTask.EnqueueCommJob(Priority.High, "restore");
        }

        public ISharpGP2D12 produceSharpGP2D12(slg.RobotAbstraction.Ids.AnalogPinId pin, int updateFrequency, double distanceChangedThreshold)
        {
            return new SharpGP2D12(this)
            {
                Pin = pin,
                UpdateFrequency = updateFrequency,              // milliseconds
                DistanceChangedThreshold = distanceChangedThreshold
            };
        }

        public ISonarSRF04 produceSonarSRF04(slg.RobotAbstraction.Ids.GpioPinId triggerPin, slg.RobotAbstraction.Ids.GpioPinId outputPin, int updateFrequency, double distanceChangedThreshold)
        {
            return new SonarSRF04(this)
            {
                TriggerPin = (Ids.GpioPinId)triggerPin,
                OutputPin = (Ids.GpioPinId)outputPin,
                UpdateFrequency = updateFrequency,              // milliseconds
                DistanceChangedThreshold = distanceChangedThreshold
            };
        }

        public IParkingSonar produceParkingSonar(int updateFrequency)
        {
            throw new NotImplementedException();
        }

        public IOdometry produceOdometry(int updateFrequency)
        {
            throw new NotImplementedException();
        }

        public IAnalogSensor produceAnalogSensor(slg.RobotAbstraction.Ids.AnalogPinId pin, int updateFrequency, double valueChangedThreshold)
        {
            return new AnalogSensor(this)
            {
                Pin = pin,
                UpdateFrequency = updateFrequency,              // milliseconds
                ValueChangedThreshold = (int)valueChangedThreshold
            };
        }

        public ICompassCMPS03 produceCompassCMPS03(int i2CAddress, int updateFrequency, double headingChangedThreshold)
        {
            return new CompassCMPS03(this)
            {
                I2CAddress = (byte)i2CAddress,
                UpdateFrequency = updateFrequency,              // milliseconds
                HeadingChangedThreshold = (short)headingChangedThreshold
            };
        }

        public IWheelEncoder produceWheelEncoder(slg.RobotAbstraction.Ids.WheelEncoderId wheelEncoderId, int updateFrequency, int resolution, int countChangedThreshold)
        {
            WheelEncoder encoder = new WheelEncoder(this)
            {
                WheelEncoderId = wheelEncoderId,
                UpdateFrequency = updateFrequency,              // milliseconds
                Resolution = resolution,                        // must be set to avoid divide by zero in WheelEncoder.cs
                CountChangedThreshold = countChangedThreshold   // ticks
            };
            return encoder;
        }

        public IDifferentialMotorController produceDifferentialMotorController()
        {
            return new DifferentialMotorController(this);
        }

        #endregion // Public Methods


        #region Internal Properties

        internal CommunicationTask CommunicationTask
		{
			get { return commTask; }
		}
	
		#endregion


		#region Internal Methods

		internal void SignalEvent(
			HardwareComponentEventHandler callback, ElementComponent componenet)
		{
			// enqueue the info about the callback for later use
			CallbackInfo ci = new CallbackInfo();
			ci.callback = callback;
			ci.component = componenet;
			lock (callbacks)
			{
				if(!callbacks.Contains(ci))
				{
					callbacks.Enqueue(ci);
				}
			}

			// if asyncCallbacks are enabled, then go ahead and run them 
			// on this thread - otherwise they wait for a explicit call to
			// PumpEvents() by the user (or the run loop)
			if (asyncCallbacks)
			{
				PumpEvents();
			}
			else
			{
				// this signals the Run() loop that there are 
				// events to be run
				eventsReady.Set();
			}
		}

        #endregion // Internal Properties


        #region Private Types

        private class CallbackInfo
		{
			public HardwareComponentEventHandler callback;
			public ElementComponent component;

			public override bool Equals(object o)
			{
				if (o == null || o.GetType() != GetType())
				{
					return false;
				}

				CallbackInfo op2 = o as CallbackInfo;
				return callback == op2.callback && component == op2.component;
			}

			public override int GetHashCode()
			{
				return callback.GetHashCode();
			}
		}

        #endregion // Private Types


        #region Private Methods

        private void Initialize()
		{
			commTask = new CommunicationTask(this);
		}

		private void SignalCommunicationStarted()
		{
            CommunicationStarted?.Invoke(this);
        }

		private void SignalCommunicationStopped()
		{
            CommunicationStopped?.Invoke(this);
        }

        private void SignalCommunicationsStarting(ICommunicationChannel sp)
        {
            CommunicationsStarting?.Invoke(sp);
        }

        private void SignalCommunicationsStopping(ICommunicationChannel sp)
        {
            CommunicationsStopping?.Invoke(sp);
        }

        #endregion // Private Methods


        #region Private variables

        private bool asyncCallbacks = AsyncCallbacksEnabledDefault;

		private AutoResetEvent eventsReady = new AutoResetEvent(false);

		private Queue<CallbackInfo> callbacks = new Queue<CallbackInfo>();

		private CommunicationTask commTask;
		private bool communicating = false;

		private string portName = PortNameDefault; 
		private int baudRate = BaudRateDefault;
        private Units units = UnitsDefault;
		private bool shutdown = false;

        #endregion // Private variables
    }
}
