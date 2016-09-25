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
using System.Threading;
using System.Diagnostics;

using slg.RobotAbstraction.Drive;

namespace slg.ArduinoRobotHardware.Controllers
{
    public class DifferentialMotorControllerT : HardwareComponent, IDifferentialMotorController
    {
        // left motor speed:
        private int lms = 0;
        private int lmsLast = 0;

        public int LeftMotorSpeed
        {
            get
            {
                throw new NotImplementedException();
            }

            set
            {
                lms = value;
            }
        }

        // right motor speed:
        private int rms = 0;
        private int rmsLast = 0;

        public int RightMotorSpeed
        {
            get
            {
                throw new NotImplementedException();
            }

            set
            {
                rms = value;
            }
        }

        public DifferentialMotorControllerT(CommunicationTask cTask, CancellationToken ct, int si)
            : base("DifferentialMotorControllerT", cTask, ct, si)
        {
            Start();
        }

        public void DriveMotors()
        {
            //Debug.WriteLine("DifferentialMotorControllerT:Drive()");
        }

        /// <summary>
        /// invalidates "Last" values, initiating a transmission of lms and rms to the controller
        /// </summary>
        public void Update()
        {
            //Debug.WriteLine("DifferentialMotorControllerT:Update()");
            lmsLast = rmsLast = int.MaxValue;
        }

        /// <summary>
        /// disables motors at hardware level, so that no current goes through them
        /// </summary>
        public void FeatherMotors()
        {
            lms = rms = 0;
            Update();
        }

        /// <summary>
        /// transmits command to the brick, making it apply brakes or otherwise stop the wheels 
        /// </summary>
        public void BrakeMotors()
        {
            lms = rms = 0;
            Update();
        }

        // a refresh counter, makes PWM command be repeated at given interval (around 1 second)
        private const int counterMax = 5;
        private int counter = 0;

        /// <summary>
        /// roundtrip() is called about 5 times per second, sends motor speed commands to the brick
        /// </summary>
        /// <returns></returns>
        protected override async Task roundtrip()
        {
            try
            {
                if (Enabled)
                {
                    if (lms != lmsLast && rms != rmsLast)
                    {
                        // both left and right sides values have changed
                        int checksum = -(1 + rms + 2 + lms);
                        string cmd = "pwm 1:" + rms + " 2:" + lms + " c" + checksum;
                        //Debug.WriteLine(cmd);
                        string resp = await commTask.SendAndReceive(cmd);   // should be "ACK"
                        if (String.IsNullOrWhiteSpace(resp) || !String.Equals("ACK", resp.Trim(), StringComparison.OrdinalIgnoreCase))
                        {
                            Debug.WriteLine("Error: DifferentialMotorControllerT:set both motors speed : invalid response: " + resp);
                        }
                        lmsLast = lms;
                        rmsLast = rms;
                        counter = 0;
                    }
                    else if (lms != lmsLast)
                    {
                        // only left side value have changed
                        int checksum = -(2 + lms);
                        string cmd = "pwm 2:" + lms + " c" + checksum;
                        //Debug.WriteLine(cmd);
                        string resp = await commTask.SendAndReceive(cmd);   // should be "ACK"
                        if (String.IsNullOrWhiteSpace(resp) || !String.Equals("ACK", resp.Trim(), StringComparison.OrdinalIgnoreCase))
                        {
                            Debug.WriteLine("Error: DifferentialMotorControllerT:set left motor speed : invalid response: " + resp);
                        }
                        lmsLast = lms;
                        counter = 0;
                    }
                    else if (rms != rmsLast)
                    {
                        // only right side value have changed
                        int checksum = -(1 + rms);
                        string cmd = "pwm 1:" + rms + " c" + checksum;
                        //Debug.WriteLine(cmd);
                        string resp = await commTask.SendAndReceive(cmd);   // should be "ACK"
                        if (String.IsNullOrWhiteSpace(resp) || !String.Equals("ACK", resp.Trim(), StringComparison.OrdinalIgnoreCase))
                        {
                            Debug.WriteLine("Error: DifferentialMotorControllerT:set right motor speed : invalid response: " + resp);
                        }
                        rmsLast = rms;
                        counter = 0;
                    }
                    else
                    {
                        // transmit current speed values every 1 second, even if nothing changed:
                        counter++;
                        if(counter >= counterMax)
                        {
                            counter = 0;
                            Update();   // issue both sides command on the next cycle 
                        }
                    }
                }
            }
            catch (Exception exc)
            {
                Debug.WriteLine("Error: DifferentialMotorControllerT: exception: " + exc);
            }
        }
    }
}
