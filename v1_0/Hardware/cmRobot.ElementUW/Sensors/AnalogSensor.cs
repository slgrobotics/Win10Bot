using cmRobot.Element.Components;
using cmRobot.Element.Ids;
using cmRobot.Element.Internal;
using System;

using slg.RobotAbstraction.Ids;
using slg.RobotAbstraction.Sensors;
using slg.RobotAbstraction.Events;
using System.Diagnostics;

namespace cmRobot.Element.Sensors
{

	/// <summary>
    /// Represents a generic analog sensor attached to an analog pin on the Element board.
	/// </summary>
	/// <include file='Docs\remarks.xml' path='/remarks/remarks[@name="AnalogSensor"]'/>
	public class AnalogSensor : QueryableComponentBase, IAnalogSensor
    {

		#region Public Constants

		/// <summary>
		/// The default value for the <c>Pin</c> property.
		/// </summary>
		public const AnalogPinId PinDefault = AnalogPinId.A1;


		/// <summary>
		/// The default value for the <c>ValueChangedThreshold</c> property.
		/// </summary>
		public const int ValueChangedThresholdDefault = 10;

		#endregion


		#region Ctors

		/// <summary>
		/// Initializes a new instance of the <c>AnalogSensor</c> class.
		/// </summary>
		public AnalogSensor()
		{
		}

		/// <summary>
		/// Initializes a new instance of the <c>AnalogSensor</c> class, attaching
        /// it to the specified <c>Element</c> instance.
		/// </summary>
		public AnalogSensor(Element element)
		{
			this.Element = element;
		}

		#endregion


		#region Public Properties

		/// <summary>
		/// Id of the pin to which the analog sensor is attached.
		/// </summary>
		public AnalogPinId Pin
		{
			get { return pin; }
			set { pin = value; }
		}

		/// <summary>
		/// The value of the analog sensor.
		/// </summary>
		public int AnalogValue
		{
			get { return value; }
		}

		/// <summary>
		/// Specifies the amount that Value must change before ValueChanged is signalled.
		/// </summary>
		public int ValueChangedThreshold 
		{
			get { return valueThreshold; }

			set 
			{
				Toolbox.AssertInRange(value, 1, Int32.MaxValue);
				valueThreshold = value; 
			}
		}

		#endregion


		#region Public Events

		/// <summary>
		/// Occurs when Value has changed by an amount greater than ValueChangedThreshold.
		/// </summary>
        public event HardwareComponentEventHandler AnalogValueChanged;

        #endregion


        #region Protected Methods

        /// <summary>
        /// Overriden to generate the command to query an analog pin on the 
        /// Element board.
        /// </summary>
        /// <returns></returns>
        protected override string GenerateCommand()
		{
			return String.Format("sensor {0}", (ushort)pin);
		}

		/// <summary>
		/// Overriden to process the repsonse from the command generated
		/// by <c>GenerateCommand</c>.
		/// </summary>
		protected override void ProcessResponse(string response)
		{
			int value = Int32.Parse(response);
			OnSetValue(value);
		}

		/// <summary>
        /// Occurs when the value of the underlying Element board's analog
		/// pin has been read.  Signals <c>ValueChanged</c>, if necessary.  
		/// Derived classes can override to perform additinal processing necessary 
		/// when the value is updated.
		/// </summary>
		/// <param name="value">The new value.</param>
		protected virtual void OnSetValue(int value)
		{
			this.value = value;
			if (Math.Abs(value - lastValue) > valueThreshold)
			{
				lastValue = value;
				Element.SignalEvent(AnalogValueChanged, this);
			}
		}

		#endregion


		#region Privates

		private AnalogPinId pin = PinDefault;
		private int value = 0;
		private int lastValue = 0;
		private int valueThreshold = ValueChangedThresholdDefault;

		#endregion

	}

}
