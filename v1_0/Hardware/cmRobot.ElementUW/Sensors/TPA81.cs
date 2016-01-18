using cmRobot.Element;
using cmRobot.Element.Components;
using System;
using System.Collections.Generic;

using slg.RobotAbstraction.Sensors;
using slg.RobotAbstraction.Events;

namespace cmRobot.Element.Sensors
{

	/// <summary>
	/// Represents a TPA81 temperature sensor.
	/// </summary>
	/// <include file='Docs\remarks.xml' path='/remarks/remarks[@name="TPA81"]'/>
	public class TPA81 : QueryableComponentBase
	{

		#region Public Constants

		/// <summary>
		/// The default value for the <c>I2CAddressDefault</c> property.
		/// </summary>
		public const byte I2CAddressDefault = 0xD0;

		/// <summary>
		/// The number of temperature values reported by the TPA81.
		/// </summary>
		public const short NumTemperatureSensors = 9;

		/// <summary>
		/// The default value for the <c>TemperatureChangedThreshold</c> property.
		/// </summary>
		public const double TemperatureChangedThresholdDefault = 1;

		#endregion


		#region Public Types

		/// <summary>
		/// An implementaion of <c>ITemperatureSensor</c> representing one of the 
		/// values reported by the TPA81 sensor.
		/// </summary>
		public class TemperatureSensor : ElementComponent, ITemperatureSensor
		{

			internal TemperatureSensor(TPA81 owner)
			{
				this.owner = owner;
			}

			/// <summary>
            /// The <c>Element</c> instance that this component is attached to.
			/// </summary>
			public new Element Element
			{
				get { return owner.Element; }

				set
				{
					throw new InvalidOperationException(
                        "Element cannot be changed directly.");
				}
			}

			/// <summary>
			/// The TPA81 instance that this <c>TemperatureSensor</c> is
			/// associated with.
			/// </summary>
			public TPA81 Owner
			{
				get { return owner; }

			}

			/// <summary>
			/// The temperature reported by the sensor.
			/// </summary>
			public double Temperature
			{
				get { return temp; }
			}

			/// <summary>
			/// Specifies the amount that <c>Temperature</c> must change before 
			/// <c>TemperatureChanged</c> is signalled.
			/// </summary>
			public double TemperatureChangedThreshold
			{
				get { return tempThreshold; }
				set { tempThreshold = value; }
			}

			/// <summary>
			/// Occurs when <c>Temperature</c> has changed by an amount greater 
			/// than <c>TemperatureChangedThreshold</c>.
			/// </summary>
			public event HardwareComponentEventHandler TemperatureChanged;

			/// <summary>
			/// Occurs when the sensor's temperature value has been updated.  <c>TemperatureChanged</c>
			/// is signalled, if necessary.  Derived classes can override to perform additinal 
			/// processing when the value is updated.
			/// </summary>
			/// <param name="temperature">The new temperature value.</param>
			public void OnSetTemperature(double temperature)
			{
				this.temp = temperature;

				// if distance change exceeds threshold, then fire event 
				if (Math.Abs(temp - lastTemp) > tempThreshold)
				{
					lastTemp = temp;
					owner.Element.SignalEvent(TemperatureChanged, this);
				}
			}

			private TPA81 owner;
			private double temp = 0;
			private double lastTemp = 0;
			private double tempThreshold = TemperatureChangedThresholdDefault;

		}

		#endregion


		#region Ctors

		/// <summary>
		/// Initializes a new instance of the <c>TPA81</c> class.
		/// </summary>
		public TPA81()
		{
			for (int i = 0; i < temps.Length; ++i)
			{
				temps[i] = new TemperatureSensor(this);
			}
		}

		/// <summary>
		/// Initializes a new instance of the <c>TPA81</c> class, attaching
        /// it to the specified <c>Element</c> instance.
		/// </summary>
		public TPA81(Element element) : this()
		{
			this.Element = element;
		}

		#endregion


		#region Public Properties

		/// <summary>
		/// I2C Address to which the TPA81 sensor is attached.
		/// </summary>
		public byte I2CAddress
		{
			get { return i2cAddr; }
			set { i2cAddr = value; }
		}

		/// <summary>
		/// The TPA81 sensor represents an array of temperature sensors, 
		/// indexed from 0 to <c>NumTemperatureSensors</c>.
		/// </summary>
		/// <param name="i">The index value.</param>
		/// <returns>An instance of a <c>TemperatureSensor</c>.</returns>
		public TemperatureSensor this[int i]
		{
			get { return temps[i]; }
		}

		#endregion


		#region Protected Methods

		/// <summary>
		/// Overridden to generate the command to query the value of
        /// a TPA81 sensor from the Element board.
		/// </summary>
		/// <returns>The generated command.</returns>
		protected override string GenerateCommand()
		{
			return String.Format("tpa81 {0}", i2cAddr);
		}

		/// <summary>
		/// Overridden to parse the string returned from the
        /// Element board in response to the command generated 
		/// by <c>GenerateCommand</c>.
		/// </summary>
		/// <param name="response">The response string.</param>
		protected override void ProcessResponse(string response)
		{
			string[] vals = response.Replace("  ", " ").Trim().Split(' ');
			for (int i = 0; i < vals.Length; ++i)
			{
				double temp = Double.Parse(vals[i]);
                // Convert to English units
                if (Element.Units == Ids.Units.English)
                {
                    double tempF = (1.8F * temp) + 32;
                    temps[i].OnSetTemperature(tempF);
                }
                else if (Element.Units == Ids.Units.Metric)
                {
                    temps[i].OnSetTemperature(temp);
                }
                // Raw units in this case are simply the default metric units
                else if (Element.Units == Ids.Units.Raw)
                {
                    temps[i].OnSetTemperature(temp);
                }
			}
		}

		#endregion


		#region Privates

		private byte i2cAddr = I2CAddressDefault;
		private TemperatureSensor[] temps = new TemperatureSensor[9];

		#endregion

	}

}
