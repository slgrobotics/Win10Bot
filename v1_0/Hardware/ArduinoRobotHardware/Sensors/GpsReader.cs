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
    public class GpsReader : HardwareComponent, IGps
    {
        public GpsReader(CommunicationTask cTask, CancellationToken ct, int si)
            : base(cTask, ct, si)
        {
            FixType = GpsFixTypes.None;
            Altitude = null;

            Start();
        }

        public double Latitude  { get; private set; }
        public double Longitude { get; private set; }
        public double? Altitude  { get; private set; }
        public int GpsHdop { get; private set; }
        public int GpsNsat { get; private set; }
        public int FixAgeMs { get; private set; }
        public DateTime TimeUTC { get; private set; }
        public GpsFixTypes FixType { get; private set; }
        public long timestamp   { get; private set; }

        public event HardwareComponentEventHandler GpsPositionChanged;

        public void Close()
        {
            throw new NotImplementedException();
        }

        public Task Open(CancellationTokenSource cts)
        {
            throw new NotImplementedException();
        }

        public override string ToString()
        {
            return String.Format("{0} {1} {2} {3} {4}", FixType, Latitude, Longitude, Altitude, GpsHdop);
        }

        private int lastFix;
        private int lastNsat;
        private int lastFixAge;
        private int lastGpsHdop;
        private double lastLatitude;
        private double lastLongitude;
        //private double? lastAltitude;

        protected override async Task roundtrip()
        {
            string cmd = "gps";
            string resp = await commTask.SendAndReceive(cmd);

            // expect:
            //   1 8 722 100 33.575926500 -117.662919667
            //   fix  nSat  ageMs  HDOP  lat  long
            //   fix==1 - 2D   fix=2 - 3D 
            // when fix==0, only the fix is reported

            if (!string.IsNullOrWhiteSpace(resp))
            {
                if (resp.Trim().IndexOf(" ") > 0)
                {
                    string[] splitResp = resp.Trim().Split(new char[] { ' ' });

                    if (splitResp.Length == 6)
                    {
                        int fix;
                        int nSat;
                        int fixAge;
                        int gpsHdop;
                        double latitude;
                        double longitude;
                        //double? altitude;

                        if (int.TryParse(splitResp[0].Trim(), out fix) && int.TryParse(splitResp[1].Trim(), out nSat)
                            && int.TryParse(splitResp[2].Trim(), out fixAge) && int.TryParse(splitResp[3].Trim(), out gpsHdop)
                            && double.TryParse(splitResp[4].Trim(), out latitude) && double.TryParse(splitResp[5].Trim(), out longitude))
                        {
                            if (lastFix != fix || lastNsat != nSat || lastFixAge != fixAge
                                || lastGpsHdop != gpsHdop || lastLatitude != latitude || lastLongitude != longitude)
                            {
                                this.FixType = fix == 1 ? GpsFixTypes.Fix2D : GpsFixTypes.Fix3D;
                                this.GpsNsat = nSat;
                                this.FixAgeMs = fixAge;
                                this.GpsHdop = gpsHdop;
                                this.Latitude = latitude;
                                this.Longitude = longitude;

                                lastFix = fix;
                                lastNsat = nSat;
                                lastFixAge = fixAge;
                                lastGpsHdop = gpsHdop;
                                lastLatitude = latitude;
                                lastLongitude = longitude;

                                GpsPositionChanged?.Invoke(this);
                            }
                            return;
                        }
                    }
                }
                FixType = GpsFixTypes.None;
            }
            Debug.WriteLineIf(!cancellationToken.IsCancellationRequested, "GpsReader: roundtrip() : could not parse response: '" + resp + "'");
        }
    }
}
