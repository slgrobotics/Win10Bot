using System;
using System.Collections.Generic;
using System.Text;

namespace cmRobot.Element.Internal
{

	internal class SimpleCommunicationJob : ICommunicationJob
	{

		#region Ctors

		public SimpleCommunicationJob(string cmd)
		{
			this.cmd = cmd;
		}

		public SimpleCommunicationJob(string format, params object[] args)
		{
			this.cmd = String.Format(format, args);
		}

		#endregion


		#region Public Properties

		public string Response
		{
			get { return resp; }
		}

		#endregion


		#region Public Methods

		public string GenerateCommand()
		{
			return cmd;
		}

		public void ProcessResponse(string response)
		{
			resp = response;
		}

		#endregion


		#region Privates

		private string cmd;
		private string resp = null;

		#endregion

	}

}
