using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
//using System.IO.Ports;
using System.Diagnostics;
using System.IO;

namespace slg.Sensors
{
    public class PixyCameraEventArgs : EventArgs
    {
        public int x;
        public int y;
        public int width;
        public int height;
        public int signature;
        public long timestamp;

        public override string ToString()
        {
            return String.Format("{0} {1} {2} {3} {4}", x, y, width, height, signature);
        }
    }

    /// <summary>
    /// Pixy camera connected via Arduino.
    /// This class opens serial to Arduino and receives lines in the form "*226 143 14 5 1" - x, y, width, height, signature
    /// See C:\Projects\Arduino\Sketchbook\PixyToSerial\PixyToSerial.ino
    /// </summary>
    public class PixyCamera
    {
        //private SerialPort _serialPort = null;
        private string ComPortName; // = "COM8";
        private int ComBaudRate; // = 115200;

        #region Public Events

        public delegate void PixyCameraEventHandler(PixyCamera sender, PixyCameraEventArgs args);

        /// <summary>
        /// Occurs when PixyCamera <c>Detected Blocks</c> has changed.
        /// </summary>
        public event PixyCameraEventHandler PixyCameraBlocksChanged;

        #endregion

        public PixyCamera(string comPortName, int comBaudRate)
        {
            ComPortName = comPortName;
            ComBaudRate = comBaudRate;
        }

        public void Open()
        {
            /*
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

                                        if (PixyCameraBlocksChanged != null)
                                        {
                                            try
                                            {
                                                string[] split = line.Split(splitChar);

                                                // Send data to whoever interested:
                                                PixyCameraBlocksChanged(this, new PixyCameraEventArgs()
                                                {
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
