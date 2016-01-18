using cmRobot.Element.Components;
using System;

using slg.RobotAbstraction.Sensors;
using slg.RobotAbstraction.Events;

namespace cmRobot.Element.Sensors
{

	/// <summary>
	/// An interface that represents a heading reading sensor.
	/// </summary>
	public interface ICompassSensor
	{

		/// <summary>
		/// The heading in degreees.
		/// </summary>
		short Heading { get; }

		/// <summary>
		/// Specifies the amount that <c>Heading</c> must change before 
		/// the <c>HeadingChanged</c> event is signalled.
		/// </summary>
		short HeadingChangedThreshold { get; set; }



		/// <summary>
		/// Occurs when <c>Heading</c> has changed by an amount greater than 
		/// <c>HeadingChangedThreshold</c>.
		/// </summary>
		event HardwareComponentEventHandler HeadingChanged;

	}

}
