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
using System.Threading;

using Windows.Storage.Streams;
using Windows.Networking.Sockets;

namespace slg.RobotBase
{
    // http://www.dzhang.com/blog/2012/09/18/a-simple-in-process-http-server-for-windows-8-metro-apps
    // https://msdn.microsoft.com/library/windows/apps/hh770532 - capabilities and isolation
    // https://ms-iot.github.io/content/en-US/win10/samples/BlinkyWebServer.htm  - IoT example
    // C:\Projects\Win10\samples-develop\App2App WebServer\WebServerApp
    // C:\Projects\Win10\WinIoT\samples-develop\App2App WebServer\HttpServer\StartupTask.cs

    // WARNING: Universal Windows does not allow processes on the same machine to access the listeners. Use different machine to hit this server. 

    /// <summary>
    /// generic HTTP Server implementing GET and POST requests. This class contains all the plumbing.
    /// </summary>
    public abstract class HttpServerBase : IDisposable
    {
        private const uint BufferSize = 8192;
        private int port;
        private StreamSocketListener listener;
        private CancellationTokenSource tokenSource;

        public static int ConnectionsCount = 0; // for debugging

        /// <summary>
        /// override this method to implement specific behavior for your server. "postData" is null for GET requests
        /// </summary>
        /// <param name="localPath"></param>
        /// <param name="postData"></param>
        /// <returns>string</returns>
        protected abstract Task<string> GetPageContent(string localPath, string postData);

        #region Lifecycle

        public HttpServerBase(int serverPort)
        {
            port = serverPort;
            tokenSource = new CancellationTokenSource();
        }

        public virtual void StartServer()
        {
            listener = new StreamSocketListener();
            listener.Control.KeepAlive = true;
            listener.Control.NoDelay = true;

            listener.ConnectionReceived += async (s, e) => { await ProcessRequestAsync(e.Socket); };

            Task.Run(async () => {
                                    await listener.BindServiceNameAsync(port.ToString(), SocketProtectionLevel.PlainSocket);
                                 },
                     tokenSource.Token);
        }

        public virtual void StopServer()
        {
            tokenSource.Cancel();

            if (listener != null)
            {
                listener.Dispose();
                listener = null;
            }
        }

        public virtual void Dispose()
        {
            StopServer();
        }

        #endregion // Lifecycle

        #region Request and Response

