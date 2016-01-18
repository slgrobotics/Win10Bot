using cmRobot.Element.Components;
using System;

using slg.RobotAbstraction.Sensors;
using slg.RobotAbstraction.Events;

namespace cmRobot.Element.Sensors
{

	/// <summary>
	/// An interface that represents a distance reading sensor.
	/// </summary>
	public interface IDistanceSensor
	{

		/// <summary>
		/// The distance reported by the sensor, in inches.
		/// </summary>
		double Distance
		{		
			get;
		}

		/// <summary>
		/// Specifies the amount that <c>Distance</c> must change before 
		/// the <c>DistanceChanged</c> event is signalled.
		/// </summary>
		double DistanceChangedThreshold
		{
			get;
			set;
		}

		/// <summary>
		/// Occurs when <c>Distance</c> has changed by an amount greater than 
		/// <c>DistanceChangedThreshold</c>.
		/// </summary>
		event HardwareComponentEventHandler DistanceChanged;

	}

}
