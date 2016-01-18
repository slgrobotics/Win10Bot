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

namespace slg.Behaviors
{

    /// <summary>
    /// implements sliding mode control
    /// https://en.wikipedia.org/wiki/Sliding_mode_control
    /// https://www.youtube.com/watch?v=BZbCdjbPTs4
    /// https://www.youtube.com/watch?v=arEkc5mIsVE
    /// 
    /// </summary>
    public class BehaviorSlidingMode : BehaviorBase
    {
        #region Behavior logic

        /// <summary>
        /// Computes drive speed based on requestedSpeed, current DrivingState, timing and sensors input
        /// </summary>
        /// <returns></returns>
        public override IEnumerator<ISubsumptionTask> Execute()
        {
            DriveInputsBase correctedDriveSpeed = new DriveInputsBase();      // a stop command unless modified 

            //behaviorData.driveInputs = correctedDriveSpeed;

            yield break;
        }

        /// <summary>
        /// finish all operations nicely
        /// </summary>
        public override void Close()
        {
            Debug.WriteLine("BehaviorSlidingMode: Close()");
            base.Close();
        }

        #endregion // Behavior logic
    }
}
