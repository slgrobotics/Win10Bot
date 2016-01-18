using System;
using System.Collections.Generic;
using System.Text;
using cmRobot.Element;

using slg.RobotAbstraction.Events;

namespace cmRobot.Element.Components
{

	/// <summary>
	/// An interface that represents a general purpose IO pin.
	/// </summary>
	/// <include file='Docs\remarks.xml' path='/remarks/remarks[@name="IGpioPin"]'/>
	public interface IGpioPin
	{

		/// <summary>
		/// The value of the IO pin.  This represents 1 or 0, on or off.
		/// </summary>
		bool State { get; set; }

		/// <summary>
		/// Occurs when <c>Value</c> changes from 0 to 1.
		/// </summary>
		event HardwareComponentEventHandler Set;

		/// <summary>
		/// Occurs when <c>Value</c> changes from 1 to 0.
		/// </summary>
		event HardwareComponentEventHandler Cleared;

		/// <summary>
		/// Occurs when <c>Value</c> changes from.
		/// </summary>
		event HardwareComponentEventHandler Changed;

	}

}
