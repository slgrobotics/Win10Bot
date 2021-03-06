﻿/*
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

namespace slg.RobotAbstraction.Drive
{
    /// <summary>
    /// allows to set LeftMotorSpeed and RightMotorSpeed for a tank (differential) drive.
    /// Hardware brick should implement it. 
    /// </summary>
    public interface IDifferentialMotorController
    {
        bool Enabled { get; set; }

        int LeftMotorSpeed { get; set; }

        int RightMotorSpeed { get; set; }

        /// <summary>
        /// communicate current properties (speeds) to motors, let them roll
        /// </summary>
        void DriveMotors();

        /// <summary>
        /// idle (feather) motors disconnecting all power.  
        /// </summary>
        void FeatherMotors();

        /// <summary>
        /// ensire that wheels will not rotate, whether via motors control or applying brakes
        /// </summary>
        void BrakeMotors();

        /// <summary>
        /// force commands send to brick, not waiting for polling interval
        /// </summary>
        void Update();
    }
}
