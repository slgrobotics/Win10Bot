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

using slg.RobotAbstraction.Drive;
using slg.LibRuntime;
using slg.RobotBase.Data;
using slg.RobotBase.Bases;
using slg.RobotBase.Interfaces;
using slg.Drive;
using slg.ControlDevices;
using slg.Sensors;
using slg.Behaviors;
using slg.Mapping;
using slg.RobotExceptions;
using System.Threading;

namespace slg.RobotShortyImpl
{
    /// <summary>
    /// ShortyTheRobot implementation - headless (controller) class.
    /// All robot components should be instantiated here, main loop ticker will be called often.
    /// </summary>
    public class ShortyTheRobot : RobotShortyHardwareBridge // IDisposable via AbstractRobot -> IRobotBase
    {
        #region Parameters

        private const double WHEEL_RADIUS_METERS = 0.061d;           // actual measured 0.061d
        private const double WHEEL_BASE_METERS = 0.345d;             // actual measured 0.328d
        private const double ENCODER_TICKS_PER_REVOLUTION = 438.0d;  // ticks per one wheel rotation

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
        private BehaviorFactory behaviorFactory;

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

        public ShortyTheRobot(ISpeaker speaker, IJoystickController joystick, int loopTimeMs)
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

            sensorsController = new SensorsControllerShorty(hardwareBrick, loopTimeMs);
            sensorsController.InitSensors(cts);

            InitDrive();

            behaviorFactory = new BehaviorFactory(dispatcher, driveGeometry, speaker);

            // we can set behavior combo now, or allow ControlDeviceCommand to set it later.
            //behaviorFactory.produce(BehaviorCompositionType.JoystickAndStop);

            robotState = new RobotState();
            robotPose = new RobotPose();

            robotPose.geoPosition.moveTo(lng, lat, elev);   // will be set to GPS coordinates, if available
            robotPose.direction.heading = headingDegrees;   // will be set to Compass Heading, if available
            robotPose.resetXY();

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
                speaker.Speak("Shorty cannot see his brick");
            }
            else
            {
                speaker.Speak("I am Shorty");
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
                    Debug.WriteLine("ShortyTheRobot: Waiting for brick to start...");
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
                    Debug.WriteLine("OK: ShortyTheRobot: brick started in " + seconds + " seconds");
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
            IDifferentialMotorController dmc = produceDifferentialMotorController();

            IDifferentialDrive ddc = new DifferentialDrive(hardwareBrick)
            {
                wheelRadiusMeters = WHEEL_RADIUS_METERS,
                wheelBaseMeters = WHEEL_BASE_METERS,
                encoderTicksPerRevolution = ENCODER_TICKS_PER_REVOLUTION,
                speedToVelocityFactor = SPEED_TO_VELOCITY_FACTOR,
                turnToOmegaFactor = TURN_TO_OMEGA_FACTOR,
                differentialMotorController = dmc
            };

            driveController = ddc;
            driveGeometry = ddc;

            driveController.Init();
        }

        /// <summary>
        /// controls exact order in which layers are closed
        /// </summary>
        public override void Close()
        {
            if (!isClosing)
            {
                isClosing = true;

                joystick.joystickEvent -= Joystick_joystickEvent;

                // first close all higher levels (behaviors), while lower levels are still operational:
                CloseRuntime();

                Debug.WriteLine("ShortyTheRobot: Close() : runtime closed, closing lower levels...");

                // we can now close lower levels which we created. Communication to the board is still open:
                sensorsController.Close();
                Task.Factory.StartNew(driveController.Close);

                // wait a bit (pumping events) and stop communication with the Hardware Brick (i.e. Element board):
                CloseCommunication();
            }
        }

        public override void Dispose()
        {
            Close();
        }

        #endregion // Lifecycle

        #region Control Device command processing

