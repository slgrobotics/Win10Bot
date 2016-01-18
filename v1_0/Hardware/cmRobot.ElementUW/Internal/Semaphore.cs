using System;
using System.Collections.Generic;
using System.Threading;

namespace cmRobot.Element.Internal
{
	
	internal class Semaphore
	{

		public Semaphore() : this(0)
		{
		}

		public Semaphore(ulong cnt)
		{
			this.cnt = cnt;
		}

		public void Up()
		{
			ulong tcnt;
			lock (lockobj)
			{
				tcnt = cnt++;
			}

			if (tcnt == 0)
			{
				are.Set();
			}
		}

		public void Down()
		{
			while (true)
			{
				if (cnt == 0)
				{
					are.WaitOne();
				}

				lock (lockobj)
				{
					if (cnt > 0)
					{
						--cnt;
						break;
					}
				}
			}
		}

		private object lockobj = new object();
		private AutoResetEvent are = new AutoResetEvent(false);
		private ulong cnt;

	}

}
