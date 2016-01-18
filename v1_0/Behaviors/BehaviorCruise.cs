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

using slg.RobotBase.Bases;
using slg.RobotBase.Interfaces;
using slg.LibRuntime;

namespace slg.Behaviors
{
    /// <summary>
    /// just go forward - non-grabbing, always FiredOn
    /// </summary>
    public class BehaviorCruise : BehaviorBase
    {
        private double cruiseSpeed;    // -100...100
        private double cruiseTurn;     // -100...100, positive - right

        /// <summary>
        /// just go forward - non-grabbing, always FiredOn behavior
        /// </summary>
        /// <param name="ddg">IDriveGeometry</param>
        /// <param name="desiredCruiseSpeed">-100...100</param>
        /// <param name="desiredCruiseTurn">-100...100, positive - right; we can cruise in a circle</param>
        public BehaviorCruise(IDriveGeometry ddg, double desiredCruiseSpeed = 20.0d, double desiredCruiseTurn = 0.0d)
            : base(ddg)
        {
            cruiseSpeed = desiredCruiseSpeed;
            cruiseTurn = desiredCruiseTurn;
        }

        #region Behavior logic

        /// <summary>
        /// Computes drive speed based on requestedSpeed, current DrivingState, timing and sensors input
        /// </summary>
        /// <returns></returns>
        public override IEnumerator<ISubsumptionTask> Execute()
        {
            FiredOn = true;

            while (!MustExit && !MustTerminate)
            {
                if (!GrabByOther())
                {
                    setSpeedAndTurn(cruiseSpeed, cruiseTurn);
                }

                yield return RobotTask.Continue;
            }

            FiredOn = false;

            Debug.WriteLine("BehaviorCruise: " + (MustExit ? "MustExit" : "completed"));

            yield break;
        }

        /// <summary>
        /// finish all operations nicely
        /// </summary>
        public override void Close()
        {
            Debug.WriteLine("BehaviorCruise: Close()");
            base.Close();
        }

        #endregion // Behavior logic
    }
}
