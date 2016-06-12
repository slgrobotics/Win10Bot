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

namespace slg.LibRobotDrive
{
    /// <summary>
    /// This class provides conversion between the Unicycle model and Differential Drive model
    /// See http://www.robotc.net/wiki/Tutorials/Arduino_Projects/Additional_Info/Turning_Calculations
    /// </summary>
    public class DifferentialDriveInputs : DriveInputsBase
    {
        #region Computed Differential Drive properties

        /// <summary>
        /// rotation of the left wheel, radians per second
        /// </summary>
        public double rotationLeftWheel  { get; private set; }

        /// <summary>
        /// rotation of the right wheel, radians per second
        /// </summary>
        public double rotationRightWheel { get; private set; }

        /// <summary>
        /// velocity of the left wheel, at the wheel edge (rim), meters per second
        /// </summary>
        public double velocityLeftWheel  { get; private set; }

        /// <summary>
        /// velocity of the right wheel, at the wheel edge (rim), meters per second
        /// </summary>
        public double velocityRightWheel { get; private set; }

        #endregion // Computed Differential Drive properties

        /// <summary>
        /// default constructor
        /// </summary>
        public DifferentialDriveInputs()
        {
            this.velocity = 0.0d;
            this.omega = 0.0d;
        }

        /// <summary>
        /// a copy constructor, intended to pass velocity and omega to differential drive before conversion from Unicycle
        /// </summary>
        /// <param name="src"></param>
        public DifferentialDriveInputs(IDriveInputs src)
        {
            this.velocity = src.velocity;
            this.omega = src.omega;
        }

        /*
         * Coursera formulas for reference:
         * 
            function [vel_r,vel_l] = uni_to_diff(obj,v,w)
                R = obj.wheel_radius;       % 31.9mm
                L = obj.wheel_base_length;  % 99.25mm
            
                vel_r = (2 * v + w * L) / 2 * R;
                vel_l = (2 * v - w * L) / 2 * R;
            end
        
            function [v,w] = diff_to_uni(obj,r,l)
                R = obj.wheel_radius;
                L = obj.wheel_base_length;
            
                v = R/2*(r+l);
                w = R/L*(r-l);
            end
         * 
         * 
            %% Output shaping - adjusting linear velocity to preserve turn velocity (omega):
        
            function [vel_r, vel_l] = ensure_w(obj, robot, v, w)
            
                % This function ensures that w is respected as best as possible
                % by scaling v.
            
                R = robot.wheel_radius;
                L = robot.wheel_base_length;
            
                vel_max = robot.max_vel;
                vel_min = robot.min_vel;
            
    %             fprintf('IN (v,w) = (%0.3f,%0.3f)\n', v, w);
            
                if (abs(v) > 0)
                    % 1. Limit v,w to be possible in the range [vel_min, vel_max]
                    % (avoid stalling or exceeding motor limits)
                    v_lim = max(min(abs(v), (R/2)*(2*vel_max)), (R/2)*(2*vel_min));
                    w_lim = max(min(abs(w), (R/L)*(vel_max-vel_min)), 0);
                
                    % 2. Compute the desired curvature of the robot's motion
                
                    [vel_r_d, vel_l_d] = robot.dynamics.uni_to_diff(v_lim, w_lim);
                
                    % 3. Find the max and min vel_r/vel_l
                    vel_rl_max = max(vel_r_d, vel_l_d);
                    vel_rl_min = min(vel_r_d, vel_l_d);
                
                    % 4. Shift vel_r and vel_l if they exceed max/min vel
                    if (vel_rl_max > vel_max)
                        vel_r = vel_r_d - (vel_rl_max-vel_max);
                        vel_l = vel_l_d - (vel_rl_max-vel_max);
                    elseif (vel_rl_min < vel_min)
                        vel_r = vel_r_d + (vel_min-vel_rl_min);
                        vel_l = vel_l_d + (vel_min-vel_rl_min);
                    else
                        vel_r = vel_r_d;
                        vel_l = vel_l_d;
                    end
                
                    % 5. Fix signs (Always either both positive or negative)
                    [v_shift, w_shift] = robot.dynamics.diff_to_uni(vel_r, vel_l);
                
                    v = sign(v)*v_shift;
                    w = sign(w)*w_shift;
                
                else
                    % Robot is stationary, so we can either not rotate, or
                    % rotate with some minimum/maximum angular velocity
                    w_min = R/L*(2*vel_min);
                    w_max = R/L*(2*vel_max);
                
                    if abs(w) > w_min
                        w = sign(w)*max(min(abs(w), w_max), w_min);
                    else
                        w = 0;
                    end
                
                
                end
            
    %             fprintf('OUT (v,w) = (%0.3f,%0.3f)\n', v, w);
                [vel_r, vel_l] = robot.dynamics.uni_to_diff(v, w);
            end

         */

        /// <summary>
        /// Computes speedLeft and speedRight based on velocity and omega,
        /// using Unicycle to Differential Drive formula and adjusting for maximum wheel speed
        /// </summary>
        /// <param name="geometry">contains wheel base and radius</param>
        public override void Compute(IDriveGeometry g)
        {
            IDifferentialDriveGeometry geometry = g as IDifferentialDriveGeometry;

            Debug.Assert(g != null);

            double velocityMax = geometry.speedToVelocityFactor * 100.0d;
            double omegaMax = geometry.turnToOmegaFactor * 100.0d;

            double velocity2 = velocity * 2.0d;
            double omegaL = omega * geometry.wheelBaseMeters; // positive - left
            double R2 = 2.0 * geometry.wheelRadiusMeters;
            double PIR2 = Math.PI * R2;

            // the uni_to_diff formula gives rotational speed of the wheel:

            int count = 0;
            do
            {
                this.rotationLeftWheel = (velocity2 - omegaL) / R2;
                this.rotationRightWheel = (velocity2 + omegaL) / R2;

                this.velocityLeftWheel = this.rotationLeftWheel * R2 / 2.0d;        // meters per second at the wheel edge (rim)
                this.velocityRightWheel = this.rotationRightWheel * R2 / 2.0d;

                // keep adjusting linear velocity to preserve turn velocity (omega):
                velocity2 *= 0.8d;
            }
            while ((this.velocityLeftWheel > velocityMax || this.velocityRightWheel > velocityMax) && count++ < 10);
        }

        public override string ToString()
        {
            return "velocity=" + velocity + "   omega=" + omega + "   velocityLeft=" + velocityLeftWheel + "   velocityRight=" + velocityRightWheel;
        }
    }
}
