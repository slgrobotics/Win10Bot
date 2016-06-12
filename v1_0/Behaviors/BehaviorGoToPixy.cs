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

using slg.LibRuntime;
using slg.RobotBase.Bases;
using slg.RobotBase.Interfaces;

namespace slg.Behaviors
{
    public class BehaviorGoToPixy : BehaviorBase
    {
        /// <summary>
        /// Pixy Camera goal bearing, relative to the robot.
        /// when we set it here, BehaviorGoToAngle should pick the direction (GoalBearing) and execute it.
        /// </summary>
        public double? goalBearingRelativeDegrees
        {
            set { 
                behaviorData.robotState.goalBearingRelativeDegrees = value;
                behaviorData.robotState.setGoalBearingByRelativeBearing(behaviorData.robotPose.direction.heading);
                //behaviorData.robotState.setGoalBearingByRelativeBearing(behaviorData.sensorsData.CompassHeadingDegrees);
            }
        }

        private double? pbd = null; // saved pixyBearingDegrees

        public double? pixyBearingDegrees
        {
            get
            {
                if (behaviorData.sensorsData.IsTargetingCameraDataValid())
                {
                    // Note: we need to lessen (constrain) the effect object displacement from center has on desired turn.
                    //       without it oscillations occur.
                    double? bearingPixy =
                        //GeneralMath.constrain(behaviorData.sensorsData.TargetingCameraBearingDegrees / 5.0d, -10.0d, 10.0d)
                        //behaviorData.sensorsData.TargetingCameraBearingDegrees / 5.0d;
                        behaviorData.sensorsData.TargetingCameraBearingDegrees;

                    //Debug.WriteLine("bearingPixy=" + bearingPixy);

                    return bearingPixy;
                }
                return null;
            }
        }

        //public IDirection directionToGoal { get { return goalGeoPosition == null ? null : new Direction() { heading = behaviorData.robotPose.direction.heading, bearing = behaviorData.robotPose.geoPosition.bearingTo(goalGeoPosition) }; } }

        public BehaviorGoToPixy(IDriveGeometry ddg)
            : base(ddg)
        {
            BehaviorActivateCondition = bd =>
            {
                pbd = pixyBearingDegrees;   // save it for the loop
                return pbd  != null;        // have colored object in Pixy Camera view
            };

            BehaviorDeactivateCondition = bd =>
            {
                return !behaviorData.sensorsData.IsTargetingCameraDataValid();  // no colored objects in Pixy Camera view
            };
        }

        #region Behavior logic

        /// <summary>
        /// Computes goalBearingDegrees if camera reports a goal and sets it to null when goal is not reported
        /// </summary>
        /// <returns></returns>
        public override IEnumerator<ISubsumptionTask> Execute()
        {
            FiredOn = true;

            while (!MustExit && !MustTerminate)
            {
                while (!MustActivate && !MustExit && !MustTerminate)   // wait for activation 
                {
                    yield return RobotTask.Continue;    // dormant state - no need to calculate
                }

                // we have pixyBearingDegrees saved in pbd

                if (MustExit || MustTerminate)
                {
                    break;
                }

                if(pixyBearingDegrees.HasValue)
                {
                    speaker.Speak("Go " + Math.Round(pbd.Value, 1));
                }
                //speaker.Speak("Go to Pixy " + Math.Round(pbd.Value, 1));
                //Debug.WriteLine("BehaviorGoToPixy: Go to relative bearing " + Math.Round(pbd.Value, 1));

                //int i = 0;
                while (!MustDeactivate && !MustExit && !MustTerminate)
                {
                    double bearing = pixyBearingDegrees.Value;
                    goalBearingRelativeDegrees = bearing;       // update robotState

                    //Debug.WriteLine("BehaviorGoToPixy: goalBearingDegrees=" + Math.Round(bearing, 1));
                    //speaker.Speak("" + Math.Round(bearing, 1));

                    yield return RobotTask.Continue;
                }

                //speaker.Speak("Lost colored object");
                speaker.Speak("Lost");
                //Debug.WriteLine("BehaviorGoToPixy: Lost colored object");
            }

            FiredOn = false;

            Debug.WriteLine("BehaviorGoToPixy: " + (MustExit ? "MustExit" : "completed"));

            yield break;
        }

        /// <summary>
        /// finish all operations nicely
        /// </summary>
        public override void Close()
        {
            Debug.WriteLine("BehaviorGoToPixy(): Close()");
            base.Close();
        }

        #endregion // Behavior logic
    }
}
