using cmRobot.Element.Components;
using cmRobot.Element.Internal;
using cmRobot.Element.Ids;
using System;
using System.Diagnostics;


namespace cmRobot.Element.Controllers
{

	/// <summary>
	/// Represents a Motor Controller which implement Proportional, Integral, Derivative
    /// Distance and Speeds algorithms.  This Motor Controller only works on robotic configurations
    /// using either two drive wheels, or two drive tracks, and those motors must be accompanied 
    /// with wheel encoders, connected to encoder port 1 and 2 on the Element.
	/// </summary>
    public class PIDMotorController : ElementComponent //QueryableComponentBase
	{

		#region PUBLIC PID CONSTANTS

		/// <summary>
		/// The default value (10) for the <c>VelocityProportional</c> property.
		/// </summary>
        /// <remarks>Default value is based on the Traxster Robot Kit drivetrain configuration</remarks>
        public const short VelocityProportionalDefault = 10;

		/// <summary>
        /// The default value (0) for the <c>VelocityIntegral</c> property.
		/// </summary>
        /// <remarks>Default value is based on the Traxster Robot Kit drivetrain configuration</remarks>
        public const short VelocityIntegralDefault = 0;

		/// <summary>
        /// The default value (5) for the <c>VelocityDerivative</c> property.
		/// </summary>
        /// <remarks>Default value is based on the Traxster Robot Kit drivetrain configuration</remarks>
        public const short VelocityDerivativeDefault = 5;

		/// <summary>
        /// The default value (10) for the <c>VelocityLoop</c> property.
		/// </summary>
        /// <remarks>Default value is based on the Traxster Robot Kit drivetrain configuration</remarks>
        public const short VelocityLoopDefault = 10;

        /// <summary>
        /// The default value (1) for the <c>DistanceProportinal</c> property.
        /// </summary>
        /// <remarks>Default value is based on the Traxster Robot Kit drivetrain configuration</remarks>
        public const short DistanceProportionalDefault = 1;

        /// <summary>
        /// The default value (0) for the <c>DistanceIntegral</c> distance property.
        /// </summary>
        /// <remarks>Default value is based on the Traxster Robot Kit drivetrain configuration</remarks>
        public const short DistanceIntegralDefault = 0;

        /// <summary>
        /// The default value (0) for the <c>DistanceDerivative</c> distance property.
        /// </summary>
        /// <remarks>Default value is based on the Traxster Robot Kit drivetrain configuration</remarks>
        public const short DistanceDerivativeDefault = 0;

        /// <summary>
        /// The default value (20) for the <c>DistanceAcceleration</c> distance property.
        /// </summary>
        /// <remarks>Default value is based on the Traxster Robot Kit drivetrain configuration</remarks>
        public const short DistanceAccelerationDefault = 20;

        /// <summary>
        /// The default value (5) for the <c>DistanceDeadband</c> distance property.
        /// </summary>
        /// <remarks>Default value is based on the Traxster Robot Kit drivetrain configuration</remarks>
        public const short DistanceDeadbandDefault = 5;

        /// <summary>
        /// The default value (3.57) for the <c>VelocityDivider</c> property
        /// </summary>
        /// <remarks>Default value is based on the Traxster Robot Kit drivetrain configuration</remarks>
        public const double VelocityDividerDefault = 3.57;

		#endregion

        #region PUBLIC CONFIGURATION CONSTANTS
        /// <summary>
        /// The default value (4) for the <c>EncoderResolution</c> property
        /// </summary>
        /// <remarks>Default value is based on the Traxster/Stinger Robot Kit gearhead motor configuration</remarks>
        public const short EncoderResolutionDefault = 624;

        /// <summary>
        /// The default value (0.0185) for the <c>GearReductionDefault</c> property
        /// </summary>
        /// <remarks>Default value is based on the Traxster/Stinger Robot Kit gearhead motor configuration</remarks>
        public const double GearReductionDefault = 0.0192;  // 52:1

