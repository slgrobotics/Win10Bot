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
using System.Runtime.Serialization;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
using System.IO;

namespace slg.LibMapping
{
    [DataContract]
    public class Track
    {
        [DataMember]
        private string trackFileName;

        [DataMember]
        public List<Trackpoint> trackpoints = new List<Trackpoint>();

        [DataMember]
        public Trackpoint home { get; private set; }

        public int Count { get { return trackpoints.Count; } }

        public Track()
        {
        }

        public void Init(string trackFileName)
        {
            this.trackFileName = trackFileName;

            trackpoints.Clear();

            readQGC110wpfile();

            home = (from w in trackpoints where w.isHome select w).FirstOrDefault();
        }

        public Trackpoint nextTargetWp
        {
            get
            {
                return (from wp in trackpoints
                        where !wp.isHome && (wp.trackpointState == TrackpointState.None || wp.trackpointState == TrackpointState.SelectedAsTarget)
                        orderby wp.number
                        select wp).FirstOrDefault();
            }
        }

        /// <summary>
        /// see http://ardupilot.org/planner/docs/common-install-mission-planner.html for track planning utility
        /// see C:\Program Files (x86)\APM Planner
        /// see C:\Projects\Win10\Missions
        /// (old) see C:\Projects\Robotics\DIY_Drones\ArduPlane-2.40\ArduPlane-2.40\Tools\ArdupilotMegaPlanner\GCSViews\FlightPlanner.cs line 1786, 1144
        /// </summary>
        /// <param name="missionFileName"></param>
        private void readQGC110wpfile()
        {
            int wp_count = 0;
            bool error = false;
            string identLine = "QGC WPL 110";

            Debug.WriteLine("IP: readQGC110wpfile()  missionFileName=" + trackFileName);

            try
            {
                using (FileStream fs = new FileStream(trackFileName, FileMode.Open, FileAccess.Read))
                {
                    using (StreamReader sr = new StreamReader(fs))
                    {
                        string header = sr.ReadLine();
                        if (header == null || !header.Contains(identLine))
                        {
                            Debug.WriteLine("Invalid Waypoint file '" + trackFileName + "' - must contain first line '" + identLine + "'");
                            return;
                        }

                        System.Globalization.CultureInfo cultureInfo = new System.Globalization.CultureInfo("en-US");

                        while (!error && !sr.EndOfStream)
                        {
                            string line = sr.ReadLine();
                            // trackpoints

                            if (line.StartsWith("#"))
                                continue;

                            string[] items = line.Split(new char[] { (char)'\t', ' ' }, StringSplitOptions.RemoveEmptyEntries);

                            if (items.Length <= 9)
                                continue;

                            try
                            {
                                Locationwp temp = new Locationwp();

                                temp.number = int.Parse(items[0]);

                                temp.ishome = byte.Parse(items[1]);

                                if (items[2] == "3")
                                {
                                    // normally altitute is above mean sea level (MSL), or relative to ground (AGL) when MAV_FRAME_GLOBAL_RELATIVE_ALT=3
                                    temp.options = 1;
                                }
                                else
                                {
                                    temp.options = 0;
                                }
                                temp.id = (byte)(int)Enum.Parse(typeof(MAV_CMD), items[3], false);
                                temp.p1 = float.Parse(items[4], cultureInfo);

                                if (temp.id == 99)
                                    temp.id = 0;

                                temp.alt = (float)(double.Parse(items[10], cultureInfo));
                                temp.lat = (float)(double.Parse(items[8], cultureInfo));
                                temp.lng = (float)(double.Parse(items[9], cultureInfo));

                                temp.p2 = (float)(double.Parse(items[5], cultureInfo));
                                temp.p3 = (float)(double.Parse(items[6], cultureInfo));
                                temp.p4 = (float)(double.Parse(items[7], cultureInfo));

                                trackpoints.Add(new Trackpoint(temp));

                                wp_count++;

                            }
                            catch { Debug.WriteLine("Error: Line invalid: " + line); }

                            if (wp_count == byte.MaxValue)
                            {
                                Debug.WriteLine("Error: Too many trackpoints!!! - limited to " + byte.MaxValue);
                                break;
                            }
                        }
                    }
                }
                Debug.WriteLine("OK: readQGC110wpfile()  done, waypoint count=" + trackpoints.Count);
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Error: Can't open file: '" + trackFileName + "'" + ex.ToString());
                throw;
            }
        }
    }
}
