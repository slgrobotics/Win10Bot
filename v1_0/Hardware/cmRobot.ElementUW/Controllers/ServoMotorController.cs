using cmRobot.Element.Components;
using cmRobot.Element.Ids;
using cmRobot.Element.Internal;
using System;


namespace cmRobot.Element.Controllers
{

	/// <summary>
	/// Represents a servo motor controller.
	/// </summary>
	/// <include file='Docs\remarks.xml' path='/remarks/remarks[@name="ServoMotorController"]'/>
	public class ServoMotorController : ElementComponent, IServoMotorController
	{

		#region Public Constants

		/// <summary>
		/// The default value for the <c>ServoMotorId</c> property.
		/// </summary>
		public const ServoMotorId ServoMotorIdDefault = ServoMotorId.ServoMotor1;

		#endregion


		#region Ctors

		/// <summary>
		/// Initializes a new instance of the <c>ServoMotorController</c> class.
		/// </summary>
		public ServoMotorController()
		{
			commJob = new DelegateCommunicationJob(delegate()
			{
				return String.Format("servo {0}:{1}", (ushort)ServoMotorId, position);
			});
		}

		/// <summary>
		/// Initializes a new instance of the <c>ServoMotorController</c> class, attaching
        /// it to the specified <c>Element</c> instance.
		/// </summary>
		public ServoMotorController(Element element) : this()
		{
			this.Element = element;
		}

		#endregion


		#region Public Properties

		/// <summary>
		/// Id of the servo motor to control.
		/// </summary>
		public ServoMotorId ServoMotorId
		{
			get { return motorId; }
			set { motorId = value; }
		}

		/// <summary>
		/// The position of the motor.  This is a value between -100 and 100, where 0 represents
		/// the neutral position and 100 represents full extention.  A positive number respresents 
		/// a forward direction and a negative number represents a reverse direction.
		/// </summary>
		public int Position
		{
			get { return position; }

			set 
			{ 
				position = value;
				Element.CommunicationTask.EnqueueCommJob(
					Priority.High, commJob);
			}
		}

		#endregion


		#region Privates

		ServoMotorId motorId = ServoMotorIdDefault;
		int position = 0;
		ICommunicationJob commJob;

		#endregion

	}

}
