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
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

using slg.RobotAbstraction;
using slg.RobotAbstraction.Events;
using slg.RobotAbstraction.Sensors;
using slg.RobotExceptions;
using slg.LibCommunication;

namespace slg.Sensors
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

        //private SerialPort _serialPort = null;
        private string ComPortName; // = "COM8";
        private int ComBaudRate; // = 115200;

        public bool running = false;
        private ICommunicationChannel serialChannel;
        private Stopwatch stopWatch = new Stopwatch();
        private CancellationTokenSource cancellationTokenSource;
        //public BufferBlock<CommandAndResponse> commandsBufferBlock { get; private set; }
        //private ActionBlock<CommandAndResponse> actionBlock;

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

            serialChannel = new CommunicationChannelSerial(cts)
            {
                Name = ComPortName,
                BaudRate = (uint)ComBaudRate,
                NewLineIn = "\r\n",
                NewLineOut = "\r\n"
            };

            try
            {
                Debug.WriteLine("IP: PixyCamera: serialPort " + ComPortName + " opening...");
                await serialChannel.Open();
                Debug.WriteLine("OK: PixyCamera: serialPort " + ComPortName + " opened");

                //commandsBufferBlock = new BufferBlock<CommandAndResponse>(
                //                        new DataflowBlockOptions() { CancellationToken = cts.Token });

                //actionBlock = new ActionBlock<CommandAndResponse>(
                //                        async prompt =>
                //                        {
                //                            await serialChannel.WriteLine(prompt.command);
                //                            string response = await serialChannel.ReadLine();
                //                            prompt.completionSource.SetResult(response);
                //                        },
                //                        new ExecutionDataflowBlockOptions() { CancellationToken = cts.Token, BoundedCapacity = 10 });

                //commandsBufferBlock.LinkTo(actionBlock, new DataflowLinkOptions { PropagateCompletion = true });

                Debug.WriteLine("IP: PixyCamera: trying to synch up with the board - resetting...");

                string resp = null;
                int count = 10;
                bool boardFound = false;

                while (count-- > 0)
                {
                    // try to sync up with the board
                    //resp = await SendAndReceive("reset");

                    Debug.WriteLine("OK: PixyCamera: 'reset' -> '" + resp + "'");

                    if (string.Equals(resp, "Arduino firmware Plucky Wheels"))
                    {
                        boardFound = true;
                        break;
                    }
                }

                if (boardFound)
                {
                    Debug.WriteLine("OK: PixyCamera: found Plucky Wheels Arduino brick");
                }
                else
                {
                    throw new CommunicationException("CommunicationTask: Could not find Plucky Wheels Arduino brick, invalid response to 'reset' at serial port " + ComPortName);
                }

                stopWatch.Start();
                running = true;
            }
            catch (AggregateException aexc)
            {
                throw new CommunicationException("CommunicationTask: Could not start communication");
            }
            catch (Exception exc)
            {
                Debug.WriteLine("Error: PixyCamera: exception while opening serial port " + ComPortName + " : " + exc);
                //await Stop();
                //throw new CommunicationException("CommunicationTask: Could not start communication");
                throw;
            }

            /*
             * TODO: port to Universal Windows
            try
            {
                _serialPort = new SerialPort(ComPortName, ComBaudRate, Parity.None, 8, StopBits.One);
                _serialPort.Handshake = Handshake.RequestToSendXOnXOff; //.None;
                _serialPort.Encoding = Encoding.ASCII;      // that's only for text read, not binary
                _serialPort.NewLine = "\r\n";
                _serialPort.ReadTimeout = 1100;
                _serialPort.WriteTimeout = 10000;
                _serialPort.DtrEnable = false;
                _serialPort.RtsEnable = false;
                //p.ParityReplace = 0;

                _serialPort.Open();
                _serialPort.DiscardInBuffer();
                _serialPort.DataReceived += new SerialDataReceivedEventHandler(serialPort_DataReceived);

                Debug.WriteLine("OK: PixyCamera Open(" + ComPortName + ") success!");
            }
            catch
            {
                Debug.WriteLine("Error: PixyCamera Open(" + ComPortName + ") failed");
                _serialPort = null;
            }
            */
        }

        public void Close()
        {
            /*
             * TODO: port to Universal Windows
            if (_serialPort != null && _serialPort.IsOpen)
            {
                _serialPort.Close();
            }
            */
        }

        ~PixyCamera()
        {
            Close();
        }

        int state = 0;
        StringBuilder sb = new StringBuilder();
        DateTime lastLineReceived;
        char[] splitChar = new char[] { ' ' };

        /*
         * TODO: port to Universal Windows
         * 
        /// <summary>
        /// Serial Port data event handler
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void serialPort_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            if (e.EventType == SerialData.Chars)
            {
                try
                {
                    while (_serialPort.BytesToRead > 0)
                    {
                        char ch = (char)_serialPort.ReadChar();
                        if (ch == '*')
                        {
                            state = 1;
                            sb.Clear();
                        }
                        else
                        {
                            switch (state)
                            {
                                case 1:
                                    if (ch == '\n')
                                    {
                                        DateTime now = DateTime.Now;

                                        // end of line - interpret Pixy values:
                                        string line = sb.ToString();
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

                                        if (TargetingCameraTargetsChanged != null && Enabled)
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

                                        state = 0;
                                        double msSinceLastReceived = (now - lastLineReceived).TotalMilliseconds;
                                        lastLineReceived = now;
                                        //Debug.WriteLine("OK: '" + line + "'      ms: " + Math.Round(msSinceLastReceived));
                                    }
                                    else
                                    {
                                        // keep accumulating chars:
                                        sb.Append(ch);
                                        if (sb.Length > 100)
                                        {
                                            state = 0;
                                            sb.Clear();
                                            Debug.WriteLine("Error: PixyCamera - invalid stream, expecting *");
                                        }
                                    }
                                    break;
                            }
                        }
                    }
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
                catch (TimeoutException ex)
                {
                    Debug.WriteLine("Error: PixyCamera - TimeoutException: " + ex);
                }
                catch (IOException ex)
                {
                    Debug.WriteLine("Error: PixyCamera - IOException: " + ex);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine("Error: PixyCamera - Exception: " + ex);
                }
            }
        }
        */
    }
}
