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
    public class OdometryReader : HardwareComponent, IOdometry
    {
        public OdometryReader(CommunicationTask cTask, CancellationToken ct, int si)
            : base(cTask, ct, si)
        {
            Start();
        }

        public long LDistanceTicks { get; set; }
        public long RDistanceTicks { get; set; }

        public double XMeters { get; set; }
        public double YMeters { get; set; }
        public double ThetaRadians { get; set; }

        public event HardwareComponentEventHandler OdometryChanged;

        /// <summary>
        /// sets all X,Y,Theta,L & R ticks to zero
        /// </summary>
        public async void ZeroAll()
        {
            if (Enabled)
            {
                string cmd = "odomreset";
                string resp = await commTask.SendAndReceive(cmd);   // should be "ACK"
            }
        }

        public long lastLDistanceTicks { get; set; }
        public long lastRDistanceTicks { get; set; }

        public double lastXMeters { get; set; }
        public double lastYMeters { get; set; }
        public double lastThetaRadians { get; set; }

        protected override async Task roundtrip()
        {
            string cmd = "odom";
            string resp = await commTask.SendAndReceive(cmd);
            if (!string.IsNullOrWhiteSpace(resp))
            {
                if (resp.Trim().IndexOf(" ") > 0)
                {
                    string[] splitResp = resp.Trim().Split(new char[] { ' ' });
                    if (splitResp.Length == 5)
                    {
                        long lTicks;
                        long rTicks;
                        double x;
                        double y;
                        double theta;

                        if (long.TryParse(splitResp[0].Trim(), out lTicks) && long.TryParse(splitResp[1].Trim(), out rTicks)
                            && double.TryParse(splitResp[2].Trim(), out x) && double.TryParse(splitResp[3].Trim(), out y)
                            && double.TryParse(splitResp[4].Trim(), out theta))
                        {
                            if (lastLDistanceTicks != lTicks || lastRDistanceTicks != rTicks
                                    || lastXMeters != x || lastYMeters != y || lastThetaRadians != theta)
                            {
                                this.LDistanceTicks = lTicks;
                                this.RDistanceTicks = rTicks;
                                this.XMeters = x;
                                this.YMeters = y;
                                this.ThetaRadians = theta;

                                lastLDistanceTicks = lTicks;
                                lastRDistanceTicks = rTicks;
                                lastXMeters = x;
                                lastYMeters = y;
                                lastThetaRadians = theta;

                                OdometryChanged?.Invoke(this);
                            }
                            return;
                        }
                    }
                }
            }
            Debug.WriteLineIf(!cancellationToken.IsCancellationRequested, "OdometryReader: roundtrip() : could not parse response: '" + resp + "'");
        }
    }
}
