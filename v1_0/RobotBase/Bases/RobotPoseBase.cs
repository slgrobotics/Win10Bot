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
using slg.LibRobotMath;
using slg.LibMapping;

namespace slg.RobotBase.Bases
{
    /// <summary>
    /// base class for all variations of robot Pose classes.
    /// Handles geo position on top of XY plane
    /// </summary>
    public class RobotPoseBase : PoseBase, IRobotPose
    {
        public IGeoPosition geoPosition { get; set; }

        public IDirection direction { get; set; }

        public double H     { get; set; }

        public void moveTo(double lng, double lat, double elevMeters)
        {
            geoPosition.moveTo(lng, lat, elevMeters);

            XMeters = 0.0d;
            YMeters = 0.0d;
            H = elevMeters;
        }

        public override void translate(double dXMeters, double dYMeters)
        {
            geoPosition.translate(new Distance(dXMeters), new Distance(dYMeters));

            base.translate(dXMeters, dYMeters);
        }

        public double? heading
        {
            get {
                return direction.heading;
            }

            set { 
                direction.heading = Direction.to360(value);
                //ThetaRadians = -DirectionMath.toRad(direction.heading.Value);
            }
        }

        public RobotPoseBase()
        {
            geoPosition = new GeoPosition(0.0d, 0.0d);
            direction = new Direction();

            H = 0.0d;
        }

        // Helpers:

        public override string ToString()
        {
            return string.Format("Pose: {0}    Theta: {1} degrees", base.ToString(), Math.Round(Direction.to360fromRad(ThetaRadians + Math.PI / 2.0d)))
                + "    GeoPos: " + geoPosition.ToStringExact()
                + "    Dir: " + direction;
        }
    }
}
