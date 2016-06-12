
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
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

using slg.LibCommunication;
using slg.LibRobotExceptions;
using slg.RobotAbstraction;

namespace slg.ArduinoRobotHardware
{
    public class CommunicationTask
    {
        public bool running = false;
        private ArduinoBrick arduinoBrick;
        private ICommunicationChannel serialChannel;
        private Stopwatch stopWatch = new Stopwatch();
        private CancellationTokenSource cancellationTokenSource;
        //public BufferBlock<CommandAndResponse> commandsBufferBlock { get; private set; }
        private ActionBlock<CommandAndResponse> actionBlock;

        public CommunicationTask(ArduinoBrick ab)
        {
            this.arduinoBrick = ab;
        }

        public async Task Start(string portName, int baudRate, CancellationTokenSource cts)
        {
            Debug.WriteLine("ArduinoBrick:CommunicationTask: Start:   port: {0}  baudRate: {1}", portName, baudRate);

            cancellationTokenSource = cts;

            //serialPort = new CommunicationChannelBT();

            serialChannel = new CommunicationChannelSerial(cts)
            {
                Name = portName,
                BaudRate = (uint)baudRate,
                NewLineIn = "\r\n>",
                NewLineOut = "\r"
            };

            try
            {
                Debug.WriteLine("IP: ArduinoBrick:CommunicationTask: serialPort " + portName + " opening...");
                await serialChannel.Open();
                Debug.WriteLine("OK: ArduinoBrick:CommunicationTask: serialPort " + portName + " opened");

                //await Task.Delay(2000);

                // Notify users to initialize any devices
                // they have before we start processing commands:
                arduinoBrick.StartingCommunication(serialChannel);

                //commandsBufferBlock = new BufferBlock<CommandAndResponse>(
                //                        new DataflowBlockOptions() { CancellationToken = cts.Token });

                actionBlock = new ActionBlock<CommandAndResponse>(
                                        async prompt =>
                                        {
                                            await serialChannel.WriteLine(prompt.command);
                                            string response = await serialChannel.ReadLine();
                                            prompt.completionSource.SetResult(response);
                                        },
                                        new ExecutionDataflowBlockOptions() { CancellationToken = cts.Token, BoundedCapacity = 10 });

                //commandsBufferBlock.LinkTo(actionBlock, new DataflowLinkOptions { PropagateCompletion = true });

                Debug.WriteLine("IP: ArduinoBrick:CommunicationTask: trying to synch up with the board - resetting...");

                string resp = null;
                int count = 10;
                bool boardFound = false;

                while (count-- > 0)
                {
                    // try to sync up with the board
                    resp = await SendAndReceive("reset");

                    Debug.WriteLine("OK: ArduinoBrick:CommunicationTask: 'reset' -> '" + resp + "'");

                    if (string.Equals(resp, "Arduino firmware Plucky Wheels"))
                    {
                        boardFound = true;
                        break;
                    }
                }

                if (boardFound)
                {
                    Debug.WriteLine("OK: ArduinoBrick:CommunicationTask: found Plucky Wheels Arduino brick");
                }
                else
                {
                    throw new CommunicationException("CommunicationTask: Could not find Plucky Wheels Arduino brick, invalid response to 'reset' at serial port " + portName);
                }

                stopWatch.Start();
                running = true;
            }
            catch (AggregateException aexc)
            {
                throw new CommunicationException("CommunicationTask: Could not start communication");
            }
            catch (Exception exc)
            {
                Debug.WriteLine("Error: ArduinoBrick:CommunicationTask: exception while opening serial port " + portName + " : " + exc);
                //await Stop();
                //throw new CommunicationException("CommunicationTask: Could not start communication");
                throw;
            }
        }

        /// <summary>
        /// sends a string to Arduino commands queue and waits for response.
        /// </summary>
        /// <param name="cmd"></param>
        /// <returns></returns>
        public async Task<String> SendAndReceive(string cmd)
        {            
            TaskCompletionSource<String> cmdCompletionSource = new TaskCompletionSource<String>();

            //commandsBufferBlock.Post(new CommandAndResponse()
            actionBlock.Post(new CommandAndResponse()
            {
                command = cmd,
                completionSource = cmdCompletionSource
            });

            string response = await cmdCompletionSource.Task;   // waiting for the queue to pop the CommandAndResponse and execute it

            return response;
        }

        public void Flush()
        {
            //if (!cancellationTokenSource.IsCancellationRequested)
            //{
            //    try
            //    {
            //        IList<CommandAndResponse> dumpList = new List<CommandAndResponse>();
            //        commandsBufferBlock.TryReceiveAll(out dumpList);
            //    }
            //    catch
            //    {
            //        ;
            //    }
            //}
        }

        public async Task Stop()
        {
            running = false;
            stopWatch.Stop();
            //commandsBufferBlock.Complete();
            actionBlock.Complete();

            Flush();

            await Task.Delay(100);

            // Notify users to clean up any devices
            // connected to the serial port if need be, before closing it.
            arduinoBrick.StoppingCommunication(serialChannel);

            if (serialChannel != null)
            {
                serialChannel.DiscardInBuffer();
                serialChannel.Close();
                serialChannel = null;
            }
        }
    }
}