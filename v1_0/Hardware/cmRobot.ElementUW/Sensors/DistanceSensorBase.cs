using cmRobot.Element.Components;
using cmRobot.Element.Ids;
using cmRobot.Element;
using System;

using slg.RobotAbstraction.Sensors;
using slg.RobotAbstraction.Events;

namespace cmRobot.Element.Sensors
{

	/// <summary>
	/// Abstract class that provides a default implementation for classes
	/// implementing the <c>IDistanceSensor</c> interface.
	/// </summary>
	public abstract class DistanceSensorBase : QueryableComponentBase, IDistanceSensor
	{

		#region Public Constants

		/// <summary>
		/// The default value for the <c>DistanceChangedThreshold</c> property.
		/// </summary>
		public const double DistanceChangedThresholdDefault = 1;

		#endregion


		#region Ctors

		/// <summary>
		/// Initializes a new instance of the <c>DistanceSensorBase</c> class.
		/// </summary>
		internal DistanceSensorBase()
		{
		}

		#endregion


		#region Public Properties

		/// <summary>
		/// The distance reported by the sensor.
		/// </summary>
		public double Distance
		{
			get { return distance; }
		}

		/// <summary>
		/// Specifies the amount that <c>Distance</c> must change before 
		/// <c>DistanceChanged</c> is signalled.
		/// </summary>
		public double DistanceChangedThreshold
		{
			get { return distanceThreshold; }
			set { distanceThreshold = value; }
		}

		#endregion


		#region Public Events

		/// <summary>
		/// Occurs when <c>Distance</c> has changed by an amount greater 
		/// than <c>DistanceChangedThreshold</c>.
		/// </summary>
		public event HardwareComponentEventHandler DistanceChanged;

		#endregion


		#region Protected Methods

		/// <summary>
		/// Occurs when the sensor's distance value has been updated.  <c>DistanceChanged</c>
		/// is signalled, if necessary.  Derived classes can override to perform additinal 
		/// processing when the value is updated.
		/// </summary>
		/// <param name="distance">The new distance value.</param>
		protected void OnSetDistance(double distance)
		{
			this.distance = distance;

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
