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
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using Windows.Devices.SerialCommunication;
using Windows.Devices.Enumeration;
using System.Diagnostics;

using slg.RobotAbstraction;
using Windows.Storage.Streams;
using slg.LibRobotExceptions;
using System.IO;

namespace slg.LibCommunication
{
    // Traditional serial COM comm channel, maybe BT over serial or USB-to-Serial
    // see https://ms-iot.github.io/content/en-US/win10/samples/SerialSample.htm
    // see https://github.com/ms-iot/samples/blob/develop/SerialSample/CS/MainPage.xaml.cs

    public class CommunicationChannelSerial : ICommunicationChannel
    {
        public string Name { get; set; }         // must be set after constructing. Either "COM..." or full device ID
        public uint BaudRate { get; set; }       // must be set after constructing

        public string NewLineIn { get; set; }
        public string NewLineOut { get; set; }
        public string Parameters { get; set; }

        private SerialDevice serialDevice;
        private CancellationTokenSource cancellationTokenSource;

        private BufferBlock<String> linesReceivedBuffer;    // helps assemble partial and multiple lines into single lines
        private StringBuilder sb;   // for received characters to be broken into lines, with NewLine as separator

        public CommunicationChannelSerial(CancellationTokenSource cts)
        {
            // We need cancellation token object to close I/O operations when closing the device
            cancellationTokenSource = cts;

            NewLineIn = "\r";   // default for read
            NewLineOut = "\r";  // default for write

            sb = new StringBuilder();

            linesReceivedBuffer = new BufferBlock<string>(new DataflowBlockOptions() { CancellationToken = cancellationTokenSource.Token });
        }

        public void DiscardInBuffer()
        {
            if (!cancellationTokenSource.IsCancellationRequested)
            {
                try
                {
                    IList<string> dumpList = new List<string>();
                    linesReceivedBuffer.TryReceiveAll(out dumpList);
                    sb.Remove(0, sb.Length);
                }
                catch
                {
                    ;
                }
            }
        }

        public async Task Open()
        {
            if(String.IsNullOrWhiteSpace(Name) || BaudRate == 0)
            {
                throw new CommunicationException("Error: must specify valid name and bauld rate for serial device");
            }

            string deviceId = Name; // either "COM..." or full device ID

            if (Name.StartsWith("COM", StringComparison.OrdinalIgnoreCase))
            {
                var selector = SerialDevice.GetDeviceSelector(Name);    // Get the serial port
                var devices = await DeviceInformation.FindAllAsync(selector);
                if (devices.Any()) // if the device is found
                {
                    deviceId = devices.First().Id;
                }
                else
                {
                    throw new CommunicationException("Error: CommunicationChannelSerial: could not find serialPort " + Name);
                }
            }

            serialDevice = await SerialDevice.FromIdAsync(deviceId);
            if (serialDevice != null)
            {
                SetupSerialDevice(serialDevice);
                await Task.Delay(2000); // let Arduino reboot due to RTS before we send "reset" over the serial.
                // start Listen task in a ThreadPool thread:
                await Task.Factory.StartNew(Listen);
            }
            else
            {
                string err = "Error: CommunicationChannelSerial: could not open serialPort " + Name;
                Debug.WriteLine(err);
                throw new CommunicationException(err);
            }
        }

        private void SetupSerialDevice(SerialDevice serialDevice)
        {
            serialDevice.BaudRate = BaudRate;
            serialDevice.StopBits = SerialStopBitCount.One;
            serialDevice.DataBits = 8;
            serialDevice.Parity = SerialParity.None;
            serialDevice.Handshake = SerialHandshake.None;
            serialDevice.IsDataTerminalReadyEnabled = true;
            serialDevice.IsRequestToSendEnabled = false;
            serialDevice.ReadTimeout = TimeSpan.FromMilliseconds(3);      // default 5 seconds, we want instant response from Arduino boards
            serialDevice.WriteTimeout = TimeSpan.FromMilliseconds(10000);
        }

        #region Listen() and its helpers

        /// <summary>
        /// Listen task works in a ThreadPool thread and passes incoming lines from serial device to linesReceivedBuffer
        /// </summary>
        private async void Listen()
        {
            DataReader dataReaderObject = null;

            try
            {
                if (serialDevice != null)
                {
                    Debug.WriteLine("IP: CommunicationChannelSerial: serialPort " + Name + " listening, " + BaudRate + " Baud...");

                    dataReaderObject = new DataReader(serialDevice.InputStream)
                    {
                        UnicodeEncoding = Windows.Storage.Streams.UnicodeEncoding.Utf8,
                        InputStreamOptions = InputStreamOptions.Partial // complete the asynchronous read operation when one or more bytes is available
                    };

                    // keep reading the serial input until canceled:
                    while (!cancellationTokenSource.IsCancellationRequested)
                    {
                        string line = await ReadAsync(cancellationTokenSource.Token, dataReaderObject);
                        if (!cancellationTokenSource.IsCancellationRequested)
                        {
                            // usually line contains whole lines, but could be partial or multiple lines.
                            sb.Append(line);

                            string sbs = sb.ToString();
                            int nlIndex = sbs.IndexOf(NewLineIn);

                            if (nlIndex == 0)
                            {
                                // only newline, just remove it:
                                sb.Remove(0, NewLineIn.Length);
                            }
                            else if (nlIndex > 0)
                            {
                                // some characters followed by newline:
                                line = sbs.Substring(0, nlIndex);
                                sb.Remove(0, nlIndex + NewLineIn.Length);
                                await linesReceivedBuffer.SendAsync(line);
                            }
                        }
                    }

                    Debug.WriteLine("OK: CommunicationChannelSerial: Listen() exiting...");
                }
            }
            catch (Exception ex)
            {
                if (ex.GetType().Name == "TaskCanceledException")
                {
                    Debug.WriteLine("OK: CommunicationChannelSerial: Reading task was cancelled, closing device and cleaning up");
                    CloseDevice();
                }
                else
                {
                    Debug.WriteLine("Error: CommunicationChannelSerial: " + ex.Message);
                }
            }
            finally
            {
                // Cleanup once complete
                if (dataReaderObject != null)
                {
                    dataReaderObject.DetachStream();
                    dataReaderObject = null;
                }
            }
        }

