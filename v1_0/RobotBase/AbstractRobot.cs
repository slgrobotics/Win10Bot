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
using System.Threading.Tasks;

using slg.RobotBase.Interfaces;
using slg.LibRuntime;

namespace slg.RobotBase
{
    public abstract class AbstractRobot : IRobotBase
    {
        #region Robot data

        public IRobotPhysics robotPhysics { get; set; }

        public IRobotPose robotPose { get; set; }

        public IRobotState robotState { get; set; }

        public abstract ISensorsData currentSensorsData { get; }

        public abstract void ControlDeviceCommand(string command);

        #endregion // Robot data

        #region Lifecycle

        public abstract Task Init(string[] args);

        public AbstractRobot()
        {
            dispatcher = new SubsumptionTaskDispatcher();
        }

        public abstract void Close();

        public void CloseRuntime()
        {
            dispatcher.Close();

            while (dispatcher.ActiveTasksCount > 0)
            {
                dispatcher.Process();
            }

            Debug.WriteLine("AbstractRobot: Close() : all dispatcher tasks closed");
        }

        public abstract void Dispose();

        #endregion // Lifecycle

        #region Real-time processing methods

        // runtime:
        protected SubsumptionTaskDispatcher dispatcher;

        /// <summary>
        /// called often, must return promptly
        /// </summary>
        public abstract void PumpEvents();

        /// <summary>
        /// called often, must return promptly
        /// </summary>
        public abstract void Process();

        /// <summary>
        /// called often, must return promptly
        /// </summary>
        public abstract void ProcessControlDevices();

        /// <summary>
        /// call this method when beginning processing cycle in the worker loop
        /// </summary>
        public abstract void StartedLoop();

        /// <summary>
        /// call this method when ending processing cycle in the worker loop
        /// </summary>
        public abstract void EndingLoop();

        #endregion // Real-time processing methods
    }
}