        /// <summary>
        /// we get here when well known (listening) socket accepts connection.
        /// </summary>
        /// <param name="socket">new socket created for communication with the client</param>
        /// <returns></returns>
        private async Task ProcessRequestAsync(StreamSocket socket)
        {
            Interlocked.Increment(ref ConnectionsCount); // for debugging

            try
            {
                // this works for text only
                StringBuilder request = new StringBuilder();
                using (IInputStream input = socket.InputStream)
                {
                    byte[] data = new byte[BufferSize];
                    IBuffer buffer = data.AsBuffer();
                    uint dataRead = BufferSize;
                    while (dataRead == BufferSize)
                    {
                        IBuffer buf = await input.ReadAsync(buffer, BufferSize, InputStreamOptions.Partial);
                        request.Append(Encoding.UTF8.GetString(data, 0, (int)buffer.Length));
                        dataRead = buffer.Length;
                    }
                }

                if (request.Length > 0)
                {
                    string[] split = request.Replace("\r", "").ToString().Split('\n');
                    if(split.Length > 0)
                    { 
                        string requestMethod = split[0];
                        string[] requestParts = requestMethod.Split(' ');

                        if (requestParts.Length > 1)
                        {
                            requestMethod = requestParts[0];
                            string localPath = requestParts[1];

                            if (requestMethod == "GET")
                            {
                                // The format of an HTTP GET is to have the HTTP headers, followed by a blank line.
                                //
                                //  GET/default.html HTTP/1.1
                                //  Host: 172.16.1.201:9098
                                //  Connection:
                                //  keep-alive
                                //  Cache-Control: max-age=0
                                //  Accept: text/html,application/xhtml+xml,application/xml;q=0.9,*/*;q=0.8,image/webp
                                //  User-Agent: Mozilla/5.0 (iPad; CPU OS 9_2 like Mac OS X) AppleWebKit/600.1.4 (KHTML, like Gecko) CriOS/43.0.2357.61 Mobile/13C75 Safari/600.1.4
                                //  Accept-Encoding: gzip, deflate, sdch
                                //  Accept-Language: en-US,en;q=0.8,ru;q=0.6

                                await WriteGETResponseAsync(localPath, socket);
                            }
                            else if (requestMethod == "POST")
                            {
                                // The format of an HTTP POST is to have the HTTP headers, followed by a blank line,
                                // followed by the request body. The POST variables are stored as key-value pairs in the body:
                                //
                                //  POST /form HTTP/1.1
                                //  Host: 172.16.1.201:9098
                                //  Connection: keep-alive
                                //  Content-Length: 25
                                //  Origin: http://172.16.1.201:9098
                                //  Content-Type: application/x-www-form-urlencoded
                                //  Accept: text/html,application/xhtml+xml,application/xml;q=0.9,*/*;q=0.8,image/webp
                                //  Referer: http://172.16.1.201:9098/test
                                //  User-Agent: Mozilla/5.0 (iPad; CPU OS 9_2 like Mac OS X) AppleWebKit/600.1.4 (KHTML, like Gecko) CriOS/43.0.2357.61 Mobile/13C75 Safari/600.1.4
                                //  Accept-Encoding: gzip, deflate
                                //  Accept-Language: en-US,en;q=0.8,ru;q=0.6
                                //                                                   <== this is empty line
                                //  foo=foovalue&bar=barvalue

                                List<string> splitList = split.ToList();
                                int i = splitList.IndexOf("");
                                //string postData = splitList[i+1];   // foo=foovalue&bar=barvalue

                                StringBuilder sb = new StringBuilder();
                                for (i = i + 1; i < splitList.Count; i++)
                                {
                                    sb.AppendLine(splitList[i]);
                                }
                                string postData = sb.ToString();

                                await WritePOSTResponseAsync(localPath, socket, postData);
                            }
                            else
                            {
                                throw new InvalidDataException("HTTP method not supported: " + requestMethod + "   requestContent: " + localPath);
                            }
                        }
                    }
                }
            }
            catch (Exception exc)
            {
                Debug.WriteLine("HttpServer:ProcessRequestAsync() : " + exc);
            }

            Interlocked.Decrement(ref ConnectionsCount); // for debugging

            // socket.Dispose();     we are trying to keep-alive
        }

        #endregion // Request and Response

        #region GET and POST Response

        private async Task WriteGETResponseAsync(string localPath, StreamSocket socket)
        {
            string html = await GetPageContent(localPath, null);

            await WritePageContent(socket, html);
        }

        private async Task WritePOSTResponseAsync(string localPath, StreamSocket socket, string postData)
        {
            string html = await GetPageContent(localPath, postData);

            await WritePageContent(socket, html);
        }

        //private string headerFormat = "HTTP/1.1 200 OK\r\nContent-Length: {0}\r\nConnection: Keep-Alive\r\nKeep-Alive:timeout=5, max=100\r\n\r\n";
        private string headerFormat = "HTTP/1.1 200 OK\r\nContent-Length: {0}\r\nConnection: close\r\n\r\n";

        private async Task WritePageContent(StreamSocket socket, string html)
        {
            // respond with the html:
            using (IOutputStream output = socket.OutputStream)
            {
                //using (Stream resp = output.AsStreamForWrite())   -- cannot use this, will be blocked on Dispose()
                Stream resp = output.AsStreamForWrite();
                {
                    //resp.WriteTimeout = 1000;    // milliseconds  -- does not work here

                    // Look in the Data subdirectory of the app package
                    byte[] bodyArray = Encoding.UTF8.GetBytes(html);
                    MemoryStream stream = new MemoryStream(bodyArray);
                    string header = String.Format(headerFormat, stream.Length);
                    byte[] headerArray = Encoding.UTF8.GetBytes(header);

                    await resp.WriteAsync(headerArray, 0, headerArray.Length, tokenSource.Token);
                    await stream.CopyToAsync(resp, bodyArray.Length, tokenSource.Token);
                    await resp.FlushAsync(tokenSource.Token);
                }
            }
        }

        #endregion // GET and POST Response
    }
}