        private async Task<string> ReadAsync(CancellationToken cancellationToken, DataReader dataReaderObject)
        {
            Task<UInt32> loadAsyncTask;

            uint ReadBufferLength = 1024;

            // If task cancellation was requested, comply:
            cancellationToken.ThrowIfCancellationRequested();

            // Create a task object to wait for data on the serialPort.InputStream:
            loadAsyncTask = dataReaderObject.LoadAsync(ReadBufferLength).AsTask(cancellationToken);

            // Launch the task and wait:
            UInt32 bytesToRead = await loadAsyncTask;
            if (bytesToRead > 0)
            {
                byte[] bytes = new byte[bytesToRead];
                dataReaderObject.ReadBytes(bytes);
                //Debug.WriteLine("OK: " + bytesToRead + " bytes read from serial port : " + bytesToRead);

                string readIn = Encoding.UTF8.GetString(bytes, 0, bytes.Length);

                //string readIn = dataReaderObject.ReadString(bytesToRead); //-- this causes exception on Element "No mapping for the Unicode character exists in the target multi-byte code page."

                //Debug.WriteLine("OK: " + bytesToRead + " bytes read from serial port : '" + readIn + "'");

                return readIn;
            }
            Debug.WriteLine("OK: zero bytes to read from serial port");
            return string.Empty;
        }

        #endregion // Listen() and its helpers

        public async Task<string> ReadLine()
        {
            try
            {
                string line = await linesReceivedBuffer.ReceiveAsync(cancellationTokenSource.Token);

                //Debug.WriteLine("OK: '" + line + "' - read from serial port ");

                return line;
            }
            catch (TaskCanceledException ex)
            {
                Debug.WriteLine("OK: CommunicationChannelSerial: ReadLine() " + ex.Message);
                return "\r\n0";
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Error: CommunicationChannelSerial: ReadLine() " + ex);
                return "\r\n0"; // should not happen, likely to cause further exceptions
            }
        }

        public async Task WriteLine(string str)
        {
            try
            {
                // Create the DataWriter object and attach to OutputStream   
                DataWriter dataWriteObject = new DataWriter(serialDevice.OutputStream);

                //Debug.WriteLine("IP: WriteLine('" + str + "')");

                // Launch the WriteAsync task to perform the write
                await WriteAsync(str + NewLineOut, cancellationTokenSource.Token, dataWriteObject);

                dataWriteObject.DetachStream();
                dataWriteObject = null;
            }
            catch (TaskCanceledException ex)
            {
                Debug.WriteLine("OK: CommunicationChannelSerial: WriteLine() " + ex.Message);
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Error: CommunicationChannelSerial: WriteLine() " + ex);
            }
        }

        private async Task WriteAsync(string str, CancellationToken cancellationToken, DataWriter dataWriteObject)
        {
            try
            {
                Task<UInt32> storeAsyncTask;

                // Load the text to the dataWriter object:
                dataWriteObject.WriteString(str);

                // Launch an async task to complete the write operation
                storeAsyncTask = dataWriteObject.StoreAsync().AsTask(cancellationToken);

                UInt32 bytesWritten = await storeAsyncTask;
                //if (bytesWritten > 0)
                //{
                //    Debug.WriteLine("OK: " + bytesWritten + " bytes written to serial port: '" + str + "'");
                //}
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Error: CommunicationChannelSerial: WriteAsync() " + ex);
            }
        }

        #region Close()

        public void Close()
        {
            try
            {
                CancelAllTasks();
                CloseDevice();
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Error: CommunicationChannelSerial: exception while closing serial port " + Name + " : " + ex);
            }
        }

        private void CancelAllTasks()
        {
            if (cancellationTokenSource != null && !cancellationTokenSource.IsCancellationRequested)
            {
                cancellationTokenSource.Cancel();
            }
        }

        private void CloseDevice()
        {
            if (serialDevice != null)
            {
                serialDevice.Dispose();   // throws unhandled COM component exception
                serialDevice = null;
            }
        }

        #endregion // Close()
    }
}
