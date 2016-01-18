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
using System.Threading.Tasks;

namespace slg.RobotBase.Interfaces
{
    /// <summary>
    /// provides access to all robot data (physics, pose, state, sensors data, control device inputs)
    /// and also abstracts lifecycle and realtime processing loop 
    /// </summary>
    public interface IRobotBase : IDisposable
    {
        #region Robot data

        IRobotPhysics robotPhysics { get; set; }

        IRobotPose robotPose { get; }

        IRobotState robotState { get; }

        ISensorsData currentSensorsData { get; }

        void ControlDeviceCommand(string command);

        #endregion // Robot data

        #region Lifecycle

        Task Init(string[] args);

        void Close();

        #endregion // Lifecycle

        #region Real-time processing methods

        /// <summary>
        /// called often, must return promptly
        /// </summary>
        void PumpEvents();

        /// <summary>
        /// called often, must return promptly
        /// </summary>
        void Process();

        /// <summary>
        /// called often, must return promptly
        /// </summary>
        void ProcessControlDevices();

        /// <summary>
        /// call this method when beginning processing cycle in the worker loop
        /// </summary>
        void StartedLoop();

        /// <summary>
        /// call this method when ending processing cycle in the worker loop
        /// </summary>
        void EndingLoop();

        #endregion // Real-time processing methods
    }
}
