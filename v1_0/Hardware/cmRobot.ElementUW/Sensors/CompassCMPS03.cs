using cmRobot.Element.Internal;
using System;

using slg.RobotAbstraction.Sensors;
using slg.RobotAbstraction.Events;

namespace cmRobot.Element.Sensors
{

	/// <summary>
	/// Represents a CMPS03 compass sensor.
	/// </summary>
	/// <include file='Docs\remarks.xml' path='/remarks/remarks[@name="CMPS03"]'/>
	public class CompassCMPS03 : CompassSensorBase, ICompassCMPS03
    {

		#region Public Constants

		/// <summary>
		/// The default value for the <c>I2CAddressDefault</c> property.
		/// </summary>
        public const byte I2CAddressDefault = 0x60;

		#endregion


		#region Ctors

		/// <summary>
		/// Initializes a new instance of the <c>CMPS03</c> class.
		/// </summary>
		public CompassCMPS03()
		{
		}

		/// <summary>
		/// Initializes a new instance of the <c>CMPS03</c> class, attaching
        /// it to the specified <c>Element</c> instance.
		/// </summary>
		public CompassCMPS03(Element element)
		{
			this.Element = element;
		}

		#endregion


		#region Public Properties

		/// <summary>
		/// I2C Address to which the CMPS03 sensor is attached.
		/// </summary>
		public byte I2CAddress
		{
			get { return i2cAddr; }
			set { i2cAddr = value; }
		}

		#endregion


		#region Protected Methods

		/// <summary>
		/// Overridden to generate the command to query the value of
        /// a CMPS03 sensor from the Element board.
		/// </summary>
		/// <returns>The generated command.</returns>
		protected override string GenerateCommand()
		{
			return String.Format("cmps03 {0}", i2cAddr);
		}

		/// <summary>
		/// Overridden to parse the string returned from the
        /// Element board in response to the command generated 
		/// by <c>GenerateCommand</c>.
		/// </summary>
		/// <param name="response">The response string.</param>
		protected override void ProcessResponse(string response)
		{
			short value = Int16.Parse(response);
			OnSetHeading(value);
		}

		#endregion


		#region Privates

		private byte i2cAddr = I2CAddressDefault;

		#endregion

	}

}
