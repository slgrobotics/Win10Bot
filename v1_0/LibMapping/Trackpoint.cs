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

namespace slg.LibMapping
{
    public enum TrackpointState
    {
        None,
        SelectedAsTarget,
        Passed,
        CouldNotReach
    }

    /// <summary>
    /// Trackpoint is similar to MavLink's Locationwp, but usable anywhere in C# planner code
    /// see http://ardupilot.org/planner/docs/common-install-mission-planner.html for track planning utility
    /// </summary>
    [DataContract]
    public class Trackpoint : WayPoint
    {
        public int number;              // sequential number taken from column 1 of the file
        public MAV_CMD id;				// command id
        public bool isHome;             // home waypoint is marked by this flag.
        public TrackpointState trackpointState;
        public DateTime? estimatedTimeOfArrival;    // when we set to reach the waypoint, we estimate arrival
        public CoordinateFrameOption coordinateFrameOption;
        public GeoPosition geoPosition;
        public double p1;				// param 1
        public double p2;				// param 2
        public double p3;				// param 3
        public double p4;				// param 4

        public Trackpoint(int num, bool ishome, double latitude, double longtitude, double? altitude = null)
            : this(new Locationwp()
            {
                id = (byte)MAV_CMD.WAYPOINT,
                number = num,
                ishome = (byte)(ishome ? 1 : 0),
                lat = latitude,
                lng = longtitude,
                alt = altitude ?? 0.0d
            })
        {
        }

        /// <summary>
        /// this constructor is used only for reading mission files
        /// </summary>
        /// <param name="lw"></param>
        internal Trackpoint(Locationwp lw)
        {
            trackpointState = TrackpointState.None;

            number = lw.number;
            id = (MAV_CMD)Enum.Parse(typeof(MAV_CMD), lw.id.ToString());    // command id

            isHome = lw.ishome != 0;

            coordinateFrameOption = lw.options == 1 ? CoordinateFrameOption.MAV_FRAME_GLOBAL_RELATIVE_ALT : CoordinateFrameOption.MAV_FRAME_GLOBAL;

            // alt is in meters, can be above the ground (AGL) or above mean sea level (MSL), depending on coordinateFrameOption
            geoPosition = new GeoPosition(lw.lng, lw.lat, lw.alt);

            // p1-p4 are just float point numbers that can be added to waypoints and be interpreted by behaviors. We pass them all directly.
            p1 = lw.p1;
            p2 = lw.p2;
            p3 = lw.p3;
            p4 = lw.p4;
        }

        public Direction directionToWp(IGeoPosition myPos, IDirection myDir) { return new Direction() { heading = myDir.heading, bearing = myPos.bearingTo(this.geoPosition) }; }

        public Distance distanceToWp(IGeoPosition myPos) { return geoPosition.distanceFrom(myPos); }
    }
}
