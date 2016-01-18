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
using System.Threading.Tasks;
using System.Diagnostics;

using Windows.Devices.Enumeration;
using Windows.Devices.HumanInterfaceDevice;

using slg.RobotBase.Interfaces;

namespace slg.ControlDevices
{
    /// <summary>
    /// finds XBox 360 controller and instantiates XboxHidController to deal with events.
    /// </summary>
    public class Xbox360Controller : IDisposable
    {
        public bool JoystickIsWorking = false;   // true if XBox controller is plugged in and operational.
        public string DeviceId { get; protected set; }

        public event EventHandler<IJoystickSubState> joystickEvent;

        private XboxHidController controller;
        private int lastControllerCount = 0;

        private static Xbox360Controller instance = null;

        public static Xbox360Controller getInstance()
        {
            if (instance == null)
            {
                instance = new Xbox360Controller();
            }
            return instance;
        }

        /// <summary>
        /// disallow direct instantiation
        /// </summary>
        private Xbox360Controller()
        {
        }

        public void Dispose()
        {
            //instance = null;
        }

        #region ----- Xbox HID-Controller -----

        /// <summary>
        /// call often to find and init XBox controllers connected to the system.
        /// Controllers can be connected and disconnected as needed.
        /// </summary>
        public async Task XboxJoystickCheck()
        {
            //EnumerateHidDevices();

            string deviceSelector = HidDevice.GetDeviceSelector(0x01, 0x05);
            DeviceInformationCollection deviceInformationCollection = await DeviceInformation.FindAllAsync(deviceSelector);
            if (deviceInformationCollection.Count != lastControllerCount)
            {
                // detected controllers number change
                lastControllerCount = deviceInformationCollection.Count;
                await XboxJoystickInit(deviceInformationCollection);
            }
        }

        private async Task XboxJoystickInit(DeviceInformationCollection deviceInformationCollection)
        {
            //string deviceSelector = HidDevice.GetDeviceSelector(0x01, 0x05);
            //DeviceInformationCollection deviceInformationCollection = await DeviceInformation.FindAllAsync(deviceSelector);

            DeviceId = null;
            int deviceCount = deviceInformationCollection.Count;

            if (deviceCount == 0)
            {
                Debug.WriteLine("Error: No Xbox360 controller found!");
                JoystickIsWorking = false;
            }
            else
            {
                foreach (DeviceInformation d in deviceInformationCollection)
                {
                    Debug.WriteLine("OK: Found: Xbox 360 Joystick Device ID: " + d.Id);

                    HidDevice hidDevice = await HidDevice.FromIdAsync(d.Id, Windows.Storage.FileAccessMode.Read);

                    if (hidDevice == null)
                    {
                        JoystickIsWorking = false;
                        try
                        {
                            var deviceAccessStatus = DeviceAccessInformation.CreateFromId(d.Id).CurrentStatus;

                            if (!deviceAccessStatus.Equals(DeviceAccessStatus.Allowed))
                            {
                                Debug.WriteLine("IP: DeviceAccess: " + deviceAccessStatus.ToString());
                            }
                        }
                        catch (Exception e)
                        {
                            Debug.WriteLine("Error: Xbox init - " + e.Message);
                        }

                        Debug.WriteLine("Error: Failed to connect to Xbox 360 Joystick controller!");
                    }
                    else
                    {
                        controller = new XboxHidController(hidDevice);
                        controller.JoystickDataChanged += Controller_JoystickDataChanged;
                        JoystickIsWorking = true;
                        DeviceId = d.Id;
                    }
                }
            }
            lastControllerCount = deviceCount;
        }

        /// <summary>
        /// print devices for debugging
        /// </summary>
        private async void EnumerateHidDevices()
        {
            //var deviceSelector = HidDevice.GetDeviceSelector(0xFF00, 0x0001);
            // USB\VID_045E&PID_028E&IG_00
            var deviceSelector = HidDevice.GetDeviceSelector(0x01, 0x05);
            var devices = await DeviceInformation.FindAllAsync(deviceSelector);

            foreach (var deviceInformation in devices)
            {
                Debug.WriteLine("HID Device Name: " + deviceInformation.Name + "- Id: " + deviceInformation.Id + "- Is Default: " + deviceInformation.IsDefault + "- Is Enabled: " + deviceInformation.IsEnabled);

                HidDevice hidDevice = await HidDevice.FromIdAsync(deviceInformation.Id, Windows.Storage.FileAccessMode.Read);

            }
        }

        private void Controller_JoystickDataChanged(object sender, XboxHidController.JoystickEventArgs e)
        {
            // receiving IJoystickRawState
            JoystickIsWorking = true;
            //Debug.WriteLine("JoystickRawState: X=" + e.jss.X + "  Y=" + e.jss.Y);

            lock(jssLock)
            {
                // keep a copy for those who poll the joystick:
                jssCurrent = new JoystickSubState(e.jss);
            }

            // if we have subscribers, notify them:
            if(joystickEvent != null)
            {
                //Debug.WriteLine("--------- 360: Controller_JoystickDataChanged");
                joystickEvent(this, (IJoystickSubState)jssCurrent.Clone());
            }

            //Debug.WriteLine("JoystickSubState: Speed=" + jssCurrent.Speed + "  Turn=" + jssCurrent.Turn);
        }

        #endregion //  ----- Xbox HID-Controller -----

        private object jssLock = new Object();  // locks jssCurrent
        private IJoystickSubState jssCurrent = null;
        private IJoystickSubState jssLast = null;

        /// <summary>
        /// supports polling requests for latest Joystick SubState.
        /// </summary>
        /// <returns></returns>
        public IJoystickSubState GetJoystickSubState()
        {
            lock (jssLock)
            {
                if (jssCurrent != null && !jssCurrent.Equals(jssLast))     // only do something if the joystick state changed
                {
                    jssLast = (IJoystickSubState)jssCurrent.Clone();
                    jssLast.IsNew = true;
                }
            }

            return jssLast;
        }
    }
}
