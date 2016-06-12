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

using slg.LibRuntime;
using slg.RobotBase.Bases;
using slg.RobotBase.Interfaces;

namespace slg.Behaviors
{
    /// <summary>
    /// sets speed and turn to 0 (stop) if obstacle is close.
    /// non-grabbing, will set FiredOn when triggered
    /// will issue EnablingRequest within the "Escape" subset
    /// </summary>
    public abstract class BehaviorStop : BehaviorBase
    {
        public double tresholdStopMeters = 0.4d;        // at velocity 0.2 m/s
        public double tresholdIrStopMeters = 0.25d;     // front sensor is 10..80 cm

        protected string escapeRecommendation { get; set; }

        public BehaviorStop()
            : base()
        {
            BehaviorActivateCondition = bd => { return bd.driveInputs != null && bd.sensorsData != null && TooClose(bd); };
        }

        #region Behavior logic

        /// <summary>
        /// sets speed and turn to 0 (stop), FiredOn to true if obstacle is close.
        /// will issue EnablingRequest within the "Escape" subset
        /// </summary>
        /// <returns></returns>
        public override IEnumerator<ISubsumptionTask> Execute()
        {
            while (!MustExit && !MustTerminate)
            {
                if (!GrabByOther())
                {

                    if (MustActivate)
                    {
                        Debug.WriteLine("BehaviorStop: activated: " + escapeRecommendation);

                        setSpeedAndTurn();  // no parameters is a stop

                        FiredOn = true;

                        if(!string.IsNullOrWhiteSpace(escapeRecommendation))
                        {
                            getCoordinatorData().EnablingRequest = escapeRecommendation; //  "Escape..." - hopefully appropriate behavior will be triggered
                        }
                    }
                    else
                    {
                        // deactivating when the activate condition is gone.
                        FiredOn = false;
                    }
                }

                yield return RobotTask.Continue;
            }

            Debug.WriteLine("BehaviorStop: " + (MustExit ? "MustExit" : "completed"));

            yield break;
        }

        /// <summary>
        /// determines if an obstacle is too close and comes up with an escape recommendation
        /// </summary>
        /// <param name="behaviorData"></param>
        /// <returns></returns>
        protected abstract bool TooClose(IBehaviorData behaviorData);

        /// <summary>
        /// finish all operations nicely
        /// </summary>
        public override void Close()
        {
            Debug.WriteLine("BehaviorStop: Close()");
            base.Close();
        }

        #endregion // Behavior logic
    }
}
