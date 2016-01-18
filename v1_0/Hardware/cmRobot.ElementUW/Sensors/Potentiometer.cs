using cmRobot.Element.Components;
using cmRobot.Element.Ids;
using cmRobot.Element;
using System;
using System.ComponentModel;

using slg.RobotAbstraction.Sensors;
using slg.RobotAbstraction.Events;

namespace cmRobot.Element.Sensors
{
    /// <summary>
    /// Represents a Potentiometer (Voltage Divider) Sensor.
    /// </summary>
    /// <include file='Docs\remarks.xml' path='/remarks/remarks[@name="Potentiometer"]'/>
//#if !WindowsCE
//    [DefaultEvent("VoltageChanged")]
//    [DefaultProperty("VoltageChangedThreshold")]
//#endif
    public class Potentiometer : AnalogSensor
    {

        #region Public Constants

        /// <summary>
        /// The default value for the <c>VoltageChangedThreshold</c> property.
        /// </summary>
        public const float VoltageChangedThresholdDefault = 1;

        #endregion

        #region Ctors

		/// <summary>
        /// Initializes a new instance of the <c>Element</c> class.
		/// </summary>
		public Potentiometer()
		{
		}

		/// <summary>
        /// Initializes a new instance of the <c>Element</c> class, attaching
        /// it to the specified Element instance.
		/// </summary>
        public Potentiometer(Element element)
		{
			this.Element = element;
		}

		#endregion


        #region Public Properties

        /// <summary>
        /// The voltage, in volts, reported by the Potentiometer sensor.
        /// </summary>
//#if !WindowsCE
//        [Browsable(false)]
//#endif
        public double Voltage
        {
            get { return voltage; }
        }

        /// <summary>
        /// Specifies the amount that <c>Voltage</c> must change before <c>VoltageChangedThreshold</c> is signalled.
        /// </summary>
//#if !WindowsCE
//        [DefaultValue(VoltageChangedThresholdDefault)]
//        [Description("Specifies the amount that Voltage must change before VoltageChanged is signalled.")]
//#endif
        public double VoltageChangedThreshold
        {
            get { return voltageThreshold; }
            set { voltageThreshold = value; }
        }

        #endregion


        #region Public Events

        /// <summary>
        /// Occurs when <c>Voltage</c> has changed by an amount greater than <c>VoltageChangedThreshold</c>.
        /// </summary>
//#if !WindowsCE
//        [Description("Occurs when Voltage has changed by an amount greater than VoltageChangedThreshold.")]
//#endif
        public event HardwareComponentEventHandler VoltageChanged;

        #endregion


        #region Protected Methods

        /// <summary>
        /// Overriden to interpret the analog value and set <c>Voltage</c>
        /// accordingly.  Signals <c>VoltageChanged</c>, if necessary.
        /// </summary>
        /// <param name="a2d"></param>
        protected override void OnSetValue(int a2d)
        {
            base.OnSetValue(a2d);

            // convert the value to a distance:
            voltage = a2d * 0.0048;

            // if voltage change exceeds threshold, then fire event 
            if (Math.Abs(voltage - lastVoltage) > voltageThreshold)
            {
                lastVoltage = voltage;
                Element.SignalEvent(VoltageChanged, this);
            }
        }

        #endregion

        #region Privates

        private double voltage = 0;
        private double lastVoltage = 0;
        private double voltageThreshold = VoltageChangedThresholdDefault;

        #endregion
    }
}
