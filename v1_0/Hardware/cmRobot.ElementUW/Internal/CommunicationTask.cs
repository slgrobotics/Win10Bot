using System;
using System.Collections.Generic;
using Windows.Devices.SerialCommunication;
using System.Threading;
using System.Diagnostics;

using Windows.Devices.Bluetooth;
using Windows.Devices.Bluetooth.Rfcomm;
using Windows.Networking.Sockets;
using Windows.Storage.Streams;
using System.Threading.Tasks;
using Windows.Devices.Enumeration;

using slg.RobotAbstraction;
using slg.LibCommunication;
using slg.LibRobotExceptions;

// see https://ms-iot.github.io/content/en-US/win10/samples/SerialSample.htm
// see https://github.com/dotMorten/NmeaParser/wiki/Using-in-a-Windows-Universal-App-(SerialPort)
// see BTSerial sample - C:\temp\samples-develop\BTSerial\MainPage.xaml.cs

namespace cmRobot.Element.Internal
{
	internal class CommunicationTask
	{
        #region Privates

        private const uint numQueues = 2;
        bool running = false;
        Element element;
        //private object jobLock = new object();
        private ICommunicationChannel serialChannel;

        private Semaphore sem = new Semaphore(0);
        private Queue<CommJobInfo>[] jobQueues = new Queue<CommJobInfo>[numQueues];
        private Stopwatch stopWatch = new Stopwatch();

        #endregion

        #region Ctors

        public CommunicationTask(Element element)
		{
			this.element = element;

			for (uint i = 0; i < numQueues; i++)
			{
				jobQueues[i] = new Queue<CommJobInfo>();
			}
		}

        #endregion // Ctors

        #region Public Methods

        public async Task Start(string portName, int baudRate, CancellationTokenSource cts)
        {
            Debug.WriteLine("Element:CommunicationTask: Start:   port: {0}  baudRate: {1}", portName, baudRate);

            //serialPort = new CommunicationChannelBT();

            serialChannel = new CommunicationChannelSerial(cts)
            {
                Name = portName,
                BaudRate = (uint)baudRate
            };

            try
            {
                Debug.WriteLine("IP: Element:CommunicationTask: serialPort " + portName + " opening...");
                await serialChannel.Open();
                Debug.WriteLine("OK: Element:CommunicationTask: serialPort " + portName + " opened");

                //await Task.Delay(2000);

                // Notify users to initialize any devices
                // they have before we start processing commands:
                element.StartingCommunication(serialChannel);

                running = true;
                ClearJobQueues();

                Task workerTask = new Task(() => ThreadProc(cts.Token), cts.Token);
                workerTask.Start();

                Debug.WriteLine("IP: Element:CommunicationTask: trying to synch up with the board - resetting...");

                // try to synch up with the board
                string resp = EnqueueCommJobAndWait(Priority.High, "reset");

                //await Task.Delay(3000); // let other processes continue

                // This HAS TO BE HERE...Since it takes the Element about a second to boot up
                // using the new Tiny Bootloader...
                //Thread.Sleep(1000);
                await Task.Delay(2000);

                // Clear receive buffer out, since the bootloader can send
                // some junk characters, which might hose subsequent command responses:
                serialChannel.DiscardInBuffer();
                stopWatch.Start();
            }
            catch (AggregateException aexc)
            {
                throw new CommunicationException("CommunicationTask: Could not start communication");
            }
            catch (Exception exc)
            {
                Debug.WriteLine("Error: Element:CommunicationTask: exception while opening serial port " + portName + " : " + exc);
                //await Stop();
                //throw new CommunicationException("CommunicationTask: Could not start communication");
                throw;
            }
        }

        public async void Send(string s)
        {
            await serialChannel.WriteLine(s);
        }

        public async Task Stop()
		{
			running = false;
            stopWatch.Stop();

            sem.Up();

            await Task.Delay(100);

            // Notify users to clean up any devices
            // connected to the serial port if need be, before closing it.
            element.StoppingCommunication(serialChannel);

			if (serialChannel != null)
			{
                // Clear receive buffer out, since the bootloader can send
                // some just characters, which might hose subsequent command responses:
                //serialPort.DiscardInBuffer();

				serialChannel.Close();
				serialChannel = null;
			}
		}

		public string EnqueueCommJobAndWait(Priority priority, string cmd)
		{
			SimpleCommunicationJob commJob = new SimpleCommunicationJob(cmd);
			EnqueueCommJobAndWait(priority, commJob);
			return commJob.Response;
		}

		public void EnqueueCommJobAndWait(Priority priority, ICommunicationJob job)
		{
			CommJobInfo info = new CommJobInfo();
			info.WaitHandle = new AutoResetEvent(false);
			info.Job = job;

			// always enqueue this job and run the highest priority one instead
			EnqueueJob(priority, info);
			info.WaitHandle.WaitOne();
		}

