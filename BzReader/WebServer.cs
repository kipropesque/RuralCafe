using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Web;

namespace BzReader
{
    /// <summary>
    /// Serves the HTTP content back to IE control
    /// </summary>
    internal class WebServer
    {
        /// <summary>
        /// The port we're listening on
        /// </summary>
        public readonly int Port;
        /// <summary>
        /// The TCP listener
        /// </summary>
        private TcpListener listener;
        /// <summary>
        /// The list of the currently executing threads
        /// </summary>
        private List<Thread> threads = new List<Thread>();
        /// <summary>
        /// Happens when the browser requests a resource
        /// </summary>
        public event UrlRequestedHandler UrlRequested;
        /// <summary>
        /// Singleton stuff
        /// </summary>
        private static WebServer instance;

        /// <summary>
        /// Singleton stuff
        /// </summary>
        public static WebServer Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = new WebServer();
                }

                return instance;
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="WebServer"/> class.
        /// </summary>
        private WebServer()
        {
            listener = new TcpListener(IPAddress.Loopback, 0);

            listener.Start();

            Port = ((IPEndPoint)listener.LocalEndpoint).Port;

            Thread t = new Thread(ProcessRequests);

            threads.Add(t);

            t.Start();
        }

        /// <summary>
        /// Stops this instance.
        /// </summary>
        public void Stop()
        {
            listener.Stop();

            lock (this)
            {
                foreach (Thread t in threads)
                {
                    if (t.IsAlive)
                    {
                        t.Abort();
                    }
                }
            }
        }

        /// <summary>
        /// Generates the URL for the given request term
        /// </summary>
        /// <param name="term">The request term to generate the URL for</param>
        /// <returns>The URL</returns>
        public string GenerateUrl(string term)
        {
            return String.Format("http://localhost:{0}/{1}", Port, HttpUtility.UrlEncode(term));
        }

        /// <summary>
        /// Sends HTTP header to the client
        /// </summary>
        /// <param name="httpVersion">Http version string</param>
        /// <param name="bytesCount">The number of bytes in the response stream</param>
        /// <param name="statusCode">HTTP status code</param>
        /// <param name="socket">The socket where to write to</param>
        public void SendHeader(string httpVersion, int bytesCount, string redirectLocation, Socket socket)
        {
            StringBuilder sb = new StringBuilder();

            sb.Append(httpVersion);
            sb.Append(" ");
            sb.Append(String.IsNullOrEmpty(redirectLocation) ? "200" : "302");
            sb.AppendLine();
            sb.AppendLine("Content-Type: text/html");
            
            if (!String.IsNullOrEmpty(redirectLocation))
            {
                sb.Append("Location: ");
                sb.AppendLine(GenerateUrl(redirectLocation));
            }

            sb.AppendLine("Accept-Ranges: bytes");
            sb.Append("Content-Length: ");
            sb.Append(bytesCount);
            sb.AppendLine();
            sb.AppendLine();

            socket.Send(Encoding.ASCII.GetBytes(sb.ToString()));
        }

        /// <summary>
        /// Processes requests coming from the web browser
        /// </summary>
        public void ProcessRequests()
        {
            Socket socket = null;

            try
            {
                socket = listener.AcceptSocket();

                Thread t = new Thread(ProcessRequests);

                lock (this)
                {
                    threads.Add(t);
                }

                t.Start();

                if (socket.Connected)
                {
                    socket.ReceiveTimeout = 200;
                    socket.SendTimeout = 200;
                    socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.DontLinger, true);

                    byte[] buf = new byte[2048];
                    int receivedLen = 0;

                    while ((receivedLen = socket.Receive(buf)) > 0)
                    {
                        string[] requestStrings = Encoding.ASCII.GetString(buf, 0, receivedLen).Split('\r', '\n');

                        if (requestStrings.Length < 2)
                        {
                            throw new Exception("Too few strings in the HTTP request");
                        }

                        string[] requestParts = requestStrings[0].Split(' ');

                        if (requestParts.Length != 3)
                        {
                            throw new Exception("Invalid number of HTTP request string parts");
                        }

                        if (!String.Equals(requestParts[0], "GET"))
                        {
                            throw new Exception("Unknown HTTP request type " + requestParts[0]);
                        }

                        string httpVersion = requestParts[2];
                        string url = requestParts[1];

                        string response = String.Empty;
                        string redirectUrl = String.Empty;

                        if (UrlRequested != null)
                        {
                            Uri uri = new Uri(new Uri(GenerateUrl(String.Empty)), url);

                            UrlRequestedEventArgs urea = new UrlRequestedEventArgs(HttpUtility.UrlDecode(uri.AbsolutePath.Substring(1)));

                            UrlRequested(this, urea);

                            redirectUrl = urea.Redirect ? urea.RedirectTarget : String.Empty;
                            response = urea.Redirect ? "302 Moved" : urea.Response;
                        }

                        byte[] sendBuf = Encoding.UTF8.GetBytes(response);

                        SendHeader(httpVersion, sendBuf.Length, redirectUrl, socket);

                        socket.Send(sendBuf);
                    }

                    lock (this)
                    {
                        threads.Remove(Thread.CurrentThread);
                    }
                }
            }
            catch (Exception)
            {
            }
            finally
            {
                try
                {
                    if (socket != null)
                    {
                        socket.Shutdown(SocketShutdown.Both);

                        socket.Close(0);
                    }
                }
                catch (Exception)
                {
                }
            }
        }
    }
}
