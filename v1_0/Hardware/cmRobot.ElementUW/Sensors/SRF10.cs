using cmRobot.Element.Components;
using cmRobot.Element.Ids;
using cmRobot.Element;
using System;

namespace cmRobot.Element.Sensors
{

	/// <summary>
	/// Represents a SRF10 sensor.
	/// </summary>
    /// <include file='Docs\examples.xml' path='examples/example[@name="SRF10"]'/>

	public class SRF10 : DistanceSensorBase
	{

		#region Public Constants

		/// <summary>
		/// The default value for the <c>I2CAddressDefault</c> property.
		/// </summary>
		public const byte I2CAddressDefault = 0xE0;

		#endregion


		#region Ctors

		/// <summary>
		/// Initializes a new instance of the <c>SRF10</c> class.
		/// </summary>
		public SRF10()
		{
		}

		/// <summary>
		/// Initializes a new instance of the <c>SRF10</c> class, attaching
        /// it to the specified <c>Element</c> instance.
		/// </summary>
		public SRF10(Element element)
		{
			this.Element = element;
		}

		#endregion


		#region Public Properties

		/// <summary>
		/// I2C Address to which the SRF10 sensor is attached.
		/// </summary>
		public byte I2CAddress
		{
			get { return i2cAddr; }
			set { i2cAddr = value; }
		}

		#endregion


		#region Protected Methods

		/// <summary>
		/// Overridden to generate the command to query the state of
        /// a SRF10 sensor from the Element board.
		/// </summary>
		/// <returns>The generated command.</returns>
		protected override string GenerateCommand()
		{
			return String.Format("srf10 {0}", i2cAddr);
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
			double value = Double.Parse(response);
			OnSetDistance(value);
		}

		#endregion


		#region Privates

		private byte i2cAddr = I2CAddressDefault;

		#endregion

	}

}
