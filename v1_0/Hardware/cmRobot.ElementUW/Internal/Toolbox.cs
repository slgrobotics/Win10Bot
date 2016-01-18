using System;
using System.Collections.Generic;
using System.Text;

namespace cmRobot.Element.Internal
{

	internal static class Toolbox
	{

		public static void AssertInRange(string name, int val, int min, int max)
		{
			if (val < min || val > max)
			{
				string msg = String.Format("value must be between {0} and {1}", min, max);
				throw new ArgumentOutOfRangeException(name, msg);
			}
		}

		public static void AssertInRange(int val, int min, int max)
		{
			if (val < min || val > max)
			{
				string msg = String.Format("value must be between {0} and {1}", min, max);
				throw new ArgumentOutOfRangeException(msg);
			}
		}

	}

}
