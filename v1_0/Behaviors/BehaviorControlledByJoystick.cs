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
using System.Threading;
using System.Diagnostics;

using slg.RobotBase.Interfaces;
using slg.RobotBase.Bases;
using slg.LibRuntime;

namespace slg.Behaviors
{
    /// <summary>
    /// basic "Remote" behavior - responds to commands from a control device (i.e. a joystick)
    /// non-grabbing, always FiredOn
    /// </summary>
    public class BehaviorControlledByJoystick : BehaviorBase
    {
        private bool fullAuto = false;
        private double fullAutoSpeed = 50.0d;   // 0...100

        private double desiredSpeed;
        private double desiredTurn;

        public BehaviorControlledByJoystick(IDriveGeometry ddg)
            : base(ddg)
        {
            // behaviors can subscribe to control device events:
            this.controlDeviceEvent += new EventHandler<ControlDeviceEventArgs>(Behavior_controlDeviceEvent);
        }

        /// <summary>
        /// interprets the command from control device (i.e. a joystick)
        /// see ...\ControlDevices\RumblePad2.cs
        /// </summary>
        /// <param name="command"></param>
        void Behavior_controlDeviceEvent(object sender, ControlDeviceEventArgs e)
        {
            string command = e.command;
            double speed = 0.0d;
            double turn = 0.0d;

            switch (command)
            {
                case "button2":
                    fullAuto = true;
                    break;

                case "button3":
                    fullAuto = false;
                    break;

                default:
                    if (command.StartsWith("speed|"))
                    {
                        string[] split = command.Split(new char[] { '|' });
                        speed = double.Parse(split[1]);      // -100...+100
                        turn = double.Parse(split[2]);       // -100...+100
                    }
                    break;
            }

            if (fullAuto)
            {
                // cruise control
                desiredSpeed = fullAutoSpeed;
                desiredTurn = 0.0d;
            }
            else
            {
                desiredSpeed = speed;
                desiredTurn = turn;
            }
        }

        #region Behavior logic

        /// <summary>
        /// Computes drive speed based on desiredSpeed and desiredTurn
        /// </summary>
        /// <returns></returns>
        public override IEnumerator<ISubsumptionTask> Execute()
        {
            FiredOn = true;

            while (!MustExit && !MustTerminate)
            {
                if (!GrabByOther())
                {
                    setSpeedAndTurn(desiredSpeed, desiredTurn);
                }

                yield return RobotTask.Continue;
            }

            FiredOn = false;

            Debug.WriteLine("BehaviorControlledByJoystick: " + (MustExit ? "MustExit" : "completed"));

            yield break;
        }

        /// <summary>
        /// finish all operations nicely
        /// </summary>
        public override void Close()
        {
            Debug.WriteLine("BehaviorControlledByJoystick: Close()");
            base.Close();
        }

        #endregion // Behavior logic
    }
}
