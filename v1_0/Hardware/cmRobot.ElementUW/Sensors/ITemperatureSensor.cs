using cmRobot.Element.Components;
using System;
using System.Collections.Generic;
using System.Text;

using slg.RobotAbstraction.Sensors;
using slg.RobotAbstraction.Events;

namespace cmRobot.Element.Sensors
{

	/// <summary>
	/// An interface that represents a temperature reading sensor.
	/// </summary>
	public interface ITemperatureSensor
	{

		/// <summary>
		/// The temperature in Fahrenheit.
		/// </summary>
		double Temperature
		{
			get;
		}

		/// <summary>
		/// Specifies the amount that <c>Temperature</c> must change before 
		/// the <c>TemperatureChanged</c> event is signalled.
		/// </summary>
		double TemperatureChangedThreshold
		{
			get;
			set;
		}

		/// <summary>
		/// Occurs when <c>Temperature</c> has changed by an amount greater than 
		/// <c>TemperatureChangedThreshold</c>.
		/// </summary>
		event HardwareComponentEventHandler TemperatureChanged;

	}

}
