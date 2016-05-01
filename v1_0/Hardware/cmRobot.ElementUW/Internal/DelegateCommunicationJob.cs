using System;
using System.Collections.Generic;
using System.Text;

namespace cmRobot.Element.Internal
{

	internal class DelegateCommunicationJob : ICommunicationJob
	{

		#region Public Types

		public delegate string GenerateCommandDelegate();

		public delegate void ProcessRsponseDelegate(string response);

		#endregion


		#region Ctors

		public DelegateCommunicationJob(
			GenerateCommandDelegate commandCallback, ProcessRsponseDelegate responseCallback)
		{
			this.commandCallback = commandCallback;
			this.responseCallback = responseCallback;
		}

		public DelegateCommunicationJob(GenerateCommandDelegate commandCallback)
		{
			this.commandCallback = commandCallback;
			this.responseCallback = null;
		}

		#endregion


		#region Public Methods

		public string GenerateCommand()
		{
			return commandCallback();
		}

		public void ProcessResponse(string response)
		{
            responseCallback?.Invoke(response);
        }

		#endregion


		#region Privates

		private GenerateCommandDelegate commandCallback;
		private ProcessRsponseDelegate responseCallback;

		#endregion

	}

}
