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
using System.Diagnostics;

using slg.RobotBase.Bases;
using slg.RobotBase.Interfaces;
using slg.LibRuntime;
using slg.Behaviors;
using slg.RobotShortyImpl.Behaviors;

namespace slg.RobotShortyImpl
{
    public enum BehaviorCompositionType
    {
        None,
        JoystickAndStop,
        CruiseAndStop,
        AroundTheBlock,
        ChaseColorBlob,
        RouteFollowing,
        Escape
    }

    /// <summary>
    /// BehaviorFactory composes behaviors and loads them to subsumption dispatcher for execution
    /// </summary>
    public class BehaviorFactory
    {
        // reference to runtime:
        private SubsumptionTaskDispatcher subsumptionDispatcher;

        // must be injected in some behaviors:
        private IDriveGeometry driveGeometry;

        private ISpeaker speaker;

        // related to "Route Following""
        // On the PC: @"C:\Users\sergei\AppData\Local\Packages\RobotPluckyPackage_sjh4qacv6p1wm\LocalState\MyTrack.xml";
        // this is how to mount Raspberry Pi SD card: \\172.16.1.175\c$
        // full path: \\172.16.1.175\c$\Data\Users\DefaultAccount\AppData\Local\Packages\RobotPluckyPackage_sjh4qacv6p1wm\LocalState\MyTrack.xml
        // full path: \\172.16.1.175\c$\Data\Users\DefaultAccount\AppxLayouts\RobotPluckyPackageVS.Debug_ARM.sergei\ParkingLot1.waypoints
        // do not supply path, just the file name:
        public string TrackFileName { private get; set; }

        // related to "Around the block":
        private DateTime activatedFW;
        private double initialCompassHeadingDegrees;
        private bool isFinishedFW;

        public BehaviorFactory(SubsumptionTaskDispatcher disp, IDriveGeometry driveGeom, ISpeaker speaker)
        {
            this.subsumptionDispatcher = disp;
            this.driveGeometry = driveGeom;
            this.speaker = speaker;
        }

