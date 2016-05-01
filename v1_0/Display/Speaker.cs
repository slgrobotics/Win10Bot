using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

using Windows.Media.SpeechSynthesis;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.Foundation;
using Windows.UI.Xaml.Controls;

using slg.RobotBase.Interfaces;

namespace slg.Display
{
    /// <summary>
    /// provides thread-safe ISpeaker implementation via WPF MediaElement control.
    /// </summary>
    public class Speaker : ISpeaker
    {
        private SpeechSynthesizer synthesizer;
        private BufferBlock<Tuple<string, int>> speakBufferBlock = new BufferBlock<Tuple<string, int>>();
        private bool speaking = false;
        private MediaElement media;

        /// <summary>
        /// have MediaElement defined in your XAML and pass it here
        /// </summary>
        /// <param name="_media"></param>
        public Speaker(MediaElement _media)
        {
            media = _media;
            media.MediaEnded += MediaElement_SpeakEnded;

            synthesizer = new SpeechSynthesizer();
        }

        /// <summary>
        /// ISpeaker implementation. This is thread safe operation.
        /// </summary>
        /// <param name="whatToSay">a string for text-to-speech conversion</param>
        /// <param name="voice">optional, default 0</param>
        public void Speak(string whatToSay, int voice = 0)
        {
            if (!String.IsNullOrWhiteSpace(whatToSay))
            {
                //Debug.WriteLine("Speak: '" + whatToSay + "'    queue: " + speakBufferBlock.Count);

                var speakArgs = new Tuple<string, int>(whatToSay, voice);

                if (!speaking && speakBufferBlock.Count == 0)
                {
                    RunSpeakTask(speakArgs);    // empty queue, not speaking - start task
                }
                else
                {
                    while (speakBufferBlock.Count > 3)
                    {
                        Tuple<string, int> sa = null;

                        if (speakBufferBlock.TryReceive(out sa))
                        {
                            Debug.WriteLine("Speak: dumped excessive message: " + sa.Item1);
                        }
                    }
                    speakBufferBlock.Post(speakArgs);
                }
            }
        }

        private void RunSpeakTask(Tuple<string, int> speakArgs)
        {
            //Debug.WriteLine("RunSpeakTask: " + speakArgs.Item1);

            speaking = true;

            Task task = Task.Factory.StartNew(() =>
            {
                var voices = SpeechSynthesizer.AllVoices;
                synthesizer.Voice = voices[speakArgs.Item2];

                var spokenStream = synthesizer.SynthesizeTextToStreamAsync(speakArgs.Item1);

                spokenStream.Completed += this.SpokenStreamCompleted;
            });
        }

        /// <summary>
        /// The spoken stream is ready.
        /// </summary>
        private async void SpokenStreamCompleted(IAsyncOperation<SpeechSynthesisStream> asyncInfo, AsyncStatus asyncStatus)
        {
            //Debug.WriteLine("SpokenStreamCompleted");

            // Make sure to be on the UI Thread.
            var synthesisStream = asyncInfo.GetResults();
            await media.Dispatcher.RunAsync(
                            Windows.UI.Core.CoreDispatcherPriority.Normal,
                            new DispatchedHandler(() => { media.AutoPlay = true; media.SetSource(synthesisStream, synthesisStream.ContentType); media.Play(); })
            );
        }

        private void MediaElement_SpeakEnded(object sender, RoutedEventArgs e)
        {
            //Debug.WriteLine("MediaElement_SpeakEnded:    queue: " + speakBufferBlock.Count);

            Tuple<string, int> speakArgs = null;

            if (speakBufferBlock.TryReceive(out speakArgs))
            {
                // more speaking tasks in the queue, get the next one:
                RunSpeakTask(speakArgs);
            }
            else
            {
                speaking = false;
            }
        }
    }
}
