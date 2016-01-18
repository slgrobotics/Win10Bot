using cmRobot.Element.Components;
using cmRobot.Element.Ids;
using cmRobot.Element;
using System;

using slg.RobotAbstraction.Sensors;
using slg.RobotAbstraction.Events;

namespace cmRobot.Element.Sensors
{

	/// <summary>
	/// Represents a SRF08 sensor.
	/// </summary>
	/// <include file='Docs\remarks.xml' path='/remarks/remarks[@name="SRF08"]'/>
	/// <include file='Docs\examples.xml' path='examples/example[@name="SRF08"]'/>
	public class SRF08 : DistanceSensorBase, ILightSensor
	{

		#region Public Constants

		/// <summary>
		/// The default value for the <c>I2CAddressDefault</c> property.
		/// </summary>
		public const byte I2CAddressDefault = 0xE0;

		/// <summary>
		/// The default value for the <c>LightLevelChangedThresholdDefault</c> property.
		/// </summary>
		public const byte LightLevelChangedThresholdDefault = 5;

		#endregion


		#region Ctors

		/// <summary>
		/// Initializes a new instance of the <c>SRF08</c> class.
		/// </summary>
		public SRF08()
		{
		}

		/// <summary>
		/// Initializes a new instance of the <c>SRF08</c> class, attaching
        /// it to the specified <c>Element</c> instance.
		/// </summary>
		public SRF08(Element element)
		{
			this.Element = element;
		}

		#endregion


		#region Public Properties

		/// <summary>
		/// I2C Address to which the SRF08 sensor is attached.
		/// </summary>
        /// <include file='Docs\examples.xml' path='examples/example[@name="SRF08"]'/>
		public byte I2CAddress
		{
			get { return i2cAddr; }
			set { i2cAddr = value; }
		}

		/// <summary>
		/// The light level reported by the SRF08 sensor.
		/// </summary>
        /// <include file='Docs\examples.xml' path='examples/example[@name="SRF08"]'/>
		public byte LightLevel
		{
			get { return lightLevel; }
		}

		/// <summary>
		/// Specifies the amount that <c>LightLevel</c> must change before 
		/// the <c>LightLevelChanged</c> event is signalled.
		/// </summary>
        /// <include file='Docs\examples.xml' path='examples/example[@name="SRF08"]'/>
		public byte LightLevelChangedThreshold
		{
			get { return lightLevelThreshold; }
			set { lightLevelThreshold = value; }
		}

		#endregion


		#region Public Events

		/// <summary>
		/// Occurs when <c>LightLevel</c> has changed by an amount greater 
		/// than <c>LightLevelChangedThreshold</c>.
		/// </summary>
		/// <include file='Docs\examples.xml' path='examples/example[@name="SRF08"]'/>
		public event HardwareComponentEventHandler LightLevelChanged;

		#endregion


		#region Protected Methods

		/// <summary>
		/// Overridden to generate the command to query the state of
        /// a SRF08 sensor from the Element board.
		/// </summary>
		/// <returns>The generated command.</returns>
		protected override string GenerateCommand()
		{
			return String.Format("srf08 {0}", i2cAddr);
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
			string[] vals = response.Split(' ');
			byte lightLevel = (byte)(Byte.Parse(vals[0]) * 100 / 255);
			double distance = Byte.Parse(vals[1]);
			OnSetDistance(distance);
			OnSetLightLevel(lightLevel);
		}

		/// <summary>
		/// Occurs when the light level read from the SRF08 sensor is updated.
		/// Signals <c>LightLevelChanged</c>, if necessary.
		/// </summary>
		/// <param name="lightLevel"></param>
		protected void OnSetLightLevel(byte lightLevel)
		{
			this.lightLevel = lightLevel;

			// if distance change exceeds threshold, then fire event 
			if (Math.Abs(lightLevel- lastLightLevel) > LightLevelChangedThreshold)
			{
				lastLightLevel = lightLevel;
				Element.SignalEvent(LightLevelChanged, this);
			}
		}

		#endregion


		#region Privates

		private byte i2cAddr = I2CAddressDefault;
		private byte lightLevel = 0;
		private byte lastLightLevel = 0;
		private byte lightLevelThreshold = LightLevelChangedThresholdDefault;

		#endregion

	}

}
