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

using slg.RobotBase.Events;

namespace slg.RobotBase.Interfaces
{
    /// <summary>
    /// common representation of Sonar, IR and other range sensors
    /// </summary>
    public interface IRangerSensor
    {
        string Name { get; }

        SensorPose Pose { get; set; }

        bool Enabled { get; set; }

        /// <summary>
        /// reliably measured range - minimum distance
        /// </summary>
        double MinDistanceMeters { get; set; }

        /// <summary>
        /// reliably measured range - maximum distance before infinity
        /// </summary>
        double MaxDistanceMeters { get; set; }

        bool InRange(double meters);

        // current (measures at the last cycle) values:
        double RangeMeters { get; }
        DateTime Timestamp { get; }

        event EventHandler<RangerSensorEventArgs> distanceChangedEvent;
    }
}
