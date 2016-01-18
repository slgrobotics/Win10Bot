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
using slg.RobotMath;
using slg.Mapping;

namespace slg.RobotBase.Bases
{
    /// <summary>
    /// base class for all variations of Pose classes
    /// see http://www.lejos.org/nxt/nxj/api/lejos/robotics/navigation/Pose.html 
    ///     http://www.digitalrune.com/Documentation/html/d995ee69-0650-4993-babd-1cdb1fd8fd7a.htm
    ///     http://docs.ros.org/groovy/api/ndt_mcl/html/2d__ndt__mcl__node_8cpp_source.html for inspiration
    /// </summary>
    public class PoseBase : IPose, ICloneable
    {
        /// <summary>
        /// meters, coordinate along the axis of the robot, positive - front direction
        /// </summary>
        public double X     { get; set; }

        /// <summary>
        /// meters, coordinate perpendicular to the axis of the robot, positive - left direction
        /// </summary>
        public double Y     { get; set; }

        /// <summary>
        /// radians, zero when along X, positive towards left turn
        /// </summary>
        public double Theta { get; set; }

        public PoseBase()
        {
            X = 0.0d;
            Y = 0.0d;
            Theta = 0.0d;
        }

        public virtual void translate(double dXMeters, double dYMeters)
        {
            X += dXMeters;
            Y += dYMeters;
        }

        public virtual void translate(Distance dist, IDirection dir)
        {
            double distMeters = dist.Meters;
            double dTheta = -DirectionMath.toRad(dir.bearing.Value);  // radians; theta is positive to left, bearing - positive to right

            this.translate(distMeters * Math.Cos(dTheta), distMeters * Math.Sin(dTheta));
        }

        public virtual void rotate(double alphaRad)
        {
            Theta -= alphaRad;  // alphaRad is positive towards right turn
        }

        public void resetXY()
        {
            X = 0.0d;
            Y = 0.0d;
        }

        public void resetRotation()
        {
            Theta = 0.0d;
        }

        /// <summary>
        /// performs translate->rotate operation on a cloned p1 and returns it
        /// example: p1 - robot pose in absolute coords, p2 - sensor pose relative to robot (in robot coords). p1 * p2 is sensor pose in absolute coords
        /// </summary>
        /// <param name="p1"></param>
        /// <param name="p2"></param>
        /// <returns></returns>
        public static PoseBase operator *(PoseBase p1, IPose p2)
        {
            PoseBase p = (PoseBase)p1.Clone();

            double alpha = p1.Theta + Math.Atan2(p2.Y, p2.X);     // polar coord - angle component of p2 coordinate
            double length = Math.Sqrt(p2.X * p2.X + p2.Y * p2.Y); // polar coord - length component of p2 coordinate

            p.translate(length * Math.Cos(alpha), length * Math.Sin(alpha));
            p.rotate(-p2.Theta);    // rotate normally takes positive-to-right argument

            // same as:
            //double x1 = p1.X + length * Math.Cos(alpha);    // abs coords
            //double y1 = p1.Y + length * Math.Sin(alpha);
            //double theta1 = p1.Theta + p2.Theta;            // p2 rotation is a sum of two

            //PoseBase p = new PoseBase() { X = x1, Y = y1, Theta = theta1 };

            return p;
        }


        // Helpers:

        public override string ToString()
        {
            return string.Format("X={0:0.00}   Y={1:0.00}   Theta={2:0.00}", X, Y, Theta);
        }

        // ICloneable implmentation:

        public object Clone()
        {
            return new PoseBase() { X = this.X, Y = this.Y, Theta = this.Theta };
        }
    }
}
