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
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Diagnostics;

using slg.RobotAbstraction.Drive;
using slg.LibRuntime;
using slg.RobotBase.Data;
using slg.RobotBase.Bases;
using slg.RobotBase.Interfaces;
using slg.LibRobotDrive;
using slg.ControlDevices;
using slg.LibSystem;
using slg.Behaviors;
using slg.LibMapping;
using slg.LibRobotExceptions;
using slg.RobotAbstraction.Sensors;
using slg.LibRobotSLAM;
using System.Collections.Generic;
using slg.RobotBase;

namespace slg.RobotPluckyImpl
{
    /// <summary>
    /// PluckyTheRobot implementation - headless (controller) class.
    /// All robot components should be instantiated here, main loop ticker will be called often.
    /// </summary>
    public class PluckyTheRobot : RobotPluckyHardwareBridge // IDisposable via AbstractRobot -> IRobotBase
    {
        #region Parameters

        private const double WHEEL_RADIUS_METERS = 0.192d;           // actual measured 
        private const double WHEEL_BASE_METERS = 0.600d;             // actual measured 
        private const double ENCODER_TICKS_PER_REVOLUTION = 853.0d;  // ticks per one wheel rotation
        private const int ODOMETRY_SAMPLING_INTERVAL_MS = 20;        // for hardware odometry working in Arduino
        private const int GPS_SAMPLING_INTERVAL_MS = 1000;           // for GPS processed in Arduino

        /*
         * Some measurements: at motor controller input 100 (full speed) the wheels rotate at 130 rpm.
         * So, max. linear speed 50 meters / minute, or 0.83 meters/second
         * From the above, max rotational speed is 5 rad/sec. Measured rotation is 4.6 rad/sec
         */

        // experimental scaling factors for turns and velocity:
        private const double SPEED_TO_VELOCITY_FACTOR = 0.0083d;    // speed -100..+100, velocity -0.83..+0.83 meters per second
        private const double TURN_TO_OMEGA_FACTOR = 0.046d;         // turn -100..+100,  omega 4.6..-4.6 radians per second, positive turn - right, positive omega - left

        #endregion // Parameters

        #region Components and variables

        private Stopwatch stopWatch = new Stopwatch();
        private long loopStartTime = 0L;
        private double loopTimeMs;     // will be used as encoders Sampling Interval.

        // robot components as interfaces:
        private ISpeaker speaker;
        private IJoystickController joystick;
        private ISensorsController sensorsController;
        private IDrive driveController;
        private IDriveGeometry driveGeometry;
        private IOdometry odometry;
        private IGps gps;
        private BehaviorFactory behaviorFactory;
        private IRobotSLAM robotSlam;

        private double lng = -117.671550d;
        private double lat = 33.600222d;
        private double elev = 0.0d;

        private double headingDegrees = 0.0d;   // relative to true North

        public IBehaviorData BehaviorData = null;

        private bool isClosing = false;

        public override ISensorsData currentSensorsData { get { return this.sensorsController.currentSensorsData; } }

        public override IJoystickSubState currentJoystickData { get; set; }

        #endregion // Components and variables

        #region Lifecycle

        public PluckyTheRobot(ISpeaker speaker, IJoystickController joystick, int loopTimeMs)
        {
            this.speaker = speaker;
            this.joystick = joystick;
            this.loopTimeMs = loopTimeMs;
        }


        // see http://code.jonwagner.com/2012/09/06/best-practices-for-c-asyncawait/
        //     http://www.c-sharpcorner.com/UploadFile/pranayamr/difference-between-await-and-continuewith-keyword-in-C-Sharp/
        //     http://code.jonwagner.com/2012/09/04/deadlock-asyncawait-is-not-task-wait/
        //     https://msdn.microsoft.com/en-us/library/ff963550.aspx  - Parallel Programming book

        public override async Task Init(CancellationTokenSource cts, string[] args)
        {
            await base.Init(cts, args);    // produces hardwareBrick and sets it for communication

            joystick.joystickEvent += Joystick_joystickEvent;

            sensorsController = new SensorsControllerPlucky(hardwareBrick, loopTimeMs);
            await sensorsController.InitSensors(cts);

            InitDrive();

            behaviorFactory = new BehaviorFactory(subsumptionTaskDispatcher, driveGeometry, speaker);

            // we can set behavior combo now, or allow ControlDeviceCommand to set it later.
            //behaviorFactory.produce(BehaviorCompositionType.JoystickAndStop);

            robotState = new RobotState();
            robotPose = new RobotPose();

            robotState.powerLevelPercent = 100;  // can be changed any time. Used by behaviors.

            robotPose.geoPosition.moveTo(lng, lat, elev);   // will be set to GPS coordinates, if available
            robotPose.direction.heading = headingDegrees;   // will be set to Compass Heading, if available
            robotPose.resetXY();

            robotSlam = new RobotSLAM();

            await InitComm(cts);     // may throw exceptions

            // see what kind of timer we have to measure durations:
            if (Stopwatch.IsHighResolution)
            {
                Debug.WriteLine("OK: operations are timed using the system's high-resolution performance counter.");
            }
            else
            {
                Debug.WriteLine("Warning: operations are timed using the DateTime class.");
            }

            stopWatch.Start();

            if (isCommError)
            {
                speaker.Speak("Plucky cannot see his brick");
            }
            else
            {
                speaker.Speak("I am Plucky");
            }
        }

