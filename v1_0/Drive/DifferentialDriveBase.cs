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

using slg.RobotBase.Interfaces;
using slg.RobotMath;

namespace slg.Drive
{
    /// <summary>
    /// base class implementing Differential Drive and its geometry.
    /// contains operations allowing to drive it as unicycle and compute odometry into Pose.
    /// </summary>
    public abstract class DifferentialDriveBase : IDifferentialDrive
    {
        // IDifferentialDriveGeometry implementation
        public double wheelRadiusMeters { get; set; }

        public double wheelBaseMeters { get; set; }

        public double encoderTicksPerRevolution { get; set; }

        /// <summary>
        /// factor to convert abstract speed value in the range -100...100 to physical velocity meters per second
        /// for example, speed -100..+100, velocity -0.83..+0.83 meters per second would give factor 0.0083
        /// speedToVelocityFactor must be a positive value.
        /// </summary>
        public double speedToVelocityFactor { get; set; }

        /// <summary>
        /// factor to convert abstract turn value in the range -100...100 to physical omega radians per second
        /// for example, turn -100..+100,  omega 4.6..-4.6 radians per second would give factor 0.046
        /// note that turn is positive to the right, and omega is positive to the left, that will be accounted for in formulas.
        /// turnToOmegaFactor must be a positive value.
        /// </summary>
        public double turnToOmegaFactor { get; set; }

        // we tweak the driveInputs setter to allocate DifferentialDriveInputs so that its Compute() method could convert Unicycle to Differential Drive
        private DifferentialDriveInputs _driveInputs;

        public IDriveInputs driveInputs
        {
            get { return _driveInputs; }
            set { _driveInputs = new DifferentialDriveInputs(value); }
        }

        public abstract void Init();

        public abstract void Close();

        /// <summary>
        /// apply driveInputs to drive - command the motors.
        /// </summary>
        public abstract void Drive();

        /// <summary>
        /// try put drive in a safe position - stop motors etc.
        /// </summary>
        public abstract void Stop();

        public DifferentialDriveOdometry odometry = new DifferentialDriveOdometry();

        /// <summary>
        /// calculates robot pose change based on current wheel encoders ticks
        /// </summary>
        /// <param name="robotPose">will be adjusted based on wheels travel</param>
        /// <param name="encoderTicks">wheel encoder ticks - left, right...</param>
        public void Odometry(IRobotPose robotPose, long[] encoderTicks)
        {
            Displacement disp = odometry.Process(encoderTicks);

            if (disp.halfPhi != 0.0d || disp.dCenter != 0.0d)
            {
                double theta = robotPose.Theta + disp.halfPhi;   // radians

                // calculate displacement in the middle of the turn:
                double dX = disp.dCenter * Math.Cos(theta);      // meters
                double dY = disp.dCenter * Math.Sin(theta);      // meters

                robotPose.translate(dX, dY);
                robotPose.rotate(disp.halfPhi * 2.0d);
            }
        }

        /// <summary>
        /// resets odometry algorithm in case of invalid wheel ticks changes
        /// </summary>
        public void OdometryReset()
        {
            odometry.Reset();
        }
    }
}
