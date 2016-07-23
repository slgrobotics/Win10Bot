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
using System.Threading;
using System.Diagnostics;

using slg.RobotAbstraction.Sensors;
using slg.RobotAbstraction.Events;
using slg.RobotAbstraction.Ids;

namespace slg.ArduinoRobotHardware.Sensors
{
    public class AnalogSensor : HardwareComponent, IAnalogSensor 
    {
        public int AnalogValue { get; set; }

        public event HardwareComponentEventHandler AnalogValueChanged;

        protected AnalogPinId pin;
        protected int valueChangedThreshold;
        protected int lastValue;

        public AnalogSensor(CommunicationTask cTask, CancellationToken ct, int si, AnalogPinId p, int vct)
            : base("AnalogSensor", cTask, ct, si)
        {
            pin = p;
            valueChangedThreshold = vct;

            Start();
        }

        protected override async Task roundtrip()
        {
            string cmd = String.Format("sensor {0}", (ushort)pin);
            string resp = await commTask.SendAndReceive(cmd);
            int iResp = 0;

            if (!string.IsNullOrWhiteSpace(resp) && int.TryParse(resp.Trim(), out iResp))
            {
                this.AnalogValue = iResp;
                if (valueChangedThreshold == 0 || Math.Abs(iResp - lastValue) > valueChangedThreshold)
                {
                    lastValue = iResp;
                    AnalogValueChanged?.Invoke(this);
                }
            }
            else
            {
                Debug.WriteLineIf(!cancellationToken.IsCancellationRequested, "AnalogSensor pin " + pin + " : roundtrip() : could not parse response: '" + resp + "'");
            }
        }
    }
}
