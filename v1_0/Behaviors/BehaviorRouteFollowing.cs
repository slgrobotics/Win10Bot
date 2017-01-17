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
using System.Threading.Tasks;
using System.Diagnostics;

using slg.LibRuntime;
using slg.RobotBase.Bases;
using slg.RobotBase.Interfaces;
using slg.LibMapping;
using slg.LibSystem;
using Windows.Foundation;
using slg.LibRobotMath;

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

        public double closeEnoughMeters = 2.0d;                 // how close from the waypoint we decide we've passed it.
        public double waitOnTrackpointsSeconds = 3.0d;          // how long to wait on trackpoint
        public double waypointCantReachSeconds = 30.0d;         // abandon a trackpoint and go to next if this timeout expires
        public double turnFactor = 20.0d;                       // how aggressively we turn towards a trackpoint pose heading
        public double trackpointHeadingToleranceDegrees = 5.0d; // we stop the turn when heading is within this error
        public double waitOnTurnSeconds = 30.0d;                // max time allowed for turn

        private string savedTrackFileName = "MyTrack.xml";
        private Track missionTrack;
        private Trackpoint nextWp = null;
        private double powerFactor;
        private DateTime? stopStartedTime = null;
        private DateTime turnStartedTime;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="ddg"></param>
        /// <param name="speaker"></param>
        /// <param name="trackFileName">can be null, for a saved track</param>
        public BehaviorRouteFollowing(IDriveGeometry ddg, ISpeaker speaker, string trackFileName, double powerFactor=1.0d)
            : base(ddg)
        {
            this.speaker = speaker;
            this.powerFactor = powerFactor;

            BehaviorActivateCondition = bd =>
            {
                return nextWp != null;
            };

            BehaviorDeactivateCondition = bd =>
            {
                return nextWp == null;
            };

            if (String.IsNullOrWhiteSpace(trackFileName))
            {
                //speaker.Speak("Loading saved track");
                try
                {
                    missionTrack = null;

                    // Load stored waypoints:
                    // on the PC, from   C:\Users\sergei\AppData\Local\Packages\RobotPluckyPackage_sjh4qacv6p1wm\LocalState\MyTrack.xml
                    //            RPi:   \\172.16.1.175\c$\Data\Users\DefaultAccount\AppData\Local\Packages\RobotPluckyPackage_sjh4qacv6p1wm\LocalState
                    Track track = SerializableStorage<Track>.Load(savedTrackFileName).Result;

                    if (track != null)
                    {
                        missionTrack = track;
                        //speaker.Speak("Loaded file " + missionTrack.Count + " trackpoints");
                    }
                }
                catch (Exception ex)
                {
                    speaker.Speak("could not load saved track file");
                }

                if(missionTrack == null)
                {
                    speaker.Speak("failed to load saved track file");
                    missionTrack = new Track();
                }
                nextWp = missionTrack.nextTargetWp;
                stopStartedTime = null;
            }
            else
            {
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
                    speaker.Speak("could not load planned track file");
                }
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

                if (MustExit || MustTerminate)
                {
                    break;
                }

                // we have next trackpoint to go to, enter the work loop:

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

                    if (distToWp.Meters < closeEnoughMeters)
                    {
                        nextWp.trackpointState = TrackpointState.Passed;            // trackpoint will be ignored on the next cycle
                        //speaker.Speak("Waypoint " + nextWp.number + " reached");

                        if(nextWp.headingDegrees.HasValue)
                        {
                            // if the trackpoint was saved with a heading, we need to turn to that heading:
                            speaker.Speak("turning to " + Math.Round(nextWp.headingDegrees.Value));

                            SetGrabByMe();
                            turnStartedTime = DateTime.Now;

                            // TODO: we overshoot a lot here. Maybe having a PID controller would help.
                            while (Math.Abs(behaviorData.robotPose.direction.heading.Value - nextWp.headingDegrees.Value) > trackpointHeadingToleranceDegrees
                                    && (DateTime.Now - turnStartedTime).TotalSeconds < waitOnTurnSeconds)
                            {
                                double heading = behaviorData.robotPose.direction.heading.Value;    // robot's heading by SLAM
                                double desiredTurnDegrees = DirectionMath.to180(heading - nextWp.headingDegrees.Value);
                                setSpeedAndTurn(0.0d, -turnFactor * Math.Sign(desiredTurnDegrees));
                                yield return RobotTask.Continue;    // let the chain work
                            }

                            if(Math.Abs(behaviorData.robotPose.direction.heading.Value - nextWp.headingDegrees.Value) > trackpointHeadingToleranceDegrees)
                            {
                                speaker.Speak("Error: turning to " + Math.Round(nextWp.headingDegrees.Value) + " failed");
                            }

                            ClearGrab();
                        }

                        // pause and declare to other behaviors that we reached trackpoing:
                        if (stopStartedTime == null)
                        {
                            speaker.Speak("waiting");
                            stopStartedTime = DateTime.Now;
                            while ((DateTime.Now - stopStartedTime.Value).TotalSeconds < waitOnTrackpointsSeconds)
                            {
                                goalBearingRelativeDegrees = null;  // BehaviorGoToAngle will stop the robot
                                goalDistanceMeters = null;
                                getCoordinatorData().EnablingRequest = "trackpoint";    // let other behaviors know we are waiting on a trackpoint
                                yield return RobotTask.Continue;    // wait a bit on the trackpoint
                            }
                            stopStartedTime = null;
                        }

                        if (missionTrack.nextTargetWp != null)
                        {
                            speaker.Speak("proceeding");
                        }
                    }
                    else if (nextWp.estimatedTimeOfArrival.HasValue
                        && (DateTime.Now - nextWp.estimatedTimeOfArrival.Value).TotalSeconds > waypointCantReachSeconds)
                    {
                        nextWp.trackpointState = TrackpointState.CouldNotReach;     // will be ignored on the next cycle
                        speaker.Speak("Waypoint " + nextWp.number + " could not reach");
                    }
                    else
                    {
                        goalBearingRelativeDegrees = dirToWp.bearingRelative.Value;       // update robotState
                        goalDistanceMeters = distToWp.Meters;

                        Debug.WriteLine(string.Format("IP: Trackpoint {0}  abs bearing= {1}  distToWp.Meters= {2}", nextWp.number, dirToWp.bearing, distToWp.Meters));

                        if (!nextWp.estimatedTimeOfArrival.HasValue)
                        {
                            nextWp.trackpointState = TrackpointState.SelectedAsTarget;
                            double maxVelocityMSec = ToVelocity(behaviorData.robotState.powerLevelPercent * powerFactor);  // m/sec                    
                            double timeToReachSec = distToWp.Meters / maxVelocityMSec;
                            nextWp.estimatedTimeOfArrival = DateTime.Now.AddSeconds(timeToReachSec);
                            speaker.Speak("Next " + Math.Round(distToWp.Meters) + " " + Math.Round(timeToReachSec));
                            //speaker.Speak("Next trackpoint " + Math.Round(distToWp.Meters) + " meters away. I expect reaching it in " + Math.Round(timeToReachSec) + " seconds");
                        }
                    }

                    yield return RobotTask.Continue;
                }

                speaker.Speak("No more trackpoints");
                goalBearingRelativeDegrees = null;      // BehaviorGoToAngle will stop the robot
                goalDistanceMeters = null;
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
