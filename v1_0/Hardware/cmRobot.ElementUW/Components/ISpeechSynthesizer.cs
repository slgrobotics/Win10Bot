using System;
using System.Collections.Generic;
using System.Text;

namespace cmRobot.Element.Components
{

	/// <summary>
	/// Interface that represents a speech synthesizer component.
	/// </summary>
	public interface ISpeechSynthesizer
	{

		/// <summary>
		/// This commands the speech synthesizer to convert the specified
		/// text to speech and vocalize it.
		/// </summary>
		/// <param name="phrase">The text to vocalize.</param>
		/// <exception cref="InvalidOperationException">
		/// Raised if the underlying device does not support text-to-speech.
		/// </exception>
		void Speak(string phrase);

		/// <summary>
		/// This commands the speech synthesizer to announce a preprogrammed
		/// phrase or sound.
		/// </summary>
		/// <param name="phraseId">The id of the phrase to announce.</param>
		/// <exception cref="InvalidOperationException">
		/// Raised if the underlying device does not support canned phrases.
		/// </exception>
		void SpeakCannedPhrase(short phraseId);

	}

}
