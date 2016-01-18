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


using slg.Mapping;

namespace slg.RobotBase.Interfaces
{
    public interface IRobotState
    {
        /// <summary>
        /// robot "unicycle" velocity, meters per second
        /// </summary>
        double velocity { get; set; }

        /// <summary>
        /// omega, robot "unicycle" turn rate, radians per second
        /// </summary>
        double omega { get; set; }

        /// <summary>
        /// Goal Bearing, degrees, can be null if not set, always constraines to 0...360 when set.
        /// As on the map, 0-North, 90-East, 180-South, 270-West
        /// </summary>
        double? goalBearingDegrees { get; set; }

        /// <summary>
        /// Goal Bearing relative to robot heading, degrees, can be null if not set, always constraines to -180...180 when set.
        /// When you set goalBearingRelativeDegrees it does not set goalBearingDegrees - that must be set separately.
        /// </summary>
        double? goalBearingRelativeDegrees { get; set; }

        /// <summary>
        /// distance to goal, if set
        /// </summary>
        double? goalDistanceMeters { get; set; }

        /// <summary>
        /// Goal GeoPosition
        /// </summary>
        GeoPosition goalGeoPosition { get; set; }

        /// <summary>
        /// given current robot location, goal bearing and distance, compute and fill goalGeoPosition
        /// </summary>
        void computeGoalGeoPosition(IRobotPose currentPose);

        /// <summary>
        /// use it when you have relative bearing and want to set goal bearing
        /// </summary>
        /// <param name="robotHeadingDegrees">take it from RobotPose</param>
        void setGoalBearingByRelativeBearing(double? robotHeadingDegrees);
    }
}
