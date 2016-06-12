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
    /// Sharp GP2Y0A02 - range 20...150 cm
    /// </summary>
    public class RangerSensorIR20_150 : RangerSensorBase
    {
        private ISharpGP2D12 gp2d12;     // GP2Y0A02 - range 20...150 cm.

        // GP2D12 is close to a GP2Y0A21 - range 10...80 cm. GP2D120 is a 4...30 cm sensor. GP2Y0A02 is a 20...150 cm.
        // sensors deliver data in centimeters or inches. See ElementRobot.cs - element.Units = Units.Metric;

        private const double metersPerUnitIR_20_150 = 0.02d; //55.0d;    // calibrate to get meters for the IR 20-150cm sensors (front and rear)

        public override bool Enabled { get { return gp2d12.Enabled; } set { gp2d12.Enabled = value; } }

        public RangerSensorIR20_150(string name, SensorPose pose, IAbstractRobotHardware brick, AnalogPinId pinId, int frequency, double threshold)
        {
            this.Name = name;
            this.Pose = pose;

            // reliably measured range:
            this.MinDistanceMeters = 0.26d;     // shows 0.22 when too close
            this.MaxDistanceMeters = 1.52d;     // shows 1.60 at infinity

            this.gp2d12 = brick.produceSharpGP2D12(pinId, frequency, threshold);

            gp2d12.DistanceChanged += new HardwareComponentEventHandler(ir_DistanceChanged);
        }

        void ir_DistanceChanged(IHardwareComponent sender)
        {
            //Debug.WriteLine("sender: " + sender + "         Value=" + gp2d12.Value.ToString());
            //Debug.WriteLine("IR: " + gp2d12.Value.ToString() + String.Format(" = {0:0.00} units", gp2d12.Distance));

            double rangeMeters = gp2d12.Distance * metersPerUnitIR_20_150;
            if (rangeMeters != RangeMeters)
            {
                RangeMeters = rangeMeters;
                Timestamp = DateTime.Now;

                OnDistanceChanged();
            }
        }
    }
}
