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

using slg.RobotBase;
using slg.RobotBase.Bases;
using slg.RobotBase.Interfaces;
using slg.LibRuntime;

namespace slg.Behaviors
{
    /// <summary>
    /// ballistic behavior to escape from an obstacle.
    /// will set a grab and FiredOn when triggered
    /// </summary>
    public class BehaviorBackAndTurn : BehaviorBase
    {
        private double escapeSpeed;   // -100...100
        private double escapeTurn;    // -100...100, positive - right
        private Random random = new Random();
        private DateTime started;

        /// <summary>
        /// ballistic "Escape" behavior, 
        /// will set a grab and FiredOn when triggered.
        /// </summary>
        /// <param name="ddg"></param>
        /// <param name="_doOnce"></param>
        /// <param name="desiredEscapeSpeed">0...-100, negative</param>
        /// <param name="desiredEscapeTurn">0...100, positive</param>
        public BehaviorBackAndTurn(IDriveGeometry ddg, double desiredEscapeSpeed = -15.0d, double desiredEscapeTurn = 20.0d)
            : base(ddg)
        {
            escapeSpeed = desiredEscapeSpeed;
            escapeTurn = desiredEscapeTurn;
        }

        #region Behavior logic

        /// <summary>
        /// Sets drive velocity and omega based on escapeSpeed, escapeTurn, current DrivingState, timing and enablingRequest
        /// </summary>
        /// <returns></returns>
        public override IEnumerator<ISubsumptionTask> Execute()
        {
            while (!MustExit && !MustTerminate)
            {
                while (GrabByOther())
                {
                    yield return RobotTask.Continue;    // chain grabbed by somebody else
                }

                while (!MustActivate && !MustExit && !MustTerminate)   // codeword triggers one cycle
                {
                    FiredOn = false;
                    yield return RobotTask.Continue;    // dormant state - no request for escaping
                }

                if (MustExit || MustTerminate)
                    continue;

                // Activated - compute speed and turn based on Enabling Request:
                SetGrabByMe();
                FiredOn = true;
                string savedEnablingRequest = getCoordinatorData().EnablingRequest;
                getCoordinatorData().EnablingRequest = string.Empty;

                speaker.Speak(Helpers.CamelCaseToSpokenString(savedEnablingRequest));

                do
                {
                    //Debug.WriteLine("BehaviorBackAndTurn: cycle started: " + savedEnablingRequest);
                    started = DateTime.Now;
                    // compute speed and turn based on Enabling Request:
                    double speed;
                    double turn;
                    ComputeSpeedAndTurn(savedEnablingRequest, out speed, out turn);

                    // perform escape sequence:

                    // go straight (back or forward) for a short time:
                    while (!DriveTerminateCondition())
                    {
                        setSpeedAndTurn(speed, 0.0d);
                        //setSpeedAndTurn(0.0d, 0.0d);
                        yield return RobotTask.Continue;
                    }

                    // turn (left or right) for a short time:
                    while (!TurnTerminateCondition())
                    {
                        setSpeedAndTurn(0.0d, turn);
                        yield return RobotTask.Continue;
                    }

                    // go straight (forward or back) for a short time, to create an offset:
                    while (!ShiftTerminateCondition())
                    {
                        setSpeedAndTurn(-speed, 0.0d);
                        yield return RobotTask.Continue;
                    }

                } while (!MustDeactivate);

                Debug.WriteLine("BehaviorBackAndTurn: deactivated: " + getCoordinatorData().EnablingRequest);

                // in case the enablingRequest is stuck, clear it:
                if (getCoordinatorData().EnablingRequest == savedEnablingRequest)
                {
                    getCoordinatorData().ClearEnablingRequest();
                }

                FiredOn = false;
                ClearGrab();
            }

            // we can forget to set velocity and omega to 0 (to stop the robot) - robot's MonitorDispatcherActivity() will stop it when tasks count is zero.
            //behaviorData.driveInputs = new DriveInputsBase();      // a stop command unless modified

            Debug.WriteLine("BehaviorBackAndTurn: " + (MustExit ? "MustExit" : "completed"));

            FiredOn = false;
            ClearGrabIfMine();
            yield break;
        }

        /// <summary>
        /// when we need to finish driving on a straight line
        /// </summary>
        /// <returns>true if we need to finish</returns>
        private bool DriveTerminateCondition()
        {
            return MustExit || MustTerminate || (DateTime.Now - started).TotalSeconds >= 1.0d;
        }

        /// <summary>
        /// when we need to finish turning
        /// </summary>
        /// <returns>true if we need to finish</returns>
        private bool TurnTerminateCondition()
        {
            return MustExit || MustTerminate || (DateTime.Now - started).TotalSeconds >= 2.0d;   // another second after straight driving
        }

        /// <summary>
        /// when we need to finish turning
        /// </summary>
        /// <returns>true if we need to finish</returns>
        private bool ShiftTerminateCondition()
        {
            double forwardSensorMeters = Math.Min(Math.Min(behaviorData.sensorsData.RangerFrontLeftMeters, behaviorData.sensorsData.RangerFrontRightMeters), behaviorData.sensorsData.IrFrontMeters);

            return MustExit || MustTerminate || forwardSensorMeters < 0.25d || (DateTime.Now - started).TotalSeconds >= 3.0d;   // another second after the turn
        }

        /// <summary>
        /// compute speed and turn based on Enabling Request
        /// </summary>
        /// <param name="speed"></param>
        /// <param name="turn"></param>
        private void ComputeSpeedAndTurn(string enablingRequest, out double speed, out double turn)
        {
            double turnFactor = 1.0d;
            double turnFactorR = 0.75d + 0.5d * random.NextDouble();    // 0.75...1.25
            double speedFactorR = 0.75d + 0.5d * random.NextDouble();   // 0.75...1.25

            if (enablingRequest.Contains("Forward"))
            {
                speedFactorR = -speedFactorR;
            }

            if (enablingRequest == "EscapeLeft")
            {
                turnFactor = -1.0d;     // will turn left after backing up
            }
            else if (enablingRequest == "EscapeRight")
            {
                turnFactor = 1.0d;      // will turn right after backing up
            }
            else if (enablingRequest == "EscapeLeftTurn")
            {
                speedFactorR = 0.0d;
                turnFactorR = 1.0d;
                turnFactor = -1.0d;      // will turn left
            }
            else if (enablingRequest == "EscapeRightTurn")
            {
                speedFactorR = 0.0d;
                turnFactorR = 1.0d;
                turnFactor = 1.0d;      // will turn right
            }
            else if (enablingRequest == "EscapeFullTurn")
            {
                speedFactorR = 0.0d;
                turnFactorR = 2.0d;
                turnFactor = random.Next(2) == 1 ? 1.0d : -1.0d;
            }
            else if (enablingRequest == "EscapeNone")
            {
                turnFactor = random.Next(2) == 1 ? 1.0d : -1.0d;
            }
            else // already StartsWith("Escape")
            {
                turnFactor = random.Next(2) == 1 ? 1.0d : -1.0d;
            }

            speed = escapeSpeed * speedFactorR;
            turn = escapeTurn * turnFactor * turnFactorR; // positive - right
        }

        /// <summary>
        /// finish all operations nicely
        /// </summary>
        public override void Close()
        {
            Debug.WriteLine("BehaviorBackAndTurn: Close()");
            ClearGrabIfMine();
            base.Close();
        }

        #endregion // Behavior logic
    }
}
