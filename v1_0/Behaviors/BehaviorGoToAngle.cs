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
using slg.LibRobotMath;
using slg.RobotBase.Bases;
using slg.RobotBase.Interfaces;

namespace slg.Behaviors
{
    public class BehaviorGoToAngle : BehaviorBase
    {
        private double cruiseSpeed;     // -100...100
        private double turnFactor;      // how aggressively we turn towards goal

        /// <summary>
        /// goal bearing, will be constrained to 0...360 when set. As on the map, 0-North, 90-East, 180-South, 270-West
        /// </summary>
        public double? goalBearingDegrees
        {
            get
            {
                return behaviorData == null || behaviorData.robotState == null ?
                    null : behaviorData.robotState.goalBearingDegrees;
            }
            set { behaviorData.robotState.goalBearingDegrees = value; }
        }

        /// <summary>
        /// just go forward towards a compass bearing - non-grabbing, always FiredOn behavior
        /// </summary>
        /// <param name="ddg">IDriveGeometry</param>
        /// <param name="desiredCruiseSpeed">-100...100</param>
        /// <param name="desiredTurnFactor">how aggressively we turn towards goal</param>
        public BehaviorGoToAngle(IDriveGeometry ddg, double desiredCruiseSpeed = 20.0d, double desiredTurnFactor = 20.0d)
            : base(ddg)
        {
            cruiseSpeed = desiredCruiseSpeed;
            turnFactor = desiredTurnFactor;
        }

        #region Behavior logic

        /// <summary>
        /// Computes drive speed based on requestedSpeed, current DrivingState, timing and sensors input
        /// </summary>
        /// <returns></returns>
        public override IEnumerator<ISubsumptionTask> Execute()
        {
            FiredOn = true;

            while (!MustExit && !MustTerminate)
            {
                double? gbd = goalBearingDegrees;

                if (!gbd.HasValue || behaviorData.robotPose.direction == null || !behaviorData.robotPose.direction.heading.HasValue)
                {
                    // no goal set in RobotState. Stop till it appears. 
                    setSpeedAndTurn(0.0d, 0.0d);
                    goalBearingDegrees = null;      // set it in robot state
                }
                else if (!GrabByOther())
                {
                    //double heading = behaviorData.sensorsData.CompassHeadingDegrees;  // robot's heading by compass
                    double heading = behaviorData.robotPose.direction.heading.Value;    // robot's heading by SLAM

                    double desiredTurnDegrees = DirectionMath.to180(heading - (double)gbd);

                    //Debug.WriteLine("BehaviorGoToAngle: desiredTurnDegrees=" + desiredTurnDegrees);

                    behaviorData.robotState.goalBearingRelativeDegrees = desiredTurnDegrees;

                    if (Math.Abs(desiredTurnDegrees) > 30.0d)
                    {
                        // sharp turn:
                        setSpeedAndTurn(0.0d, -turnFactor * Math.Sign(desiredTurnDegrees));
                    }
                    else
                    {
                        // adjust trajectory towards goal:
                        setSpeedAndTurn(cruiseSpeed, -turnFactor * desiredTurnDegrees / 30.0d);
                    }
                }

                yield return RobotTask.Continue;
            }

            FiredOn = false;

            Debug.WriteLine("BehaviorGoToAngle: " + (MustExit ? "MustExit" : "completed"));

            yield break;
        }

        /// <summary>
        /// finish all operations nicely
        /// </summary>
        public override void Close()
        {
            Debug.WriteLine("BehaviorGoToAngle(): Close()");
            base.Close();
        }

        #endregion // Behavior logic
    }
}
