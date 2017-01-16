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

using slg.LibRobotMath;

namespace slg.LibMapping
{
    /// <summary>
    /// Relative Position is robot-related coordinate system expressed in meters and degrees (if angular)
    /// All objects in MapperVicinity have RelPosition coordinates and move as the robot moves.
    /// </summary>
    public class RelPosition : IComparable, ICloneable
    {
        // rectangular grid:

        public double XMeters;    // meters from the robot, forward is positive
        public double YMeters;    // meters from the robot, left is positive

        // angular system:

        public Distance dist;
        public Direction dir;   // straight forward is 0, right is positive


        public RelPosition(Direction direction, Distance distance)
        {
            this.dir = (Direction)direction.Clone();
            this.dist = (Distance)distance.Clone();

            if (direction.bearingRelative.HasValue)
            {
                double bearingRad = DirectionMath.toRad(direction.bearingRelative.Value);

                XMeters = distance.Meters * Math.Sin(bearingRad);
                YMeters = -distance.Meters * Math.Cos(bearingRad);
            }
            else if (direction.bearing.HasValue)
            {
                double bearingRad = DirectionMath.toRad(direction.bearing.Value);

                XMeters = distance.Meters * Math.Sin(bearingRad);
                YMeters = -distance.Meters * Math.Cos(bearingRad);
            }
        }

        public override string ToString()
        {
            return string.Format("({0},{1}):({2},{3})", XMeters, YMeters, dist, dir);
        }

        #region ICloneable Members

        public object Clone()
        {
            RelPosition ret = (RelPosition) this.MemberwiseClone();  // shallow copy, only value types are cloned

            ret.dist = (Distance)this.dist.Clone();
            ret.dir = (Direction)this.dir.Clone();

            return ret;
        }

        #endregion // ICloneable Members

        #region ICompareable Members

        /// <summary>
        /// Comparing two obstacles
        /// the closer the oject, the larger it is for Compare purposes
        /// </summary>
        /// <param name="other"></param>
        /// <returns></returns>
        public int CompareTo(object other)
        {
            return (int)((dist.Meters - ((RelPosition)other).dist.Meters) * 100.0d);    // precision up to 10mm
        }

        #endregion // ICompareable Members

    }
}
