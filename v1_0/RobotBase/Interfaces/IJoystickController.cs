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
using System.Threading.Tasks;

namespace slg.RobotBase.Interfaces
{
    public interface IJoystickController : IDisposable
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
        /// must be in the range 0...50 - defines what power will be used to compute Speed and Turn when throttle ("Z") position is around minimum
        /// </summary>
        double JoystickMinimumPower { get; set; }

        bool IsEnabled { get; }

        // to support joystick events (call PollJoystick() first to connect or maintain connection):
        event EventHandler<IJoystickSubState> joystickEvent;

        /// <summary>
        /// Supports polling. Will also try connecting the device. Call to connect or maintain connection.
        /// </summary>
        /// <returns>null if device not connected</returns>
        Task<IJoystickSubState> PollJoystick();
    }
}
