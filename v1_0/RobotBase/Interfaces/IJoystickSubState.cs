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

using slg.LibRobotMath;

namespace slg.RobotBase.Interfaces
{
    public interface IJoystickSubState : ICloneable
    {
        /// <summary>
        /// "Gamepad" or "Joystick"
        /// </summary>
        string ControlDeviceType { get; }

        /// <summary>
        /// for example, "Saitek ST290 Pro"
        /// </summary>
        string ProductName { get; }

        /// <summary>
        /// for example, "Saitek ST290 Pro"
        /// </summary>
        string InstanceName { get; }

        /// <summary>
        /// right turn positive, range -100.0...+100.0
        /// </summary>
        double Speed { get; }

        /// <summary>
        /// right turn positive, range -100.0...+100.0
        /// </summary>
        double Turn { get; }

        /// <summary>
        /// throttle 100% - all way forward/up
        /// </summary>
        double Power { get; }

        /// <summary>
        /// Joystick, Xbox360 or RamblePad2 buttons. Index 0 is Button 1
        /// </summary>
        bool[] Buttons { get; }

        /// <summary>
        /// takes state and converts it to a string command, easy to pass around
        /// </summary>
        /// <returns></returns>
        string GetCommand();

        /// <summary>
        /// A service flag to be set when JoystickSubState is created but not yet processed
        /// </summary>
        bool IsNew { get; set; }

    }
}
