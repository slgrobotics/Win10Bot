/*
 * Copyright (c) 2016..., Sergei Grichine   http://trackroamer.com
 * 
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at 
 *    http://www.apache.org/licenses/LICENSE-2.0
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 *    
 * this is a no-warranty no-liability permissive license - you do not have to publish your changes,
 * although doing so, donating and contributing is always appreciated
 */

using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Diagnostics;

using slg.RobotAbstraction.Events;
using slg.RobotAbstraction.Sensors;
using slg.RobotBase;

namespace slg.Sensors
{
    /// <summary>
    /// Raspberry Pi camera connected via Ethernet, running Python script and OpenCV.
    /// This class creates HTTP Server and receives lines in the form "*226 143 14 5 1" - x, y, width, height, signature
    /// WARNING: Universal Windows does not allow processes on the same machine to access the listeners.
    /// Use different machine to hit this server. Raspberry Pi with camera running linux and Python/OpenCV is such machine.
    /// </summary>
    public class RPiCamera : HttpServerBase, IDisposable, ITargetingCamera
    {
        public string Name { get; set; }

        public bool Enabled { get; set; }

        //private SerialPort _serialPort = null;
        private int HttpPort; // = "9097";

        #region Public Events

        /// <summary>
        /// Occurs when RPiCamera <c>Detected Blocks</c> has changed.
        /// </summary>
        public event EventHandler<TargetingCameraEventArgs> TargetingCameraTargetsChanged;

        #endregion

        #region Lifecycle

        public RPiCamera(string cameraName, int httpPort)
            : base(httpPort)
        {
            Name = cameraName;
            HttpPort = httpPort;
        }

        public async Task Open(CancellationTokenSource cts)
        {
            try
            {
                base.StartServer();

                Debug.WriteLine("OK: RPiCamera " + Name + "  Open(" + HttpPort + ") success!");
            }
            catch
            {
                Debug.WriteLine("Error: RPiCamera " + Name + "  Open(" + HttpPort + ") failed");
            }
        }

        public void Close()
        {
            base.StopServer();
        }

        ~RPiCamera()
        {
            Close();
            base.Dispose();
        }

        #endregion // Lifecycle

        protected override async Task<string> GetPageContent(string localPath, string postData)
        {
            string pageContent = string.Empty;

            if (String.IsNullOrWhiteSpace(postData))
            {
                pageContent = "NAK";
            }
            else
            {
                if (Enabled)
                {
                    await CameraDataReceived(postData);
                }
                pageContent = "ACK";
            }

            return pageContent;
        }

        DateTime lastLineReceived;
        char[] splitChar = new char[] { ' ' };

        /// <summary>
        /// Serial Port data event handler
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async Task CameraDataReceived(string line)
        {
            //Debug.WriteLine(line);

            // line in the form "226 143 14 5 1" - x, y, width, height, signature

            // Field of view (320x240 pixels; 0,0 at the upper left):
            //     goal 45 degrees  left  x=10
            //                    middle  x=160
            //     goal 45 degrees right  x=310
            //
            //     goal 30 degrees  up    y=10
            //                    middle  y=90
            //     goal 30 degrees down   y=190
            //

            DateTime now = DateTime.Now;

            if (TargetingCameraTargetsChanged != null)
            {
                try
                {
                    string[] split = line.Split(splitChar);

                    // Send data to whoever interested:
                    TargetingCameraTargetsChanged(this, new TargetingCameraEventArgs()
                    {
                        cameraName = Name,
                        x = int.Parse(split[0]),
                        y = int.Parse(split[1]),
                        width = int.Parse(split[2]),
                        height = int.Parse(split[3]),
                        signature = int.Parse(split[4]),
                        timestamp = now.Ticks
                    });
                }
                catch { }
            }

            double msSinceLastReceived = (now - lastLineReceived).TotalMilliseconds;
            lastLineReceived = now;
            //Debug.WriteLine("OK: '" + line + "'      ms: " + Math.Round(msSinceLastReceived));
        }
    }
}
