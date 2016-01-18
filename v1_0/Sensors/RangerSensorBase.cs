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

using slg.RobotBase;
using slg.RobotBase.Interfaces;
using slg.RobotBase.Events;

namespace slg.Sensors
{
    public class RangerSensorBase : IRangerSensor
    {
        public string Name { get; set; }

        public SensorPose Pose { get; set; }

        public virtual bool Enabled { get; set; }

        /// <summary>
        /// reliably measured range - minimum distance
        /// </summary>
        public double MinDistanceMeters { get; set; }

        /// <summary>
        /// reliably measured range - maximum distance before infinity
        /// </summary>
        public double MaxDistanceMeters { get; set; }

        public bool InRange(double meters) { return meters >= this.MinDistanceMeters && meters <= this.MaxDistanceMeters; }

        // current (measures at the last cycle) values:
        public double RangeMeters { get; protected set; }
        public DateTime Timestamp { get; protected set; }

        public event EventHandler<RangerSensorEventArgs> distanceChangedEvent;

        protected void OnDistanceChanged()
        {
            EventHandler<RangerSensorEventArgs> handler = distanceChangedEvent;
            if (handler != null)
            {
                handler(this, new RangerSensorEventArgs() { Name = this.Name, RangeMeters = this.RangeMeters, TimeTicks = this.Timestamp.Ticks });
            }
        }
    }
}
