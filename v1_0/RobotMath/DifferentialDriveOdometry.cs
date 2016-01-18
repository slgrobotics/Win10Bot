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

namespace slg.RobotMath
{
    /// <summary>
    /// returned values for Odometry calculations
    /// </summary>
    public class Displacement
    {
        public double dCenter = 0.0d;
        public double halfPhi = 0.0d;
    }

    /// <summary>
    /// calculates robot displacement based on current wheel encoders ticks,
    /// memorizes previous values.
    /// </summary>
    public class DifferentialDriveOdometry
    {
        double wheelBaseMeters;
        double wheelRadiusMeters;
        double encoderTicksPerRevolution;

        public void Init(double _wheelBaseMeters, double _wheelRadiusMeters, double _encoderTicksPerRevolution)
        {
            wheelBaseMeters = _wheelBaseMeters;
            wheelRadiusMeters = _wheelRadiusMeters;
            encoderTicksPerRevolution = _encoderTicksPerRevolution;
        }

        #region Odometry variable and methods

        // we memorize previous calls to Odometry() to calculate wheels travel.
        private long lastWheelEncoderLeftTicks = 0L;
        private long lastWheelEncoderRightTicks = 0L;
        private bool firstTime = true;  // flag to initialize "last" values

        /// <summary>
        /// resets odometry algorithm in case of invalid wheel ticks changes
        /// </summary>
        public void Reset()
        {
            lastWheelEncoderLeftTicks = 0L;
            lastWheelEncoderRightTicks = 0L;
            firstTime = true;
        }

        /// <summary>
        /// calculates robot displacement based on current wheel encoders ticks
        /// </summary>
        /// <param name="robotPose">will be adjusted based on wheels travel</param>
        /// <param name="encoderTicks">wheel encoder ticks - left, right...</param>
        public Displacement Process(long[] encoderTicks)
        {
            #region estimate pose using wheel encoders and odometry formula

            Displacement ret = new Displacement();

            long wheelEncoderLeftTicks = encoderTicks[0];
            long wheelEncoderRightTicks = encoderTicks[1];

            if (firstTime && (wheelEncoderLeftTicks != 0L || wheelEncoderRightTicks != 0L))
            {
                lastWheelEncoderLeftTicks = wheelEncoderLeftTicks;
                lastWheelEncoderRightTicks = wheelEncoderRightTicks;
                firstTime = false;
            }
            else
            {
                long dLticks = wheelEncoderLeftTicks - lastWheelEncoderLeftTicks;
                long dRticks = wheelEncoderRightTicks - lastWheelEncoderRightTicks;

                if (dLticks != 0L || dRticks != 0L)
                {
                    double metersPerTick = 2.0d * Math.PI * wheelRadiusMeters / encoderTicksPerRevolution;    // meters per tick

                    double distanceLeftMeters = dLticks * metersPerTick;
                    double distanceRightMeters = dRticks * metersPerTick;

                    // Now, calculate the final angle, and use that to estimate
                    // the final position.  See Gary Lucas' paper for derivations
                    // of the equations.

                    /*
                     * Coursera formulas for reference:
                     * 
                        d_right = (right_ticks - prev_right_ticks) * m_per_tick;
                        d_left = (left_ticks - prev_left_ticks) * m_per_tick;
            
                        d_center = (d_right + d_left)/2;
                        phi = (d_right - d_left)/L;
            
                        x_dt = d_center*cos(theta);
                        y_dt = d_center*sin(theta);
                        theta_dt = phi;
                        
                        theta_new = theta + theta_dt;
                        x_new = x + x_dt;
                        y_new = y + y_dt;                           
                     */

                    ret.halfPhi = (distanceLeftMeters - distanceRightMeters) / (wheelBaseMeters * 2.0d);   // radians, assuming really small value for phi
                    ret.dCenter = (distanceRightMeters + distanceLeftMeters) / 2.0d;

                    lastWheelEncoderLeftTicks = wheelEncoderLeftTicks;
                    lastWheelEncoderRightTicks = wheelEncoderRightTicks;
                }
            }

            return ret;

            #endregion // estimate pose using wheel encoders and odometry formula
        }

        #endregion // Odometry variable and methods
    }
}
