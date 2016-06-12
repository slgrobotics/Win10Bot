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
    /// SRF04 sonar - range 3cm - 2.5m 
    /// </summary>
    public class RangerSensorSonar : RangerSensorBase
    {
        private ISonarSRF04 srf04;

        // sensors deliver data in centimeters or inches. See ElementRobot.cs - element.Units = Units.Metric;

        private const double metersPerUnitSonar = 0.01d; //25.4d;        // calibrate to get meters for the sonars

        public override bool Enabled { get { return srf04.Enabled; } set { srf04.Enabled = value; } }

        public RangerSensorSonar(string name, SensorPose pose, IAbstractRobotHardware brick, GpioPinId triggerPin, GpioPinId outputPin, int frequency, double threshold)
        {
            this.Name = name;
            this.Pose = pose;

            // reliably measured range:
            this.MinDistanceMeters = 0.04d; // shows 3cm all right
            this.MaxDistanceMeters = 2.5d;  // shows 2.55m at infinity

            this.srf04 = brick.produceSonarSRF04(triggerPin, outputPin, frequency, threshold);

            srf04.DistanceChanged += new HardwareComponentEventHandler(ir_DistanceChanged);        
        }

        void ir_DistanceChanged(IHardwareComponent sender)
        {
            //Debug.WriteLine("sender: " + sender + "         Value=" + srf04.Distance.ToString());
            //Debug.WriteLine("Sonar: " + srf04.Distance.ToString() + String.Format(" = {0:0.00} units", srf04.Distance));

            double rangeMeters = srf04.Distance * metersPerUnitSonar;

            // we cannot trust minimum readings, the SRF04 sonar often goes to near 0 (0-9cm) for unknown reasons.
            if (rangeMeters > MinDistanceMeters && rangeMeters != RangeMeters)
            {
                RangeMeters = rangeMeters;
                Timestamp = DateTime.Now;

                OnDistanceChanged();
            }
        }
    }
}
