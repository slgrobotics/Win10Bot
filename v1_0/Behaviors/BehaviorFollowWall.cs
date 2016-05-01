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
    public class BehaviorFollowWall : BehaviorBase
    {
        public double cruiseSpeed = 10.0d;            // 0...100
        public double avoidanceTurnFactor = 20.0d;    // 0...90 "degrees" - adjust how aggressive the turns should be

        public double distanceToWallMeters = 0.25d;
        public double factorFollowWall = 1.0d;         // "P" parameter while normal wall following 
        public double factorConvexCornerTurn = 0.3d;   // "P" parameter when lost the wall and trying to catch it again (convex corner) 
        public double factorConcaveCornerTurn = 0.5d;  // sharp turn when in a concave corner (rad/sec)
        public double factorCornerTurnTrigger = 1.5d;  // triggers a sharp turn when in a concave corner, using front sonar (rad/sec)

        public bool fireOnLeft = false;
        public bool fireOnRight = false;

        private DateTime started;
        private DateTime lostTheWallLast = DateTime.MinValue;
        private DateTime deactivatedLast = DateTime.MinValue;

        public BehaviorFollowWall(IDriveGeometry ddg)
            : base(ddg)
        {
            BehaviorActivateCondition = bd =>
            {
                if ((DateTime.Now - deactivatedLast).TotalSeconds < 2.0d)
                {
                    return false;   // dead zone after deactivation for heading
                }

                //if (BehaviorBase.getCoordinatorData().EnablingRequest.StartsWith("FollowWall"))
                {
                    double irLeftMeters = bd.sensorsData.IrLeftMeters;
                    double sonarLeftMeters = behaviorData.sensorsData.RangerFrontLeftMeters;
                    double irRightMeters = bd.sensorsData.IrRightMeters;
                    double sonarRightMeters = behaviorData.sensorsData.RangerFrontRightMeters;

                    double activateDistanceToWallMeters = distanceToWallMeters * 0.9d;

                    if (irLeftMeters < activateDistanceToWallMeters || (irLeftMeters < distanceToWallMeters && sonarLeftMeters < distanceToWallMeters))
                    {
                        fireOnLeft = true;
                    }

                    if (irRightMeters < activateDistanceToWallMeters || (irRightMeters < distanceToWallMeters && sonarRightMeters < distanceToWallMeters))
                    {
                        fireOnRight = true;
                    }

                    return fireOnLeft || fireOnRight;
                }
                //return false;
            };

            BehaviorDeactivateCondition = bd =>
            {
                //return (DateTime.Now - started).TotalSeconds > 6.0d;

                double? goalBearing = bd.robotState.goalBearingDegrees;
                if (goalBearing.HasValue)
                {
                    double heading = bd.sensorsData.CompassHeadingDegrees;
                    if (Math.Abs(heading - goalBearing.Value) < 10.0d)
                    {
                        deactivatedLast = DateTime.Now;
                        return true;
                    }
                }

                deactivatedLast = DateTime.MinValue;
                double irLeftMeters = bd.sensorsData.IrLeftMeters;
                double irRightMeters = bd.sensorsData.IrRightMeters;

                double deactivateDistanceToWallMeters = distanceToWallMeters * 2.0d;

                bool noWall = lostTheWall(irLeftMeters, irRightMeters, deactivateDistanceToWallMeters);

                // "lost the wall" condition:
                if (noWall && lostTheWallLast == DateTime.MinValue)
                {
                    // just lost it, set timer:
                    lostTheWallLast = DateTime.Now;
                }
                else if(!noWall)
                {
                    // acquired the wall, reset timer interval:
                    lostTheWallLast = DateTime.MinValue;
                }

                return noWall && (DateTime.Now - lostTheWallLast).TotalSeconds > 2.0d;  // no wall for some time
            };
        }

        /// <summary>
        /// "lost the wall" condition
        /// </summary>
        /// <param name="irLeftMeters"></param>
        /// <param name="irRightMeters"></param>
        /// <param name="lostTheWallTreshold"></param>
        /// <returns></returns>
        private bool lostTheWall(double irLeftMeters, double irRightMeters, double lostTheWallTreshold)
        {
            return fireOnLeft && irLeftMeters >= lostTheWallTreshold || fireOnRight && irRightMeters >= lostTheWallTreshold;
        }

        #region Behavior logic

        /// <summary>
        /// Computes drive speed based on requestedSpeed, current DrivingState, timing and sensors input
        /// </summary>
        /// <returns></returns>
        public override IEnumerator<ISubsumptionTask> Execute()
        {
            FiredOn = false;

            while (!MustExit && !MustTerminate)
            {
                while (GrabByOther())
                {
                    yield return RobotTask.Continue;    // chain grabbed by somebody else
                }

                while (!MustActivate && !MustExit && !MustTerminate)   // wait for activation 
                {
                    FiredOn = false;
                    fireOnLeft = false;
                    fireOnRight = false;
                    yield return RobotTask.Continue;    // dormant sate - no need to wall following
                }

                if (MustExit || MustTerminate)
                    continue;

                // Activated - grab control of the tasks stack:
                SetGrabByMe();
                FiredOn = true;
                started = DateTime.Now;
                lostTheWallLast = DateTime.MinValue;
                string savedEnablingRequest = getCoordinatorData().EnablingRequest;
                getCoordinatorData().EnablingRequest = string.Empty;

                //Debug.WriteLine("+++++++++++++++++++++" + (fireOnLeft ? "Left" : "Right") + " Wall Following activated");
                //speaker.Speak((fireOnLeft ? "Left" : "Right") + " Wall Following");

                // keep following the wall till we are told to deactivate:
                while (!MustDeactivate && !MustExit && !MustTerminate)
                {
                    // compute turn based on how close the wall is:
                    double avoidanceOmega = 0.0d;
                    bool wallAhead = false;

                    double forwardSensorMeters = Math.Min(Math.Min(behaviorData.sensorsData.RangerFrontLeftMeters, behaviorData.sensorsData.RangerFrontRightMeters), behaviorData.sensorsData.IrFrontMeters);

                    if (fireOnLeft)
                    {
                        double irLeftMeters = behaviorData.sensorsData.IrLeftMeters;
                        double wff = wallFieldFactor(irLeftMeters, forwardSensorMeters, out wallAhead);

                        avoidanceOmega = ToOmega(-avoidanceTurnFactor * wff); // weer to the right
                    }

                    if (fireOnRight)
                    {
                        double irRightMeters = behaviorData.sensorsData.IrRightMeters;
                        double wff = wallFieldFactor(irRightMeters, forwardSensorMeters, out wallAhead);

                        avoidanceOmega = ToOmega(avoidanceTurnFactor * wff); // weer to the left
                    }

                    requestedVelocity = ToVelocity(wallAhead ? 0.0d : cruiseSpeed);
                    requestedOmega = avoidanceOmega;

                    Debug.WriteLine("BehaviorFollowWall: requestedVelocity=" + requestedVelocity + "    avoidanceOmega=" + avoidanceOmega + "  X=" + behaviorData.robotPose.X + "  Y=" + behaviorData.robotPose.Y + "    seconds:" + (DateTime.Now - FiredOnTimestamp).TotalSeconds);

                    setVelocityAndOmega(requestedVelocity, requestedOmega);

                    yield return RobotTask.Continue;
                }

                speaker.Speak("Deactivated Follow Wall");

                Debug.WriteLine("BehaviorFollowWall: deactivated: " + getCoordinatorData().EnablingRequest + " ------------------------------------");

                // in case the enablingRequest is stuck, clear it:
                if (getCoordinatorData().EnablingRequest == savedEnablingRequest)
                {
                    getCoordinatorData().ClearEnablingRequest();
                }

                FiredOn = false;
                fireOnLeft = false;
                fireOnRight = false;

                ClearGrabIfMine();
            }

            FiredOn = false;

            Debug.WriteLine("BehaviorFollowWall: " + (MustExit ? "MustExit" : "completed"));

            yield break;
        }

        private double wallFieldFactor(double sideSensorMeters, double forwardSensorMeters, out bool wallAhead)
        {
            double wff;
            wallAhead = false;

            // it turns out that front sonars, although at 30 degrees, cannot see the wall when it is parallel.

            if (forwardSensorMeters < distanceToWallMeters * factorCornerTurnTrigger)
            {
                // we've driven to a corner, need a sharp turn:
                wff = -factorConcaveCornerTurn;
                wallAhead = true;
            }
            else if (sideSensorMeters > distanceToWallMeters * 1.8d)
            {
                // possibly a convex corner, try to catch the wall
                wff = factorConvexCornerTurn;
            }
            else
            {
                // following the wall
                wff = (sideSensorMeters - distanceToWallMeters) * factorFollowWall;
            }

            return wff;
        }

        /// <summary>
        /// finish all operations nicely
        /// </summary>
        public override void Close()
        {
            Debug.WriteLine("BehaviorFollowWall(): Close()");
            base.Close();
        }

        #endregion // Behavior logic
    }
}
