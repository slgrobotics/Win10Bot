using System;
using System.Collections.Generic;
using System.Text;

using slg.RobotAbstraction.Sensors;
using slg.RobotAbstraction.Events;

namespace cmRobot.Element.Sensors
{
	/// <summary>
	/// Represents an Ambient Air Temperature sensor 
	/// </summary>
    public class AmbientTemperatureSensor : AnalogSensor
    {
        #region Public Constants

        /// <summary>
        /// The default value for the <c>TemperatureChangedThreshold</c> property.
        /// </summary>
        public const int TemperatureChangedThresholdDefault = 1;

        #endregion

        #region CTORS
        /// <summary>
		/// Initializes a new instance of the <c>AmbientTemperatureSensor</c> class.
		/// </summary>
		public AmbientTemperatureSensor()
		{
		}

		/// <summary>
		/// Initializes a new instance of the <c>AmbientTemperatureSensor</c> class, attaching
        /// it to the specified <c>Element</c> instance.
		/// </summary>
        public AmbientTemperatureSensor(Element element)
		{
			this.Element = element;
        }
        #endregion

        #region PROPERTIES

        /// <summary>
        /// The temperature read by the analog sensor:
        /// </summary>
        public double Temperature
        {
            get { return _temperature; }
        }

        /// <summary>
        /// Specifies the amount that Temperature must change before TemperatureChanged is signalled.
        /// </summary>
        public int TemperatureChangedThreshold
        {
            get { return _temperatureChangedThreshold; }
            set {_temperatureChangedThreshold = value; }
        }
	
        #endregion

        #region EVENTS
        /// <summary>
        /// Occurs when Temperature has changed by an amount greater than TemperatureChangedThreshold.
        /// </summary>
        public event HardwareComponentEventHandler TemperatureChanged;
        #endregion

        #region Protected Methods

        /// <summary>
        /// Occurs when the temperature value has changed.  Signals <c>TemperatureChanged</c>, if necessary.  
        /// Derived classes can override to perform additinal processing necessary when the value is updated.
        /// </summary>
        /// <param name="a2d">The new value.</param>
        protected override void OnSetValue(int a2d)
        {
            if (a2d <= 0)
            {
                a2d = 1;
            }
            double voltage = (a2d * 5.0) / 1024;

            base.OnSetValue(a2d);

            // convert the voltage to a temperature (degrees C):
            _temperature = (voltage * 100) - 273.15;

            if (Element.Units == Ids.Units.English)
            {
                _temperature = (_temperature * 1.8) + 32; // convert to Farenheit;
            }

            // else no-op - already in degrees C, and the raw reading can
            // be obtained from the AnalogSensor base class Value property

            // if temperature change exceeds threshold, then fire event 
            if (Math.Abs(_temperature - _lastTemperature) > TemperatureChangedThreshold)
            {
                _lastTemperature = _temperature;
                Element.SignalEvent(TemperatureChanged, this);
            }
        }

        #endregion

        #region PRIVATES
        private double _temperature;
        private double _lastTemperature;
        private int _temperatureChangedThreshold = TemperatureChangedThresholdDefault;

        #endregion

    }
}
