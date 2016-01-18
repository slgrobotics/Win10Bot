using cmRobot.Element.Components;
using System;
using System.Collections.Generic;
using System.Text;

using slg.RobotAbstraction.Sensors;
using slg.RobotAbstraction.Events;

namespace cmRobot.Element.Sensors
{

	/// <summary>
	/// An interface that represents a light level reading sensor.
	/// </summary>
	public interface ILightSensor
	{

		/// <summary>
		/// The light level as a percentage of the sensors range.
		/// </summary>
		byte LightLevel
		{
			get;
		}

		/// <summary>
		/// Specifies the amount that <c>LightLevel</c> must change before 
		/// the <c>LightLevelChangedThreshold</c> event is signalled.
		/// </summary>
		byte LightLevelChangedThreshold
		{
			get;
			set;
		}

		/// <summary>
		/// Occurs when <c>LightLevel</c> has changed by an amount greater than 
		/// <c>LightLevelChangedThreshold</c>.
		/// </summary>
		event HardwareComponentEventHandler LightLevelChanged;

	}

}
