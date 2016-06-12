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

//using System.Configuration;

namespace slg.LibMapping
{
    public static class MapperSettings
    {
        public static readonly int nW = 39;
        public static readonly int nH = 39;
        public static readonly double elementSizeMeters = 0.10d;
        public static readonly double robotWidthMeters  = 0.35d;
        public static readonly double robotLengthMeters = 0.26d;
        //public static readonly double referenceCircleRadiusMeters = 1.0d;

        public const int UNITS_DISTANCE_DEFAULT = 1;    // Distance.UNITS_DISTANCE_M;

        public static int unitsDistance = UNITS_DISTANCE_DEFAULT;
        public static int coordStyle = 1;				// "N37°28.893'  W117°43.368'"

        static MapperSettings()
        {
            // Warning: in Designer the ConfigurationManager will not read App.config properly.
            // make sure the defaults are reasonable and absence of App.config does not cause undefined values.

            //int tmpSz;

            //if (int.TryParse(ConfigurationManager.AppSettings["MapperVicinityMapSize"], out tmpSz))
            //{
            //    nH = nW = tmpSz;

            //    double.TryParse(ConfigurationManager.AppSettings["MapperVicinityMapElementSizeMeters"], out elementSizeMeters);
            //    double.TryParse(ConfigurationManager.AppSettings["RobotWidthMeters"], out robotWidthMeters);
            //    double.TryParse(ConfigurationManager.AppSettings["RobotLengthMeters"], out robotLengthMeters);
            //    //double.TryParse(ConfigurationManager.AppSettings["ReferenceCircleRadiusMeters"], out referenceCircleRadiusMeters);
            //}

            //nH = nW - 4;        // for debugging make it non-square
        }
    }
}
