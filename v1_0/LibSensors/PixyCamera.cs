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
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

using slg.RobotAbstraction;
using slg.RobotAbstraction.Events;
using slg.RobotAbstraction.Sensors;
using slg.LibRobotExceptions;
using slg.LibCommunication;

namespace slg.LibSensors
{
    /// <summary>
    /// Pixy camera connected via Arduino.
    /// This class opens serial to Arduino and receives lines in the form "*226 143 14 5 1" - x, y, width, height, signature
    /// See C:\Projects\Arduino\Sketchbook\PixyToSerial\PixyToSerial.ino
    /// </summary>
    public class PixyCamera : ITargetingCamera
    {
        public string Name { get; set; }

        public bool Enabled { get; set; }

        public string ComPortName { get; set; }
        private int ComBaudRate; // = 115200;

        public bool running = false;
        private ICommunicationChannel serialChannel;
        private CancellationTokenSource cancellationTokenSource;

        #region Public Events

        /// <summary>
        /// Occurs when PixyCamera <c>Detected Blocks</c> has changed.
        /// </summary>
        public event EventHandler<TargetingCameraEventArgs> TargetingCameraTargetsChanged;

        #endregion

        public PixyCamera(string cameraName, string comPortName, int comBaudRate)
        {
            Name = cameraName;
            ComPortName = comPortName;
            ComBaudRate = comBaudRate;
        }

        public async Task Open(CancellationTokenSource cts)
        {
            cancellationTokenSource = cts;

            serialChannel = new CommunicationChannelSerial(cts, false)
            {
                Name = ComPortName,
                BaudRate = (uint)ComBaudRate,
                NewLineIn = "\n",
                NewLineOut = "\n"   // not really used
            };

            try
            {
                Debug.WriteLine("IP: PixyCamera: serialPort " + ComPortName + " opening...");
                await serialChannel.Open();
                Debug.WriteLine("OK: PixyCamera: serialPort " + ComPortName + " opened");

                running = true;

                Task workerTask = new Task(() => ThreadProc(cts.Token), cts.Token);
                workerTask.Start();
            }
            catch (AggregateException aexc)
            {
                throw new CommunicationException("PixyCamera: Could not start communication to Pixy board");
            }
            catch (Exception exc)
            {
                Debug.WriteLine("Error: PixyCamera: exception while opening serial port " + ComPortName + " : " + exc);
                throw;
            }
        }

        public void Close()
        {
            running = false;

            if (serialChannel != null)
            {
                serialChannel.Close();
                serialChannel = null;
            }
        }

        ~PixyCamera()
        {
            Close();
        }

        private async void ThreadProc(CancellationToken ct)
        {
            Debug.WriteLine("PixyCamera: Started Worker Task");

            while (!ct.IsCancellationRequested)
            {
                if (!running)
                {
                    break;
                }

                string line = await serialChannel.ReadLine();     // *70 128 26 18 1
                //Debug.WriteLine(line);
                interpretPixyValues(line);
            }
            running = false;

            Debug.WriteLine("PixyCamera: Worker Task finished");
        }

        private DateTime lastLineReceived;
        private char[] splitChar = new char[] { ' ' };

        private void interpretPixyValues(string line)
        {
            DateTime now = DateTime.Now;

            //Debug.WriteLine(line);

            // line in the form "*226 143 14 5 1" - x, y, width, height, signature (asterisk is not in sb)

            // On Arduino:
            //      pixy.blocks[i].signature    The signature number of the detected object (1-7)
            //      pixy.blocks[i].x       The x location of the center of the detected object (0 to 319)
            //      pixy.blocks[i].y       The y location of the center of the detected object (0 to 199)
            //      pixy.blocks[i].width   The width of the detected object (1 to 320)
            //      pixy.blocks[i].height  The height of the detected object (1 to 200)

            // Field of view:
            //     goal 45 degrees  left  x=10
            //                    middle  x=160
            //     goal 45 degrees right  x=310
            //
            //     goal 30 degrees  up    y=10
            //                    middle  y=90
            //     goal 30 degrees down   y=190
            //

            if (TargetingCameraTargetsChanged != null && Enabled && line.StartsWith("*") && line.Length > 10 && line.Length < 30)
            {
                try
                {
                    string[] split = line.Substring(1).Split(splitChar);

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
                catch (ArgumentException ex)
                {
                    if (ex.Message == "Offset and length were out of bounds for the array or count is greater than the number of elements from index to the end of the source collection.")
                    {
                        Debug.WriteLine("Error: PixyCamera - Invalid Baud Rate");
                    }
                    else
                    {
                        Debug.WriteLine("Error: PixyCamera - Error Reading From Serial Port");
                    }
                }
                catch { }
            }

            double msSinceLastReceived = (now - lastLineReceived).TotalMilliseconds;
            lastLineReceived = now;
            //Debug.WriteLine("OK: interpretPixyValues(): '" + line + "'      ms: " + Math.Round(msSinceLastReceived));
        }
    }
}
