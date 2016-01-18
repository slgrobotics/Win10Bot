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

using Windows.Devices.HumanInterfaceDevice;

using slg.RobotBase.Interfaces;

namespace slg.ControlDevices
{
    /// <summary>
    /// this class hides specific controller implementation (Gamepad, Xbox360 or Joystick) and presents simple interface to the robot
    /// </summary>
    public class GenericJoystick : IJoystickController, IDisposable
    {
        public string ControlDeviceType { get; protected set; }

        public string ProductName { get; protected set; }

        public string InstanceName { get; protected set; }

        /// <summary>
        /// must be in the range 0...50 - defines what power will be used to compute Speed and Turn when throttle ("Z") position is around minimum
        /// </summary>
        public double JoystickMinimumPower { get; set; }

        public bool IsEnabled { get; protected set; }

        public event EventHandler<IJoystickSubState> joystickEvent;

        //private HidDevice joystick;
        private Xbox360Controller joystick;

        #region Joystick lifecycle

        public GenericJoystick()
        {
            ControlDeviceType = "Gamepad";
            ProductName = "XBox360 Controller";
            InstanceName = "None";

            joystick = Xbox360Controller.getInstance();
            joystick.joystickEvent += XBox360Controller_JoystickEvent;
        }

        /// <summary>
        /// pass XBox360 Controller Joystick Event to subscribers of GenericJoystick class
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void XBox360Controller_JoystickEvent(object sender, IJoystickSubState e)
        {
            if(joystickEvent != null)
            {
                //Debug.WriteLine("--------- Generic: Joystick Event");
                joystickEvent(this, e);
            }
        }

        /// <summary>
        /// Finds a joystick, sets IsEnabled to true or if it cannot be found - false.
        /// </summary>
        /// <returns></returns>
        public async Task<bool> FindOrCheck()
        {
            await joystick.XboxJoystickCheck();
            IsEnabled = joystick.JoystickIsWorking;
            this.InstanceName = IsEnabled ? joystick.DeviceId : "None";
            return IsEnabled;
        }

        public void Close()
        {
            if (IsEnabled && joystick != null)
            {
                Debug.WriteLine("IP: GenericJoystick  Close(): unacquiring joystick...");

                joystick.joystickEvent -= XBox360Controller_JoystickEvent;
                //joystick.SetNotification(null);
                //joystick.Unacquire();
                joystick.Dispose();
                IsEnabled = false;
                joystick = null;
            }
        }

        public void Dispose()
        {
            Close();
        }

        /// <summary>
        /// Find a joystick, and if connected - poll it for latest Joystick SubState.
        /// May return null if no joystick found.
        /// </summary>
        /// <returns></returns>
        public async Task<IJoystickSubState> PollJoystick()
        {
            bool haveAJoystick = await FindOrCheck();

            if (haveAJoystick)
            {
                IJoystickSubState jss = joystick.GetJoystickSubState();
                return jss;
            }
            else
            {
                return null;
            }
        }

        #endregion // Joystick lifecycle
    }
}
