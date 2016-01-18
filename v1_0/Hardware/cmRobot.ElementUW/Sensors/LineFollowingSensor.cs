using cmRobot.Element.Components;
using cmRobot.Element.Ids;
using cmRobot.Element;
using System;

using slg.RobotAbstraction.Sensors;
using slg.RobotAbstraction.Events;

namespace cmRobot.Element.Sensors
{
	/// <summary>
	/// Represents an I2C Line Following sensor (with 5 sensors, sold by RoboticsConnection.com)
	/// </summary>
    public class LineFollowingSensor : QueryableComponentBase
    {
        #region Public Constants

        /// <summary>
        /// The default value for the <c>I2CAddressDefault</c> property.
        /// </summary>
        public const byte I2CAddressDefault = 0x50;

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <c>LineFollowingSensor</c> class.
        /// </summary>
        public LineFollowingSensor()
		{
			for (int i = 0; i < sensor.Length; ++i)
			{
				sensor[i] = new int();
			}
		}

        /// <summary>
        /// Initializes a new instance of the <c>LineFollowingSensor</c> class, attaching
        /// it to the specified <c>Element</c> instance.
        /// </summary>
        /// <param name="s"></param>
        public LineFollowingSensor(Element s)
            : this()
		{
			this.Element = s;
		}

		#endregion

        #region Public Properties

        /// <summary>
        /// I2C Address to which the LineFollowingSensor is attached.
        /// </summary>
        public byte I2CAddress
        {
            get { return i2cAddr; }
            set { i2cAddr = value; }
        }

        /// <summary>
        /// The LineFollowingSensor represents an array of on/off sensors, 
        /// indexed from 0 to 4, represented by a 1 or 0, respectively.
        /// </summary>
        /// <param name="i">The index value.</param>
        /// <returns>An instance of a single sensor at the specified index.</returns>
        public int this[int i]
        {
            get { return sensor[i]; }
        }

		/// <summary>
		/// If you wish to receive constant LineFollowerChanged event notifications, whether
		/// the sensor readings have changed or not, then set this property to true.
		/// Otherwise, LineFollowerChanged event notifications will only be issued
		/// if the value actually changes.
		/// </summary>
		public bool ConstantNotifications
		{
			get { return constantNotifications; }
			set { constantNotifications = value; }
		}
	

        #endregion

        #region Event
        /// <summary>
        /// Occurs when a sensor on the LineFollowingSensor has changed
        /// </summary>
        public event HardwareComponentEventHandler LineFollowerChanged;
        #endregion

        #region Protected Methods
        /// <summary>
        /// Overridden to generate the command to query the value of
        /// a LineFollowingSensor from the Element board.
        /// </summary>
        /// <returns>The generated command.</returns>
        protected override string GenerateCommand()
        {
            return String.Format("line {0}", i2cAddr);
        }

        /// <summary>
        /// Overridden to parse the string returned from the
        /// Element board in response to the command generated 
        /// by <c>GenerateCommand</c>.
        /// </summary>
        /// <param name="response">The response string.</param>
        protected override void ProcessResponse(string response)
        {
			bool changed = false;
            string[] vals = response.Trim().Split(' ');
            for (int i=0; i<5; i++)
            {
				int v = Int16.Parse(vals[i]);

				// If the sensor value changed...
				if (sensor[i] != v)
				{
					sensor[i] = v;
					changed = true;
				}
            }

			// If the sensor value changed, or if we want constant
			// notifications, whether it changed or not...
			if (changed || constantNotifications)
			{
				// Notify listeners that line following sensor has changed:
				Element.SignalEvent(LineFollowerChanged, this);
			}
        }

        #endregion

        #region Private Members
        private int[] sensor = new int[5];
        private byte i2cAddr = I2CAddressDefault;
		private bool constantNotifications = false;
        #endregion
    }
}
