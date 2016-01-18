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
    public interface IDriveGeometry
    {
        // factors allow to convert abstract speed and turn values in the range -100...100 to physical values 

        /// <summary>
        /// factor to convert abstract speed value in the range -100...100 to physical velocity meters per second
        /// for example, speed -100..+100, velocity -0.83..+0.83 meters per second would give factor 0.0083
        /// speedToVelocityFactor must be a positive value.
        /// </summary>
        double speedToVelocityFactor { get; set; }

        /// <summary>
        /// factor to convert abstract turn value in the range -100...100 to physical omega radians per second
        /// for example, turn -100..+100,  omega 4.6..-4.6 radians per second would give factor 0.046
        /// note that turn is positive to the right, and omega is positive to the left, that will be accounted for in formulas.
        /// turnToOmegaFactor must be a positive value.
        /// </summary>
        double turnToOmegaFactor { get; set; }
    }
}
