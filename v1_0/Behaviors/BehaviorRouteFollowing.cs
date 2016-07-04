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
using slg.LibMapping;

namespace slg.Behaviors
{
    public class BehaviorRouteFollowing : BehaviorBase
    {
        /// <summary>
        /// Next trackpoint bearing, relative to the robot.
        /// when we set it here, BehaviorGoToAngle should pick the direction (GoalBearing) and execute it.
        /// </summary>
        public double? goalBearingRelativeDegrees
        {
            set
            {
                behaviorData.robotState.goalBearingRelativeDegrees = value;
                behaviorData.robotState.setGoalBearingByRelativeBearing(behaviorData.robotPose.direction.heading);
                //behaviorData.robotState.setGoalBearingByRelativeBearing(behaviorData.sensorsData.CompassHeadingDegrees);
            }
        }

        public double? goalDistanceMeters
        {
            set
            {
                behaviorData.robotState.goalDistanceMeters = value;
            }
        }

        private const double WAYPOINT_CANTREACH_SECONDS = 30;

        private Track missionTrack;

        private Trackpoint nextWp = null;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="ddg"></param>
        /// <param name="trackFileName"></param>
        /// <param name="maxVelocityPercent">0...100</param>
        public BehaviorRouteFollowing(IDriveGeometry ddg, ISpeaker speaker, string trackFileName)
            : base(ddg)
        {
            this.speaker = speaker;

            BehaviorActivateCondition = bd =>
            {
                return nextWp != null;
            };

            BehaviorDeactivateCondition = bd =>
            {
                return nextWp == null;
            };

            speaker.Speak("Loading file " + trackFileName);

            missionTrack = new Track();

            try
            {
                missionTrack.Init(trackFileName);

                speaker.Speak("Loaded file " + missionTrack.Count + " trackpoints");

                nextWp = missionTrack.nextTargetWp;
            }
            catch (Exception ex)
            {
                speaker.Speak("could not load track file");
            }
        }

        #region Behavior logic

            /// <summary>
            /// Computes goalBearingDegrees based on current location and the next trackpoint
            /// </summary>
            /// <returns></returns>
        public override IEnumerator<ISubsumptionTask> Execute()
        {
            FiredOn = true;

            while (!MustExit && !MustTerminate)
            {
                nextWp = missionTrack.nextTargetWp;

                while (!MustActivate && !MustExit && !MustTerminate)   // wait for activation 
                {
                    yield return RobotTask.Continue;    // dormant state - no need to calculate
                }

                // we have next trackpoint to go to:

                if (MustExit || MustTerminate)
                {
                    break;
                }

                //int i = 0;
                while (!MustDeactivate && !MustExit && !MustTerminate)
                {
                    nextWp = missionTrack.nextTargetWp;

                    if(nextWp == null)
                    {
                        break;
                    }

                    Direction dirToWp = nextWp.directionToWp(behaviorData.robotPose.geoPosition, behaviorData.robotPose.direction);
                    Distance distToWp = nextWp.distanceToWp(behaviorData.robotPose.geoPosition);

                    if (distToWp.Meters < 2.0d)
                    {
                        nextWp.trackpointState = TrackpointState.Passed;     // will be ignored on the next cycle
                        speaker.Speak("Waypoint " + nextWp.number + " passed");
                    }
                    else if (nextWp.estimatedTimeOfArrival.HasValue
                        && (DateTime.Now - nextWp.estimatedTimeOfArrival.Value).TotalSeconds > WAYPOINT_CANTREACH_SECONDS)
                    {
                        nextWp.trackpointState = TrackpointState.CouldNotReach;     // will be ignored on the next cycle
                        speaker.Speak("Waypoint " + nextWp.number + " could not reach");
                    }
                    else
                    {
                        goalBearingRelativeDegrees = dirToWp.bearingRelative.Value;       // update robotState
                        goalDistanceMeters = distToWp.Meters;

                        Debug.WriteLine(string.Format("IP: Trackpoint {0}  distToWp.Meters= {1}", nextWp.number, distToWp.Meters));

                        if (!nextWp.estimatedTimeOfArrival.HasValue)
                        {
                            nextWp.trackpointState = TrackpointState.SelectedAsTarget;
                            double maxVelocityMSec = ToVelocity(behaviorData.robotState.powerLevelPercent);  // m/sec                    
                            double timeToReachSec = distToWp.Meters / maxVelocityMSec;
                            nextWp.estimatedTimeOfArrival = DateTime.Now.AddSeconds(timeToReachSec);
                            speaker.Speak("Next trackpoint " + Math.Round(distToWp.Meters) + " away. I expect reaching it in " + Math.Round(timeToReachSec) + " seconds");
                        }
                    }

                    yield return RobotTask.Continue;
                }

                speaker.Speak("No more trackpoints");
            }

            FiredOn = false;

            Debug.WriteLine("BehaviorRouteFollowing: " + (MustExit ? "MustExit" : "completed"));

            yield break;
        }

        /// <summary>
        /// finish all operations nicely
        /// </summary>
        public override void Close()
        {
            Debug.WriteLine("BehaviorRouteFollowing(): Close()");
            base.Close();
        }

        #endregion // Behavior logic
    }
}
