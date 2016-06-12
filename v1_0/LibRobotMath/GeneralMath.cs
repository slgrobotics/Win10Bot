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
    public static class GeneralMath
    {
        /// <summary>
        /// see https://www.arduino.cc/en/reference/map
        /// </summary>
        /// <param name="x"></param>
        /// <param name="in_min"></param>
        /// <param name="in_max"></param>
        /// <param name="out_min"></param>
        /// <param name="out_max"></param>
        /// <returns>x mapped from in to out ranges</returns>
        public static int map(int x, int in_min, int in_max, int out_min, int out_max)
        {
            return (int)Math.Round(((double)(x - in_min)) * ((double)(out_max - out_min)) / (double)(in_max - in_min)) + out_min;
        }

        public static long map(long x, long in_min, long in_max, long out_min, long out_max)
        {
            return (long)Math.Round(((double)(x - in_min)) * ((double)(out_max - out_min)) / (double)(in_max - in_min)) + out_min;
        }

        public static double map(double x, double in_min, double in_max, double out_min, double out_max)
        {
            return (x - in_min) * (out_max - out_min) / (in_max - in_min) + out_min;
        }

        /// <summary>
        /// see https://www.arduino.cc/en/Reference/Constrain
        /// </summary>
        /// <param name="amt"></param>
        /// <param name="low"></param>
        /// <param name="high"></param>
        /// <returns>amt constrained by low,high</returns>
        public static int constrain(int amt, int low, int high)
        {
            return (amt < low ? low : (amt > high ? high : amt));
        }

        public static long constrain(long amt, long low, long high)
        {
            return (amt < low ? low : (amt > high ? high : amt));
        }

        public static double constrain(double amt, double low, double high)
        {
            return (amt < low ? low : (amt > high ? high : amt));
        }

        public static double? constrain(double? amt, double? low, double? high)
        {
            return (amt < low ? low : (amt > high ? high : amt));
        }
    }
}
