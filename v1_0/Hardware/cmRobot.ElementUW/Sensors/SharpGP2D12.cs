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
	/// Represents a Sharp GP2D12 Infrared Sensor.
	/// </summary>
	/// <include file='Docs\remarks.xml' path='/remarks/remarks[@name="GP2D12"]'/>
//#if !WindowsCE
//	[DefaultEvent("DistanceChanged")]
//	[DefaultProperty("DistanceChangedThreshold")]
//#endif
    public class SharpGP2D12 : AnalogSensor, IDistanceSensor, ISharpGP2D12
    {

		#region Public Constants

		/// <summary>
		/// The default value for the <c>DistanceChangedThreshold</c> property.
		/// </summary>
		public const float DistanceChangedThresholdDefault = 1;

		#endregion


		#region Ctors

		/// <summary>
        /// Initializes a new instance of the <c>Element</c> class.
		/// </summary>
		public SharpGP2D12()
		{
		}

		/// <summary>
        /// Initializes a new instance of the <c>Element</c> class, attaching
        /// it to the specified Element instance.
		/// </summary>
		public SharpGP2D12(Element element)
		{
			this.Element = element;
		}

		#endregion


		#region Public Properties

		/// <summary>
		/// The distance, in inches, reported by the Sharp GP2D12 sensor.
		/// </summary>
//#if !WindowsCE
//		[Browsable(false)]
//#endif
		public double Distance
		{
			get { return distance; }
		}

		/// <summary>
		/// Specifies the amount that <c>Distance</c> must change before <c>DistanceChangedThreshold</c> is signalled.
		/// </summary>
//#if !WindowsCE
//		[DefaultValue(DistanceChangedThresholdDefault)]
//		[Description("Specifies the amount that Distance must change before DistanceChanged is signalled.")]
//#endif
		public double DistanceChangedThreshold
		{
			get { return distanceThreshold; }
			set { distanceThreshold = value; }
		}

		#endregion


		#region Public Events

		/// <summary>
		/// Occurs when <c>Distance</c> has changed by an amount greater than <c>DistanceChangedThreshold</c>.
		/// </summary>
//#if !WindowsCE
//		[Description("Occurs when Distance has changed by an amount greater than DistanceChangedThreshold.")]
//#endif
		public event HardwareComponentEventHandler DistanceChanged;

		#endregion


		#region Protected Methods

		/// <summary>
		/// Overriden to interpret the analog value and set <c>Distance</c>
		/// accordingly.  Signals <c>DistanceChanged</c>, if necessary.
		/// </summary>
		/// <param name="a2d"></param>
		protected override void OnSetValue(int a2d)
		{
            if (a2d < 4)
            {
                a2d = 4;
            }

			base.OnSetValue(a2d);

			// convert the value to a distance:
            distance = (6787 / (a2d - 3)) - 4;
            distance = (distance < 80) ? distance : 80;
            distance = (distance > 10) ? distance : 10;

            if (Element.Units == Ids.Units.English)
            {
                distance = distance / 2.54; // convert to inches;
            }
            // else no-op - already in cm, and the raw reading can
            // be obtained from the AnalogSensor base class Value property
        
			// if distance change exceeds threshold, then fire event 
			if (Math.Abs(distance - lastDistance) > distanceThreshold)
			{
				lastDistance = distance;
				Element.SignalEvent(DistanceChanged, this);
			}
		}

		#endregion

		#region Privates

		private double distance = 0;
		private double lastDistance = 0;
		private double distanceThreshold = DistanceChangedThresholdDefault;

		#endregion

    }
}
