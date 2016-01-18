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

namespace slg.Behaviors
{
    public enum BehaviorCompositionType
    {
        JoystickAndStop,
        CruiseAndStop,
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

                    subsumptionDispatcher.Dispatch(new BehaviorGoToAngle(driveGeometry, 10.0d, 10.0d)
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
                        //distanceToWallMeters = 0.25d,
                        //BehaviorDeactivateCondition = bd => { return BehaviorBase.getCoordinatorData().EnablingRequest.StartsWith("Escape"); },
                        //BehaviorTerminateCondition = bd => { return bd.sensorsData.IrRearMeters < 0.2d; }
                    });

                    subsumptionDispatcher.Dispatch(new BehaviorStop()
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

                case BehaviorCompositionType.JoystickAndStop:

                    subsumptionDispatcher.Dispatch(new BehaviorControlledByJoystick(driveGeometry) {
                        name = "BehaviorControlledByJoystick",
                        speaker = this.speaker
                    });

                    subsumptionDispatcher.Dispatch(new BehaviorAvoidObstacles(driveGeometry)
                    {
                        name = "BehaviorAvoidObstacles",
                        speaker = this.speaker
                    });

                    subsumptionDispatcher.Dispatch(new BehaviorStop()
                    {
                        name = "BehaviorStop",
                        speaker = this.speaker
                    });
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

                    subsumptionDispatcher.Dispatch(new BehaviorStop() {
                        name = "BehaviorStop",
                        speaker = this.speaker
                    });
                    break;
            }
        }
    }
}
