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
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace RuralCafe
{
    /// <summary>
    /// An abstract proxy class for implementing the local and remote proxies.
    /// </summary>
    public abstract class RCProxy
    {
        public enum NetworkStatusCode
        {
            Online = 0,
            Slow = 1,
            Offline = 2
        };

        // default names for the local and remote proxies
        public const string LOCAL_PROXY_NAME = "Local Proxy";
        public const string REMOTE_PROXY_NAME = "Remote Proxy";

        // max speed in bytes per second
        public const int UNLIMITED_BANDWIDTH = -1;
        public int MAXIMUM_DOWNLINK_BANDWIDTH = UNLIMITED_BANDWIDTH;

        // gatewayProxy
        protected WebProxy _gatewayProxy;

        // proxy settings
        protected IPAddress _listenAddress;
        protected int _listenPort;
        protected Logger _logger;
        protected string _cachePath;
        protected string _packagesCachePath;
        protected string _name;

        // online or offline
        protected int _networkStatus;

        // bandwidth measurement
        // lock object
        private static Object _downlinkBWLockObject = new Object();
        private static DateTime _bwStartTime = DateTime.Now;
        private static int _bwDataSent = 0;

        // blacklist
        protected List<string> _blacklistedDomains = new List<string>();

        // next item Id
        protected int _nextRequestId = 1;

        # region Property Accessors

        /// <summary>Path to the proxy's cache.</summary>
        public string CachePath
        {
            get { return _cachePath; }
        }
        /// <summary>Path to the proxy's packages.</summary>
        public string PackagesPath
        {
            get { return _packagesCachePath; }
        }
        /// <summary>The gateway proxy used to connect to the Internet.</summary>
        public WebProxy GatewayProxy
        {
            get { return _gatewayProxy; }
        }
        /// <summary>Path to the proxy's packages.</summary>
        public int NetworkStatus
        {
            get { return _networkStatus; }
            set { _networkStatus = value; }
        }
        /// <summary>Path to the proxy's packages.</summary>
        public int NextRequestId
        {
            get { return _nextRequestId; }
            set { _nextRequestId = value; }
        }

        # endregion

        /// <summary>
        /// Constructor for proxy base class.
        /// </summary>
        /// <param name="proxyName">Name of the proxy.</param>
        /// <param name="listenAddress">Address the proxy listens on.</param>
        /// <param name="listenPort">Port the proxy listens on.</param>
        /// <param name="proxyPath">Directory path the proxy is running in.</param>
        /// <param name="cachePath">Path to the proxy's cache</param>
        /// <param name="packageCachePath">Path to the proxy's packages</param>
        /// <param name="logsPath">Path to the proxy's logs</param>
        protected RCProxy(string name, IPAddress listenAddress, int listenPort, 
            string proxyPath, string cachePath, string packageCachePath, string logsPath)
        {
            _name = name;
            // setup proxy listener variables
            _listenAddress = listenAddress;
            _listenPort = listenPort;

            //create and initialize the logger
            _logger = new Logger(name, proxyPath + logsPath);

            bool success = false;

            // initialize the cache directory
            success = InitializeCache(cachePath);
            if (!success)
            {
                Console.WriteLine("Error initializing the " + name + " cache.");
            }

            // initialize the packages cache
            success = InitializePackagesCache(proxyPath + packageCachePath);
            if (!success)
            {
                Console.WriteLine("Error initializing the " + name + " packages cache.");
            }
        }

        /// <summary>
        /// Initializes the cache by making sure that the directory exists.
        /// </summary>
        /// <param name="cachePath">Path of the cache.</param>
        protected bool InitializeCache(string cachePath)
        {
            _cachePath = cachePath;

            try
            {
                if (!Directory.Exists(_cachePath))
                {
                    System.IO.Directory.CreateDirectory(_cachePath);
                }
            }
            catch (Exception)
            {
                return false;
            }
            return true;
        }

        /// <summary>
        /// Initializes the packages by making sure that the directory exists.
        /// </summary>
        /// <param name="cachePath">Path of the cache.</param>
        /// <returns>True or false for success or failure.</returns>
        protected bool InitializePackagesCache(string packagesCachePath)
        {
            _packagesCachePath = packagesCachePath;

            try
            {
                if (!Directory.Exists(_packagesCachePath))
                {
                    System.IO.Directory.CreateDirectory(_packagesCachePath);
                }
            }
            catch (Exception)
            {
                return false;
            }
            return true;
        }

        /// <summary>
        /// Sets the gateway proxy for the remote proxy.
        /// </summary>
        /// <param name="proxyAddress">The IP address of the gateway.</param>
        /// <param name="proxyPort">The port of the gateway.</param>
        /// <param name="login">The login for the gateway.</param>
        /// <param name="password">The password for the gateway.</param>
        public void SetGatewayProxy(IPAddress proxyAddress, int proxyPort, string login, string password)
        {
            if (proxyAddress == null)
            {
                _gatewayProxy = null;
            }
            else
            {
                _gatewayProxy = new WebProxy(proxyAddress.ToString(), proxyPort);
                _gatewayProxy.Credentials = new NetworkCredential(login, password);
            }
        }

        /// <summary>
        /// Abstract method for starting listener.
        /// </summary>
        public abstract void StartListener();

        /// <summary>
        /// Write message interface to the logger.
        /// </summary>
        /// <param name="requestId">Request ID.</param>
        /// <param name="entry">Message string.</param>
        public void WriteMessage(int requestId, string entry)
        {
            _logger.WriteMessage(requestId, entry);
        }

        /// <summary>
        /// Write debug interface to the logger for the proxies.
        /// </summary>
        /// <param name="entry">Message string.</param>
        protected void WriteDebug(string entry)
        {
            _logger.WriteDebug(0, entry);
        }

        /// <summary>
        /// Write debug interface to the logger for RequestHandlers.
        /// </summary>
        /// <param name="requestId">Request ID.</param>
        /// <param name="entry">Message string.</param>
        public void WriteDebug(int requestId, string str)
        {
            _logger.WriteDebug(requestId, str);
        }

        /// <summary>
        /// Checks to see if the proxy still has free downlink bandwidth.
        /// Used to rate limit the downlink transfer speed.
        /// </summary>
        /// <param name="bytesToSend">Number of bytes the proxy wants to send.</param>
        /// <returns>True or false if the proxy has free bandwidth or not.</returns>
        public bool HasDownlinkBandwidth(int bytesToSend)
        {
            lock (_downlinkBWLockObject)
            {
                if (MAXIMUM_DOWNLINK_BANDWIDTH == UNLIMITED_BANDWIDTH)
                {
                    return true;
                }
                TimeSpan elapsed = DateTime.Now - _bwStartTime;
                if (elapsed.TotalMilliseconds > 1000)
                {
                    // reset
                    _bwStartTime = DateTime.Now;
                    _bwDataSent = bytesToSend;
                    return true;
                }
                else
                {
                    int bps = _bwDataSent / (Convert.ToInt32(elapsed.TotalSeconds) + 1);
                    if (bps > MAXIMUM_DOWNLINK_BANDWIDTH)
                    {
                        return false;
                    }
                    else
                    {
                        _bwDataSent += bytesToSend;
                        return true;
                    }
                }
            }
        }

        /// <summary>
        /// Loads the blacklist from a file.
        /// </summary>
        /// <param name="fileName">The name of the blacklist file.</param>
        public void LoadBlacklist(string fileName)
        {
            try
            {
                string s = System.IO.File.ReadAllText(fileName);

                string[] domains = s.Split('\n');

                foreach (string domain in domains)
                {
                    if (domain.Trim().Length > 0)
                    {
                        _blacklistedDomains.Add(domain.Trim());
                    }
                }
            }
            catch (Exception)
            {
                // do nothing
            }
        }

        /// <summary>
        /// Checks whether a URI is blacklisted.
        /// </summary>
        /// <param name="requestUri">URI to check.</param>
        /// <returns>True or false for blacklisted or not.</returns>
        public bool IsBlacklisted(string requestUri)
        {
            // ignore all toolbar queries
            if (requestUri.Contains("toolbarqueries"))
            {
                return true;
            }

            // trim the "http://"
            if (requestUri.StartsWith("http://"))
            {
                requestUri = requestUri.Substring("http://".Length);
            }

            // check against all domains in the blacklist
            foreach (string domain in _blacklistedDomains)
            {
                if (requestUri.StartsWith(domain))
                {
                    return true;
                }
            }
            return false;
        }
    }
}