        /// <summary>
        /// The default value (2.9) for the <c>WheelDiameter</c> property
        /// NOTE: Traxtser = 2.44"
        ///       Stinger  = 2.25"
        /// </summary>
        /// <remarks>Default value is based on the Traxster Robot Kit drivetrain configuration.</remarks>
        public const double WheelDiameterDefault = 2.44;

        /// <summary>
        /// The default value (10) for the <c>Track</c> property (distance between center of drive wheels)
        /// NOTE: Traxster = 9.75"
        ///       Stinger  = 8.75";
        /// </summary>
        /// <remarks>Default value is based on the Traxster/Stinger Robot Kit drivetrain configuration</remarks>
        public const double TrackDefault = 9.75;

        #endregion

        #region PRIVATE CONFIGURATION CONSTANTS
        private const double TicksPerRevolutionDefault = 624;
        private const double WheelCircumferenceDefault = 9.11;
        private const double TrackCircumferenceDefault = 22.777;  // track * pi -- used for rotational calcs
        #endregion

        #region CTORS

        /// <summary>
		/// Initializes a new instance of the <c>PidDCMotorController</c> class.
		/// </summary>
		public PIDMotorController()
		{
			vPidCommJob = new DelegateCommunicationJob(delegate()
			{
				return String.Format(
					"vpid {0}:{1}:{2}:{3}", vProp, vInteg, vDeriv, vLoop);
			});

            stopCommJob = new DelegateCommunicationJob(delegate()
            {
                return String.Format("stop");
            });

            dPidCommJob = new DelegateCommunicationJob(delegate()
            {
                return String.Format(
                    "dpid {0}:{1}:{2}:{3}", dProp, dInteg, dDeriv, dAccel);
            });

            dPidDBCommJob = new DelegateCommunicationJob(delegate()
            {
                return String.Format(
                    "dpid {0}:{1}:{2}:{3}:{4}", dProp, dInteg, dDeriv, dAccel, dDeadband);
            });

            mogoCommJob = new DelegateCommunicationJob(delegate()
            {
                return String.Format("mogo 1:{0} 2:{1}", motor1Velocity, motor2Velocity);
            });

            digoCommJob = new DelegateCommunicationJob(delegate()
            {
                return String.Format("digo 1:{0}:{1} 2:{2}:{3}", motor1Distance, motor1Velocity, motor2Distance, motor2Velocity);
            });

            stingerPidCommJob = new DelegateCommunicationJob(delegate()
            {
                return String.Format("rpid s");
            });

            traxsterPidCommJob = new DelegateCommunicationJob(delegate()
            {
                return String.Format("rpid t");
            });
        }

		/// <summary>
		/// Initializes a new instance of the <c>PidDCMotorController</c> class, attaching
        /// it to the specified <c>Element</c> instance.
		/// </summary>
		public PIDMotorController(Element element)
			: this()
		{
            this.Element = element;
        }

		#endregion		
			
        #region VPID PROPERTIES

        /// <summary>
        /// The Proportional value used in the Velocity PID calculation algorithm in the Element firmware..
		/// </summary>
        /// <remarks>See PID section of the Element User Guide for more information on operation of this parameter</remarks>
		public short VelProportional
		{
			get { return vProp; }

			set 
			{
				Toolbox.AssertInRange(value, 0, Int16.MaxValue);
				vProp = value;
                OnVpidChanged();
			}
		}

		/// <summary>
        /// The Integral value used in the Velocity PID calculation algorithm in the Element firmware..
        /// </summary>		
        /// <remarks>See PID section of the Element User Guide for more information on operation of this parameter</remarks>
		public short VelIntegral
		{
			get { return vInteg; }

			set 
			{
				Toolbox.AssertInRange(value, 0, Int16.MaxValue);
				vInteg = value;
                OnVpidChanged();
            }
		}

		/// <summary>
        /// The Derivative value used in the Velocity PID calculation algorithm in the Element firmware..
		/// </summary>
        /// <remarks>See PID section of the Element User Guide for more information on operation of this parameter</remarks>
        public short VelDerivative
		{
			get { return vDeriv; }

			set 
			{
				Toolbox.AssertInRange(value, 0, Int16.MaxValue);
				vDeriv = value;
                OnVpidChanged();
            }
		}

