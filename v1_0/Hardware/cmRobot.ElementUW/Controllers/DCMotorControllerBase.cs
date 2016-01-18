using cmRobot.Element.Components;
using cmRobot.Element.Ids;
using cmRobot.Element.Internal;
using System;
using System.Diagnostics;


namespace cmRobot.Element.Controllers
{

	/// <summary>
	/// Abstract class that provides a default implementation for classes
	/// implementing the <c>IDCMotorController</c> interface.
	/// </summary>
	public abstract class DCMotorControllerBase : QueryableComponentBase, IDCMotorController
	{

		#region Public Constants

		/// <summary>
		/// The default value for the <c>DCMotorId</c> property.
		/// </summary>
		public const DCMotorId DCMotorIdDefault = DCMotorId.DCMotor1;

		#endregion

		#region Ctors

		internal DCMotorControllerBase()
		{
			Enabled = false;
		}

		#endregion		
		
		
		#region Public Properties

		/// <summary>
		/// Identifies the motor to be controlled.
		/// </summary>
		public DCMotorId DCMotorId
		{
			get { return motorId; }
			set { motorId = value; }
		}
	
		/// <summary>
		/// The speed of the DC motor.
		/// </summary>
		public int Speed
		{
			get { return speed; }
			set
			{
				if (speed != value)
				{
   					speed = value;
					OnSpeedChanged(speed);
				}
			}
		}

		/// <summary>
		/// Gets the velocity of the DC motor.
		/// NOTE: To get the latest velocity value, invoke the Update() method, then query this property.
		/// </summary>
		public int Velocity
		{
			get { return velocity; }
		}
	
		#endregion


		#region Protected Methods

		/// <summary>
		/// Occurs when the <c>Speed</c> property is updated.
		/// Derived classes can override to perform additinal processing 
		/// necessary when the speed is updated.
		/// </summary>
		/// <param name="speed">The new speed value.</param>
		protected abstract void OnSpeedChanged(int speed);

		/// <summary>
		/// Overridden to generate the command to query the value of
        /// a Ping sensor from the Element board.
		/// </summary>
		/// <returns>The generated command.</returns>
		protected override string GenerateCommand()
		{
			string cmd = String.Format("vel {0}", (ushort)DCMotorId);
            Debug.WriteLine("Cmd: " + cmd);
            return cmd;
		}

		/// <summary>
		/// Overridden to parse the string returned from the
        /// Element board in response to the command generated 
		/// by <c>GenerateCommand</c>.
		/// </summary>
		/// <param name="response">The response string.</param>
		protected override void ProcessResponse(string response)
		{
			// NOTE: We don't have to perform any unit conversions
            // for sonars, since it's performed on the Element itself:
			int value = Int32.Parse(response);
			velocity = value;
		}

		#endregion


		#region Privates

		private int velocity;
		private int speed = 0;
		private DCMotorId motorId;

		#endregion

	}

}
