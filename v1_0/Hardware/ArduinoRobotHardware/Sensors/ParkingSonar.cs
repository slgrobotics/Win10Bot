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

namespace slg.ArduinoRobotHardware.Sensors
{
    public class ParkingSonar : HardwareComponent, IParkingSonar
    {
        public double RangeFRcm { get; private set; }
        public double RangeFLcm { get; private set; }
        public double RangeBRcm { get; private set; }
        public double RangeBLcm { get; private set; }

        public event HardwareComponentEventHandler DistanceChanged;

        public ParkingSonar(CommunicationTask cTask, CancellationToken ct, int uf)
            : base(cTask, ct, uf)
        {
            Start();
        }

        private int lastRangeFRcm;
        private int lastRangeFLcm;
        private int lastRangeBRcm;
        private int lastRangeBLcm;

        protected override async Task roundtrip()
        {
            string cmd = "psonar";
            string resp = await commTask.SendAndReceive(cmd);
            if (!string.IsNullOrWhiteSpace(resp))
            {
                if (resp.Trim().IndexOf(" ") > 0)
                {
                    string[] splitResp = resp.Trim().Split(new char[] { ' ' });
                    if (splitResp.Length == 4)
                    {
                        int rangeFRcm;
                        int rangeFLcm;
                        int rangeBRcm;
                        int rangeBLcm;

                        if (int.TryParse(splitResp[0].Trim(), out rangeFRcm) && int.TryParse(splitResp[1].Trim(), out rangeFLcm)
                            && int.TryParse(splitResp[2].Trim(), out rangeBRcm) && int.TryParse(splitResp[3].Trim(), out rangeBLcm))
                        {
                            if (lastRangeFRcm != rangeFRcm || lastRangeFLcm != rangeFLcm || lastRangeBRcm != rangeBRcm || lastRangeBLcm != rangeBLcm)
                            {
                                this.RangeFRcm = rangeFRcm;
                                this.RangeFLcm = rangeFLcm;
                                this.RangeBRcm = rangeBRcm;
                                this.RangeBLcm = rangeBLcm;

                                lastRangeFRcm = rangeFRcm;
                                lastRangeFLcm = rangeFLcm;
                                lastRangeBRcm = rangeBRcm;
                                lastRangeBLcm = rangeBLcm;

                                DistanceChanged?.Invoke(this);
                            }
                            return;
                        }
                    }
                }
            }
            Debug.WriteLineIf(!cancellationToken.IsCancellationRequested, "ParkingSonar: roundtrip() : could not parse response: '" + resp + "'");
        }
    }
}
