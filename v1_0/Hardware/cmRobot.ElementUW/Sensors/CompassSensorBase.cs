using cmRobot.Element.Components;
using cmRobot.Element.Internal;
using System;

using slg.RobotAbstraction.Sensors;
using slg.RobotAbstraction.Events;

namespace cmRobot.Element.Sensors
{

	/// <summary>
	/// Abstract class that provides a default implementation for classes
	/// implementing the <c>ICompassSensor</c> interface.
	/// </summary>
	public abstract class CompassSensorBase : QueryableComponentBase
	{

		#region Public Contants

		/// <summary>
		/// The default value for the <c>HeadingChangedThreshold</c> property.
		/// </summary>
		public const short HeadingChangedThresholdDefault = 5;

		#endregion


		#region Ctors

		internal CompassSensorBase()
		{
		}

		#endregion


		#region Public Properties

		/// <summary>
		/// The heading reported by the compass sensor.
		/// </summary>
		public int Heading
		{
			get { return heading; }
		}

		/// <summary>
		/// Specifies the amount that <c>Heading</c> must change before 
		/// <c>HeadingChanged</c> is signalled.
		/// </summary>
		public short HeadingChangedThreshold
		{
			get { return headingThreshold; }
			
			set 
			{
				Toolbox.AssertInRange(value, 0, Int16.MaxValue);
				headingThreshold = value; 
			}
		}

		#endregion


		#region Public Events

		/// <summary>
		/// Occurs when <c>Heading</c> has changed by an amount greater 
		/// than <c>HeadingChangedThreshold</c>.
		/// </summary>
		public event HardwareComponentEventHandler HeadingChanged;

		#endregion


		#region Protected Methods

		/// <summary>
		/// Occurs when the sensor's heading value has been updated.  <c>HeadingChanged</c>
		/// is signalled, if necessary.  Derived classes can override to perform additinal 
		/// processing when the value is updated.
		/// </summary>
		/// <param name="heading">The new heading value.</param>
		protected void OnSetHeading(short heading)
		{
			this.heading = heading;
			if (Math.Abs(heading - lastHeading) > headingThreshold)
			{
				lastHeading = heading;
				SignalHeadingChanged();
			}
		}

		#endregion


		#region Private Methods

		private void SignalHeadingChanged()
		{
			Element.SignalEvent(HeadingChanged, this);
		}

		#endregion


		#region Privates

		private short heading = 0;
		private short lastHeading = 0;
		private short headingThreshold = HeadingChangedThresholdDefault;

		#endregion

	}

}
