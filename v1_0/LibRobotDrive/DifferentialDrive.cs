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

using slg.RobotAbstraction;
using slg.RobotAbstraction.Drive;
using slg.RobotBase;

namespace slg.LibRobotDrive
{
    /// <summary>
    /// concrete class implementing Differential Drive.
    /// contains operations allowing to drive it as unicycle and compute odometry into Pose.
    /// deals with specifics of controller board inputs.
    /// </summary>
    public class DifferentialDrive : DifferentialDriveBase  // base class implements IDifferentialDrive
    {
        private IAbstractRobotHardware hardwareBrick;

        public IDifferentialMotorController differentialMotorController { get; set; }

        public DifferentialDrive(IAbstractRobotHardware brick)
        {
            this.hardwareBrick = brick;
        }

        private bool _enabled = false;

        public override bool Enabled {
            get { return _enabled; }
            set
            {
                differentialMotorController.Enabled = value;
                if (hardwareBrickOdometry != null)
                {
                    hardwareBrickOdometry.Enabled = value;
                }
            }
        }

        public override void Init()
        {
            if (hardwareBrickOdometry == null)
            {
                odometry.Init(wheelBaseMeters, wheelRadiusMeters, encoderTicksPerRevolution);
            }
        }

        public override void Close()
        {
            Debug.WriteLine("DifferentialDrive: Close() - stopping motors");

            this.Stop();

            //while (pmc.QueryStatus())
            //{
            //    Debug.WriteLine("IP: ...stopping motors - dmc busy...");
            //    hardwareBrick.PumpEvents();
            //}
        }

        #region Driving logic

        /// <summary>
        /// apply driveInputs to drive - command the motors.
        /// </summary>
        public override void Drive()
        {
            DifferentialDriveInputs di = this.driveInputs as DifferentialDriveInputs;

            Debug.Assert(di != null);

            di.Compute(this);   // turn Unicycle into Differential Drive

            this.Drive(di);
        }

        public override void Stop()
        {
            differentialMotorController.RightMotorSpeed = differentialMotorController.LeftMotorSpeed = 0;
            differentialMotorController.DriveMotors();  // set PWM
            differentialMotorController.Update();       // force command to be sent immediately
        }

        private int correctedSpeedLastLeft = 0;
        private int correctedSpeedLastRight = 0;

        /// <summary>
        /// executes command to drive at certain speed. correctedSpeed can be null.
        /// </summary>
        /// <param name="ddi"></param>
        private void Drive(DifferentialDriveInputs ddi)
        {
            if (ddi == null)
            {
                ddi = new DifferentialDriveInputs();  // practically a stop command
            }

            // the board accepts integers in the range -100...+100. Dead zone around -20...20 (see MotorsDeadZone)
            // we operate in ints here, as we consider a change in power by at least 1 to be significant. Otherwise we ignore the change.
            int correctedTrimmedSpeedLeft = VelocityToMotorSpeed(ddi.velocityLeftWheel);
            int correctedTrimmedSpeedRight = VelocityToMotorSpeed(ddi.velocityRightWheel);

            //Debug.WriteLine("Drive:  " + ddi.ToString() + "   L=" + correctedTrimmedSpeedLeft + "   R=" + correctedTrimmedSpeedRight);

            bool leftSpeedChanged = correctedSpeedLastLeft != correctedTrimmedSpeedLeft;
            bool rightSpeedChanged = correctedSpeedLastRight != correctedTrimmedSpeedRight;
            bool speedChanged = leftSpeedChanged || rightSpeedChanged;

            // only command to the motors if speed changes:
            if (speedChanged)
            {
                differentialMotorController.LeftMotorSpeed = correctedTrimmedSpeedLeft; // == 0 ? correctedTrimmedSpeedLeft : (correctedTrimmedSpeedLeft - 1);
                differentialMotorController.RightMotorSpeed = correctedTrimmedSpeedRight; // == 0 ? correctedTrimmedSpeedRight : (correctedTrimmedSpeedRight - 1);

                differentialMotorController.DriveMotors();    // this call commands the Hardware Brick (i.e. Element board) via the serial line and takes bandwidth. Avoid unneccessary calls. 

                correctedSpeedLastLeft = correctedTrimmedSpeedLeft;
                correctedSpeedLastRight = correctedTrimmedSpeedRight;
            }
        }

        #endregion // Driving logic

        private const double MOTORS_DEAD_ZONE = 25.0d;
        private const double VIRTUAL_DEAD_ZONE = 2.0d;

        #region Helpers

        /// <summary>
        /// converts velocity to MotorSpeed, adjusting for dead zone -20..+20
        /// </summary>
        /// <param name="velocity"></param>
        /// <returns>integers in the range -100...+100 to be fed to Drive as MotorSpeed</returns>
        private int VelocityToMotorSpeed(double velocity)
        {
            int motorSpeed = 0;

            double ms = Math.Max(-100.0d, Math.Min(100.0d, velocity / speedToVelocityFactor));

            // have a small dead zone here, to avoid unneccessary calls to Drive:
            if (ms > VIRTUAL_DEAD_ZONE)
            {
                motorSpeed = (int)Math.Round(map(ms, 0.0d, 100.0d, MOTORS_DEAD_ZONE, 100.0d));
            }
            else if (ms < -VIRTUAL_DEAD_ZONE)
            {
                motorSpeed = (int)Math.Round(map(ms, -100.0d, 0.0d, -100.0d, -MOTORS_DEAD_ZONE));
            }
            // else remains at 0

            return motorSpeed;
        }

        /// <summary>
        /// see http://arduino.cc/en/reference/map
        /// </summary>
        /// <param name="x"></param>
        /// <param name="fromLow"></param>
        /// <param name="fromHigh"></param>
        /// <param name="toLow"></param>
        /// <param name="toHigh"></param>
        /// <returns></returns>
        private double map(double x, double fromLow, double fromHigh, double toLow, double toHigh)
        {
            return (x - fromLow) * (toHigh - toLow) / (fromHigh - fromLow) + toLow;
        }

        #endregion // Helpers

    }
}
