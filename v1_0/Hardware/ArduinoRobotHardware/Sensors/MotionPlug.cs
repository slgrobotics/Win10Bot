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

using System.Threading.Tasks;
using System.Threading;
using System.Diagnostics;

using slg.RobotAbstraction.Events;
using slg.RobotAbstraction.Sensors;
using System;

namespace slg.ArduinoRobotHardware.Sensors
{
    public class MotionPlug : HardwareComponent, IAhrs
    {
        public int Yaw { get; set; }    // -180..+180

        public MotionPlug(CommunicationTask cTask, CancellationToken ct, int si)
            : base(cTask, ct, si)
        {
            Start();
        }

        public event HardwareComponentEventHandler ValuesChanged;

        private int lastYaw;

        protected override async Task roundtrip()
        {
            string cmd = "compass";
            string resp = await commTask.SendAndReceive(cmd);
            if (!string.IsNullOrWhiteSpace(resp))
            {
                double yaw;

                if (double.TryParse(resp.Trim(), out yaw))
                {
                    int iYaw = (int)Math.Round(yaw);

                    if (iYaw != lastYaw)
                    {
                        this.Yaw = iYaw;

                        lastYaw = iYaw;

                        ValuesChanged?.Invoke(this);
                    }
                    return;
                }
            }
            Debug.WriteLineIf(!cancellationToken.IsCancellationRequested, "MotionPlug: roundtrip() : could not parse response: '" + resp + "'");
        }
    }
}
