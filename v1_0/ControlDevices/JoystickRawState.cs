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

namespace slg.ControlDevices
{
    public class JoystickRawState : IJoystickRawState
    {
        public int X { get; set; }    // left stick horizontal movement on RumblePad2, XBox360
        public int Y { get; set; }    // left stick vertical movement on RumblePad2, XBox360
        public int Z { get; set; }    // throttle on gaming joysticks, right stick horizontal movement on RumblePad2, XBox360

        public int XR { get; set; }   // right stick horizontal movement on XBox360
        public int YR { get; set; }   // right stick vertical movement on XBox360

        public int LT { get; set; }   // left trigger on XBox360
        public int RT { get; set; }   // right trigger on XBox360

        public ControllerDpadDirection DpadDirection { get; set; }  // D-Pad state on XBox360

        protected bool[] buttons = new bool[10];    // we can process all 10 buttons on RumblePad2, XBox360

        public bool[] Buttons { get { return buttons; } }

        internal JoystickRawState()
        {
        }

        /// <summary>
        /// cloning constructor, also useful for derived classes
        /// </summary>
        /// <param name="raw"></param>
        public JoystickRawState(IJoystickRawState raw)
        {
            this.X = raw.X;
            this.Y = raw.Y;
            this.Z = raw.Z;

            this.XR = raw.XR;
            this.YR = raw.YR;

            this.LT = raw.LT;
            this.RT = raw.RT;

            this.DpadDirection = raw.DpadDirection;

            this.buttons = (bool[])raw.Buttons.Clone();
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

            JoystickRawState jsObj = (JoystickRawState)obj;

            if (X != jsObj.X || Y != jsObj.Y || Z != jsObj.Z
                || XR != jsObj.XR || YR != jsObj.YR
                || LT != jsObj.LT || RT != jsObj.RT
                || DpadDirection != jsObj.DpadDirection)
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

        /// <summary>
        /// needed for "Equals"
        /// </summary>
        /// <returns></returns>
        public override int GetHashCode()
        {
            int hash = 13;
            hash = (hash * 7) + X.GetHashCode();
            hash = (hash * 7) + Y.GetHashCode();
            hash = (hash * 7) + Z.GetHashCode();
            hash = (hash * 7) + XR.GetHashCode();
            hash = (hash * 7) + YR.GetHashCode();
            hash = (hash * 7) + LT.GetHashCode();
            hash = (hash * 7) + RT.GetHashCode();
            hash = (hash * 7) + DpadDirection.GetHashCode();
            hash = (hash * 7) + buttons.GetHashCode();
            return hash;
        }
    }
}