		public void EnqueueCommJob(Priority priority, string cmd)
		{
			SimpleCommunicationJob job = new SimpleCommunicationJob(cmd);
			EnqueueCommJob(priority, job);
		}

		public void EnqueueCommJob(Priority priority, ICommunicationJob job)
		{
			CommJobInfo info = new CommJobInfo();
			info.WaitHandle = null;
			info.Job = job;

			// always enqueue this job and run the highest priority one instead
			EnqueueJob(priority, info);
		}

#endregion // Public Methods


#region Private Types

        private class CommJobInfo
		{
			public AutoResetEvent WaitHandle = null;
			public ICommunicationJob Job;

			public override bool Equals(object o)
			{
				if (o == null || o.GetType() != GetType())
				{
					return false;
				}

				CommJobInfo op2 = o as CommJobInfo;
				return Job == op2.Job;
			}

			public override int GetHashCode()
			{
				return Job.GetHashCode();
			}
		}

#endregion // Private Types


#region Private Methods

        private void EnqueueJob(Priority priority, CommJobInfo job)
		{
			Queue<CommJobInfo> jobQueue = jobQueues[(int)priority];
			lock (jobQueue)
			{
				if (!jobQueue.Contains(job))
				{
					jobQueue.Enqueue(job);
                    sem.Up();
                }
			}
		}

		private CommJobInfo DequeueJob()
		{
			sem.Down();

			for (uint i = 0; i < numQueues; ++i)
			{
				Queue<CommJobInfo> jobQueue = jobQueues[i];
				lock (jobQueue)
				{
					if (jobQueue.Count > 0)
					{
						CommJobInfo job = jobQueue.Dequeue();
						return job;
					}
				}
			}

			return null;
		}

		private void ClearJobQueues()
		{
			for (uint i = 0; i < numQueues; ++i)
			{
				Queue<CommJobInfo> jobQueue = jobQueues[i];
				lock (jobQueue)
				{
					jobQueue.Clear();
				}
			}
		}

        private async Task ProcessJob(CommJobInfo jobInfo, CancellationToken ct)
		{
			// generate the command string for this job
			string cmd = jobInfo.Job.GenerateCommand();
			string resp = "";

			// send the command in a retry loop
			// in case we get back NACKs
			const uint tryThreshold = 3;
			uint tryCnt;
			for (tryCnt = 1; tryCnt <= tryThreshold && !ct.IsCancellationRequested; tryCnt++)
			{
				try
				{
                    long loopStartTime = stopWatch.ElapsedMilliseconds;  // mark the start time of the cycle.
                    //Debug.WriteLine(String.Format("Comm: Try {0}: cmd: '{1}'", tryCnt, cmd));
                    await SerialPortWriteLine(cmd);
					resp = await SerialPortReadLine();
                    long elapsedTime = stopWatch.ElapsedMilliseconds - loopStartTime;

                    //if(cmd.StartsWith("pwm"))
                    //    Debug.WriteLine(String.Format("Comm: Try {0}: '{1}' -> '{2}'  {3} ms", tryCnt, cmd, resp, elapsedTime));

                    // we usually get an ACK or a number (sensor reading) in response.
					if (resp != "NACK")
					{
						break;
					}
				}
				catch (TimeoutException e)
				{
                    Debug.WriteLine(String.Format("Element:CommunicationTask: CommErr: Try {0}: '{1}' -> (timeout) - {2}", tryCnt, cmd, e.Message));
				}
			}

            if (!ct.IsCancellationRequested)
            {
                if (tryCnt >= tryThreshold)
                {
                    //TODO: communication exception
                    Debug.WriteLine(String.Format(
                        "#: aborting '{0}' after {1} tries", cmd, tryCnt - 1));
                }
                else
                {
                    // process the response
                    jobInfo.Job.ProcessResponse(resp);
                }
            }
		}

		private async Task<string> SerialPortReadLine()
		{
            // this is specific to Element board:
			serialChannel.NewLineIn = "\r\n>";
            string str = await serialChannel.ReadLine();
            if (str.StartsWith("\r\n"))
                return str.Length > 2 ? str.Substring(2) : String.Empty;    // remove trailing CRLF, coming from Element board
            else
                return str;
		}

		private async Task SerialPortWriteLine(string s)
		{
			serialChannel.NewLineOut = "\r";
            await serialChannel.WriteLine(s);
		}

		private async void ThreadProc(CancellationToken ct)
		{
            while (!ct.IsCancellationRequested)
            {
                // select the next (highest priority) job
                CommJobInfo jobInfo = DequeueJob();
                if (!running)
                {
                    break;
                }

                if (jobInfo != null)
                {
                    try
                    {
                        await ProcessJob(jobInfo, ct);
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine(ex);
                    }
                    finally
                    {
                        if (jobInfo.WaitHandle != null)
                        {
                            jobInfo.WaitHandle.Set();
                        }
                    }
                }
                else
                {
                    await Task.Delay(10);
                }
            }
            running = false;
            ClearJobQueues();
		}

#endregion // Private Methods
    }
}
