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

using slg.RobotBase.Interfaces;

namespace slg.RobotBase.Data
{
    public class BehaviorData : IBehaviorData
    {
        public BehaviorData()
        {
            timestamp = DateTime.Now;
        }

        /// <summary>
        /// robot state estimate
        /// </summary>
        public IRobotState  robotState  { get; set; }

        /// <summary>
        /// robot pose estimate
        /// </summary>
        public IRobotPose robotPose { get; set; }

        /// <summary>
        /// sensors data combo - all readings at the moment
        /// </summary>
        public ISensorsData sensorsData { get; set; }

        /// <summary>
        /// computed command to drive, or null
        /// </summary>
        public IDriveInputs driveInputs { get; set; }

        /// <summary>
        /// timestamp when this dataset was first created
        /// </summary>
        public DateTime timestamp { get; private set; }
    }
}
