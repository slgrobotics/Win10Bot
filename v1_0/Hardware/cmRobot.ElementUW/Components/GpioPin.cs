using cmRobot.Element.Ids;
using cmRobot.Element.Internal;
using System;

using slg.RobotAbstraction.Sensors;
using slg.RobotAbstraction.Events;

namespace cmRobot.Element.Components
{
	/// <summary>
	/// Represents a general purpose IO pin.
	/// </summary>
	/// <include file='Docs\remarks.xml' path='/remarks/remarks[@name="GpioPin"]'/>
	public class GpioPin : QueryableComponentBase, IGpioPin
	{

		#region Public Constants

		/// <summary>
		/// The default value for the <c>GpioPinId</c> property.
		/// </summary>
		public const GpioPinId GpioPinIdDefault = GpioPinId.Pin1;

		#endregion


		#region Ctors

		/// <summary>
		/// Initializes a new instance of the <c>GpioPin</c> class.
		/// </summary>
		public GpioPin()
		{
			commJob = new DelegateCommunicationJob(delegate
			{
                int p = 0;

                if (pin == GpioPinId.Pin0)
                    p = 0;
                else if (pin == GpioPinId.Pin1)
                    p = 1;
                else if (pin == GpioPinId.Pin2)
                    p = 2;
                else if (pin == GpioPinId.Pin3)
                    p = 3;
                else if (pin == GpioPinId.Pin4)
                    p = 4;
                else if (pin == GpioPinId.Pin5)
                    p = 5;
                else if (pin == GpioPinId.Pin6)
                    p = 6;
                else if (pin == GpioPinId.Pin7)
                    p = 7;
                else if (pin == GpioPinId.Pin8)
                    p = 8;
                else if (pin == GpioPinId.Pin9)
                    p = 9;
                else if (pin == GpioPinId.PinHB)
                    p = 10;
                else if (pin == GpioPinId.PinSCL)
                    p = 11;
                else if (pin == GpioPinId.PinSDA)
                    p = 12;
                //else if (pin == GpioPinId.PinRTS)
                //    p = 13;
                //else if (pin == GpioPinId.PinCTS)
                //    p = 14;
                
                if (state == false)
                    return String.Format("setio {0}:0", p);
                else
                    return String.Format("setio {0}:1", p);                
			});
		}

		/// <summary>
		/// Initializes a new instance of the <c>GpioPin</c> class, attaching
        /// it to the specified <c>Element</c> instance.
		/// </summary>
		public GpioPin(Element element) : this()
        {
			this.Element = element;
		}

		#endregion


		#region Public Properties

		/// <summary>
        /// The state of the General Purpose IO pin on the Element board.
		/// </summary>
		public bool State
		{
			get { return state; }
			set { SetStateFromUser(value); }
		}

		/// <summary>
		/// Id of the physical GPIO pin.
		/// </summary>
		public GpioPinId Pin
		{
			get { return pin; }
			set { pin = value; }
		}

		#endregion


		#region Public Events

		/// <summary>
		/// Occurs when <c>State</c> changes from 0 to 1.
		/// </summary>
		public event HardwareComponentEventHandler Set;

		/// <summary>
		/// Occurs when <c>State</c> changes from 1 to 0.
		/// </summary>
		public event HardwareComponentEventHandler Cleared;

		/// <summary>
		/// Occurs when <c>State</c> changes.
		/// </summary>
		public event HardwareComponentEventHandler Changed;

		#endregion


		#region Protected Methods

		/// <summary>
		/// Overridden to generate the command to query the state of
        /// the general purpose IO pin on the Element board.
		/// </summary>
		/// <returns>The generated command.</returns>
		protected override string GenerateCommand()
		{
            int p = 0;

            if (pin == GpioPinId.Pin0)
                p = 0;
            else if (pin == GpioPinId.Pin1)
                p = 1;
            else if (pin == GpioPinId.Pin2)
                p = 2;
            else if (pin == GpioPinId.Pin3)
                p = 3;
            else if (pin == GpioPinId.Pin4)
                p = 4;
            else if (pin == GpioPinId.Pin5)
                p = 5;
            else if (pin == GpioPinId.Pin6)
                p = 6;
            else if (pin == GpioPinId.Pin7)
                p = 7;
            else if (pin == GpioPinId.Pin8)
                p = 8;
            else if (pin == GpioPinId.Pin9)
                p = 9;
            else if (pin == GpioPinId.PinHB)
                p = 10;
            else if (pin == GpioPinId.PinSCL)
                p = 11;
            else if (pin == GpioPinId.PinSDA)
                p = 12;
            //else if (pin == GpioPinId.PinRTS)
            //    p = 13;
            //else if (pin == GpioPinId.PinCTS)
            //    p = 14;
            return String.Format("getio {0}", p);
		}

		/// <summary>
		/// Overridden to parse the string returned from the
        /// Element board in response to the command generated 
		/// by <c>GenerateCommand</c>.
		/// </summary>
		/// <param name="response">The response string.</param>
		protected override void ProcessResponse(string response)
		{
			SetStateFromSensor(Int32.Parse(response) == 1);
		}

		#endregion


		#region Private Methods

		private void SetStateFromUser(bool newState)
		{
            // setting here will keep interrupt from occuring on next read
            state = newState;
            Element.CommunicationTask.EnqueueCommJob(Priority.High, commJob);
		}

		private void SetStateFromSensor(bool newState)
		{
			if (state != newState)
			{
				state = newState;
				Element.SignalEvent(Changed, this);

				if (state)
				{
					Element.SignalEvent(Set, this);
				}
				else
				{
					Element.SignalEvent(Cleared, this);
				}
			}
		}

		#endregion


		#region Privates

		private ICommunicationJob commJob;
		private GpioPinId pin = GpioPinIdDefault;
		private bool state = false;

		#endregion

	}

}
