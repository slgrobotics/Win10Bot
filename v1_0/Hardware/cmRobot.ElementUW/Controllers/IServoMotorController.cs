using cmRobot.Element.Ids;
using System;
using System.Collections.Generic;
using System.Text;

namespace cmRobot.Element.Controllers
{

	/// <summary>
	/// An interface that represents a DC motor controller.
	/// </summary>
	public interface IServoMotorController
	{

		/// <summary>
		/// The id of the servo motor to control.
		/// </summary>
		ServoMotorId ServoMotorId
		{
			get;
			set;
		}

		/// <summary>
		/// The position of the motor.  This is a value between -100 and 100, where 0 represents
		/// the neutral position and 100 represents full extention.  A positive number respresents 
		/// a forward direction and a negative number represents a reverse direction.
		/// </summary>
		int Position
		{
			get;
			set;
		}

	}

}
