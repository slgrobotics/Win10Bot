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
    public enum ControllerDpadDirection { None = 0, Up, UpRight, Right, DownRight, Down, DownLeft, Left, UpLeft }

    public interface IJoystickRawState
    {
        int X { get; }    // left stick horizontal movement on RumblePad2, XBox360
        int Y { get; }    // left stick vertical movement on RumblePad2, XBox360
        int Z { get; }    // throttle on gaming joysticks, right stick horizontal movement on RumblePad2, adjuated by D-Pad up/down on XBox360

        int XR { get; }   // right stick horizontal movement on XBox360
        int YR { get; }   // right stick vertical movement on XBox360

        int LT { get; }   // left trigger on XBox360
        int RT { get; }   // right trigger on XBox360

        ControllerDpadDirection DpadDirection { get; }  // D-Pad state on XBox360

        bool[] Buttons { get; } // buttons state, when translated to generic button1...button10 notation
    }
}
