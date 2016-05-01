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
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using System.Text;

using Windows.System;
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
using Windows.Web.Http;
using Windows.Devices.SerialCommunication;
using Windows.Devices.Enumeration;

using slg.RobotPluckyImpl;
using slg.RobotBase.Interfaces;
using slg.RobotBase.Data;
using slg.RobotExceptions;
using slg.ControlDevices;
using slg.Display;
using slg.DisplayWebServer;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace RobotPlucky
{
    /// <summary>
    /// PluckyTheRobot implementation - head (view) class
    /// </summary>
    public sealed partial class MainPage : Page, ISpeaker, IDeviceOpener, IComputerManager
    {
        private PluckyTheRobot plucky;
        private string currentSerialPort;
        private const int desiredLoopTimeMs = 50;   // will be used for timer and as encoders Sampling Interval.
        private const int HTTP_SERVER_PORT = 9098;

        private bool isWorkerRunning = false;

        private CancellationTokenSource tokenSource;
        private ISpeaker speakerImpl;
        private IJoystickController joystick;
        private HttpServer httpServer;

        #region Lifecycle

        public MainPage()
        {
            this.InitializeComponent();

            this.Background = new SolidColorBrush(Colors.Gainsboro);

            this.Loaded += MainPage_Loaded;
            this.Unloaded += MainPage_Unloaded;
        }

        private async void MainPage_Loaded(object sender, RoutedEventArgs e)
        {
            List<SerialPortTuple> serialPorts = await GetAvailableSerialPorts();

            SerialPortComboBox.DisplayMemberPath = "Name";
            SerialPortComboBox.SelectedValuePath = "Id";
            SerialPortComboBox.ItemsSource = serialPorts;

            speakerImpl = new Speaker(media);
            joystick = new GenericJoystick();
            InitWebServer(serialPorts);
        }

        private void InitWebServer(List<SerialPortTuple> serialPorts)
        {
            httpServer = new HttpServer(HTTP_SERVER_PORT);
            IAsyncAction asyncAction = Windows.System.Threading.ThreadPool.RunAsync(
                async (workItem) =>
                {
                    httpServer.StartServer(this, this, serialPorts);

                    // test loop:
                    while(true)
                    {
                        await Task.Delay(5000);
                        using (HttpClient client = new HttpClient())
                        {
                            try
                            {
                                // inside the same process isolation does not block access. Other processes on the same computer are blocked.
                                // use another computer to hit this URL in the browser: 
                                string response = await client.GetStringAsync(new Uri("http://localhost:" + HTTP_SERVER_PORT + "/robotUI.html"));
                                //string response = await client.GetStringAsync(new Uri("http://172.16.1.201:" + HTTP_SERVER_PORT + "/robotUI.html"));
                                Debug.WriteLine("HttpClient: got response: " + response); 
                            }
                            catch (Exception exc)
                            {
                                // possibly a 404
                                Debug.WriteLine("Error: HttpClient: got " + exc);
                            }
                        }
                    }
                });
        }

        private void MainPage_Unloaded(object sender, RoutedEventArgs e)
        {
            if(httpServer != null)
                httpServer.Dispose();

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
        private async Task<List<SerialPortTuple>> GetAvailableSerialPorts()
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

            return ports;
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

        private void ShutdownButtonButton_Click(object sender, RoutedEventArgs e)
        {
            ShutdownComputer();
        }

        /// <summary>
        /// click on the button that selects the serial port and then starts backround worker, thus starting the robot.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void OpenCloseButton_Click(object sender, RoutedEventArgs e)
        {
            try {
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
                    currentSerialPort = "" + SerialPortComboBox.SelectedValue;

                    if (!string.IsNullOrWhiteSpace(currentSerialPort))
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
                            if (plucky != null)
                            {
                                plucky.Close();
                                plucky = null;
                            }
                        }
                        else
                        {
                            // plucky is connected to hardware brick.
                            SerialPortComboBox.IsEnabled = false;
                            OpenCloseButton.Content = "Close";
                            EnableOpenCloseButton("");
                        }

                        if (httpServer != null)
                            httpServer.robot = plucky;
                    }
                    else
                    {
                        Speak("Select serial port");
                        StatusLabel.Text = "Please select serial port to connect to " + plucky + " hardware brick";
                        ResetOpenCloseButton("");
                    }
                }
            }
            catch (Exception exc)
            {
                Speak("Oops");
                StatusLabel.Text = exc.Message;
                Debug.WriteLine("Exception: " + exc);
                ResetOpenCloseButton("");
                plucky = null;
                if (httpServer != null)
                    httpServer.robot = plucky;
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

        #region IDeviceOpener implementation

        public async Task<bool> OpenDevice(string deviceId)
        {
            currentSerialPort = deviceId;

            Speak("connecting");
            await Task.Delay(1000);

            try {
                await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, async () => await doOpenDevice(deviceId));

                return isWorkerRunning && plucky != null && !plucky.isCommError;
            }
            catch (Exception exc)
            {
                Speak("could not connect");
            }
            return false;
        }

        private async Task<bool> doOpenDevice(string deviceId)
        {
            try
            {
                // start the worker process:
                await StartWorker();

                if (!isWorkerRunning || plucky == null || plucky.isCommError)
                {
                    Speak("Hardware brick does not connect");
                    if (plucky != null)
                    {
                        plucky.Close();
                        plucky = null;
                    }
                }
                else
                {
                    // plucky is connected to hardware brick.
                }

                if (httpServer != null)
                    httpServer.robot = plucky;

                return true;
            }
            catch (Exception exc)
            {
                Speak("could not connect");
            }
            return false;
        }

        public bool IsDeviceOpen { get; set; }

        public DateTime LastConnectionTime { get; set; }

        public async Task<bool> CloseDevice()
        {
            try {
                if (isWorkerRunning && plucky != null)
                {
                    Speak("Disconnecting Plucky");
                    await Task.Factory.StartNew(StopWorker);
                }
                else
                {
                    // plucky is not connected to hardware brick.
                }

                return true;
            }
            catch (Exception exc)
            {
                Speak("could not disconnect");
            }
            return false;
        }

        #endregion // IDeviceOpener implementation

        #region Worker Threads management

        private async Task StartWorker()
        {
            try
            {
                IsDeviceOpen = false;

                tokenSource = new CancellationTokenSource();
                isWorkerRunning = true;

                plucky = new PluckyTheRobot(this, joystick, desiredLoopTimeMs);
                await plucky.Init(tokenSource, new string[] { currentSerialPort });  // may throw exceptions

                if (plucky.isCommError)
                {
                    isWorkerRunning = false;
                }
                else
                {
                    await InitializeTickers();
                    LastConnectionTime = DateTime.Now;
                    IsDeviceOpen = true;
                }
            }
            catch (AggregateException exc)
            {
                isWorkerRunning = false;
                Debug.WriteLine("Error: StartWorker(): AggregateException - " + exc);
                throw;
            }
            catch (CommunicationException exc)
            {
                isWorkerRunning = false;
                Debug.WriteLine("Error: StartWorker(): CommunicationException - " + exc);
                throw;
            }
            catch (Exception exc)
            {
                isWorkerRunning = false;
                Debug.WriteLine("Error: StartWorker(): " + exc);
                throw;
            }
        }

        private void StopWorker()
        {
            try
            {
                IsDeviceOpen = false;
                StopTickers().Wait();   // triggers tokenSource.Cancel()
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

            if (httpServer != null)
                httpServer.robot = plucky;

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

        #region IComputerManager implementation

        /// <summary>
        /// IComputerManager implementation
        /// </summary>
        public void ShutdownComputer()
        {
            try
            {
                ShutdownManager.BeginShutdown(ShutdownKind.Shutdown, TimeSpan.FromSeconds(1.0));
                Speak("shutting down");
            }
            catch
            {
                Speak("Cannot shut down this computer");
            }
        }

        #endregion // IComputerManager implementation

        #region Display all data on the UI

        private DateTime lastBatteryVoltageAlarmed = DateTime.Now;
        private DateTime lastJoystickDisplayed = DateTime.Now;
        private DateTime lastSensorsDisplayed = DateTime.Now;
        private DateTime lastStateDisplayed = DateTime.Now;
        private DateTime lastPoseDisplayed = DateTime.Now;
        private DateTime lastPosePrinted = DateTime.Now;

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

            if ((Now - lastJoystickDisplayed).TotalSeconds > 1.0d && plucky.currentJoystickData != null)
            {
                lastJoystickDisplayed = Now;
                robotDashboard1.DisplayRobotJoystick(plucky.currentJoystickData);
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

                TimeSpan sinceStart = Now - LastConnectionTime;
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