        private async Task InitComm(CancellationTokenSource cts)
        {
            try
            {
                isCommError = false;
                isBrickComStarted = false;

                await Task.Factory.StartNew(StartCommunication); // we just await the factory, not the thread

                int seconds = 0;
                while (!isBrickComStarted && !isCommError && seconds < 20)
                {
                    Debug.WriteLine("PluckyTheRobot: Waiting for brick to start...");
                    await Task.Delay(1000);
                    seconds++;
                }

                if (!isBrickComStarted)
                {
                    cts.Cancel();
                    throw new CommunicationException("Could not start communication after " + seconds + " seconds - brick does not open.");
                }
                else
                {
                    Debug.WriteLine("OK: PluckyTheRobot: brick started in " + seconds + " seconds");
                }
            }
            catch (AggregateException aexc)
            {
                cts.Cancel();
                isCommError = true;
                throw new CommunicationException("Could not start communication - brick does not open");
            }
            catch (CommunicationException exc)
            {
                cts.Cancel();
                isCommError = true;
                throw; // new CommunicationException("Could not start communication - brick does not open");
            }
            catch (Exception exc)
            {
                cts.Cancel();
                isCommError = true;
                throw new CommunicationException("Could not start communication - brick does not open");
            }
        }

        private void InitDrive()
        {
            IDifferentialMotorController dmc = hardwareBrick.produceDifferentialMotorController();

            driveController = new DifferentialDrive(hardwareBrick)
            {
                wheelRadiusMeters = WHEEL_RADIUS_METERS,
                wheelBaseMeters = WHEEL_BASE_METERS,
                encoderTicksPerRevolution = ENCODER_TICKS_PER_REVOLUTION,
                speedToVelocityFactor = SPEED_TO_VELOCITY_FACTOR,
                turnToOmegaFactor = TURN_TO_OMEGA_FACTOR,
                differentialMotorController = dmc
            };

            this.odometry = hardwareBrick.produceOdometry(ODOMETRY_SAMPLING_INTERVAL_MS);

            this.odometry.OdometryChanged += ((SensorsControllerPlucky)sensorsController).ArduinoBrickOdometryChanged;

            this.gps = hardwareBrick.produceGps(GPS_SAMPLING_INTERVAL_MS);

            this.gps.GpsPositionChanged += ((SensorsControllerPlucky)sensorsController).ArduinoBrickGpsChanged;
            this.gps.Enabled = true;

            driveController.hardwareBrickOdometry = odometry;
            driveGeometry = (IDifferentialDrive)driveController;

            driveController.Init();
            driveController.Enabled = true;
        }

        /// <summary>
        /// controls exact order in which layers are closed
        /// </summary>
        public override void Close()
        {
            if (!isClosing)
            {
                isClosing = true;

                //joystick.joystickEvent -= Joystick_joystickEvent;

                // first close all higher levels (behaviors), while lower levels are still operational:
                CloseRuntime();

                Debug.WriteLine("PluckyTheRobot: Close() : runtime closed, closing lower levels...");

                // we can now close lower levels which we created. Communication to the board is still open:
                sensorsController.Close();
                driveController.Close();
                //Task.Factory.StartNew(driveController.Close);   // sets PWM to 0
                //Task.Delay(2000).Wait();

                // wait a bit (pumping events) and stop communication with the Hardware Brick (i.e. Element board):
                //CloseCommunication().Wait();

                hardwareBrick.Close();
            }
        }

        public override void Dispose()
        {
            Close();
        }

        #endregion // Lifecycle

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

        public override async void ControlDeviceCommand(string command)
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
                        robotPose.resetXY();
                        //robotPose.resetRotation();
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
                            bool allowStaleGps = true;
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
                                        currentSensorsData.GpsAltitude
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

        #region Main Loop processing

        /// <summary>
        /// mark the beginning of the processing cycle in the worker loop
        /// </summary>
        public override void StartedLoop()
        {
            loopStartTime = stopWatch.ElapsedMilliseconds;  // mark the start time of the cycle.

            this.PumpEvents();
        }

        private int elapsedTimePrintInterval = 0;

