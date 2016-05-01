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

using System.Threading.Tasks;
using System.Threading;

using slg.RobotAbstraction;
using System.Diagnostics;

namespace slg.ArduinoRobotHardware
{
    public abstract class HardwareComponent : IHardwareComponent
    {
        public virtual bool Enabled { get; set; }

        protected CommunicationTask commTask;
        protected CancellationToken cancellationToken;
        protected int samplingIntervalMs;

        public HardwareComponent(CommunicationTask cTask, CancellationToken ct, int si)
        {
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
            while (!cancellationToken.IsCancellationRequested)
            {
                if (Enabled && commTask.running)
                {
                    try
                    {
                        roundtrip().Wait();
                    }
                    catch
                    {
                        ;
                    }
                }
                Task.Delay(samplingIntervalMs).Wait();
            }
        }

        protected abstract Task roundtrip();
    }
}
