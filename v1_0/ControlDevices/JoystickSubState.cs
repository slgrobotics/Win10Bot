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
using System.Diagnostics;

using System.Runtime.Serialization;
using Windows.Devices.HumanInterfaceDevice;

using slg.RobotBase.Interfaces;
using slg.RobotMath;

namespace slg.ControlDevices
{
    [DataContract]
    public class JoystickSubState : JoystickRawState, IJoystickSubState
    {
        private const int JS_DEADZONE = 5000;

        private static double CurrentMaxPower = 100.0d; // Percent, limits the power in "Speed" and "Turn" when emulated throttle is used.

        /// <summary>
        /// must be in the range 0...50 - defines what power will be used to compute Speed and Turn when throttle ("Z") position is around minimum
        /// </summary>
        public double JoystickMinimumPower { get; set; }

        /// <summary>
        /// "Gamepad" or "Joystick"
        /// </summary>
        public string ControlDeviceType { get; set; }

        /// <summary>
        /// for example, "ST290 Pro"
        /// </summary>
        public string ProductName { get; set; }

        /// <summary>
        /// for example, "ST290 Pro"
        /// </summary>
        public string InstanceName { get; set; }

        /// <summary>
        /// A service flag to be set when JoystickSubState is created but not yet processed
        /// </summary>
        public bool IsNew { get; set; }

        // conversion methods joystick -> robot commands

        public double Speed
        {
            get {
                long y = 32767 - Y;
                if (Math.Abs(y) < JS_DEADZONE)
                    return 0.0d;

                if(y > 0L)
                    y = GeneralMath.map(y, JS_DEADZONE, 32767L, 0L, 32767L);
                else
                    y = GeneralMath.map(y, -JS_DEADZONE, -32767L, 0L, -32767L);

                return y * Power / 32768.0d;
            }
        }

        // right turn positive
        public double Turn
        {
            get
            {
                long x = X - 32767;
                if (Math.Abs(x) < JS_DEADZONE)
                    return 0.0d;

                if (x > 0L)
                    x = GeneralMath.map(x, JS_DEADZONE, 32767L, 0L, 32767L);
                else
                    x = GeneralMath.map(x, -JS_DEADZONE, -32767L, 0L, -32767L);

                return x * Power / 32768.0d;
            }
        }

        // throttle 100% - all way forward/up
        public double Power
        {
            get
            {
                double power = 0.0d;

                switch (ControlDeviceType)
                {
                    case "Joystick":
                        // use throttle on gaming joysticks:
                        power = Math.Max(JoystickMinimumPower, (65535 - Z) * 100.0d / 65536.0d);
                        break;

                    default:
                    case "Gamepad":
                        // use emulated throttle via buttons 5 (up) and 7 (down) on RumblePad2:
                        power = CurrentMaxPower;
                        break;
                }

                return power;  
            }
        }

        private JoystickSubState()
        {
        }

        public JoystickSubState(IJoystickRawState raw)
            : base(raw)
        {
        }

        /*
        // emulate throttle via buttons 5 and 7 on RumblePad2:
        private static bool clickUp;
        private static bool clickDown;

        public JoystickSubState(HidInputReport js, string controlDeviceType, double joystickMinimumPower)
        {
            this.ControlDeviceType = controlDeviceType;
            this.JoystickMinimumPower = joystickMinimumPower;

            // retrieve DirectInput values:
            X = js.X;   // left stick horizontal movement on RumblePad2
            Y = js.Y;   // left stick vertical movement on RumblePad2
            Z = js.Z;   // throttle on gaming joysticks, right stick horizontal movement on RumblePad2
            // there is no DirectInput access to right stick vertical movement on RumblePad2

            //Debug.WriteLine("JoystickSubState() : X=" + X + "     Y=" + Y + "     Z=" + Z + "       Buttons: " + js.Buttons.Length);

            //StringBuilder sb = new StringBuilder();

            // js.Buttons.Length is usually 128

            for (int i = 0; i < buttons.Length && i < js.Buttons.Length; i++)
            {
                buttons[i] = js.Buttons[i];

                //if(i > 0 && i % 10 == 0)
                //    sb.AppendLine("");

                //sb.Append(" " + (buttons[i] ? "x" : "o"));
            }
            //Debug.WriteLine(sb.ToString());
            //Debug.WriteLine("JoystickSubState   : CurrentPower=" + CurrentPower);

            // emulate throttle via buttons 7 and 8 on RumblePad2:
            if (controlDeviceType == "Gamepad")
            {
                if (buttons[7]) // button 8 on RumblePad2
                {
                    if(!clickUp)
                        CurrentPower += 20.0d;

                    clickUp = true;
                }
                else if (buttons[6]) // button 7 on RumblePad2
                {
                    if(!clickDown)
                        CurrentPower -= 20.0d;

                    clickDown = true;
                }
                else
                {
                    clickUp = clickDown = false;
                }
                CurrentPower = Math.Min(Math.Max(CurrentPower, this.JoystickMinimumPower), 100.0d);
            }
        }
        */

