using System;
using System.Collections.Generic;
using System.Text;

namespace cmRobot.Element.Internal
{

	internal interface ICommunicationJob
	{

		string GenerateCommand();

		void ProcessResponse(string response);

	}

}