		/// <summary>
        /// The Loop value used in the Velocity PID calculation algorithm in the Element firmware..
		/// </summary>
        /// <remarks>See PID section of the Element User Guide for more information on operation of this parameter</remarks>
        public short VelLoop
		{
			get { return vLoop; }

			set 
			{
				Toolbox.AssertInRange(value, 0, Int16.MaxValue);
				vLoop = value;
                OnVpidChanged();
            }
		}

        /// <summary>
        /// Value used to divide Speed property by to arrive at the correct
        /// motor velocity based on the vpid settings.<br/>This allows customers to use
        /// the speed property to specify a value between 0 and 100, and that value
        /// is then divied by the VelocityDivider to arrive at the proper velocity
        /// for the mogo and digo commands.<br/>To determine your VelocityDivider property value,
        /// first determine your max motor velocity (see PID section of the Element User Guide),
        /// then divide 100 by that max motor velocity.  This value is your VelocityDivider.
        /// </summary>
        public double VelocityDivider
        {
            get { return velDivider; }
            set { velDivider = value; }
        }
        #endregion

        #region DPID PROPERTIES
        /// <summary>
        /// The Proportional value used in the Distance PID calculation algorithm in the Element firmware.
        /// </summary>
        /// <remarks>See PID section of the Element User Guide for more information on operation of this parameter</remarks>
        public short DistProportional
        {
            get { return dProp; }

            set
            {
                Toolbox.AssertInRange(value, 0, Int16.MaxValue);
                dProp = value;
                OnDpidChanged();
            }
        }

        /// <summary>
        /// The Integral value used in the Distance PID calculation algorithm in the Element firmware..
        /// </summary>
        /// <remarks>See PID section of the Element User Guide for more information on operation of this parameter</remarks>
        public short DistIntegral
        {
            get { return dInteg; }

            set
            {
                Toolbox.AssertInRange(value, 0, Int16.MaxValue);
                dInteg = value;
                OnDpidChanged();
            }            
        }

        /// <summary>
        /// The Derivative value used in the Distance PID calculation algorithm in the Element firmware..
        /// </summary>
        /// <remarks>See PID section of the Element User Guide for more information on operation of this parameter</remarks>
        public short DistDerivative
        {
            get { return dDeriv; }

            set
            {
                Toolbox.AssertInRange(value, 0, Int16.MaxValue);
                dDeriv = value;
                OnDpidChanged();
            }
        }

        /// <summary>
        /// The Acceleration value used in the Distance PID calculation algorithm in the Element firmware..
        /// </summary>
        /// <remarks>See PID section of the Element User Guide for more information on operation of this parameter</remarks>
        public short DistAcceleration
        {
            get { return dAccel; }

            set
            {
                Toolbox.AssertInRange(value, 0, Int16.MaxValue);
                dAccel = value;
                OnDpidChanged();
            }
        }

        /// <summary>
        /// Since the Distance Deadband only works with firmware versions 1.5.1 on, 
        /// this property can be used to disable the setting of the deadband parameter
        /// in the distance pid params.
        /// </summary>
        public bool DeadbandEnabled
        {
            get { return dDeadbandEnabled; }
            set { dDeadbandEnabled = value; }
        }

        /// <summary>
        /// The Deadband value used in the Distance PID calculation algorithm in the Element firmware...
        /// (Only for Firmware versions 1.5.1 and after).
        /// </summary>
	    public short DistDeadband
	    {
		    get { return dDeadband;}
		    set 
            {
                Toolbox.AssertInRange(value, 0, 100);
                dDeadband = value;

                if (DeadbandEnabled)
                {
                    OnDpidChanged();
                }
            }
	    }
	

        #endregion

        #region PHYSICAL PROPERTIES
        /// <summary>
        /// EncoderResolution property used to specify the wheel encoder disk resolution (e.g. 4 spoke, 44 spoke, etc. encoder disk).
        /// </summary>
        public short EncoderResolution
        {
            get { return encoderResolution; }
            set 
            { 
                encoderResolution = value;
                ticksPerRevolution = encoderResolution * (1 / GearReduction);
            }
        }

