using cmRobot.Element.Components;
using cmRobot.Element.Internal;
using System;
using System.Diagnostics;

using slg.RobotAbstraction.Drive;

namespace cmRobot.Element.Controllers
{
	/// <summary>
	/// Represents a Differential Motor Controller.
	/// </summary>
	public class DifferentialMotorController : QueryableComponentBase, IDifferentialMotorController
    {
		#region Ctors

		/// <summary>
		/// Initializes a new instance of the <c>PwmDCMotorController</c> class.
		/// </summary>
		public DifferentialMotorController()
		{
			 Enabled = false;
			 commJob = new DelegateCommunicationJob(delegate()
			 {
                 string cmd = String.Format("pwm {0}:{1} {2}:{3}", (ushort)_rightMotorId, _rightMotorSpeed, (ushort)_leftMotorId, _leftMotorSpeed);
                 //Debug.WriteLine("Command: " + cmd);
                 return cmd;
			 });

             rampCommJob = new DelegateCommunicationJob(delegate()
             {
                 string cmd = String.Format("pwm r:{0} {1}:{2} {3}:{4}", _rampingRate, (ushort)_rightMotorId, _rightMotorSpeed, (ushort)_leftMotorId, _leftMotorSpeed);
                 //Debug.WriteLine("Command: " + cmd);
                 return cmd;
             });
        }

		/// <summary>
		/// Initializes a new instance of the <c>PwmDCMotorController</c> class, attaching
        /// it to the specified <c>Element</c> instance.
		/// </summary>
		public DifferentialMotorController(Element element)
			: this()
		{
			this.Element = element;
		}

		#endregion

        #region Public Methods
        /// <summary>
        /// Commands the Element to set the speed for the left and right DC motors simultaneously,
        /// based on the LeftMotorSpeed and RightMotorSpeed properties.  If RampingEnabled property is set to true,
        /// then the motors will be ramped up to speed (SEE USER GUIDE FOR PROPER RAMPING OPERATION).
        /// </summary>
        public void DriveMotors()
        {
            if (_rampingEnabled)
                Element.CommunicationTask.EnqueueCommJob(Priority.High, rampCommJob);
            else
                Element.CommunicationTask.EnqueueCommJob(Priority.High, commJob);
        }

        public void FeatherMotors()
        {
            throw new NotImplementedException();
        }

        public void BrakeMotors()
        {
            throw new NotImplementedException();
        }

        #endregion

        #region Public Properties

        /// <summary>
        /// Sets the motor speeds for the left and right motors to the specified speed,
        /// and insures the speeds are set so as to rotate the differential drive robot
        /// in a left hand (CCW looking down at the robot) direction.
        /// </summary>
        public int RotateLeftSpeed
        {
            get { return _rotateLeftSpeed; }
            set 
            { 
                if (_rotateLeftSpeed != value)
                {
                    _rotateLeftSpeed = value;
                    _leftMotorSpeed = -value;
                    _rightMotorSpeed = value;
                }
            }
        }

        /// <summary>
        /// Sets the motor speeds for the left and right motors to the specified speed,
        /// and insures the speeds are set so as to rotate the differential drive robot
        /// in a right hand (CW looking down at the robot) direction.
        /// </summary>
        public int RotateRightSpeed
        {
            get { return _rotateRightSpeed; }
            set 
            { 
                if (_rotateRightSpeed != value)
                {
                    _rotateRightSpeed = value;
                    _leftMotorSpeed = value;
                    _rightMotorSpeed = -value;
                }
            }
        }

        /// <summary>
        /// Enables/Disables ramping functionality
        /// </summary>
        public bool RampingEnabled
        {
            get { return _rampingEnabled; }
            set { _rampingEnabled = value; }
        }

        /// <summary>
        /// Sets/Gets the current ramping rate for the DC motors (SEE USER GUIDE FOR PROPER RAMPING OPERATION).
        /// </summary>
        public int RampingRate
        {
            get { return _rampingRate; }
            set { _rampingRate = value; }
        }

        /// <summary>
        /// Sets/Gets the LeftMotorId.  By default the LeftMotorId is 2.
        /// </summary>
		public int LeftMotorId
		{
			get { return _leftMotorId; }
			set { _leftMotorId = value; }
		}

        /// <summary>
        /// Sets/Gets the RightMotorId.  By default the LeftMotorId is 1.
        /// </summary>
		public int RightMotorId
		{
			get { return _rightMotorId; }
			set { _rightMotorId = value; }
		}

        /// <summary>
        /// Sets/Gets the current LeftMotorSpeed.
        /// </summary>
		public int LeftMotorSpeed
		{
			get 
            { 
                return _leftMotorSpeed; 
            }
			set 
            { 
                if (_leftMotorSpeed != value)
                {
                    _leftMotorSpeed = value;
                }
            }
		}

        /// <summary>
        /// Sets/Gets the current RightMotorSpeed.
		/// </summary>
		public int RightMotorSpeed
		{
			get 
            { 
                return _rightMotorSpeed; 
            }
			set 
            {
                if (_rightMotorSpeed != value)
                {
                    _rightMotorSpeed = value;
                }
            }
		}

		/// <summary>
        /// Gets velocity of Left Motor (not Speed!).  This value is being calculated internally in the Element
		/// NOTE: To get the latest velocity value, invoke the Update() method, then query this property.
		/// </summary>
		public int LeftVelocity
		{
			get { return _leftVel; }
		}

		/// <summary>
        /// Gets velocity of Right Motor (not Speed!).  This value is being calculated internally in the Element
		/// NOTE: To get the latest velocity value, invoke the Update() method, then query this property.
		/// </summary>
		public int RightVelocity
		{
			get { return _rightVel; }
		}
	

		#endregion

		#region Protected Methods

		/// <summary>
		/// Overridden to generate the command to query the value of
        /// a Ping sensor from the Element board.
		/// </summary>
		/// <returns>The generated command.</returns>
		protected override string GenerateCommand()
		{
			return String.Format("vel {0} {1}", LeftMotorId, RightMotorId);
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
			string[] vels = response.Split();
			_leftVel = Int32.Parse(vels[0]);
			_rightVel = Int32.Parse(vels[1]);
		}

        #endregion

        #region Privates

        private int _rampingRate = 0;
        private int _leftMotorId = 2;
        private int _leftMotorSpeed = 0;
        private int _rightMotorId = 1;
        private int _rightMotorSpeed = 0;
        private bool _rampingEnabled = false;
        private int _rotateLeftSpeed = 0;
        private int _rotateRightSpeed = 0;
		private int _leftVel = 0;
		private int _rightVel = 0;

        private ICommunicationJob commJob;
        private ICommunicationJob rampCommJob;

        #endregion
	}
}
