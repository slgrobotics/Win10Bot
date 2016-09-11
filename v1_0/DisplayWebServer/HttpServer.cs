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
using System.IO;
using System.Threading.Tasks;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Diagnostics;
using Windows.Storage.Streams;
using Windows.Networking.Sockets;

using slg.RobotBase;
using slg.RobotBase.Interfaces;
using slg.RobotBase.Data;

namespace slg.DisplayWebServer
{
    // WARNING: Universal Windows does not allow processes on the same machine to access the listeners. Use different machine to hit this server. 

    public sealed partial class HttpServer : HttpServerBase, IDisposable
    {
        public IDeviceOpener deviceOpener { get; set; }
        public IComputerManager computerManager { get; set; }
        public IRobotBase robot { get; set; }
        public List<SerialPortTuple> serialPorts { get; set; }

        private ASP asp;

        private string HtmlErrorStringFormat = "<html><head><title>Robot UI</title></head><body>Error: page '{0}' not found</body></html>";

        #region Lifecycle

        public HttpServer(int serverPort)
            : base(serverPort)
        {
            robot = null;
        }

        public void StartServer(IDeviceOpener devOp, IComputerManager compMan, List<SerialPortTuple> sps)
        {
            this.deviceOpener = devOp;
            this.computerManager = compMan;
            this.serialPorts = sps;
            asp = new ASP(this);

            base.StartServer();
        }

        public override void Dispose()
        {
            base.Dispose();
        }

        #endregion // Lifecycle

        #region Page Content

        /// <summary>
        /// Gets a page from disk and applies ASP tags interpreter to it.
        /// </summary>
        /// <param name="localPath"></param>
        /// <returns></returns>
        protected override async Task<string> GetPageContent(string localPath, string postData)
        {
            // it is either a command or a local page or file.
            localPath = await TryParseCommand(localPath, postData);

            if (String.IsNullOrWhiteSpace(localPath))
            {
                localPath = deviceOpener.IsDeviceOpen ? "/Default.html" : "/OpenConnection.html";
            }
            else if (localPath.StartsWith("##")) // command interpreter prepared content for us
            {
                return(localPath.Substring(2));
            }

            string pageContent = string.Empty;
            string pagePath = getPagePath(localPath);

            if (File.Exists(pagePath))
            {
                // have ASP.cs produce content, by reading the file and interpreting tags:
                pageContent = asp.InterpretAspTags(File.ReadAllText(pagePath));
            }
            else
            {
                // produce error message:
                pageContent = string.Format(HtmlErrorStringFormat, localPath);
            }
            return pageContent;
        }

        #endregion // Page Content

        #region State and Sensors data feed 

        public void DisplayRobotState(IRobotState robotState, IRobotPose robotPose)
        {
            Debug.WriteLine("HttpServer: DisplayRobotState()   state: " + robotState.ToString());
        }

        public void DisplayRobotSensors(ISensorsData sensorsData)
        {
            Debug.WriteLine("HttpServer: DisplayRobotSensors()   sensors: " + sensorsData.ToString());
        }

        #endregion // State and Sensors data feed 
    }
}