        /// <summary>
        /// GearReduction property.  Any gear reduction which occurs between the motor output shaft,
        /// and the encoder shaft (e.g. 52:1 = 0.0192).  Using the Encoder Resolution, and GearReduction, 
        /// the library calculates the number of TPR (Ticks Per Revolution), and stores it internally.
        /// </summary>
        public double GearReduction
        {
            get { return gearReduction; }
            set 
            { 
                gearReduction = value;
                ticksPerRevolution = EncoderResolution * (1 / gearReduction);
            }
        }


        /// <summary>
        /// TicksPerRevolution Property.  This property specifies the number of encoder ticks
        /// that the Element will see for each revolution of the drive wheel.
        /// </summary>
        public double TicksPerRevolution
        {
            get { return ticksPerRevolution; }
            set { ticksPerRevolution = value; }
        }


        /// <summary>
        /// WheelDiameter property.  The library automatically calculates wheel circumference,
        /// and stores it internally when this property is set.
        /// </summary>
        public double WheelDiameter
        {
            get { return wheelDiameter; }
            set 
            { 
                wheelDiameter = value;
                // Automatically set the wheel circumference:
                wheelCircumference = Math.PI * WheelDiameter;
            }
        }

        /// <summary>
        /// WheelTrack property which represents the distance between drive wheels or drive tracks.
        /// The library automatically calculates track rotational circumference, and stores it 
        /// internally when this property is set.  These values are used to determine the proper
        /// encoder tick count when rotating a specified angle.
        /// </summary>
        public double WheelTrack
        {
            get { return track; }
            set 
            { 
                track = value;
                // Automatically set the track circumference:
                trackCircumference = track * Math.PI;
            }
        }

        #endregion

        #region OPERATING PROPERTIES

        /// <summary>
        /// Speed Property, in a range of 0 to 100, where 0 is stopped, and 100 is full speed.
        /// This is a special property because the actual speed isn't the value that get's sent
        /// to the Element during a PID operation.  The value of speed is divided by the value
        /// from the <c>VelocityDivider</c> property, to arrive at the correct velocity used in
        /// the PID algorithms in the Element Firmware.  So, you must figure out what your
        /// Velocity Divider will be for your robot configuration, and set the property appropriately
        /// before invoking the PID interface.<br/>Make sure you've gone thru and
        /// calculated all of your PID parameters, determined your exact drivetrain
        /// measurements, and configured the appropriate properties before invoking 
        /// the PID interface!!!<br/>If you're using this interface to drive a Traxster Robot
        /// Kit, then all properties default to the correct values.  Hence, you can
        /// simply invoke the interface.  NOTE: The ultimate velocity calculated
        /// is set identically for each motor.
        /// </summary>
        /// <remarks>Set the value of this property before invoking TravelAtSpeed(), TravelDistance(), or Rotate().
        /// Otherwise, the speed will remain zero (by default), and your both won't do anything.</remarks>
        public int Speed
        {
            get { return speed; }
            set
            {
                speed = value;
                motor1Velocity = motor2Velocity = (int)(speed / velDivider);
            }
        }

        /// <summary>
        /// Motor1Speed Property, in a range of 0 to 100, where 0 is stopped, and 100 is full speed.
        /// This is a special property because the actual speed isn't the value that get's sent
        /// to the Element during a PID operation.  The value of speed is divided by the value
        /// from the <c>VelocityDivider</c> property, to arrive at the correct velocity used in
        /// the PID algorithms in the Element Firmware.  So, you must figure out what your
        /// Velocity Divider will be for your robot configuration, and set the property appropriately
        /// before invoking the PID interface.<br/>Make sure you've gone thru and
        /// calculated all of your PID parameters, determined your exact drivetrain
        /// measurements, and configured the appropriate properties before invoking 
        /// the PID interface!!!<br/>If you're using this interface to drive a Traxster Robot
        /// Kit, then all properties default to the correct values.  Hence, you can
        /// simply invoke the interface.  NOTE: The ultimate velocity calculated
        /// is set identically for each motor.
        /// </summary>
        /// <remarks>Set the value of this property before invoking TravelAtSpeed(), TravelDistance(), or Rotate().
        /// Otherwise, the speed will remain zero (by default), and your both won't do anything.</remarks>
        public int Motor1Speed
        {
            get { return motor1Speed; }
            set 
            { 
                motor1Speed = value;
                motor1Velocity = (int)(motor1Speed / velDivider);
            }
        }

