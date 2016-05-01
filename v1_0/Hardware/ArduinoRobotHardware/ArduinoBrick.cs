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
using System.Diagnostics;

using slg.RobotExceptions;
using slg.RobotAbstraction;
using slg.RobotAbstraction.Ids;
using slg.RobotAbstraction.Events;
using slg.RobotAbstraction.Sensors;
using slg.RobotAbstraction.Drive;
using slg.ArduinoRobotHardware.Sensors;
using slg.ArduinoRobotHardware.Controllers;

namespace slg.ArduinoRobotHardware
{
    public class ArduinoBrick : IAbstractRobotHardware, IHardwareComponent
    {
        private CommunicationTask commTask;
        private bool communicating = false;
        private CancellationTokenSource tokenSource;

        public ArduinoBrick(CancellationTokenSource cts)
        {
            // we create the Brick every time we open the COM port
            tokenSource = cts;
            commTask = new CommunicationTask(this);
        }

        public void PumpEvents()
        {
            // not needed, threads and dataflow do the work
        }

        public void Close()
        {
            // all threads will be canceled via CancellationTokenSource - no need to do anything here.
        }

        #region Sensors and Controllers factories

        public ISharpGP2D12 produceSharpGP2D12(AnalogPinId pin, int updateFrequency, double distanceChangedThreshold)
        {
            throw new NotImplementedException();
        }

        public ISonarSRF04 produceSonarSRF04(GpioPinId triggerPin, GpioPinId outputPin, int updateFrequency, double distanceChangedThreshold)
        {
            throw new NotImplementedException();
        }

        public IParkingSonar produceParkingSonar(int updateFrequency)
        {
            return new ParkingSonar(commTask, tokenSource.Token, updateFrequency);
        }

        public IOdometry produceOdometry(int updateFrequency)
        {
            return new OdometryReader(commTask, tokenSource.Token, updateFrequency);
        }

        public IAnalogSensor produceAnalogSensor(AnalogPinId pin, int updateFrequency, double valueChangedThreshold)
        {
            return new AnalogSensor(commTask, tokenSource.Token, updateFrequency, pin, (int)valueChangedThreshold);
        }

        public ICompassCMPS03 produceCompassCMPS03(int i2CAddress, int updateFrequency, double headingChangedThreshold)
        {
            throw new NotImplementedException();
        }

        public IWheelEncoder produceWheelEncoder(WheelEncoderId wheelEncoderId, int updateFrequency, int resolution, int countChangedThreshold)
        {
            return new WheelEncoder(tokenSource.Token);
        }

        public IDifferentialMotorController produceDifferentialMotorController()
        {
            return new DifferentialMotorController(commTask, tokenSource.Token, 1000);
        }

        #endregion // Sensors and Controllers factories

        #region Serial Device related

        /*
        On Windows IoT you have to use Windows.Devices.SerialCommunication namespace to access serial ports.
        You have to have Windows 10 IoT Extension SDK (installer should be bundled with the Windows 10 image file for you board,
        you have to register there for downloads to be availble) installed and added as reference to be able to access that namespace.
        Keep in mind though that if you use Raspberry Pi onboard UART will be inaccessible anyway, as it is used for kernel debugger.
        */
        public string PortName { get; set; }

        public int BaudRate { get; set; }

        public CommunicationPortType PortType { get; set; }

        public event HardwareComponentEventHandler CommunicationStarted;

        public event CommunicationChannelEventHandler CommunicationsStarting;

        public event CommunicationChannelEventHandler CommunicationsStopping;

        public event HardwareComponentEventHandler CommunicationStopped;

        public async Task StartCommunication(CancellationTokenSource cts)
        {
            if (!communicating)
            {
                try
                {
                    Debug.WriteLine("ArduinoBrick: Trying port: " + PortName + " at baud rate " + BaudRate);
                    await commTask.Start(PortName, BaudRate, cts);
                    communicating = true;

                    SignalCommunicationStarted();
                }
                catch (AggregateException aexc)
                {
                    throw new CommunicationException("Could not start communication");
                }
                catch (CommunicationException exc)
                {
                    throw;
                }
                catch (Exception exc)
                {
                    throw new CommunicationException("Could not start communication");
                }
            }
        }

        internal void StartingCommunication(ICommunicationChannel sp)
        {
            if (!communicating)
            {
                SignalCommunicationsStarting(sp);
            }
        }

        internal void StoppingCommunication(ICommunicationChannel sp)
        {
            if (communicating)
            {
                SignalCommunicationsStopping(sp);
            }
        }

        /// <summary>
        /// Stops all communication with the Arduino board.
        /// </summary>
        public async Task StopCommunication()
        {
            if (communicating)
            {
                await commTask.Stop();
                communicating = false;

                SignalCommunicationStopped();
            }
        }

        #endregion // Serial Device related

        #region Private Methods

        private void SignalCommunicationStarted()
        {
            CommunicationStarted?.Invoke(this);
        }

        private void SignalCommunicationStopped()
        {
            CommunicationStopped?.Invoke(this);
        }

        private void SignalCommunicationsStarting(ICommunicationChannel sp)
        {
            CommunicationsStarting?.Invoke(sp);
        }

        private void SignalCommunicationsStopping(ICommunicationChannel sp)
        {
            CommunicationsStopping?.Invoke(sp);
        }

        #endregion // Private Methods
    }
}
