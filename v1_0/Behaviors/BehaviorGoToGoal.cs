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
using slg.Mapping;

namespace slg.Behaviors
{
    public class BehaviorGoToGoal : BehaviorBase
    {
        /// <summary>
        /// goal bearing, will be constrained to 0...360 when set. As on the map, 0-North, 90-East, 180-South, 270-West
        /// </summary>
        public double? goalBearingDegrees
        {
            set { behaviorData.robotState.goalBearingDegrees = value; }
        }

        public GeoPosition goalGeoPosition
        {
            get { return behaviorData == null || behaviorData.robotState == null ? null : behaviorData.robotState.goalGeoPosition; }
        }

        public IDirection directionToGoal { get { return goalGeoPosition == null ? null : new Direction() { heading = behaviorData.robotPose.direction.heading, bearing = behaviorData.robotPose.geoPosition.bearingTo(goalGeoPosition) }; } }

        public IDistance distanceToGoal { get { return goalGeoPosition == null ? null : behaviorData.robotPose.geoPosition.distanceFrom(goalGeoPosition); } }

        public BehaviorGoToGoal(IDriveGeometry ddg)
            : base(ddg)
        {
            BehaviorActivateCondition = bd =>
            {
                return distanceToGoal != null && directionToGoal != null;
            };

            BehaviorDeactivateCondition = bd =>
            {
                return distanceToGoal == null || directionToGoal == null || distanceToGoal.Meters < 0.2d;
            };
        }

        #region Behavior logic

        /// <summary>
        /// Computes goalBearingDegrees and sets it to null when goal is reached
        /// </summary>
        /// <returns></returns>
        public override IEnumerator<ISubsumptionTask> Execute()
        {
            FiredOn = true;

            while (!MustExit && !MustTerminate)
            {
                while (!MustActivate && !MustExit && !MustTerminate)   // wait for activation 
                {
                    goalBearingDegrees = null;
                    yield return RobotTask.Continue;    // dormant sate - no need to calculate
                }

                speaker.Speak("Go to goal " + Math.Round(distanceToGoal.Meters, 1) + " meters");

                while (!MustDeactivate && !MustExit && !MustTerminate)
                {
                    double bearing = directionToGoal.bearing.Value;
                    goalBearingDegrees = bearing;
                    Debug.WriteLine("BehaviorGoToGoal: goalBearingDegrees=" + Math.Round(bearing, 1) + "    distanceToGoal=" + Math.Round(distanceToGoal.Meters, 1));

                    yield return RobotTask.Continue;
                }

                speaker.Speak("Reached goal");
            }

            FiredOn = false;

            Debug.WriteLine("BehaviorGoToGoal: " + (MustExit ? "MustExit" : "completed"));

            yield break;
        }

        /// <summary>
        /// finish all operations nicely
        /// </summary>
        public override void Close()
        {
            Debug.WriteLine("BehaviorGoToGoal(): Close()");
            base.Close();
        }

        #endregion // Behavior logic
    }
}