        /// <summary>
        /// Motor2Speed Property, in a range of 0 to 100, where 0 is stopped, and 100 is full speed.
        /// This is a special property because the actual speed isn't the value that get's sent
        /// to the Element during a PID operation.  The value of speed is divided by the value
        /// from the <c>VelocityDivider</c> property, to arrive at the correct velocity used in
        /// the PID algorithms in the Element Firmware.  So, you must figure out what your
        /// Velocity Divider will be for your robot configuration, and set the property appropriately
        /// before invoking the PID interface.<br/>Make sure you've gone thru and
        /// calculated all of your PID parameters, determined your exact drivetrain
        /// measurements, and configured the appropriate properties before invoking 
        /// the PID interface!!!<br/>If you're using this interface to drive a Traxster Robot
        /// Kit, then all properties default to the correct values.  Hence, you can
        /// simply invoke the interface.  NOTE: The ultimate velocity calculated
        /// is set identically for each motor.
        /// </summary>
        /// <remarks>Set the value of this property before invoking TravelAtSpeed(), TravelDistance(), or Rotate().
        /// Otherwise, the speed will remain zero (by default), and your both won't do anything.</remarks>
        public int Motor2Speed
        {
            get { return motor2Speed; }
            set 
            { 
                motor2Speed = value;
                motor2Velocity = (int)(motor2Speed / velDivider);
            }
        }
	
        /// <summary>
        /// Distance Property used to command a robot to travel a specified distance.<br/>
        /// The calculations are based on the wheel diameter/circumference, 
        /// encoder resolution, and motor gearing.  Make sure you've gone thru and
        /// calculated all of your PID parameters, determined your exact drivetrain
        /// measurements, and configured the appropriate properties before invoking 
        /// the PID interface!!!  If you're using this interface to drive a Traxster Robot
        /// Kit, then all properties default to the correct values.  Hence, you can
        /// simply invoke the interface.
        /// </summary>
        /// <remarks>Set the value of this property before invoking TravelDistance().
        /// Otherwise, your both won't do anything.</remarks>
        public double Distance
        {
            get { return distance; }
            set
            {
                distance = value;
                
                // Determine how many ticks it'll take to go the specified distance:
                distanceTicks = (int)(distance * (1 / wheelCircumference) * ticksPerRevolution);

                // Set the distance (in ticks) for each motor to travel...
                motor1Distance = (int)distanceTicks;
                motor2Distance = (int)distanceTicks;
            }
        }

        /// <summary>
        /// RotationAngle Property used to command a robot to rotate a specified angle .
        /// (positive angle = CW rotation, negative angle = (CCW) rotation, 0 angle = no-op)
        /// The calculations are based on the wheel diameter/circumference, encoder resolution, 
        /// track distance/circumference, and motor gearing.  Make sure you've gone thru and
        /// calculated all of your PID parameters, determined your exact drivetrain
        /// measurements, and configured the appropriate properties before invoking 
        /// the PID interface!!!  If you're using this interface to drive a Traxster Robot
        /// Kit, then all properties default to the correct values.  Hence, you can
        /// simply invoke the interface.
        /// </summary>
        /// <remarks>Set the value of this property before invoking Rotate().
        /// Otherwise, your both won't do anything.</remarks>
        public double RotationAngle
        {
            get { return angle; }
            set
            {
                double tmpAngle;
                double fractionalRotation = 0;
                double angularDistancePercentage = 0;

                tmpAngle = Math.Abs(value);

                if (tmpAngle == 0)
                    return;

                // Figure out how far in ticks the motors need to turn...
                // Number of complete rotations:
                int fullRotations = (int)tmpAngle / 360;

                // Fractional Rotation left over:
                if ((tmpAngle % 360) != 0)
                    fractionalRotation = (360 / (tmpAngle - (fullRotations * 360)));

                // Fractional Distance:
                if (fractionalRotation > 0)
                    angularDistancePercentage = trackCircumference / fractionalRotation;

                // Final Distance:
                double finalDistance = (trackCircumference * fullRotations) + angularDistancePercentage;

                // Determine how many ticks it'll take to go the specified distance:
                double revolutions = finalDistance / wheelCircumference;

                distanceTicks = (int)(revolutions * ticksPerRevolution);

                // Set the distance (in ticks) for each motor to travel...
                if (value > 0)
                {
                    // Right Turn Clyde!
                    motor1Distance = distanceTicks;   // Left Motor
                    motor2Distance = -distanceTicks;  // Right Motor
                }
                else
                {
                    // Left Turn Clyde!
                    motor1Distance = -distanceTicks;  // Left Motor
                    motor2Distance = distanceTicks;   // Right Motor
                }

                // Save for next time thru:
                angle = value;
            }
        }
	
