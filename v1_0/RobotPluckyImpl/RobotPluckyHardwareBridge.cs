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
using System.Threading.Tasks;
using System.Diagnostics;

using slg.RobotBase;
using slg.RobotAbstraction;
using slg.RobotAbstraction.Drive;
using slg.RobotAbstraction.Events;
using slg.LibRobotExceptions;

// specific Hardware Brick (i.e. Arduino based board, see PluckyWheels sketch):
using slg.ArduinoRobotHardware;

namespace slg.RobotPluckyImpl
{
    /// <summary>
    /// this class bridges hardware brick (i.e. Element hardware) to AbstractRobot - so that the rest of the framework
    /// does not need to know which hardware it operates on.
    /// </summary>
    public abstract class RobotPluckyHardwareBridge : AbstractRobot
    {
        protected IAbstractRobotHardware hardwareBrick;   // instance of ConcreteRobotHardware, for example Element

        protected bool isBrickComStarted = false;
        public bool isCommError = false;

        /// <summary>
        /// produces hardwareBrick and sets it for communication
        /// </summary>
        /// <param name="args"></param>
        public override async Task Init(CancellationTokenSource cts, string[] args)
        {
            cancellationTokenSource = cts;

            string port = args[0];  // for Element board based robot we must pass serial port name here

            // create an instance of ConcreteRobotHardware:
            hardwareBrick = new ArduinoBrick(cts)
            {
                PortType = CommunicationPortType.Serial,
                PortName = port,
                BaudRate = 115200 // 19200
            };

            Debug.WriteLine("OK: RobotPluckyHardwareBridge: created Arduino hardware brick on port " + port);

            hardwareBrick.CommunicationsStarting += new CommunicationChannelEventHandler(bridge_CommunicationsStarting);
            hardwareBrick.CommunicationsStopping += new CommunicationChannelEventHandler(bridge_CommunicationsStopping);
            hardwareBrick.CommunicationStarted += new HardwareComponentEventHandler(bridge_CommunicationStarted);
        }

        protected async Task StartCommunication()
        {
            try { 
                await hardwareBrick.StartCommunication(cancellationTokenSource);
                isBrickComStarted = true;
            }
            catch (AggregateException aexc)
            {
                Debug.WriteLine("Error: Could not start communication");
                isCommError = true;
                //throw new CommunicationException("Could not start communication");
            }
            catch (Exception exc)
            {
                Debug.WriteLine("Error: Could not start communication");
                isCommError = true;
                //throw new CommunicationException("Could not start communication");
            }
        }

        public override void PumpEvents()
        {
            hardwareBrick.PumpEvents();
        }

        private async Task WaitPumping(int ms)
        {
            int j = ms / 20;
            for (int i = 0; i < 20; i++)
            {
                PumpEvents();
                await Task.Delay(20);
            }
        }

        /// <summary>
        /// call it after all finalizing sensors and drive commands have been completed.
        /// </summary>
        public async Task CloseCommunication()
        {
            await WaitPumping(3000);
            await hardwareBrick.StopCommunication();
            isBrickComStarted = false;
        }

        private void bridge_CommunicationsStopping(ICommunicationChannel serialDevice)
        {
            Debug.WriteLine("RobotPluckyHardwareBridge: bridge_CommunicationsStopping: Clean up your serial device here");
        }

        private void bridge_CommunicationsStarting(ICommunicationChannel serialDevice)
        {
            Debug.WriteLine("RobotPluckyHardwareBridge: bridge_CommunicationsStarting: Init your serial device here");
        }

        private void bridge_CommunicationStarted(IHardwareComponent sender)
        {
            Debug.WriteLine("RobotPluckyHardwareBridge: bridge_CommunicationStarted: Communication started");

            //isBrickComStarted = true;
        }
    }
}
