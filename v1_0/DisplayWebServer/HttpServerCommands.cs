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
using System.Linq;
using System.Text;
using System.Net.Http;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Diagnostics;

using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.ApplicationModel.Background;
using Windows.ApplicationModel.AppService;
using Windows.Storage.Streams;
using Windows.System.Threading;
using Windows.Networking.Sockets;

using slg.RobotBase.Interfaces;
using slg.RobotBase.Data;
using slg.RobotBase;

namespace slg.DisplayWebServer
{
    // http://www.dzhang.com/blog/2012/09/18/a-simple-in-process-http-server-for-windows-8-metro-apps
    // https://msdn.microsoft.com/library/windows/apps/hh770532 - capabilities and isolation
    // https://ms-iot.github.io/content/en-US/win10/samples/BlinkyWebServer.htm  - IoT example
    // C:\Projects\Win10\samples-develop\App2App WebServer\WebServerApp

    // WARNING: Universal Windows does not allow processes on the same machine to access the listeners. Use different machine to hit this server. 

    public sealed partial class HttpServer : IDisposable
    {
        #region Commands interpreter

        /// <summary>
        /// tries to parse localPath as a command, and returns it (or null)
        /// </summary>
        /// <param name="localPath">string like "/Connect" or local path to a web page</param>
        /// <param name="postData">string like "serialportid=%5C%5..."</param>
        /// <returns></returns>
        private async Task<string> TryParseCommand(string localPath, string postData)
        {
            if (!String.IsNullOrWhiteSpace(localPath))
            {
                switch (localPath.Replace("/", ""))
                {
                    case "Connect":  // comes from OpenConnection.html
                                     // postData contains device ID, URL-encoded, with whitespace at the end:
                                     //      serialportid=%5C%5C%3F%5CBTHENUM%23%7B00001101-0000-1000-8000-00805f9b34fb%7D_LOCALMFG%26000f%238%26145bd395%260%26001153070031_C00000000%23%7B86e0d1e0-8089-11d0-9ce4-08003e301f73%7D
                        if (postData.TrimStart().StartsWith("serialportid=", StringComparison.OrdinalIgnoreCase))
                        {
                            postData = postData.Split(new char[] { '&' })[0].Trim();    // first pair is "serialPortId=..."
                            string serialPortId = WebUtility.UrlDecode(postData.Split(new char[] { '=' })[1]).Trim();
                            if (await deviceOpener.OpenDevice(serialPortId))
                            {
                                await Task.Delay(5000); // wait a bit to have the page sense the connected state and refresh to Default.html
                            }
                        }
                        localPath = null;   // will figure it out
                        break;

                    case "Shutdown":  // comes from OpenConnection.html or Default.html
                        computerManager.ShutdownComputer();
                        localPath = null;   // will figure it out
                        break;

                    case "Disconnect":  // comes from Default.html
                        await deviceOpener.CloseDevice();
                        localPath = null;   // will figure it out
                        break;

                    case "DateTime":  // AJAX call
                        localPath = "##" + DateTime.Now.ToString() 
                                         + (deviceOpener.IsDeviceOpen ? (" since start: " + getSinceStartTime()) : "")
                                         + " Count: " + HttpServerBase.ConnectionsCount;
                        break;

                    case "JoystickData":  // AJAX call
                        localPath = "##" + GetJoystickDataHTML();
                        break;

                    case "RobotState":  // AJAX call
                        localPath = "##" + GetRobotStateHTML();
                        break;

                    case "SensorsData":  // AJAX call
                        localPath = "##" + GetSensorsDataHTML();
                        break;

                    case "Command":  // comes from Default.html
                        break;

                    default:        // not a command
                        break;
                }
            }

            return localPath;   // can be null
        }

        private string getSinceStartTime()
        {
            TimeSpan sinceStart = DateTime.Now - deviceOpener.LastConnectionTime;
            string[] split = sinceStart.ToString().Split(new char[] { '.' });   // remove milliseconds
            return split[0];
        }

        #endregion // Commands interpreter

        #region Joystick Data

        private string GetJoystickDataHTML()
        {
            string ret = robot == null || robot.currentJoystickData == null ? "no data" : robot.currentJoystickData.ToString();

            return wrapP(ret);
        }

        #endregion // Joystick Data

        #region Robot State Data

        private string GetRobotStateHTML()
        {
            string ret = robot == null || robot.robotState == null ? "no data" : robot.robotState.ToString();

            if(robot != null && robot.robotPose != null)
            {
                ret += "    " + robot.robotPose.ToString();
            }

            return wrapP(ret);
        }

        #endregion // Robot State Data

        #region Sensors Data

        private string GetSensorsDataHTML()
        {
            string ret = robot == null || robot.robotState == null ? "no data" : robot.currentSensorsData.ToString();

            return wrapP(ret);
        }

        #endregion // Sensors Data

        private string wrapP(string html)
        {
            return "<p>" + html.Replace("    ", "<br/>") + "</p>";  // four spaces replaced by newline
        }
    }
}