        /// <summary>
        /// converts SubState to a command.
        /// Note: when button is released, command defaults to "speed:..."
        /// </summary>
        /// <returns></returns>
        public string GetCommand()
        {
            //Debug.WriteLine("X=" + this.X + "     Y=" + this.Y + "     Z=" + this.Z + "       Buttons: " + this.Buttons[0] + "  " + this.Buttons[1] + "  " + this.Buttons[2] + "  " + this.Buttons[3]);

            string command = null;
            int btnLength = this.Buttons.Length;

            if (this.Buttons[0])
            {
                command = "button1";    // "A" on XBox 360 controller
            }
            else if (this.Buttons[1])
            {
                command = "button2";    // "B"
            }
            else if (this.Buttons[2])
            {
                command = "button3";    // "X"
            }
            else if (this.Buttons[3])
            {
                command = "button4";    // "Y"
            }
            else if (btnLength > 4 && this.Buttons[4])
            {
                command = "button5";    // Left Bumper on XBox 360 controller
            }
            else if (btnLength > 5 && this.Buttons[5])
            {
                command = "button6";    // Right Bumper on XBox 360 controller
            }
            else if (btnLength > 6 && this.Buttons[6])
            {
                command = "button7";    // "Back" on XBox 360 controller
            }
            else if (btnLength > 7 && this.Buttons[7])
            {
                command = "button8";    // "Start" on XBox 360 controller
            }
            else if (btnLength > 8 && this.Buttons[8])
            {
                command = "button9";
            }
            else if (btnLength > 9 && this.Buttons[9])
            {
                command = "button10";
            }
            else
            {
                command = "speed|" + Math.Round(this.Speed, 2) + "|" + Math.Round(this.Turn, 2);  // speed and turn in the range of -100...+100
            }
            return command;
        }

        public override string ToString()
        {
            return "speed=" + Math.Round(this.Speed) + "  turn=" + Math.Round(this.Turn) + "  power=" + Math.Round(this.Power) + "  Buttons: " + this.buttons[0] + "  " + this.buttons[1] + "  " + this.buttons[2] + "  " + this.buttons[3];
        }

        /// <summary>
        /// needed for detecting a change in joystick state
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public override bool Equals(object obj)
        {
            if (obj == null)
            {
                return false;
            }

            JoystickSubState jsObj = (JoystickSubState)obj;

            if (X != jsObj.X || Y != jsObj.Y || Z != jsObj.Z)
            {
                return false;
            }

            for (int i = 0; i < buttons.Length && i < jsObj.buttons.Length; i++)
            {
                if (buttons[i] != jsObj.buttons[i])
                {
                    return false;
                }
            }

            // all state elements we were interested in are the same
            return true;
        }

        public override int GetHashCode()
        {
            int hash = 13;
            hash = (hash * 7) + X.GetHashCode();
            hash = (hash * 7) + Y.GetHashCode();
            hash = (hash * 7) + Z.GetHashCode();
            hash = (hash * 7) + buttons.GetHashCode();
            return hash;
        }

        public object Clone()
        {
            JoystickSubState jss = new JoystickSubState()
            {
                ControlDeviceType = this.ControlDeviceType,
                InstanceName = this.InstanceName,
                JoystickMinimumPower = this.JoystickMinimumPower,
                ProductName = this.ProductName,
                X = this.X,
                Y = this.Y,
                Z = this.Z,
                buttons = (bool[])this.buttons.Clone()
            };
            return jss;
        }
    }
}