        #endregion

        #region PUBLIC METHODS
        /// <summary>
        /// Sets the default Stinger PID params on the Element (using 'rpid s')
        /// </summary>
        public void SetDefaultStingerPidParams()
        {
            Element.CommunicationTask.EnqueueCommJob(Priority.High, stingerPidCommJob);
        }

        /// <summary>
        /// Sets the default Traxster PID params on the Element (using 'rpid t')
        /// </summary>
        public void SetDefaultTraxsterPidParams()
        {
            Element.CommunicationTask.EnqueueCommJob(Priority.High, traxsterPidCommJob);
        }

        /// <summary>
        /// Commands the Element to invoke the Mogo Element interface to run at the previously configured <c>Speed</c>.
        /// </summary>
        /// <remarks>Make sure you've gone thru and
        /// calculated all of your PID parameters, determined your exact drivetrain
        /// measurements, and configured the appropriate properties before invoking 
        /// the PID interface!!!  If you're using this interface to drive a Traxster Robot
        /// Kit, then all properties default to the correct values.  Hence, you can
        /// simply invoke the interface.</remarks>
        public void TravelAtSpeed()
        {
            Element.CommunicationTask.EnqueueCommJob(Priority.High, mogoCommJob);
        }

        /// <summary>
        /// Commands the Element to invoke the Digo Element interface to travel the previously configured <c>Distance</c>.
        /// </summary>
        /// <remarks>Make sure you've gone thru and
        /// calculated all of your PID parameters, determined your exact drivetrain
        /// measurements, and configured the appropriate properties before invoking 
        /// the PID interface!!!  If you're using this interface to drive a Traxster Robot
        /// Kit, then all properties default to the correct values.  Hence, you can
        /// simply invoke the interface.</remarks>
        public void TravelDistance()
        {
            Element.CommunicationTask.EnqueueCommJob(Priority.High, digoCommJob);
        }

        /// <summary>
        /// Commands the Element to invoke the Digo Element interface to rotate the previously configured <c>RotationAngle</c>.
        /// </summary>
        /// <remarks>Make sure you've gone thru and
        /// calculated all of your PID parameters, determined your exact drivetrain
        /// measurements, and configured the appropriate properties before invoking 
        /// the PID interface!!!  If you're using this interface to drive a Traxster Robot
        /// Kit, then all properties default to the correct values.  Hence, you can
        /// simply invoke the interface.</remarks>
        public void Rotate()
        {
            Element.CommunicationTask.EnqueueCommJob(Priority.High, digoCommJob);
        }

        /// <summary>
        /// Commands the Element to invoke the Stop Element interface to immediately stop the drive motors.
        /// </summary>
        public void Stop()
        {
            Element.CommunicationTask.EnqueueCommJob(Priority.High, stopCommJob);
        }

