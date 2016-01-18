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
using System.Threading;
using System.Diagnostics;

using slg.RobotBase.Bases;
using slg.RobotBase.Interfaces;
using slg.LibRuntime;

namespace slg.Behaviors
{
    public class BehaviorAmIStuck : BehaviorBase
    {

        public double stuckIntervalSeconds = 2.0d;

        /// <summary>
        /// Looks at encoders and expected them to change when wheels power is not 0.
        /// will issue EnablingRequest within the "Escape" subset
        /// </summary>
        /// <returns></returns>
        public override IEnumerator<ISubsumptionTask> Execute()
        {
            last = DateTime.Now;
            LeftTicksLast = behaviorData.sensorsData.WheelEncoderLeftTicks;
            RightTicksLast = behaviorData.sensorsData.WheelEncoderRightTicks;

            string escapeRecommendation;

            while (!MustExit && !MustTerminate)
            {

                if (behaviorData.driveInputs != null && amIStuck(out escapeRecommendation))
                {
                    //speaker.Speak("I am stuck - " + Helpers.CamelCaseToSpokenString(escapeRecommendation));
                    speaker.Speak("I am stuck");
                    ClearGrab();
                    getCoordinatorData().EnablingRequest = escapeRecommendation; //  "Escape..." - hopefully appropriate behavior will be triggered
                }

                yield return RobotTask.Continue;
            }

            Debug.WriteLine("BehaviorAmIStuck: " + (MustExit ? "MustExit" : "completed"));

            yield break;
        }

        private long LeftTicksLast = 0L;
        private long RightTicksLast = 0L;
        private DateTime last = DateTime.MinValue;

        private bool amIStuck(out string escapeRecommendation)
        {
            double commandedVelocity = behaviorData.driveInputs.velocity;    // behaviorData.driveInputs guaranteed not null
            double commandedOmega = behaviorData.driveInputs.omega;

            escapeRecommendation = commandedVelocity > 0.0d ? "Escape" : "EscapeForward";

            ISensorsData sensorsData = behaviorData.sensorsData;

            if (Math.Abs(commandedVelocity) < 0.001 && Math.Abs(commandedOmega) < 0.001)
            {
                // no movement expected, keep resetting the measurement start:
                last = DateTime.Now;
                LeftTicksLast = sensorsData.WheelEncoderLeftTicks;
                RightTicksLast = sensorsData.WheelEncoderRightTicks;

                return false;
            }

            bool ret = false;

            if ((DateTime.Now - last).TotalSeconds >= stuckIntervalSeconds)
            {
                if (sensorsData.WheelEncoderLeftTicks == LeftTicksLast && sensorsData.WheelEncoderRightTicks == RightTicksLast)
                {
                    ret = true;
                }

                last = DateTime.Now;
                LeftTicksLast = sensorsData.WheelEncoderLeftTicks;
                RightTicksLast = sensorsData.WheelEncoderRightTicks;
            }

            return ret;
        }
    }
}