        /// <summary>
        /// call this method when ending processing cycle in the worker loop
        /// </summary>
        public override void EndingLoop()
        {
            this.PumpEvents();

            long elapsedTime = stopWatch.ElapsedMilliseconds - loopStartTime;

            if((elapsedTimePrintInterval++) % 20 == 0)
            {
                Debug.WriteLine("Process(): elapsed: " + elapsedTime);
            }
        }

        /// <summary>
        /// called often, must return promptly. It is a thread safe function.
        /// </summary>
        public override void Process()
        {
            StartedLoop();

            // every cycle we create new behaviorData, populating it with existing data objects:
            IBehaviorData behaviorData = new BehaviorData()
            {
                sensorsData = this.currentSensorsData,
                robotState = this.robotState,
                robotPose = this.robotPose
            };

            // use sensor data to update robotPose:
            robotSlam.EvaluatePoseAndState(behaviorData, driveController);

            // populate all behaviors with current behaviorData:
            foreach (ISubsumptionTask task in subsumptionTaskDispatcher.Tasks)
            {
                BehaviorBase behavior = task as BehaviorBase;

                if (behavior != null)
                {
                    behavior.behaviorData = behaviorData;
                }
            }

            subsumptionTaskDispatcher.Process();     // calls behaviors, which take sensor outputs, and may compute drive inputs

            // look at ActiveTasksCount - it is an indicator of behaviors completed or removed. Zero count means we may need new behaviors combo.
            MonitorDispatcherActivity();

            // when active behavior is waiting (yielding), no action items are computed.
            // otherwise driveInputs will be non-null:
            if (behaviorData.driveInputs != null)
            {
                //Debug.WriteLine("PluckyTheRobot: Process()   - have drive inputs V=" + behaviorData.driveInputs.velocity + "   Omega=" + behaviorData.driveInputs.omega);

                driveController.driveInputs = behaviorData.driveInputs;

                driveController.Drive();    // apply driveInputs to motors

                robotState.velocity = behaviorData.driveInputs.velocity;
                robotState.omega = behaviorData.driveInputs.omega;
            }

            this.BehaviorData = behaviorData;   // for tracing

            sensorsController.Process();        // let sensorsController do maintenance

            EndingLoop();
        }

        private int lastActiveTasksCount = -1;
        private BehaviorsCoordinatorData coordinatorData = BehaviorBase.getCoordinatorData();  // get the singleton

        /// <summary>
        /// behaviors in dispatcher may all exit, which will require producing a new behaviors combo.
        /// </summary>
        private void MonitorDispatcherActivity()
        {
            // ActiveTasksCount is indicator of behaviors completed or removed. Zero count means we may need new behaviors combo.
            int dispatcherActiveTasksCount = subsumptionTaskDispatcher.ActiveTasksCount;

            if (dispatcherActiveTasksCount != lastActiveTasksCount)
            {
                Debug.WriteLine("Warning: PluckyTheRobot: Process: active tasks count: " + dispatcherActiveTasksCount);

                if (dispatcherActiveTasksCount == 0)
                {
                    this.SafePose(); // ensure the robot is stopped when all behaviors have exited
                    speaker.Speak("active tasks count zero");
                }
                else if (lastActiveTasksCount <= 0)
                {
                    speaker.Speak(string.Format("{0} tasks", dispatcherActiveTasksCount));
                }

                lastActiveTasksCount = dispatcherActiveTasksCount;
            }

            // clear the grab, if any:
            string gbn = coordinatorData.GrabbingBehaviorName;
            if (!string.IsNullOrWhiteSpace(gbn))
            {
                ISubsumptionTask grabber = (from b in subsumptionTaskDispatcher.Tasks where b.name == gbn select b).FirstOrDefault();

                if (grabber != null)
                {
                    BehaviorBase bBase = (BehaviorBase)grabber;
                    if (!bBase.FiredOn)
                    {
                        // Grabbing behavior is not firing any more, can clear the grab:
                        coordinatorData.ClearGrabbingBehavior();
                    }
                }
            }
        }

        #endregion // Main Loop processing

        #region Helpers

        /// <summary>
        /// try put drive and other actuators in a safe position - stop motors etc.
        /// </summary>
        public void SafePose()
        {
            driveController.Stop();
        }

        private void ComputeGoal()
        {
            robotState.goalBearingDegrees = currentSensorsData.CompassHeadingDegrees;
            robotState.goalDistanceMeters = 2.0d;
            robotState.computeGoalGeoPosition(robotPose);

            robotPose.resetXY();
            robotPose.direction = new Direction() { heading = currentSensorsData.CompassHeadingDegrees, bearingRelative = 0.0d, distanceToGoalMeters = 2.0d };
            driveController.OdometryReset();
        }

        public override string ToString()
        {
            return "Plucky The Robot";
        }

        #endregion // Helpers
    }
}
