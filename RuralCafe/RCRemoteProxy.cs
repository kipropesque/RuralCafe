/*
   Copyright 2010 Jay Chen

   Licensed under the Apache License, Version 2.0 (the "License");
   you may not use this file except in compliance with the License.
   You may obtain a copy of the License at

       http://www.apache.org/licenses/LICENSE-2.0

   Unless required by applicable law or agreed to in writing, software
   distributed under the License is distributed on an "AS IS" BASIS,
   WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
   See the License for the specific language governing permissions and
   limitations under the License.

*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.IO;
using System.Threading;
using System.Collections;

namespace RuralCafe
{
    /// <summary>
    /// Remote proxy implementation, inherits from GenericProxy.
    /// </summary>
    public class RCRemoteProxy : RCProxy
    {
        /// <summary>
        /// Constructor for remote proxy.
        /// </summary>
        /// <param name="listenAddress">Address to listen for requests on.</param>
        /// <param name="listenPort">Port to listen for requests on.</param>
        /// <param name="proxyPath">Path to the proxy's executable.</param>
        /// <param name="cachePath">Path to the proxy's cache.</param>
        /// <param name="packagesPath">Path to the proxy's packages</param>
        /// <param name="logsPath">Path to the proxy's logs</param>
        public RCRemoteProxy(IPAddress listenAddress, int listenPort, string proxyPath, 
            string cachePath, string packagesPath, string logsPath)
            : base(REMOTE_PROXY_NAME, listenAddress, listenPort, proxyPath, 
            cachePath, packagesPath, logsPath)
        {
            _requestQueue = new List<string>();
        }

        /// <summary>
        /// Starts the listener for connections from local proxy.
        /// The remote proxy could potentially serve multiple local proxies.
        /// </summary>
        public override void StartListener()
        {
            WriteDebug("Started Listener on " +
                _listenAddress + ":" + _listenPort);
            try
            {
                // create a listener for the proxy port
                TcpListener sockServer = new TcpListener(_listenAddress, _listenPort);
                sockServer.Start();

                // loop and listen for the next connection request
                while (true)
                {
                    // accept connections on the proxy port (blocks)
                    Socket socket = sockServer.AcceptSocket();

                    // handle the accepted connection in a separate thread
                    RemoteRequestHandler requestHandler = new RemoteRequestHandler(this, socket);
                    Thread proxyThread = new Thread(new ThreadStart(requestHandler.Go));
                    proxyThread.Start();
                }
            }
            catch (SocketException ex)
            {
                WriteDebug("SocketException in StartRemoteListener, errorcode: " + ex.NativeErrorCode);
            }
            catch (Exception e)
            {
                WriteDebug("Exception in StartRemoteListener: " + e.StackTrace + " " + e.Message);
            }
        }

        # region Unused

        // requests from the local proxy
        public List<string> _requestQueue;

        /// <summary>
        /// Add a request to the queue.
        /// Unused and untested at the moment.
        /// </summary>
        /// <param name="requestUri">The request URI.</param>
        /// <returns>True if the request is added, and false if the URI is already in the queue.</returns>
        public bool AddRequest(string requestUri)
        {
            if (!_requestQueue.Contains(requestUri))
            {
                _requestQueue.Add(requestUri);
                return true;
            }
            
            return false;
        }

        /// <summary>
        /// Removes a request from the queue.
        /// Unused and untested at the moment.
        /// </summary>
        /// <param name="requestUri">The URI of the request.</param>
        /// <returns>True if the request is removed, and false if the URI is not in the queue.</returns>
        public bool RemoveRequest(string requestUri)
        {
            if (!_requestQueue.Contains(requestUri))
            {
                return false;
            }

            _requestQueue.Remove(requestUri);

            return true;
        }

        # endregion
    }
}
