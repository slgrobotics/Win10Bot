﻿/*
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


using slg.LibMapping;

namespace slg.RobotBase.Interfaces
{
    /// <summary>
    /// describes robot location and orientation and defines basic operations related to these entities
    /// </summary>
    public interface IRobotPose : IPose
    {
        IGeoPosition geoPosition { get; set; }

        IDirection direction { get; set; }

        /// <summary>
        /// meters
        /// </summary>
        double H { get; set; }

        void moveTo(double lng, double lat, double elevMeters);
    }
}
