using cmRobot.Element.Internal;
using System;


namespace cmRobot.Element.Controllers
{

	/// <summary>
	/// Represents a Pulse Width Modulation motor controller.
	/// </summary>
	/// <include file='Docs\remarks.xml' path='/remarks/remarks[@name="PwmDCMotorController"]'/>
	public class PwmDCMotorController : DCMotorControllerBase
	{

		#region Ctors

		/// <summary>
		/// Initializes a new instance of the <c>PwmDCMotorController</c> class.
		/// </summary>
		public PwmDCMotorController()
		{
			 commJob = new DelegateCommunicationJob(delegate()
			 {
				 return String.Format("pwm r:{0} {1}:{2}", _rampingRate, (ushort)DCMotorId, Speed);
			 });

			 velCommJob = new DelegateCommunicationJob(delegate()
			 {
				 return String.Format("vel {0}", (ushort)DCMotorId);
			 });
		}

		/// <summary>
		/// Initializes a new instance of the <c>PwmDCMotorController</c> class, attaching
        /// it to the specified <c>Element</c> instance.
		/// </summary>
		public PwmDCMotorController(Element element)
			: this()
		{
			this.Element = element;
		}

		#endregion

        #region Properties

        /// <summary>
        /// Rate at which motors will be ramped up to the specified speed.  A value of 0 produces no ramping,
        /// thus motors will reach the specified speed immediately.  A (max) value of 100 will take the longest
        /// amount of time to ramp the motors to the specified speed.
        /// </summary>
        public int RampingRate
        {
            get { return _rampingRate; }
            set { _rampingRate = value; }
        }

        #endregion


        #region Protected Methods

        /// <summary>
        /// Overridden to command the Element board to set the
		/// DC motors speed accordingly.
		/// </summary>
		/// <param name="speed">The new speed value.</param>
		protected override void OnSpeedChanged(int speed)
		{
			Element.CommunicationTask.EnqueueCommJob(Priority.High, commJob);
		}

		#endregion


		#region Privates

		private ICommunicationJob commJob;
		private ICommunicationJob velCommJob;
		private int _rampingRate = 0;
		#endregion

	}

}
