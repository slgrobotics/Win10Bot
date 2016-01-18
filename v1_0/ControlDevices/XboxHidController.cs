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

using Windows.Devices.HumanInterfaceDevice;

using slg.RobotBase;
using System.Diagnostics;

namespace slg.ControlDevices
{
    /// <summary>
    /// deals with Xbox 360 controller at the HID level. Raise IJoystickRawState based event if controls move.
    /// </summary>
    public class XboxHidController
    {
        /// <summary>
        /// Handle to the actual controller HidDevice
        /// </summary>
        private HidDevice deviceHandle { get; set; }

        private JoystickRawState jssLast = new JoystickRawState();

        /// <summary>
        /// Initializes a new instance of the XboxHidController class from a HidDevice handle
        /// </summary>
        /// <param name="deviceHandle">Handle to the HidDevice</param>
        public XboxHidController(HidDevice deviceHandle)
        {
            this.deviceHandle = deviceHandle;
            deviceHandle.InputReportReceived += inputReportReceived;
        }

        /// <summary>
        /// Handler for processing/filtering input from the controller
        /// </summary>
        /// <param name="sender">HidDevice handle to the controller</param>
        /// <param name="args">InputReport received from the controller</param>
        private void inputReportReceived(HidDevice sender, HidInputReportReceivedEventArgs args)
        {
            int dPad = (int)args.Report.GetNumericControl(0x01, 0x39).Value;

            ControllerDpadDirection dpadDirection = (ControllerDpadDirection)dPad;

            // see http://sviluppomobile.blogspot.com/2013/11/hid-communication-for-windows-81-store.html

            // Adjust X/Y so (0,0) is neutral position

            // sticks - left and right:
            int _leftStickX = (int)(args.Report.GetNumericControl(0x01, 0x30).Value);
            int _leftStickY = (int)(args.Report.GetNumericControl(0x01, 0x31).Value);

            int _rightStickX = (int)(args.Report.GetNumericControl(0x01, 0x33).Value);
            int _rightStickY = (int)(args.Report.GetNumericControl(0x01, 0x34).Value);

            // triggers - left and right:
            int _LT = (int)Math.Max(0, args.Report.GetNumericControl(0x01, 0x32).Value - 32768);
            int _RT = (int)Math.Max(0, (-1) * (args.Report.GetNumericControl(0x01, 0x32).Value - 32768));

            JoystickRawState jss = new JoystickRawState()
            {
                X = _leftStickX, Y = _leftStickY, Z = 0,
                XR = _rightStickX, YR = _rightStickY,
                LT = _LT, RT = _RT,
                DpadDirection = dpadDirection
            };

            /*
            * Buttons Boolean ID's mapped to 0-9 array
            * A (button1) - 5 
            * B (button2) - 6
            * X (button3) - 7
            * Y (button4) - 8
            * LB (Left Bumper, button5) - 9
            * RB (Right Bumper, button6) - 10
            * Back (button7) - 11
            * Start (button8) - 12
            * LStick - 13
            * RStick - 14
            */
            foreach (var btn in args.Report.ActivatedBooleanControls)
            {
                // both press and release button event processed here:
                jss.Buttons[btn.Id - 5] = btn.IsActive;
            }

            // only invoke event if there was a change:
            if (!jss.Equals(jssLast))
            {
                jssLast = jss;
                if(JoystickDataChanged != null)
                {
                    //Debug.WriteLine("--------- HID: Joystick event");
                    JoystickDataChanged(this, new JoystickEventArgs(jss));
                }
            }
        }

        /// <summary>
        /// Event raised when the controller input changes
        /// </summary>
        public event EventHandler<JoystickEventArgs> JoystickDataChanged;

        public class JoystickEventArgs : EventArgs
        {
            public IJoystickRawState jss { get; private set; }

            public JoystickEventArgs(IJoystickRawState j)
            {
                jss = j;
            }
        }
    }
}
