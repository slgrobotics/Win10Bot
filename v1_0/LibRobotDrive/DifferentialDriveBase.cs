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

using slg.LibRobotMath;
using slg.LibMapping;
using slg.RobotBase.Interfaces;
using slg.RobotAbstraction.Sensors;

namespace slg.LibRobotDrive
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

        public IOdometry hardwareBrickOdometry { get; set; }    // can be null - then software computation is used

        public DifferentialDriveOdometry odometry = new DifferentialDriveOdometry();    // for software computation

        public abstract bool Enabled { get; set; }

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

        /// <summary>
        /// calculates robot pose change based on current wheel encoders ticks
        /// </summary>
        /// <param name="robotPose">Used to retrieve current position only. Will be adjusted in SLAM based on wheels travel.</param>
        /// <param name="encoderTicks">raw data - wheel encoder ticks - left, right...  - ignored if using hardwareBrickOdometry</param>
        /// <returns>Displacement - to be applied in SLAM module. Can return null if there is no displacement</returns>
        public IDisplacement OdometryCompute(IRobotPose robotPose, long[] encoderTicks)
        {
            IDisplacement ret = null;    // null will be returned if there is no displacement

            if (hardwareBrickOdometry != null)
            {
                // already calculated odometry comes from the hardware brick (i.e. Arduino)

                double dx = hardwareBrickOdometry.XMeters - robotPose.XMeters;
                double dy = hardwareBrickOdometry.YMeters - robotPose.YMeters;
                double dz = 0.0d;
                double dth = hardwareBrickOdometry.ThetaRadians - robotPose.ThetaRadians;

                if (dx != 0.0d || dy != 0.0d  || dz != 0.0d || dth != 0.0d)
                {
                    ret = new Displacement(dx, dy, dz, dth);
                }
            }
            else
            {
                // we have wheels encoders data and must calculate odometry here:

                DisplacementOdometry disp = odometry.Process(encoderTicks);

                if (disp.halfPhi != 0.0d || disp.dCenter != 0.0d)
                {
                    double thetaMid = robotPose.ThetaRadians + disp.halfPhi;   // radians in the middle of the turn

                    // calculate displacement in the middle of the turn:
                    double dx = disp.dCenter * Math.Cos(thetaMid);      // meters
                    double dy = disp.dCenter * Math.Sin(thetaMid);      // meters
                    double dz = 0.0d;
                    double dThetaRadians = disp.halfPhi * 2.0d;         // actual turn

                    ret = new Displacement(dx, dy, dz, dThetaRadians);
                }
            }

            return ret;
        }

        /// <summary>
        /// resets odometry algorithm in case of invalid wheel ticks changes
        /// </summary>
        public void OdometryReset()
        {
            if(hardwareBrickOdometry != null)
            {
                hardwareBrickOdometry.ZeroAll();
            }
            else
            {
                odometry.Reset();
            }
        }
    }
}
