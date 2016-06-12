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


using slg.LibMapping;

namespace slg.RobotBase.Interfaces
{
    /// <summary>
    /// describes location and orientation in 2D plane and defines basic operations related to these entities
    /// </summary>
    public interface IPose
    {
        /// <summary>
        /// meters
        /// </summary>
        double XMeters { get; set; }

        /// <summary>
        /// meters
        /// </summary>
        double YMeters { get; set; }

        /// <summary>
        /// Radians, positive towards left turn
        /// </summary>
        double ThetaRadians { get; set; }

        void translate(double dXMeters, double dYMeters);

        void translate(Distance dist, IDirection dir);

        void rotate(double alphaRad);

        void resetXY();

        void resetRotation();
    }
}
