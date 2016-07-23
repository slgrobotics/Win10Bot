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
    public class DifferentialMotorController : HardwareComponent, IDifferentialMotorController
    {
        public int LeftMotorSpeed
        {
            get
            {
                throw new NotImplementedException();
            }

            set
            {
                if(Enabled)
                { 
                    //Debug.WriteLine("DifferentialMotorController:set_LeftMotorSpeed : " + value);
                    Task.Factory.StartNew(async () => {
                        try
                        {
                            int checksum = -(2 + value);
                            string cmd = "pwm 2:" + value + " c" + checksum;
                            string resp = await commTask.SendAndReceive(cmd);   // should be "ACK"
                            if (String.IsNullOrWhiteSpace(resp) || !String.Equals("ACK", resp.Trim()))
                            {
                                Debug.WriteLine("Error: DifferentialMotorController:set_LeftMotorSpeed : invalid response: " + resp);
                            }
                        }
                        catch { }
                   });
                }
            }
        }

        public int RightMotorSpeed
        {
            get
            {
                throw new NotImplementedException();
            }

            set
            {
                if (Enabled)
                {
                    //Debug.WriteLine("DifferentialMotorController:set_RightMotorSpeed : " + value);
                    Task.Factory.StartNew(async () => {
                        try
                        {
                            int checksum = -(1 + value);
                            string cmd = "pwm 1:" + value + " c" + checksum;
                            string resp = await commTask.SendAndReceive(cmd);   // should be "ACK"
                            if (String.IsNullOrWhiteSpace(resp) || !String.Equals("ACK", resp.Trim()))
                            {
                                Debug.WriteLine("Error: DifferentialMotorController:set_RightMotorSpeed : invalid response: " + resp);
                            }
                        }
                        catch { }
                    });
                }
            }
        }

        public DifferentialMotorController(CommunicationTask cTask, CancellationToken ct, int si)
            : base("DifferentialMotorController", cTask, ct, si)
        {
            Start();
        }

        public void DriveMotors()
        {
            //Debug.WriteLine("DifferentialMotorController:Drive()");
        }

        public void Update()
        {
            //Debug.WriteLine("DifferentialMotorController:Update()");
        }

        public void FeatherMotors()
        {
            throw new NotImplementedException();
        }

        public void BrakeMotors()
        {
            throw new NotImplementedException();
        }

        protected override async Task roundtrip()
        {
            // not really doing anything here yet
            await Task.Delay(1, cancellationToken);
        }
    }
}
