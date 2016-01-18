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
    public class BehaviorAvoidObstacles : BehaviorBase
    {
        public double avoidanceTresholdMeters = 0.6d;      // how far we see, at velocity 0.2 m/s
        public double avoidanceFactorOmega = 0.5d;         // how aggressively we turn away, rad/sec
        public double avoidanceFactorVelocity = 0.25d;     // how aggressively we slow down when turning
        public double tresholdIrAvoidanceMeters = 0.35d;   // front sensor is 10..80 cm

        private DateTime lostObstacleLast = DateTime.MinValue;
        private DateTime deactivatedLast = DateTime.MinValue;
        private double lastAvoidanceOmega = 0.0d;

        public BehaviorAvoidObstacles(IDriveGeometry ddg)
            : base(ddg)
        {
        }

        #region Behavior logic

        /// <summary>
        /// Computes drive speed based on requestedSpeed, current DrivingState, timing and sensors input
        /// </summary>
        /// <returns></returns>
        public override IEnumerator<ISubsumptionTask> Execute()
        {

            //DriveInputsBase correctedDriveSpeed = new DriveInputsBase();      // a stop command unless modified 

            //behaviorData.driveInputs = correctedDriveSpeed;

            while (!MustExit && !MustTerminate)
            {
                if (!GrabByOther())
                {
                    if (behaviorData.driveInputs != null)
                    {
                        double avoidanceOmega = 0.0d;                        
                        double velocity = behaviorData.driveInputs.velocity;    // behaviorData.driveInputs guaranteed not null

                        bool avoiding = computeAvoidanceOmega(ref velocity, ref avoidanceOmega);

                        //Debug.WriteLine("BehaviorAvoidObstacles: avoidanceOmega=" + avoidanceOmega);

                        if (avoiding)
                        {
                            FiredOn = true;
                            //setVelocityAndOmega(velocity, behaviorData.driveInputs.omega + avoidanceOmega);
                            setVelocityAndOmega(velocity, avoidanceOmega);
                        }
                        else
                        {
                            FiredOn = false;
                        }
                    }
                }

                yield return RobotTask.Continue;
            }

            FiredOn = false;

            Debug.WriteLine("BehaviorAvoidObstacles: " + (MustExit ? "MustExit" : "completed"));

            yield break;
        }

        private bool computeAvoidanceOmega(ref double velocity, ref double avoidanceOmega)
        {
            bool avoid = false;

            ISensorsData sensorsData = behaviorData.sensorsData;

            double adjustedAvoidanceTresholdMeters = Math.Max(Math.Min(Math.Abs(avoidanceTresholdMeters * velocity / 0.2d), 0.6d), 0.20d); // limit 0.2 ... 0.6 meters depending on velocity

            bool fronIrClose = sensorsData.IrFrontMeters <= tresholdIrAvoidanceMeters;
            bool leftSonarClose = sensorsData.SonarLeftMeters <= adjustedAvoidanceTresholdMeters;
            bool rightSonarClose = sensorsData.SonarRightMeters <= adjustedAvoidanceTresholdMeters;
            bool leftIrClose = sensorsData.IrLeftMeters <= adjustedAvoidanceTresholdMeters;
            bool rightIrClose = sensorsData.IrRightMeters <= adjustedAvoidanceTresholdMeters;

            if (velocity > 0.0d &&
                (fronIrClose || leftSonarClose || rightSonarClose || leftIrClose || rightIrClose))
            {
                bool leftSideFree = !leftSonarClose && !leftIrClose;
                bool rightSideFree = !rightSonarClose && !rightIrClose;

                if (!leftSideFree && !rightSideFree)
                {
                    avoidanceOmega = avoidanceFactorOmega * Math.Sign(sensorsData.SonarLeftMeters - sensorsData.SonarRightMeters);
                    velocity *= avoidanceFactorVelocity;
                    avoid = true;
                }
                else if (!leftSideFree)
                {
                    avoidanceOmega = -avoidanceFactorOmega;  // weer to the right
                    velocity *= avoidanceFactorVelocity;
                    avoid = true;
                }
                else if (!rightSideFree)
                {
                    avoidanceOmega = avoidanceFactorOmega;  // weer to the left
                    velocity *= avoidanceFactorVelocity;
                    avoid = true;
                }
                else if (fronIrClose)
                {
                    avoidanceOmega = avoidanceFactorOmega * Math.Sign(sensorsData.SonarLeftMeters - sensorsData.SonarRightMeters);
                    velocity *= -avoidanceFactorVelocity;
                    avoid = true;
                }
            }

            if (!avoid)
            {
                if (lostObstacleLast == DateTime.MinValue)
                {
                    lostObstacleLast = DateTime.Now;
                }

                double since = (DateTime.Now - lostObstacleLast).TotalSeconds;
                double adjustmentInterval = 0.5d;

                if (since < adjustmentInterval)
                {
                    // keep avoiding for short time after losing the obstacle:
                    double factor = (adjustmentInterval - since) / adjustmentInterval; // 1..0 during the adjustment interval
                    avoidanceOmega = lastAvoidanceOmega * factor;
                    velocity *= (avoidanceFactorVelocity + (1.0d - avoidanceFactorVelocity) * (1.0d - factor));  // avoidanceFactorVelocity..1 during the adjustment interval
                    avoid = true;
                }
            }
            else
            {
                lastAvoidanceOmega = avoidanceOmega;
                lostObstacleLast = DateTime.MinValue;
            }

            return avoid;
        }

        /// <summary>
        /// finish all operations nicely
        /// </summary>
        public override void Close()
        {
            Debug.WriteLine("BehaviorAvoidObstacles(): Close()");
            base.Close();
        }

        #endregion // Behavior logic
    }
}
