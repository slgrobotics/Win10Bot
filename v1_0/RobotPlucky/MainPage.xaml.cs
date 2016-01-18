﻿/*
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
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using Windows.Devices.SerialCommunication;
using Windows.Devices.Enumeration;

using slg.RobotPluckyImpl;
using slg.RobotBase.Interfaces;
using slg.RobotExceptions;
using slg.ControlDevices;
using slg.Display;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace RobotPlucky
{
    /// <summary>
    /// PluckyTheRobot implementation - head (view) class
    /// </summary>
    public sealed partial class MainPage : Page, ISpeaker
    {
        private PluckyTheRobot plucky;
        private string currentPort;
        private const int desiredLoopTimeMs = 50;   // will be used for timer and as encoders Sampling Interval.

        private bool isWorkerRunning = false;

        private CancellationTokenSource tokenSource;
        private ISpeaker speakerImpl;
        private IJoystickController joystick;

        #region Lifecycle

        public MainPage()
        {
            this.InitializeComponent();

            this.Background = new SolidColorBrush(Colors.Gainsboro);

            this.Loaded += MainPage_Loaded;
            this.Unloaded += MainPage_Unloaded;
        }

        private void MainPage_Loaded(object sender, RoutedEventArgs e)
        {
            FillSerialPortComboBox();
            speakerImpl = new Speaker(media);
            joystick = new GenericJoystick();
        }

        private void MainPage_Unloaded(object sender, RoutedEventArgs e)
        {
            if (isWorkerRunning)
            {
                StopWorker();
            }
            joystick.Dispose();
            joystick = null;
        }

        #endregion // Lifecycle

        #region UI initialization

        /// <summary>
        /// initialize SerialPortComboBox with names of all available serial ports
        /// </summary>
        private async void FillSerialPortComboBox()
        {
            /*
            SEE https://social.msdn.microsoft.com/Forums/windowsapps/en-US/0c638b8e-482d-462a-97e6-4d8bc86d8767/uwp-windows-10-apps-windowsdevicesserialcommunicationserialdevice-class-not-working?forum=wpdevelop

                (a) There are several ways to get the Id
                //[1] via COM
                var selector = SerialDevice.GetDeviceSelector("COM3"); //Get the serial port on port '3'
                var myDevices = await DeviceInformation.FindAllAsync(selector);
                //[2] via USB VID-PID
                ushort vid = 0x0403;
                ushort pid = 0x6001;
                string aqs = SerialDevice.GetDeviceSelectorFromUsbVidPid(vid, pid);
                var myDevices = await Windows.Devices.Enumeration.DeviceInformation.FindAllAsync(aqs, null);
                //[3] More general
                string aqs = SerialDevice.GetDeviceSelector();
                var myDevices = await Windows.Devices.Enumeration.DeviceInformation.FindAllAsync(aqs, null);
                (b) Check if there is at least one
                            if (myDevices.Count == 0)
                            {
                                //Report error            
                                return;
                            }
                (c) Use first instance
                            string id = myDevices[0].Id;  //Or if more than one use index other than 1
                            serialDevice = await SerialDevice.FromIdAsync(id); 
                     if( serialDevice == null)
                     {
                  //Report error
                  return;
                     }
                        serialDevice.Baud = 9600;
                            serialDevice.DataBits = 8;
                            serialDevice.Parity = SerialParity.None;
                            serialDevice.StopBits = SerialStopBitCount.One;
                            serialDevice.Handshake = SerialHandshake.None;
                     //async code for send and receive
                    .. etc
                (d) Close connection
                      serialDevice.Close();
                </code>
            */

            //string[] ports = new string[] { "COM3", "COM7" };     // SerialPort.GetPortNames(); - not available in UW: System.IO.Ports
            //SerialPortComboBox.ItemsSource = ports;

            string aqs = SerialDevice.GetDeviceSelector();
            DeviceInformationCollection myDevices = await Windows.Devices.Enumeration.DeviceInformation.FindAllAsync(aqs, null);

            List<SerialPortTuple> ports = new List<SerialPortTuple>();

            if (myDevices.Any())
            {
                foreach (var device in myDevices)
                {
                    ports.Add(new SerialPortTuple() { Name = device.Name, Id = device.Id });
                }
            }
            else
            {
                StatusLabel.Text = "there are no serial devices representing Hardware Brick";
            }

            SerialPortComboBox.DisplayMemberPath = "Name";
            SerialPortComboBox.SelectedValuePath = "Id";
            SerialPortComboBox.ItemsSource = ports;
        }

        class SerialPortTuple
        {
            public string Name { get; set; }
            public string Id { get; set; }
        }

        #endregion // UI initialization

        #region UI control events

        private void EnableOpenCloseButton(object obj)
        {
            OpenCloseButton.IsEnabled = true;
        }

        private void ResetOpenCloseButton(object obj)
        {
            SerialPortComboBox.IsEnabled = true;
            OpenCloseButton.Content = "Open";
            OpenCloseButton.IsEnabled = true;
        }

        /// <summary>
        /// click on the button that selects the serial port and then starts backround worker, thus starting the robot.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void OpenCloseButton_Click(object sender, RoutedEventArgs e)
        {
            OpenCloseButton.IsEnabled = false;

            if (isWorkerRunning)
            {
                Speak("disconnecting");
                await Task.Delay(100);

                // cancel the worker process, dispose of plucky - but don't wait for it:
                Task task = Task.Factory.StartNew(StopWorker);
                await Task.Delay(1000);

                ResetOpenCloseButton("");
            }
            else
            {
                currentPort = "" + SerialPortComboBox.SelectedValue;

                if (!string.IsNullOrWhiteSpace(currentPort))
                {
                    Speak("connecting");
                    await Task.Delay(1000);

                    // start the worker process:
                    await StartWorker();

                    if (!isWorkerRunning || plucky.isCommError)
                    {
                        StatusLabel.Text = plucky.ToString() + " cannot connect to hardware brick";
                        ResetOpenCloseButton("");
                        Speak("Hardware brick does not connect");
                        plucky.Close();
                        plucky = null;
                    }
                    else
                    {
                        // plucky is connected to hardware brick.
                        SerialPortComboBox.IsEnabled = false;
                        OpenCloseButton.Content = "Close";
                        EnableOpenCloseButton("");
                    }
                }
                else
                {
                    Speak("Select serial port");
                    StatusLabel.Text = "Please select serial port to connect to " + plucky.ToString() + " hardware brick";
                    ResetOpenCloseButton("");
                }
            }
        }

        /// <summary>
        /// closes application when error popup button is clicked
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ExitPopupButton_Click(object sender, RoutedEventArgs e)
        {
            Environment.FailFast("failed");
        }

        #endregion // UI control events

        #region Worker Threads management

        private async Task StartWorker()
        {
            try
            {
                tokenSource = new CancellationTokenSource();
                isWorkerRunning = true;

                plucky = new PluckyTheRobot(this, joystick, desiredLoopTimeMs);
                await plucky.Init(new string[] { currentPort });  // may throw exceptions

                if (plucky.isCommError)
                {
                    isWorkerRunning = false;
                }
                else
                {
                    await InitializeTickers();
                    lastConnectionTime = DateTime.Now;
                }
            }
            catch (AggregateException exc)
            {
                isWorkerRunning = false;
                Debug.WriteLine("Error: StartWorker(): AggregateException - " + exc);
            }
            catch (CommunicationException exc)
            {
                isWorkerRunning = false;
                Debug.WriteLine("Error: StartWorker(): CommunicationException - " + exc);
            }
            catch (Exception exc)
            {
                isWorkerRunning = false;
                Debug.WriteLine("Error: StartWorker(): " + exc);
            }
        }

        private void StopWorker()
        {
            try
            {
                StopTickers().Wait();
            }
            catch (AggregateException exc)
            {
                Debug.WriteLine("Error: StopWorker(): AggregateException - " + exc);
            }
            catch (CommunicationException exc)
            {
                Debug.WriteLine("Error: StopWorker(): CommunicationException - " + exc);
            }
            catch (Exception exc)
            {
                Debug.WriteLine("Error: StopWorker(): " + exc);
            }

            if (plucky != null)
            {
                plucky.Dispose();
                plucky = null;
            }

            isWorkerRunning = false;
        }

        #endregion // Worker Threads management

        #region Tickers to periodically run robot logic and service control devices (joystick).

        private Timer periodicTimer;

        /// <summary>
        /// starts threads to periodically run robot logic and service control devices (joystick).
        /// </summary>
        /// <returns></returns>
        private async Task InitializeTickers()
        {
            // this is for the main processing loop, running by a timer, borrowing threads from the threadpool:
            int dueTimeMs = 1000;

            periodicTimer = new Timer(DoPeriodicWorkInThread, tokenSource.Token, dueTimeMs, desiredLoopTimeMs);

            // dedicate one thread from threadpool to joystick:
            await Task.Factory.StartNew(DoControlDevices);
        }

        /// <summary>
        /// stops robot logic and control devices threads
        /// </summary>
        private async Task StopTickers()
        {
            tokenSource.Cancel();
            periodicTimer.Dispose();
            await Task.Delay(1000);
        }

        private Object lockObject = new Object();

        /// <summary>
        /// called by Timer in a ThreadPool thread. Can be called in multiple threads simultaneously.
        /// </summary>
        /// <param name="obj"></param>
        private void DoPeriodicWorkInThread(Object obj)
        {
            CancellationToken token = (CancellationToken)obj;

            // Repeat this loop until cancelled.
            if (!token.IsCancellationRequested)
            {
                // Any other thread that tries to reenter will bypass this.
                // We allow "skipping a beat" if Process() takes longer than timer period:
                if (Monitor.TryEnter(lockObject))
                {
                    try
                    {
                        // Code that accesses resources that are protected by the Monitor lock:
                        plucky.Process();

                        // called from any thread - a thread safe function:
                        DisplayAll();
                    }
                    catch (Exception exc)
                    {
                        Debug.WriteLine("Error: DoPeriodicWorkInThread(): " + exc);
                    }
                    finally
                    {
                        Monitor.Exit(lockObject);
                    }
                }
            }
        }

        /// <summary>
        /// services control devices (joystick).
        /// </summary>
        private async void DoControlDevices()
        {
            // Repeat this loop until cancelled.
            while (!tokenSource.Token.IsCancellationRequested)
            {
                try
                {
                    // call to open and maintain connection to joystick device:
                    IJoystickSubState jss = await joystick.PollJoystick();     // just check for joystick existence. Data arrives in events.

                    if (!joystick.IsEnabled)
                    {
                        Debug.WriteLine("PluckyTheRobot: Xbox Controller Not Found");
                    }

                    plucky.ProcessControlDevices();     // give robot implementation a chance to maintain other control devices.
                }
                catch (Exception exc)
                {
                    Debug.WriteLine("Error: DoControlDevices(): " + exc);
                }
                await Task.Delay(1000);
            }
        }

        #endregion // Tickers to periodically run robot logic and service control devices (joystick).

        #region Display all data on the UI

        private DateTime lastBatteryVoltageAlarmed = DateTime.Now;
        private DateTime lastSensorsDisplayed = DateTime.Now;
        private DateTime lastStateDisplayed = DateTime.Now;
        private DateTime lastPoseDisplayed = DateTime.Now;
        private DateTime lastPosePrinted = DateTime.Now;
        private DateTime lastConnectionTime = DateTime.Now;

        /// <summary>
        /// all logging and displaying in UI is handled here. It is a thread safe function.
        /// </summary>
        private void DisplayAll()
        {
            DateTime Now = DateTime.Now;

            if ((Now - lastPosePrinted).TotalSeconds > 5.0d)
            {
                lastPosePrinted = Now;
                Debug.WriteLine("FYI: RunWorker: estimated robot pose: " + plucky.robotPose.ToString());
                Debug.WriteLine("                sensorsData: " + plucky.BehaviorData.sensorsData.ToString());
            }

            if ((Now - lastPoseDisplayed).TotalSeconds > 0.2d)
            {
                lastPoseDisplayed = Now;
                robotDashboard1.DisplayRobotPose(plucky.robotPose);     // maps current pose
            }

            if ((Now - lastSensorsDisplayed).TotalSeconds > 1.0d)
            {
                lastSensorsDisplayed = Now;
                robotDashboard1.DisplayRobotSensors(plucky.currentSensorsData);

                if (plucky.currentSensorsData.BatteryVoltage / 3.0d < 3.4d && (Now - lastBatteryVoltageAlarmed).TotalSeconds > 10.0d)
                {
                    lastBatteryVoltageAlarmed = Now;
                    Speak("Trouble - battery voltage below " + Math.Round(plucky.currentSensorsData.BatteryVoltage / 3.0d, 1) + " per cell");
                }
            }

            if ((Now - lastStateDisplayed).TotalSeconds > 1.0d)
            {
                lastStateDisplayed = Now;
                robotDashboard1.DisplayRobotState(plucky.robotState, plucky.robotPose);

                TimeSpan sinceStart = Now - lastConnectionTime;
                string[] split = sinceStart.ToString().Split(new char[] { '.' });   // remove milliseconds

                // update our status text:
                DisplayStatusMessage("OK " + Now + "     since start: " + split[0]);
            }
        }

        public void DisplayStatusMessage(string message)
        {
            Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () => { StatusLabel.Text = message; }).AsTask().Wait();
        }

        #endregion // Display all data on the UI

        #region Speak

        // ISpeaker implementation
        public async void Speak(string whatToSay, int voice = 0)
        {
            await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () => speakerImpl.Speak(whatToSay, voice));
        }

        #endregion // Speak
    }
}