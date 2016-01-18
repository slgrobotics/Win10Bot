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


namespace slg.RobotBase.Interfaces
{
    /// <summary>
    /// Describes Unicycle model of a robot - velocity and turning rate defines its movement
    /// </summary>
    public interface IDriveInputs
    {
        #region Unicycle model properties

        /// <summary>
        /// linear velocity, meters per second, positive - forward
        /// </summary>
        double velocity { get; set; }

        /// <summary>
        /// angular velocity, radians per second, positive - left
        /// </summary>
        double omega { get; set; }

        #endregion // Unicycle model properties

        /// <summary>
        /// Computes physical drive parameters based on velocity and omega,
        /// using Unicycle to specific Drive formula and adjusting for maximum speed
        /// </summary>
        /// <param name="geometry">contains wheel base and radius or similar drive characteristics</param>
        void Compute(IDriveGeometry g);
    }
}
