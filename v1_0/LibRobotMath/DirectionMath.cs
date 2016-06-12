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

namespace slg.LibRobotMath
{
    public class DirectionMath
    {
        /// <summary>
        /// normalize angle to be within 0...360
        /// </summary>
        /// <param name="angle"></param>
        /// <returns></returns>
        public static double? to360(double? angle)
        {
            double? ret = null;

            if (angle.HasValue)
            {
                ret = angle % 360.0d;
                if (ret < 0.0d)
                {
                    ret += 360.0d;
                }
            }
            return ret;
        }

        public static double to360(double angle)
        {
            angle %= 360.0d;

            if (angle < 0.0d)
            {
                angle += 360.0d;
            }
            return angle;
        }

        public static double to360fromRad(double angle)
        {
            angle = angle * 180.0d / Math.PI;

            angle %= 360.0d;

            if (angle < 0.0d)
            {
                angle += 360.0d;
            }
            return angle;
        }

        /// <summary>
        /// normalize angle to be within -180...180
        /// </summary>
        /// <param name="angle"></param>
        /// <returns></returns>
        public static double? to180(double? angle)
        {
            double? ret = null;

            if (angle.HasValue)
            {
                ret = angle % 360.0d;

                if (ret > 180.0d)
                {
                    ret -= 360.0d;
                }
                if (ret < -180.0d)
                {
                    ret += 360.0d;
                }
            }
            return ret;
        }

        public static double to180(double angle)
        {
            angle %= 360.0d;

            if (angle > 180.0d)
            {
                angle -= 360.0d;
            }
            if (angle < -180.0d)
            {
                angle += 360.0d;
            }
            return angle;
        }

        /// <summary>
        /// for most direction display operations we need int, given angle in rads
        /// </summary>
        /// <param name="angle"></param>
        /// <returns></returns>
        public static int to180fromRad(double angle)
        {
            angle = angle * 180.0d / Math.PI;

            angle %= 360.0d;

            if (angle > 180.0d)
            {
                angle -= 360.0d;
            }
            if (angle < -180.0d)
            {
                angle += 360.0d;
            }
            return (int)Math.Round(angle);
        }

        public static double toDegrees(double angleRad)
        {
            return angleRad * 180.0d / Math.PI;
        }

        public static double toRad(double angleDegrees)
        {
            return angleDegrees * Math.PI / 180.0d;
        }
    }
}
