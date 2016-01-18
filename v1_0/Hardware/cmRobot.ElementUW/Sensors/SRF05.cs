using cmRobot.Element.Components;
using cmRobot.Element.Ids;
using cmRobot.Element;
using System;


namespace cmRobot.Element.Sensors
{
	/// <summary>
	/// Represents a SRF05 sensor.
	/// </summary>
    /// <include file='Docs\examples.xml' path='examples/example[@name="SRF05"]'/>
	public class SRF05 : DistanceSensorBase
	{

		#region Public Constants

		/// <summary>
		/// The default value for the <c>Pin</c> property.
		/// </summary>
		public const GpioPinId PinDefault = GpioPinId.Pin1;

		#endregion


		#region Ctors

		/// <summary>
		/// Initializes a new instance of the <c>SRF05</c> class.
		/// </summary>
		public SRF05()
		{
		}

		/// <summary>
		/// Initializes a new instance of the <c>SRF05</c> class, attaching
        /// it to the specified <c>Element</c> instance.
		/// </summary>
		public SRF05(Element element)
		{
			this.Element = element;
		}

		#endregion


		#region Public Properties

		/// <summary>
		/// Id of the pin to which the SRF05 sensor is attached.
		/// </summary>
		public GpioPinId Pin
		{
			get { return pin; }
			set { pin = value; }
		}

		#endregion


		#region Protected Methods

		/// <summary>
		/// Overridden to generate the command to query the value of
        /// a SRF05 sensor from the Element board.
		/// </summary>
		/// <returns>The generated command.</returns>
		protected override string GenerateCommand()
		{
			return String.Format("srf05 {0}", (ushort)pin);
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

		private GpioPinId pin = PinDefault;

		#endregion

	}

}
