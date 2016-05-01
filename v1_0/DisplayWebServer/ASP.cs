using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using slg.RobotBase.Data;

namespace slg.DisplayWebServer
{
    /// <summary>
    /// We support ASP-like syntax: <%=somefunction()%> - by direct replacement.
    /// </summary>
    internal class ASP
    {
        private HttpServer server;

        private string RedirectIfDisconnectedPage = "<html><head><META http-equiv=\"refresh\" content=\"1;URL=OpenConnection.html\"></head></html>";
        private string RedirectIfConnectedPage = "<html><head><META http-equiv=\"refresh\" content=\"1;URL=Default.html\"></head></html>";

        public ASP(HttpServer s)
        {
            server = s;
        }

        public string InterpretAspTags(string pageSource)
        {
            // we only work hard if there are tags to replace:
            if (pageSource.IndexOf("<%=") == -1)
                return pageSource;

            if (server.robot == null && pageSource.IndexOf("<%=RedirectIfDisconnected()%>") >= 0)
                return RedirectIfDisconnectedPage;

            if (server.robot != null && pageSource.IndexOf("<%=RedirectIfConnected()%>") >= 0)
                return RedirectIfConnectedPage;

            StringBuilder pageFinal = new StringBuilder(pageSource);

            // basic stuff:
            pageFinal.Replace("<%=RedirectIfDisconnected()%>", "");
            pageFinal.Replace("<%=RedirectIfConnected()%>", "");
            pageFinal.Replace("<%=DateTime.Now%>", DateTime.Now.ToString());

            // Connect tag:
            pageFinal.Replace("<%=ConnectSelector%>", server.robot == null ? ExpandOpenTag() : "");

            // robot state, sensors etc.:
            if (server.robot != null)
            {
                pageFinal.Replace("<%=joystickData%>", server.robot.currentJoystickData == null ? "" : server.robot.currentJoystickData.ToString());
                pageFinal.Replace("<%=robotState%>", server.robot.robotState == null ? "" : server.robot.robotState.ToString());
                pageFinal.Replace("<%=currentSensorsData%>", server.robot.currentSensorsData == null ? "" : server.robot.currentSensorsData.ToString());
            }

            return pageFinal.ToString();
        }

        public string ExpandOpenTag()
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("<select name='serialportid' class='selectpicker' data-style='btn-primary'>");
            foreach(SerialPortTuple port in server.serialPorts)
            {
                sb.AppendFormat("<option value='{0}'>{1}</option>", port.Id, port.Name);
            }
            sb.AppendLine("</select>");
            return sb.ToString();
        }
    }
}
