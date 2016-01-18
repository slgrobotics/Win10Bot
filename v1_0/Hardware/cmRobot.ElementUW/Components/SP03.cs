using cmRobot.Element.Internal;
using System;

namespace cmRobot.Element.Components
{

	/// <summary>
	/// Represenets a SP03 SpeechSynthesizer.
	/// </summary>
	/// <include file='Docs\remarks.xml' path='/remarks/remarks[@name="SP03"]'/>
	public class SP03 : ElementComponent, ISpeechSynthesizer
	{

		#region Public Constants

		/// <summary>
		/// The default value for the <c>I2CAddressDefault</c> property.
		/// </summary>
		public const byte I2CAddressDefault = 0xC0;

		#endregion


		#region Ctors

		/// <summary>
		/// Initializes a new instance of the <c>SP03</c> class.
		/// </summary>
		public SP03()
		{
		}

		/// <summary>
		/// Initializes a new instance of the <c>SP03</c> class, attaching
        /// it to the specified <c>Element</c> instance.
		/// </summary>
		public SP03(Element element)
		{
			this.Element = element;
		}

		#endregion


		#region Public Properties

		/// <summary>
		/// I2C Address to which the SP03 sensor is attached.
		/// </summary>
		public byte I2CAddress
		{
			get { return i2cAddr; }
			set { i2cAddr = value; }
		}

		#endregion


		#region Public Methods

		/// <summary>
		/// Converts the specified text to speech and vocalizes it.
		/// </summary>
		/// <param name="phrase">The text to announce.</param>
		public void Speak(string phrase)
		{
			Element.CommunicationTask.EnqueueCommJob(
				Priority.High, String.Format("sp03 {0}", phrase));
		}

		/// <summary>
		/// Vocalized the specified preprogrammed phrase.
		/// </summary>
		/// <param name="phraseId">The id of the phrase to announce.</param>
		public void SpeakCannedPhrase(short phraseId)
		{
			Toolbox.AssertInRange(phraseId, 0, MaxPhraseId);

			Element.CommunicationTask.EnqueueCommJob(
				Priority.High, String.Format("sp03 {0}", phraseId));
		}

		#endregion


		#region Privates

		private const short MaxPhraseId = 16;
		private byte i2cAddr = I2CAddressDefault;
		
		#endregion

	}

}