        /// <summary>
        /// must return promptly
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="jss"></param>
        private void Joystick_joystickEvent(object sender, IJoystickSubState jss)
        {
            jss.IsNew = false;
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

        private string previousButtonCommand = string.Empty;

        public override void ControlDeviceCommand(string command)
        {
            //Debug.WriteLine("ShortyTheRobot: ControlDeviceCommand: " + command);

            bool isDispatcherCommand = true;   // if true, dispatcher will be forwarding command to behaviors

            if (command.StartsWith("button") && previousButtonCommand != command)   // avoid button commands repetitions
            {
                if (command == "button4")
                {
                    Debug.WriteLine("ShortyTheRobot: ControlDeviceCommand: " + command);
                    robotPose.resetXY();
                    //robotPose.resetRotation();
                    robotPose.direction = new Direction() { heading = currentSensorsData.CompassHeadingDegrees };
                    driveController.OdometryReset();
                    isDispatcherCommand = false;    // no need for further processing
                    speaker.Speak("Reset X Y");
                }
                else if (command == "button1")
                {
                    Debug.WriteLine("ShortyTheRobot: ControlDeviceCommand: " + command);
                    lastActiveTasksCount = -1;  // initiate reporting in MonitorDispatcherActivity()
                    behaviorFactory.produce(BehaviorCompositionType.Escape);
                    isDispatcherCommand = false;    // no need for further command processing
                    speaker.Speak("Escape");
                }
                else if (command == "button5")
                {
                    Debug.WriteLine("ShortyTheRobot: ControlDeviceCommand: " + command);
                    lastActiveTasksCount = -1;  // initiate reporting in MonitorDispatcherActivity()
                    behaviorFactory.produce(BehaviorCompositionType.JoystickAndStop);
                    isDispatcherCommand = false;    // no need for further command processing
                    speaker.Speak("Joystick control");
                }
                else if (command == "button6")
                {
                    Debug.WriteLine("ShortyTheRobot: ControlDeviceCommand: " + command);
                    lastActiveTasksCount = -1;  // initiate reporting in MonitorDispatcherActivity()

                    ComputeGoal();

                    behaviorFactory.produce(BehaviorCompositionType.CruiseAndStop);
                    //behaviorFactory.produce(BehaviorCompositionType.AroundTheBlock);
                    isDispatcherCommand = false;    // no need for further command processing
                    //speaker.Speak("Around The Block");
                    speaker.Speak("Cruise control");
                }

                // buttons 7 and 8 on RamblePad2 reserved for throttle up/down control.

                previousButtonCommand = command;
            }

            if (isDispatcherCommand)
            {
                // dispatcher will be forwarding command like "speed" to behaviors
                dispatcher.ControlDeviceCommand(command);
            }
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

            if ((elapsedTimePrintInterval++) % 20 == 0)
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

            EvaluatePoseAndState(behaviorData);

            // populate all behaviors with current behaviorData:
            foreach (ISubsumptionTask task in dispatcher.Tasks)
            {
                BehaviorBase behavior = task as BehaviorBase;

                if (behavior != null)
                {
                    behavior.behaviorData = behaviorData;
                }
            }

            dispatcher.Process();     // calls behaviors, which take sensor outputs, and may compute drive inputs

            // look at ActiveTasksCount - it is an indicator of behaviors completed or removed. Zero count means we may need new behaviors combo.
            MonitorDispatcherActivity();

            // when active behavior is waiting (yielding), no action items are computed.
            // otherwise driveInputs will be non-null:
            if (behaviorData.driveInputs != null)
            {
                //Debug.WriteLine("ShortyTheRobot: Process()   - have drive inputs V=" + behaviorData.driveInputs.velocity + "   Omega=" + behaviorData.driveInputs.omega);

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
            int dispatcherActiveTasksCount = dispatcher.ActiveTasksCount;

            if (dispatcherActiveTasksCount != lastActiveTasksCount)
            {
                Debug.WriteLine("Warning: ShortyTheRobot: Process: active tasks count: " + dispatcherActiveTasksCount);

                if (dispatcherActiveTasksCount == 0)
                {
                    driveController.Stop(); // ensure the robot is stopped when all behaviors have exited
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
                ISubsumptionTask grabber = (from b in dispatcher.Tasks where b.name == gbn select b).FirstOrDefault();

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

        /// <summary>
        /// takes current state/pose, new sensors data and evaluates new state and pose
        /// </summary>
        /// <param name="behaviorData"></param>
        private void EvaluatePoseAndState(IBehaviorData behaviorData)
        {
            long[] encoderTicks = new long[] { behaviorData.sensorsData.WheelEncoderLeftTicks, behaviorData.sensorsData.WheelEncoderRightTicks };

            driveController.OdometryCompute(behaviorData.robotPose, encoderTicks);
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
            return "Shorty The Robot";
        }

        #endregion // Helpers
    }
}
