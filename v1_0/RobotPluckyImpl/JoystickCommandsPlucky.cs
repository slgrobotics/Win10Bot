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
using System.Threading.Tasks;
using System.Diagnostics;

using slg.RobotBase.Interfaces;
using slg.LibMapping;
using slg.RobotBase;
using slg.LibSystem;
using slg.RobotAbstraction.Sensors;

namespace slg.RobotPluckyImpl
{
    partial class PluckyTheRobot
    {
        #region Control Device (joystick) command processing

        /// <summary>
        /// must return promptly
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="jss"></param>
        private void Joystick_joystickEvent(object sender, IJoystickSubState jss)
        {
            jss.IsNew = false;
            currentJoystickData = (IJoystickSubState)jss.Clone();
            string command = jss.GetCommand();
            if (!string.IsNullOrWhiteSpace(command))
            {
                //Debug.WriteLine("Joystick event: command: " + command);
                this.ControlDeviceCommand(command);
            }
        }

        /// <summary>
        /// called once a second to open and maintain control devices, must return promptly.
        /// </summary>
        public override void ProcessControlDevices()
        {
        }

        private BehaviorCompositionType currentBehavior = BehaviorCompositionType.None;
        private string previousButtonCommand = string.Empty;
        private Track currentTrack = new Track();        // stores waypoints collected on Button 9 (Left Joystick push) while joystick driving.
        private int waypointsIndex = 0;
        private string waypointsFileName = "MyTrack.xml";

        public override void ControlDeviceCommand(string command)
        {
            //Debug.WriteLine("PluckyTheRobot: ControlDeviceCommand: " + command);

            bool isDispatcherCommand = true;   // if true, dispatcher will be forwarding command to behaviors

            if (command.StartsWith("button") && previousButtonCommand != command)   // avoid button commands repetitions
            {
                Debug.WriteLine("PluckyTheRobot: ControlDeviceCommand: " + command);
                isDispatcherCommand = false;    // no need for further command processing

                switch (command)
                {
                    case "button1":  // "A" button - caution: screen side effects
                        terminatePreviousBehavior(command);
                        currentBehavior = BehaviorCompositionType.Escape;
                        behaviorFactory.produce(currentBehavior);
                        speaker.Speak("Escape");
                        break;

                    case "button2":  // "B" button terminates current behavior
                        terminatePreviousBehavior(command);
                        behaviorFactory.produce(currentBehavior);   // None - closes all active tasks in Dispatcher
                        break;

                    //case "button3":  // "X" button
                    //    break;

                    case "button4":   // "Y" button
                        robotPose.reset();
                        robotPose.direction = new Direction() { heading = currentSensorsData.CompassHeadingDegrees };
                        driveController.OdometryReset();
                        speaker.Speak("Reset X Y");
                        break;

                    case "button5":  // "Left Bumper"
                        terminatePreviousBehavior(command);
                        currentTrack = new Track() { trackFileName = waypointsFileName };
                        waypointsIndex = 0;
                        currentBehavior = BehaviorCompositionType.JoystickAndStop;
                        behaviorFactory.produce(currentBehavior);
                        speaker.Speak("Joystick control");
                        break;

                    case "button6":  // "Right Bumper"
                        terminatePreviousBehavior(command);

                        ComputeGoal();  // for CruiseAndStop - corner-to-corner 

                        //currentBehavior = BehaviorCompositionType.CruiseAndStop;
                        currentBehavior = BehaviorCompositionType.ChaseColorBlob;
                        //currentBehavior = BehaviorCompositionType.AroundTheBlock;
                        behaviorFactory.produce(currentBehavior);
                        //speaker.Speak("Around The Block");
                        //speaker.Speak("Cruise control");
                        speaker.Speak("Chase color");
                        break;

                    // buttons 7 and 8 on RamblePad2 reserved for throttle up/down control. Not on Xbox360 controller.

                    case "button7":  // "Back" to the left of the sphere
                        terminatePreviousBehavior(command);

                        // Load stored waypoints:
                        // from C:\Users\sergei\AppData\Local\Packages\RobotPluckyPackage_sjh4qacv6p1wm\LocalState\MyTrack.xml

                        currentBehavior = BehaviorCompositionType.RouteFollowing;
                        behaviorFactory.TrackFileName = null;   // will be replaced with "MyTrack.xml" and serializer will be used.
                        behaviorFactory.produce(currentBehavior);
                        speaker.Speak("Saved trackpoints following");
                        break;

                    case "button8":  // "Start" to the right of the sphere
                        terminatePreviousBehavior(command);
                        currentBehavior = BehaviorCompositionType.RouteFollowing;
                        behaviorFactory.TrackFileName = "ParkingLot1.waypoints";
                        behaviorFactory.produce(currentBehavior);
                        speaker.Speak("Planned trackpoints following");
                        break;

                    case "button9":  // Left Joystick push
                        {
                            bool allowStaleGps = false;
                            bool allowOnlyGpsFix3D = false;

                            if (!allowStaleGps && (DateTime.Now - new DateTime(currentSensorsData.GpsTimestamp)).TotalSeconds > 3)
                            {
                                speaker.Speak("Cannot add waypoint, GPS data stale");
                            }
                            else
                            {
                                if (!allowOnlyGpsFix3D || allowOnlyGpsFix3D && currentSensorsData.GpsFixType == GpsFixTypes.Fix3D)
                                {
                                    currentTrack.trackpoints.Add(new Trackpoint(
                                        waypointsIndex,
                                        false, //waypointsIndex == 0,       // home
                                        currentSensorsData.GpsLatitude,
                                        currentSensorsData.GpsLongitude,
                                        currentSensorsData.GpsAltitude,
                                        robotPose.direction.heading,
                                        robotPose.ToString()                // for debugging
                                    ));
                                    waypointsIndex++;
                                    speaker.Speak("Added waypoint " + currentTrack.Count);
                                }
                                else
                                {
                                    speaker.Speak("Cannot add waypoint, low GPS fix: " + currentSensorsData.GpsFixType);
                                }
                            }
                        }
                        break;

                    //case "button10":  // right stick push
                    //    break;

                    default:
                        speaker.Speak(command + " not supported");
                        break;
                }
                previousButtonCommand = command;    // avoid repetitions
            }

            if (isDispatcherCommand)
            {
                // dispatcher will be forwarding command like "speed" to behaviors
                subsumptionTaskDispatcher.ControlDeviceCommand(command);
            }
        }

        private void terminatePreviousBehavior(string command)
        {
            if (currentBehavior != BehaviorCompositionType.None)
            {
                speaker.Speak("terminating " + Helpers.CamelCaseToSpokenString(currentBehavior.ToString()));

                switch (currentBehavior)
                {
                    case BehaviorCompositionType.JoystickAndStop:
                        if (currentTrack.Count > 2)
                        {
                            // Save accumulated waypoints:
                            speaker.Speak("saving track - " + currentTrack.Count + " trackpoints");
                            // saved in:  PC:    C:\Users\sergei\AppData\Local\Packages\RobotPluckyPackage_sjh4qacv6p1wm\LocalState\MyTrack.xml
                            //            RPi:   \\172.16.1.175\c$\Data\Users\DefaultAccount\AppData\Local\Packages\RobotPluckyPackage_sjh4qacv6p1wm\LocalState
                            SerializableStorage<Track>.Save(waypointsFileName, currentTrack);
                        }
                        break;
                }
                currentBehavior = BehaviorCompositionType.None;
            }
            lastActiveTasksCount = -1;  // initiate reporting in MonitorDispatcherActivity()
        }

        #endregion // Control Device command processing

    }
}
