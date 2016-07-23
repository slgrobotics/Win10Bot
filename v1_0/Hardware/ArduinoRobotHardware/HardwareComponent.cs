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
using System.Threading;
using System.Diagnostics;

using slg.RobotAbstraction;

namespace slg.ArduinoRobotHardware
{
    public abstract class HardwareComponent : IHardwareComponent
    {
        public virtual bool Enabled { get; set; }
        public string ComponentName { get; set; }

        protected CommunicationTask commTask;
        protected CancellationToken cancellationToken;
        protected int samplingIntervalMs;

        public HardwareComponent(string name, CommunicationTask cTask, CancellationToken ct, int si)
        {
            ComponentName = name;
            commTask = cTask;
            cancellationToken = ct;
            Debug.Assert(si > 1, "samplingIntervalMs must be larger than 1");
            samplingIntervalMs = si;
        }

        protected void Start()
        {
            Task.Factory.StartNew(DoWork, cancellationToken);
        }

        private void DoWork()
        {
            Debug.WriteLine("HardwareComponent: DoWork: " + ComponentName + " started");

            while (!cancellationToken.IsCancellationRequested)
            {
                if (Enabled && commTask.running)
                {
                    try
                    {
                        roundtrip().Wait();
                        //roundtrip().Wait(cancellationToken); - cannot do it, will not re-open comm port
                    }
                    catch (OperationCanceledException)
                    {
                        Debug.WriteLine("HardwareComponent: DoWork: " + ComponentName + "roundtrip(): canceled");
                    }
                    catch (Exception exc)
                    {
                        Debug.WriteLine("Error: HardwareComponent: DoWork: " + ComponentName + "roundtrip(): " + exc);
                    }
                }
                try
                {
                    //Task.Delay(samplingIntervalMs).Wait();
                    Task.Delay(samplingIntervalMs).Wait(cancellationToken);
                }
                catch { }
            }

            Debug.WriteLine("HardwareComponent: DoWork: " + ComponentName + " exited");
        }

        protected abstract Task roundtrip();
    }
}