        /// <summary>
        /// compose behaviors and load them to dispatcher for execution
        /// </summary>
        /// <param name="compType"></param>
        public void produce(BehaviorCompositionType compType)
        {
            Debug.WriteLine("BehaviorFactory: produce() : " + compType);

            // close all running tasks before composing new behavior set:
            subsumptionDispatcher.Close();
            while (subsumptionDispatcher.ActiveTasksCount > 0)
            {
                subsumptionDispatcher.Process();
            }

            Debug.WriteLine("BehaviorFactory: produce() : all dispatcher tasks closed, creating new combo '" + compType + "'");

            isFinishedFW = false;

            switch (compType)
            {
                case BehaviorCompositionType.CruiseAndStop:

                    //dispatcher.Dispatch(new BehaviorCruise(driveGeometry) {
                    //    name = "BehaviorCruise",
                    //    speaker = this.speaker
                    //});

                    //dispatcher.Dispatch(new BehaviorGoToGoal(driveGeometry)
                    //{
                    //    name = "BehaviorGoToGoal",
                    //    speaker = this.speaker
                    //});

                    //dispatcher.Dispatch(new BehaviorGoToPixy(driveGeometry)
                    //{
                    //    name = "BehaviorGoToPixy",
                    //    speaker = this.speaker
                    //});

                    subsumptionDispatcher.Dispatch(new BehaviorGoToAngle(driveGeometry, 20.0d, 20.0d)
                    {
                        name = "BehaviorGoToAngle",
                        speaker = this.speaker
                    });

                    subsumptionDispatcher.Dispatch(new BehaviorAvoidObstacles(driveGeometry)
                    {
                        name = "BehaviorAvoidObstacles",
                        speaker = this.speaker
                    });

                    subsumptionDispatcher.Dispatch(new BehaviorFollowWall(driveGeometry)
                    {
                        name = "BehaviorFollowWall",
                        speaker = this.speaker,
                        cruiseSpeed = 20.0d,
                        avoidanceTurnFactor = 50.0d,
                        //distanceToWallMeters = 0.17d,
                        //distanceToWallMeters = 0.25d,
                        //BehaviorDeactivateCondition = bd => { return BehaviorBase.getCoordinatorData().EnablingRequest.StartsWith("Escape"); },
                        //BehaviorTerminateCondition = bd => { return bd.sensorsData.IrRearMeters < 0.2d; }
                    });

                    subsumptionDispatcher.Dispatch(new BehaviorStopShorty()
                    {
                        name = "BehaviorStop",
                        speaker = this.speaker,
                        tresholdStopMeters = 0.2d
                    });

                    subsumptionDispatcher.Dispatch(new BehaviorBackAndTurn(driveGeometry)
                    {
                        name = "Escape",
                        speaker = this.speaker,
                        BehaviorActivateCondition = bd => { return BehaviorBase.getCoordinatorData().EnablingRequest.StartsWith("Escape"); },
                        BehaviorDeactivateCondition = bd => { return true; },  // deactivate after one cycle
                        BehaviorTerminateCondition = bd => { return false; }   // do not terminate
                    });

                    subsumptionDispatcher.Dispatch(new BehaviorAmIStuck() {
                        name = "BehaviorAmIStuck",
                        speaker = this.speaker
                    });

                    BehaviorBase.getCoordinatorData().ClearEnablingRequest();
                    BehaviorBase.getCoordinatorData().ClearGrabbingBehavior();
                    break;

                case BehaviorCompositionType.AroundTheBlock:

                    BehaviorFollowWall bfw = new BehaviorFollowWall(driveGeometry)
                    {
                        name = "BehaviorFollowWall",
                        speaker = this.speaker,
                        cruiseSpeed = 20.0d,
                        avoidanceTurnFactor = 50.0d,
                        distanceToWallMeters = 0.17d,
                        //BehaviorDeactivateCondition = bd => { return BehaviorBase.getCoordinatorData().EnablingRequest.StartsWith("Escape"); },
                        //BehaviorTerminateCondition = bd => { return bd.sensorsData.IrRearMeters < 0.2d; }
                    };

                    bfw.BehaviorDeactivateCondition = bd =>
                    {
                        isFinishedFW =
                            //(DateTime.Now - bfw.FiredOnTimestamp).TotalSeconds > 5.0d
                            (DateTime.Now - activatedFW).TotalSeconds > 5.0d
                            //&& Math.Abs(DirectionMath.to180(bd.sensorsData.CompassHeadingDegrees - initialCompassHeadingDegrees)) < 5.0d;

                            && Math.Abs(bd.robotPose.XMeters) < 0.08d   // forward
                            && Math.Abs(bd.robotPose.YMeters) < 0.25d;  // sides

                        return isFinishedFW;
                    };

                    bfw.BehaviorActivateCondition = bd => {

                        if (isFinishedFW) return false;

                        double irLeftMeters = bd.sensorsData.IrLeftMeters;
                        double sonarLeftMeters = bd.sensorsData.RangerFrontLeftMeters;
                        double irRightMeters = bd.sensorsData.IrRightMeters;
                        double sonarRightMeters = bd.sensorsData.RangerFrontRightMeters;

                        double activateDistanceToWallMeters = bfw.distanceToWallMeters * 1.5d;

                        if (irLeftMeters < activateDistanceToWallMeters || (irLeftMeters < bfw.distanceToWallMeters && sonarLeftMeters < bfw.distanceToWallMeters))
                        {
                            bfw.fireOnLeft = true;
                        }

                        if (irRightMeters < activateDistanceToWallMeters || (irRightMeters < bfw.distanceToWallMeters && sonarRightMeters < bfw.distanceToWallMeters))
                        {
                            bfw.fireOnRight = true;
                        }

                        if ((bfw.fireOnLeft || bfw.fireOnRight) && bd.sensorsData.CompassHeadingDegrees.HasValue)
                        {
                            // remember the initial CompassHeadingDegrees:
                            initialCompassHeadingDegrees = bd.sensorsData.CompassHeadingDegrees.Value;
                            activatedFW = DateTime.Now;
                        }

                        return bfw.fireOnLeft || bfw.fireOnRight;
                    };
                    subsumptionDispatcher.Dispatch(bfw);

                    subsumptionDispatcher.Dispatch(new BehaviorStopShorty()
                    {
                        name = "BehaviorStop",
                        speaker = this.speaker,
                        BehaviorActivateCondition = bd => { return true; }
                    });

                    break;

                case BehaviorCompositionType.ChaseColorBlob:

                    subsumptionDispatcher.Dispatch(new BehaviorGoToPixy(driveGeometry) {
                        name = "BehaviorGoToPixy",
                        speaker = this.speaker
                    });

                    subsumptionDispatcher.Dispatch(new BehaviorGoToAngle(driveGeometry, 100.0d, 100.0d)
                    {
                        name = "BehaviorGoToAngle",
                        speaker = this.speaker
                    });

                    subsumptionDispatcher.Dispatch(new BehaviorStopShorty()
                    {
                        name = "BehaviorStop",
                        speaker = this.speaker,
                        tresholdStopMeters = 0.6d,
                        //BehaviorActivateCondition = bd => { return bd.driveInputs != null && bd.sensorsData != null && TooClose(bd); }
                        //BehaviorActivateCondition = bd => { return false; }
                    });

                    //subsumptionDispatcher.Dispatch(new BehaviorBackAndTurn(driveGeometry)
                    //{
                    //    name = "Escape",
                    //    speaker = this.speaker,
                    //    BehaviorActivateCondition = bd => { return BehaviorBase.getCoordinatorData().EnablingRequest.StartsWith("Escape"); },
                    //    BehaviorDeactivateCondition = bd => { return true; },  // deactivate after one cycle
                    //    BehaviorTerminateCondition = bd => { return false; }   // do not terminate
                    //});

                    break;

                case BehaviorCompositionType.RouteFollowing:

                    subsumptionDispatcher.Dispatch(new BehaviorRouteFollowing(driveGeometry, this.speaker, TrackFileName, 0.3d)
                    {
                        name = "BehaviorRouteFollowing",
                        closeEnoughMeters = 0.15d
                    });

                    subsumptionDispatcher.Dispatch(new BehaviorGoToAngle(driveGeometry, 30.0d, 30.0d)
                    {
                        name = "BehaviorGoToAngle",
                        speaker = this.speaker
                    });

                    //subsumptionDispatcher.Dispatch(new BehaviorAvoidObstacles(driveGeometry)
                    //{
                    //    name = "BehaviorAvoidObstacles",
                    //    speaker = this.speaker
                    //});

                    //subsumptionDispatcher.Dispatch(new BehaviorStopShorty()
                    //{
                    //    name = "BehaviorStop",
                    //    speaker = this.speaker,
                    //    tresholdStopMeters = 0.2d
                    //});

                    //subsumptionDispatcher.Dispatch(new BehaviorBackAndTurn(driveGeometry)
                    //{
                    //    name = "Escape",
                    //    speaker = this.speaker,
                    //    BehaviorActivateCondition = bd => { return BehaviorBase.getCoordinatorData().EnablingRequest.StartsWith("Escape"); },
                    //    BehaviorDeactivateCondition = bd => { return true; },  // deactivate after one cycle
                    //    BehaviorTerminateCondition = bd => { return false; }   // do not terminate
                    //});

                    //subsumptionDispatcher.Dispatch(new BehaviorAmIStuck()
                    //{
                    //    name = "BehaviorAmIStuck",
                    //    speaker = this.speaker
                    //});

                    BehaviorBase.getCoordinatorData().ClearEnablingRequest();
                    BehaviorBase.getCoordinatorData().ClearGrabbingBehavior();
                    break;

                case BehaviorCompositionType.JoystickAndStop:

                    subsumptionDispatcher.Dispatch(new BehaviorControlledByJoystick(driveGeometry) {
                        name = "BehaviorControlledByJoystick",
                        speaker = this.speaker
                    });

                    //subsumptionDispatcher.Dispatch(new BehaviorAvoidObstacles(driveGeometry)
                    //{
                    //    name = "BehaviorAvoidObstacles",
                    //    speaker = this.speaker
                    //});

                    //subsumptionDispatcher.Dispatch(new BehaviorStop()
                    //{
                    //    name = "BehaviorStop",
                    //    speaker = this.speaker
                    //});
                    break;

                case BehaviorCompositionType.Escape:

                    subsumptionDispatcher.Dispatch(new BehaviorBackAndTurn(driveGeometry)
                    {
                        name = "Escape",
                        speaker = this.speaker,
                        BehaviorActivateCondition = bd => { return true; },
                        BehaviorDeactivateCondition = bd => { return true; },  // deactivate after one cycle
                        // BehaviorTerminateCondition = new BehaviorTerminateConditionDelegate(delegate(IBehaviorData bd) { return true; })
                        BehaviorTerminateCondition = bd => { return true; }    // terminate after first drive-turn cycle
                    });

                    BehaviorBase.getCoordinatorData().EnablingRequest = "Escape";   // set it immediately to see the escape action

                    subsumptionDispatcher.Dispatch(new BehaviorStopShorty() {
                        name = "BehaviorStop",
                        speaker = this.speaker
                    });
                    break;
            }
        }
    }
}
