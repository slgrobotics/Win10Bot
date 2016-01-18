using cmRobot.Element.Ids;
using System;
using System.Collections.Generic;
using System.Text;

namespace cmRobot.Element.Controllers
{

	/// <summary>
	/// An interface that represents a DC motor controller.
	/// </summary>
	public interface IDCMotorController
	{

		/// <summary>
		/// The id of the DC motor to control.
		/// </summary>
		DCMotorId DCMotorId
		{
			get;
			set;
		}

		/// <summary>
		/// The speed of the motor.  This is a value between -100 and 100, where 0 represents
		/// no movement and 100 represents full speed.  A positive number respresents a forward 
		/// direction and a negative number represents a reverse direction.
		/// </summary>
		int Speed
		{
			get;
			set;
		}

	}

}
