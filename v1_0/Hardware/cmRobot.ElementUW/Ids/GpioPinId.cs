using System;
using System.Collections.Generic;
using System.Text;

namespace cmRobot.Element.Ids
{
    /// <summary>
    /// This identifies the general purpose IO pins supported by the Element.
    /// RobotAbstraction.Ids.GpioPinId enumerations are directly mapping here.
    /// We still want to keep this class as Pins 10,11,12 here have special assignments.
    /// </summary>
    public enum GpioPinId
	{
		/// <summary>
        /// General purpose IO pin 0 on Element.
		/// </summary>
		Pin0 = 0,

		/// <summary>
        /// General purpose IO pin 1 on Element.
		/// </summary>
		Pin1 = 1,

		/// <summary>
        /// General purpose IO pin 2 on Element.
		/// </summary>
		Pin2 = 2,

		/// <summary>
        /// General purpose IO pin 3 on Element.
		/// </summary>
		Pin3 = 3,

		/// <summary>
        /// General purpose IO pin 4 on Element.
		/// </summary>
		Pin4 = 4,

		/// <summary>
        /// General purpose IO pin 5 on Element.
		/// </summary>
		Pin5 = 5,

		/// <summary>
        /// General purpose IO pin 6 on Element.
		/// </summary>
		Pin6 = 6,

		/// <summary>
        /// General purpose IO pin 7 on Element.
		/// </summary>
		Pin7 = 7,

		/// <summary>
        /// General purpose IO pin 8 on Element.
		/// </summary>
		Pin8 = 8,

		/// <summary>
        /// General purpose IO pin 9 on Element.
		/// </summary>
		Pin9 = 9,

		/// <summary>
        /// General purpose IO pin HB on Element.  This enables/disables
        /// the onboard h-bridges.  You can have the h-bridges driving motors,
        /// and turn them on/off with this pin.  
        /// This is only available for Element version 3.0 and above
		/// </summary>
		PinHB = 10,

		/// <summary>
        /// General purpose IO pin SCL on Element.  The SCL pin serves as a dual
        /// purpose I/O line and I2C SCL clock line.
        /// This is only available for Element version 3.0 and above
		/// </summary>
		PinSCL = 11,

		/// <summary>
        /// General purpose IO pin SDA on Element. The SDA pin serves as a dual
        /// purpose I/O line and I2C SDA data line.
        /// This is only available for Element version 3.0 and above
		/// </summary>
		PinSDA = 12
    }
}
