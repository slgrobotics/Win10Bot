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

            listener.ConnectionReceived += async (s, e) => {
                try
                {
                    await ProcessRequestAsync(e.Socket);
                }
                catch(Exception exc)
                {
                    ;
                }
            };

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

            bool keepAlive = false;

            try
            {
                do
                {
                    // this works on a PC but not or RPi:
                    //StringBuilder request = new StringBuilder();
                    //DataReader reader = new DataReader(socket.InputStream);
                    //reader.InputStreamOptions = InputStreamOptions.Partial;
                    //uint bytesRead = await reader.LoadAsync(BufferSize);  // reads what is available
                    //string strRead = reader.ReadString(reader.UnconsumedBufferLength);  // convert data to string
                    //request.Append(strRead);

                    //bytesRead = await reader.LoadAsync(BufferSize);  // reads a certain size of data
                    //if (bytesRead > 0)
                    //{
                    //    strRead = reader.ReadString(reader.UnconsumedBufferLength);  // get the string
                    //    request.Append(strRead);
                    //}

                    /*
                     * original version - works on PC and RPi: */
                    // this works for text only
                    StringBuilder request = new StringBuilder();
                    IInputStream input = socket.InputStream;
                    //using (IInputStream input = socket.InputStream)   -- cannot close anything in Keep-Alive mode
                    {
                        byte[] data = new byte[BufferSize];
                        IBuffer buffer = data.AsBuffer();
                        uint dataRead = BufferSize;
                        while (dataRead == BufferSize)
                        {
                            await Task.Delay(100);
                            IBuffer buf = await input.ReadAsync(buffer, BufferSize, InputStreamOptions.Partial);
                            request.Append(Encoding.UTF8.GetString(data, 0, (int)buffer.Length));
                            dataRead = buffer.Length;
                        }
                    }
                    /* */

                    if (request.Length > 0)
                    {
                        keepAlive = request.ToString().IndexOf("keep-alive", StringComparison.OrdinalIgnoreCase) >= 0;

                        string[] split = request.Replace("\r", "").ToString().Split('\n');
                        if (split.Length > 0)
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
                                    //  GET /default.html HTTP/1.1
                                    //  Host: 172.16.1.201:9098
                                    //  Connection: keep-alive
                                    //  Cache-Control: max-age=0
                                    //  Accept: text/html,application/xhtml+xml,application/xml;q=0.9,*/*;q=0.8,image/webp
                                    //  User-Agent: Mozilla/5.0 (iPad; CPU OS 9_2 like Mac OS X) AppleWebKit/600.1.4 (KHTML, like Gecko) CriOS/43.0.2357.61 Mobile/13C75 Safari/600.1.4
                                    //  Accept-Encoding: gzip, deflate, sdch
                                    //  Accept-Language: en-US,en;q=0.8,ru;q=0.6

                                    await WriteGETResponseAsync(localPath, socket, keepAlive);
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

                                    // parse Content-Length
                                    int contentLength = 0;

                                    foreach(string s in split)
                                    {
                                        if(s.StartsWith("Content-Length:"))
                                        {
                                            string[] clSplit = s.Split(new char[] { ':' });
                                            contentLength = int.Parse(clSplit[1].Trim());
                                            break;
                                        }
                                    }

                                    // post data follows empty line:
                                    List<string> splitList = split.ToList();
                                    int i = splitList.IndexOf("");
                                    //string postData = splitList[i+1];   // foo=foovalue&bar=barvalue

                                    StringBuilder sb = new StringBuilder();
                                    for (i = i + 1; i < splitList.Count; i++)
                                    {
                                        sb.AppendLine(splitList[i]);
                                    }
                                    string postData = sb.ToString().Trim();

                                    // when using jQuery and Bootstrap-select, post data might come separate from the header:
                                    if (contentLength > postData.Length)
                                    {
                                        Debug.WriteLine("HttpServerBase: POST second read: contentLength=" + contentLength + "  postData.Length=" + postData.Length);
                                        
                                        // this works on a PC but not or RPi:
                                        //bytesRead = await reader.LoadAsync((uint)contentLength);  // reads a certain size of data
                                        //if (bytesRead > 0)
                                        //{
                                        //    postData = reader.ReadString(reader.UnconsumedBufferLength);  // get the string
                                        //}

                                        // this works on RPi:
                                        {
                                            byte[] data = new byte[BufferSize];
                                            IBuffer buffer = data.AsBuffer();
                                            uint dataRead = BufferSize;
                                            while (dataRead == BufferSize)
                                            {
                                                await Task.Delay(100);
                                                IBuffer buf = await input.ReadAsync(buffer, BufferSize, InputStreamOptions.Partial);
                                                postData += Encoding.UTF8.GetString(data, 0, (int)buffer.Length);
                                                dataRead = buffer.Length;
                                            }
                                        }
                                    }

                                    await WritePOSTResponseAsync(localPath, socket, keepAlive, postData);
                                }
                                else
                                {
                                    throw new InvalidDataException("HTTP method not supported: " + requestMethod + "   requestContent: " + localPath);
                                }
                            }
                        }
                    }
                } while (keepAlive);
            }
            catch (ObjectDisposedException exc)
            {
                // we hope that client will keep connection alive, but they closed it. No big deal. They can do it.
                ;
            }
            catch (Exception exc)
            {
                Debug.WriteLine("HttpServer:ProcessRequestAsync() : " + exc);
            }

            Interlocked.Decrement(ref ConnectionsCount); // for debugging

            // socket.Dispose();     we are trying to keep-alive - cannot close anything in Keep-Alive mode
        }

        #endregion // Request and Response

        #region GET and POST Response

        private async Task WriteGETResponseAsync(string localPath, StreamSocket socket, bool keepAlive)
        {
            string html = await GetPageContent(localPath, null);

            DateTime? lastModifiedUtc = getLastModifiedUtc(localPath);
            string contentType = getContentType(localPath);

            await WritePageContent(socket, html, lastModifiedUtc, contentType, keepAlive, canBeCached(localPath));
        }

        private async Task WritePOSTResponseAsync(string localPath, StreamSocket socket, bool keepAlive, string postData)
        {
            string html = await GetPageContent(localPath, postData);

            DateTime? lastModifiedUtc = getLastModifiedUtc(localPath);
            string contentType = getContentType(localPath);

            await WritePageContent(socket, html, lastModifiedUtc, contentType, keepAlive, canBeCached(localPath));
        }

        private bool canBeCached(string localPath)
        {
            return localPath.EndsWith(".css") || localPath.EndsWith(".js");
        }

        /// <summary>
        /// to enable browser caching, provided Last Modified UTC date for selected folders only
        /// </summary>
        /// <param name="localPath"></param>
        /// <returns></returns>
        private DateTime? getLastModifiedUtc(string localPath)
        {
            if (!String.IsNullOrWhiteSpace(localPath) &&
                (localPath.Contains("Content/") 
                || localPath.Contains("Scripts/") 
                || localPath.Contains("fonts/")
                || localPath.Contains("favicon.ico")
                ))
            {
                FileInfo fi = new FileInfo(getPagePath(localPath));
                if (fi.Exists)
                {
                    return fi.LastWriteTimeUtc;
                }
            }
            return null;
        }

        /// <summary>
        /// gets file system path for a page
        /// </summary>
        /// <param name="localPath"></param>
        /// <returns></returns>
        public String getPagePath(string localPath)
        {
            string workDir = Directory.GetCurrentDirectory() + @"\slg.DisplayWebServer\Web";
            // something like this: C:\Projects\Win10\Win10Bot\RobotPlucky\bin\x64\Debug\AppX\slg.DisplayWebServer\Web
            // make sure your HTML, css and js files are all marked "Copy Always".

            if (localPath.StartsWith("/"))
            {
                localPath = localPath.Substring(1);
            }
            return Path.Combine(workDir, String.IsNullOrWhiteSpace(localPath) ? "Default.html" : localPath);
        }

        /// <summary>
        /// guesses content type based on file extension
        /// </summary>
        /// <param name="localPath"></param>
        /// <returns></returns>
        private string getContentType(string localPath)
        {
            if(localPath.EndsWith(".js"))
            {
                return "application/x-javascript";
            }
            else if(localPath.EndsWith(".css"))
            {
                return "text/css";
            }
            else if (localPath.EndsWith(".ico"))
            {
                return "image/x-icon";
            }
            else
                return "text/html";
        }

        /// <summary>
        /// Writes page content back to socket
        /// </summary>
        /// <param name="socket"></param>
        /// <param name="html"></param>
        /// <param name="lastModifiedUtc"></param>
        /// <param name="contentType"></param>
        /// <param name="keepAlive"></param>
        /// <param name="canBeCached"></param>
        /// <returns></returns>
        private async Task WritePageContent(StreamSocket socket, string html, DateTime ? lastModifiedUtc, string contentType, bool keepAlive, bool canBeCached)
        {
            // respond with the html:
            IOutputStream output = socket.OutputStream;
            //using (IOutputStream output = socket.OutputStream)   -- cannot close anything in Keep-Alive mode
            {
                //using (Stream resp = output.AsStreamForWrite())   -- cannot use this, will be blocked on Dispose()
                Stream respStream = output.AsStreamForWrite(262144);    // 256K for uninterrupted send of most sizes
                {
                    //resp.WriteTimeout = 1000;    // milliseconds  -- does not work here

                    // Look in the Data subdirectory of the app package
                    byte[] bodyArray = Encoding.UTF8.GetBytes(html);
                    MemoryStream stream = new MemoryStream(bodyArray);
                    string header = generateHeader(lastModifiedUtc, contentType, stream.Length, keepAlive, canBeCached);
                    byte[] headerArray = Encoding.UTF8.GetBytes(header);

                    await respStream.WriteAsync(headerArray, 0, headerArray.Length, tokenSource.Token);
                    await stream.CopyToAsync(respStream, bodyArray.Length, tokenSource.Token);
                    await respStream.FlushAsync(tokenSource.Token);
                }
            }

            if (!keepAlive)
            {
                output.Dispose();
            }
        }

        /// <summary>
        /// generates HTTP Response Header based on options provided
        /// </summary>
        /// <param name="lastModifiedUtc"></param>
        /// <param name="contentType"></param>
        /// <param name="contentLength"></param>
        /// <param name="keepAlive"></param>
        /// <param name="canBeCached"></param>
        /// <returns></returns>
        private string generateHeader(DateTime? lastModifiedUtc, string contentType, long contentLength, bool keepAlive, bool canBeCached)
        {
            StringBuilder sb = new StringBuilder();

            /*
                HTTP/1.1 200 OK
                Content-Type: application/x-javascript
                Last-Modified: Sat, 06 Aug 2016 20:55:34 GMT
                Accept-Ranges: bytes
                ETag: "cb5276e024f0d11:0"
                Server: Microsoft-IIS/10.0
                X-Powered-By: ASP.NET
                Date: Sun, 07 Aug 2016 06:23:01 GMT
                Content-Length: 86351
             */
            sb.Append("HTTP/1.1 200 OK\r\n");
            sb.AppendFormat("Content-Type: {0}\r\n", contentType);
            if (lastModifiedUtc.HasValue)
            {
                // we need to send this to allow browser's caching to work:
                sb.AppendFormat("Last-Modified: {0}\r\n", lastModifiedUtc.Value.ToString("r")); // RFC1123 date
            }
            sb.Append("Accept-Ranges: none\r\n");   // we don't support ranges
            sb.Append("Server: SLG Robotics .NET Custom\r\n");
            sb.AppendFormat("Content-Length: {0}\r\n", contentLength);

            if (keepAlive)
            {
                sb.Append("Connection: Keep-Alive\r\n");
                sb.Append("Keep-Alive:timeout=5, max=100\r\n");
            }
            else
            {
                sb.Append("Connection: close\r\n");
            }

            if (!canBeCached)
            {
                sb.Append("Cache-Control: no-cache, no-store, must-revalidate\r\n");
            }

            sb.Append("\r\n");

            return sb.ToString();
        }

        #endregion // GET and POST Response
    }
}
