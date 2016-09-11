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
using System.Threading.Tasks;

using slg.Behaviors;
using slg.RobotBase.Interfaces;

namespace slg.RobotPluckyImpl.Behaviors
{
    public class BehaviorStopPlucky : BehaviorStop
    {
        /// <summary>
        /// determines if an obstacle is too close and comes up with an escape recommendation
        /// </summary>
        /// <param name="behaviorData"></param>
        /// <param name="escapeRecommendation">output</param>
        /// <returns></returns>
        protected override bool TooClose(IBehaviorData behaviorData)
        {
            escapeRecommendation = string.Empty;

            ISensorsData sensorsData = behaviorData.sensorsData;
            double velocity = behaviorData.driveInputs.velocity;    // behaviorData.driveInputs guaranteed not null

            double adjustedTresholdStopMeters = Math.Max(Math.Min(Math.Abs(tresholdStopMeters * velocity / 0.2d), 0.4d), 0.15d); // limit 0.15 ... 0.4 meters depending on velocity

            if (velocity > 0.0d && (sensorsData.IrFrontMeters <= tresholdIrStopMeters || sensorsData.RangerFrontLeftMeters <= adjustedTresholdStopMeters || sensorsData.RangerFrontRightMeters <= adjustedTresholdStopMeters))
            {
                bool leftSideFree = sensorsData.RangerFrontLeftMeters > adjustedTresholdStopMeters && sensorsData.IrLeftMeters > adjustedTresholdStopMeters;      // IR left and right - 10..80 cm
                bool rightSideFree = sensorsData.RangerFrontRightMeters > adjustedTresholdStopMeters && sensorsData.IrRightMeters > adjustedTresholdStopMeters;

                if (!leftSideFree)
                {
                    escapeRecommendation = "EscapeRight";
                }
                else if (!rightSideFree)
                {
                    escapeRecommendation = "EscapeLeft";
                }
                else
                {
                    escapeRecommendation = "Escape";    // advise random turn
                }

                return true;    // activate stop, with escape recommendation
            }

            if (velocity < 0.0d && (sensorsData.IrRearMeters < adjustedTresholdStopMeters))
            {
                escapeRecommendation = "EscapeForward";
                return true;    // activate stop, with escape recommendation
            }

            //if (velocity == 0.0d && behaviorData.driveInputs.omega == 0.0d)
            //{
            //    bool leftSideFree = sensorsData.IrLeftMeters > adjustedTresholdStopMeters;
            //    bool rightSideFree = sensorsData.IrRightMeters > adjustedTresholdStopMeters;
            //    bool rearFree = sensorsData.IrRearMeters > adjustedTresholdStopMeters;

            //    if (leftSideFree)
            //    {
            //        escapeRecommendation = "EscapeLeftTurn";
            //    }
            //    else if (rightSideFree)
            //    {
            //        escapeRecommendation = "EscapeRightTurn";
            //    }
            //    else if (rearFree)
            //    {
            //        escapeRecommendation = "EscapeFullTurn";
            //    }
            //    else
            //    {
            //        escapeRecommendation = "EscapeNone";
            //    }

            //    return true;    // activate stop, with escape recommendation
            //}

            return false;    // deactivate, no stopping and no escape recommendation
        }
    }
}
