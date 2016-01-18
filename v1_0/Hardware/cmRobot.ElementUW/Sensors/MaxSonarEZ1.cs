using cmRobot.Element.Components;
using cmRobot.Element.Ids;
using cmRobot.Element;
using System;


namespace cmRobot.Element.Sensors
{
    /// <summary>
    /// Represents a MAXSonarEZ-1 sonar sensor.
    /// </summary>
    /// <include file='Docs\examples.xml' path='examples/example[@name="MAXSonarEZ1"]'/>
    public class MaxSonarEZ1 : DistanceSensorBase
    {
        
		#region Public Constants

		/// <summary>
		/// The default value for the <c>TriggerPin</c> property.
		/// </summary>
		public const GpioPinId TriggerPinDefault = GpioPinId.Pin3;
        /// <summary>
        /// The default value for the <c>OutputPin</c> property.
        /// </summary>
        public const GpioPinId OutputPinDefault = GpioPinId.Pin4;

		#endregion


		#region Ctors

		/// <summary>
		/// Initializes a new instance of the <c>MaxSonarEZ1</c> class.
		/// </summary>
		public MaxSonarEZ1()
		{
		}

		/// <summary>
		/// Initializes a new instance of the <c>MaxSonarEZ1</c> class, attaching
        /// it to the specified <c>Element</c> instance.
		/// </summary>
		public MaxSonarEZ1(Element element)
		{
			this.Element = element;
		}

		#endregion


		#region Public Properties

		/// <summary>
		/// Id of the trigger pin to which the MaxSonarEZ1 sensor is attached.
		/// </summary>
        public GpioPinId TriggerPin
		{
			get { return triggerPin; }
			set { triggerPin = value; }
		}

        /// <summary>
        /// Id of the output pin to which the MaxSonarEZ1 sensor is attached.
        /// </summary>
        public GpioPinId OutputPin
        {
            get { return outputPin; }
            set { outputPin = value; }
        }

		#endregion


		#region Protected Methods

		/// <summary>
		/// Overridden to generate the command to query the value of
        /// a MaxSonarEZ1 sensor from the Element board.
		/// </summary>
		/// <returns>The generated command.</returns>
		protected override string GenerateCommand()
		{
			return String.Format("maxez1 {0} {1}", (ushort)triggerPin, (ushort)outputPin);
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

		private GpioPinId triggerPin = TriggerPinDefault;
        private GpioPinId outputPin = OutputPinDefault;

		#endregion

	}
}