        /// <summary>
        /// Queries the status of the internal PID algorithms in the Element.   
        /// NOTE: This method blocks until it has received the status back from the Element.
        /// This method uses the 'pids' command to query the PID algorithm state.
        /// A value of 'True' means the algorithm is busy, and 'False' means it has completed operation.
        /// </summary>
        /// <returns></returns>
        public bool QueryStatus()
        {
            int status = 1;
            string response = Element.CommunicationTask.EnqueueCommJobAndWait(Priority.Low, "pids");

            if (!String.IsNullOrEmpty(response))
            {
                response = response.Trim();

                try
                {
                    status = Convert.ToInt16(response);
                }
                catch (Exception e)
                {
                    Debug.WriteLine("Exception: QueryStatus Conversion response: " + response + ": " + e.Message);
                    status = 1;
                }
            }
            if (status == 1)
                return true;
            else
                return false;
        }

        /// <summary>
        /// Queries the current velocity of the motors using the Element 'vel' command.
        /// NOTE: This method blocks until it has received the status back from the Element.peration.
        /// </summary>
        /// <param name="vel1">motor velocity 1</param>
        /// <param name="vel2">motor velocity 2</param>
        public void QueryVelocities(ref int vel1, ref int vel2)
        {
            vel1 = 0;
            vel2 = 0;
            string response = Element.CommunicationTask.EnqueueCommJobAndWait(Priority.Low, "vel 1 2").Trim();
            
            if (!String.IsNullOrEmpty(response))
            {
                string[] value = response.Trim().Split();

                try
                {
                    if (value.Length == 2)
                    {
                        vel1 = Int32.Parse(value[0]);
                        vel2 = Int32.Parse(value[1]);
                    }
                }
                catch (Exception e)
                {
                    Debug.WriteLine("Exception: QueryVelocities response: " + response + ": " + e.Message);
                }
            }
        }
        #endregion

        #region PROTECTED METHODS

		/// <summary>
		/// Handler invoked when user sets VPID properties, which sends
        /// the appropriate command to the Element to change these properties.
		/// </summary>
        protected void OnVpidChanged()
        {
            Element.CommunicationTask.EnqueueCommJob(Priority.Low, vPidCommJob);
        }

		/// <summary>
		/// Handler invoked when user sets DPID properties, which sends
        /// the appropriate command to the Element to change these properties.
		/// </summary>
        protected void OnDpidChanged()
        {
            if (DeadbandEnabled)
                Element.CommunicationTask.EnqueueCommJob(Priority.Low, dPidDBCommJob);
            else
                Element.CommunicationTask.EnqueueCommJob(Priority.Low, dPidCommJob);
        }

		#endregion

		#region PRIVATES

		private ICommunicationJob vPidCommJob;
        private ICommunicationJob dPidCommJob;
        private ICommunicationJob dPidDBCommJob;
        private ICommunicationJob mogoCommJob;
        private ICommunicationJob digoCommJob;
        private ICommunicationJob stopCommJob;
        private ICommunicationJob stingerPidCommJob;
        private ICommunicationJob traxsterPidCommJob;

		private short vProp  = VelocityProportionalDefault;
		private short vInteg = VelocityIntegralDefault;
		private short vDeriv = VelocityDerivativeDefault;
		private short vLoop  = VelocityLoopDefault;

        private short dProp  = DistanceProportionalDefault;
        private short dInteg = DistanceIntegralDefault;
        private short dDeriv = DistanceDerivativeDefault;
        private short dAccel = DistanceAccelerationDefault;
        private short dDeadband = DistanceDeadbandDefault;
        private bool dDeadbandEnabled = false;

        private double velDivider = VelocityDividerDefault;

        private double distance = 0.0;
        private int distanceTicks = 0;
        private double angle = 0.0;

        private int motor1Distance = 0;
        private int motor1Velocity = 0;

        private int motor2Distance = 0;
        private int motor2Velocity = 0;

        private int motor1Speed = 0;
        private int motor2Speed = 0;
        private int speed = 0;

        // Physical measurements (initialized for Traxster):
        private short encoderResolution = EncoderResolutionDefault;
        private double gearReduction = GearReductionDefault;
        private double ticksPerRevolution = TicksPerRevolutionDefault;
        private double wheelDiameter = WheelDiameterDefault;
        private double wheelCircumference = WheelCircumferenceDefault;
        private double track = TrackDefault;
        private double trackCircumference = TrackCircumferenceDefault;
 
		#endregion
	}
}
