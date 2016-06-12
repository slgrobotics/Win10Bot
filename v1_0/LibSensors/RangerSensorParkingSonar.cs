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

using slg.RobotBase;
using slg.RobotAbstraction;
using slg.RobotAbstraction.Ids;
using slg.RobotAbstraction.Events;
using slg.RobotAbstraction.Sensors;

namespace slg.LibSensors
{
    /// <summary>
    /// Parking sonar - range 0m - 2.5m 
    /// </summary>
    public class RangerSensorParkingSonar : RangerSensorBase
    {
        private IParkingSonar parkingSonar;

        public double RangeFRmeters { get; private set; }
        public double RangeFLmeters { get; private set; }
        public double RangeBRmeters { get; private set; }
        public double RangeBLmeters { get; private set; }

        public override bool Enabled { get { return parkingSonar.Enabled; } set { parkingSonar.Enabled = value; } }

        public RangerSensorParkingSonar(string name, SensorPose pose, IAbstractRobotHardware brick, int frequency)
        {
            this.Name = name;
            this.Pose = pose;

            // reliably measured range:
            this.MinDistanceMeters = 0.0d;  // shows 0cm all right
            this.MaxDistanceMeters = 2.5d;  // shows 2.55m at infinity

            this.parkingSonar = brick.produceParkingSonar(frequency);

            parkingSonar.DistanceChanged += new HardwareComponentEventHandler(psonar_DistanceChanged);        
        }

        void psonar_DistanceChanged(IHardwareComponent sender)
        {
            RangeFRmeters = parkingSonar.RangeFRcm * 0.01d;
            RangeFLmeters = parkingSonar.RangeFLcm * 0.01d;
            RangeBRmeters = parkingSonar.RangeBRcm * 0.01d;
            RangeBLmeters = parkingSonar.RangeBLcm * 0.01d;

            Timestamp = DateTime.Now;

            OnDistanceChanged(new double[] { RangeFRmeters, RangeFLmeters, RangeBRmeters, RangeBLmeters });
        }
    }
}
