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

using slg.RobotBase.Interfaces;
using slg.Mapping;
using slg.RobotMath;

namespace slg.RobotBase.Bases
{
    public class RobotStateBase : IRobotState
    {
        /// <summary>
        /// robot "unicycle" velocity, meters per second
        /// </summary>
        public double velocity { get; set; }

        /// <summary>
        /// omega, robot "unicycle" turn rate, radians per second
        /// </summary>
        public double omega { get; set; }

        private double? _goalBearingDegrees;

        /// <summary>
        /// Goal Bearing, degrees, can be null if not set, always constraines to 0...360 when set.
        /// As on the map, 0-North, 90-East, 180-South, 270-West
        /// </summary>
        public double? goalBearingDegrees
        {
            get { return _goalBearingDegrees; }
            set { _goalBearingDegrees = value.HasValue ? DirectionMath.to360(value) : null; }
        }

        /// <summary>
        /// Goal Bearing relative to robot heading, degrees, can be null if not set, always constraines to -180...180 when set.
        /// When you set goalBearingRelativeDegrees it does not set goalBearingDegrees - that must be set separately.
        /// </summary>
        public double? goalBearingRelativeDegrees { get; set; }

        /// <summary>
        /// use it when you have relative bearing and want to set goal bearing
        /// </summary>
        /// <param name="robotHeadingDegrees">take it from RobotPose</param>
        public void setGoalBearingByRelativeBearing(double? robotHeadingDegrees)
        {
            goalBearingDegrees = robotHeadingDegrees + goalBearingRelativeDegrees;
        }

        /// <summary>
        /// distance to goal, if set
        /// </summary>
        public double? goalDistanceMeters { get; set; }

        /// <summary>
        /// Goal GeoPosition
        /// </summary>
        public GeoPosition goalGeoPosition { get; set; }

        /// <summary>
        /// given current robot location, goal bearing and distance, compute and fill goalGeoPosition
        /// </summary>
        public void computeGoalGeoPosition(IRobotPose currentPose)
        {
            var pos = new GeoPosition(currentPose.geoPosition);
            pos.translateToDirection(new Direction() { heading = goalBearingDegrees }, new Distance(goalDistanceMeters.Value));
            goalGeoPosition = pos;
        }

        public override string ToString()
        {
            return String.Format("velocity={0} m/s ({1} cm/s)    omega={2} rad/s ({3} degrees/s)   goal at: {4} / {5} degrees, {6} meters",
                                  velocity, Math.Round(velocity * 100.0), omega, Math.Round(omega * 180.0d / Math.PI), goalBearingDegrees, goalBearingRelativeDegrees, goalDistanceMeters);
        }
    }
}
