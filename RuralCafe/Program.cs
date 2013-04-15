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
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.IO;
using System.Threading;

namespace RuralCafe
{
    static class Program
    {
        private static int NETWORK_STATUS;

        // Local Proxy Settings
        private static IPAddress LOCAL_PROXY_IP_ADDRESS;
        private static int LOCAL_PROXY_LISTEN_PORT;
        private static string DEFAULT_SEARCH_PAGE;

        // Remote Proxy Settings
        private static IPAddress REMOTE_PROXY_IP_ADDRESS;
        private static int REMOTE_PROXY_LISTEN_PORT;

        // Remote Remote Proxy Settings (behind Amrita firewall)
        private static IPAddress GATEWAY_PROXY_IP_ADDRESS;
        private static int GATEWAY_PROXY_LISTEN_PORT;
        private static string GATEWAY_PROXY_LOGIN;
        private static string GATEWAY_PROXY_PASS;

        private static int DEFAULT_QUOTA;
        private static int DEFAULT_DEPTH;
        private static string DEFAULT_RICHNESS;
        private static int DEFAULT_LOW_WATERMARK;
        private static int MAXIMUM_DOWNLINK_SPEED;

        // Path Settings
        private static string LOCAL_PROXY_PATH = Directory.GetCurrentDirectory() 
			+ Path.DirectorySeparatorChar + "LocalProxy" + Path.DirectorySeparatorChar;
        private static string REMOTE_PROXY_PATH = Directory.GetCurrentDirectory() 
			+ Path.DirectorySeparatorChar + "RemoteProxy" + Path.DirectorySeparatorChar;
        private static string PACKAGE_PATH = "Packages" + Path.DirectorySeparatorChar;
        private static string LOGS_PATH = "Logs" + Path.DirectorySeparatorChar;
        private static string INDEX_PATH;
        private static string LOCAL_CACHE_PATH;
        private static string REMOTE_CACHE_PATH;
        private static string WIKI_DUMP_FILE;

        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        private static void Main()
        {
            StartRuralCafe();

            // for adding a list of URLs to the index
            //CacheIndexer.IndexSquidLog("urls.txt");

            // for analyzing search result pages
            //AnalysisTools.CountEmbeddedObjects();
        }

        /// <summary>
        /// Starts RuralCafe
        /// </summary>
        private static void StartRuralCafe()
        {
            Console.WindowWidth = Console.LargestWindowWidth;
            Console.WindowHeight = Console.LargestWindowHeight;
            Console.SetWindowPosition(0, 0);

            // fill extension map
            Util.FillExtMap();

            // load Configuration Settings
            LoadConfigFile();

            /* JJJ: Disabled query suggestions
            // load LDC data
            NGrams.Load1Grams();
            NGrams.Load2Grams();
            NGrams.Load3Grams();
            NGrams.Load4Grams();
            */

            // start the local proxy
            if (LOCAL_PROXY_IP_ADDRESS != null)
            {
                StartLocalProxy();
            }

            // start the remote proxy only if we're not starting the local proxy or both proxies are running on the same box
            if (REMOTE_PROXY_IP_ADDRESS != null)
            {
                StartRemoteProxy();
            }
        }

        /// <summary>
        /// Loads the configuration file for both proxies. 
        /// </summary>
        public static void LoadConfigFile()
        {
            // create reader, open config file
            string s = System.IO.File.ReadAllText("config.txt");

            // parse config file
            string[] lines = s.Split('\n');
            Dictionary<string, string> configSettings = new Dictionary<string, string>();
            foreach (string line in lines)
            {
                // ignore whitespace lines
                if (line.Trim().Equals(""))
                {
                    continue;
                }
                string[] configSetting = line.Split('=');
                configSettings.Add(configSetting[0].Trim(), configSetting[1].Trim());
            }

            try
            {
                // Local Proxy Settings
                LOCAL_PROXY_IP_ADDRESS = IPAddress.Parse(configSettings["LOCAL_PROXY_IP_ADDRESS"]);
                LOCAL_PROXY_LISTEN_PORT = Int32.Parse(configSettings["LOCAL_PROXY_LISTEN_PORT"]);

                INDEX_PATH = configSettings["INDEX_PATH"] +	Path.DirectorySeparatorChar;
                LOCAL_CACHE_PATH = configSettings["LOCAL_CACHE_PATH"] + Path.DirectorySeparatorChar;
                if (!INDEX_PATH.Contains(":\\"))
                {
                    INDEX_PATH = LOCAL_PROXY_PATH + INDEX_PATH;
                } 
                if (!LOCAL_CACHE_PATH.Contains(":\\"))
                {
                    LOCAL_CACHE_PATH = LOCAL_PROXY_PATH + LOCAL_CACHE_PATH;
                }
                WIKI_DUMP_FILE = Directory.GetCurrentDirectory() + Path.DirectorySeparatorChar+configSettings["WIKI_DUMP_DIR"]
					+ Path.DirectorySeparatorChar + configSettings["WIKI_DUMP_FILE"];

                /*
                string a = Directory.GetCurrentDirectory();
                char b = Path.DirectorySeparatorChar;
                string c = configSettings["WIKI_DUMP_DIR"];
                char d = Path.DirectorySeparatorChar;
                string e = configSettings["WIKI_DUMP_FILE"];
                */

                // remote proxy settings
                if (configSettings["REMOTE_PROXY_IP_ADDRESS"].Equals(""))
                {
                    REMOTE_PROXY_IP_ADDRESS = null;
                }
                else
                {
                    REMOTE_PROXY_IP_ADDRESS = IPAddress.Parse(configSettings["REMOTE_PROXY_IP_ADDRESS"]);
                }
                if (configSettings["REMOTE_PROXY_LISTEN_PORT"].Equals(""))
                {
                    REMOTE_PROXY_LISTEN_PORT = 0;
                }
                else
                {
                    REMOTE_PROXY_LISTEN_PORT = Int32.Parse(configSettings["REMOTE_PROXY_LISTEN_PORT"]);
                }

                REMOTE_CACHE_PATH = configSettings["REMOTE_CACHE_PATH"]+ Path.DirectorySeparatorChar;
				
                if (!REMOTE_CACHE_PATH.Contains(":\\")) {
                    REMOTE_CACHE_PATH = REMOTE_PROXY_PATH + REMOTE_CACHE_PATH;
                }

                // external proxy settings (to get through a firewall)
                if (configSettings["EXTERNAL_PROXY_IP_ADDRESS"].Equals(""))
                {
                    GATEWAY_PROXY_IP_ADDRESS = null;
                    GATEWAY_PROXY_LISTEN_PORT = 0;
                    GATEWAY_PROXY_LOGIN = "";
                    GATEWAY_PROXY_PASS = "";
                }
                else
                {
                    GATEWAY_PROXY_IP_ADDRESS = IPAddress.Parse(configSettings["EXTERNAL_PROXY_IP_ADDRESS"]);
                    GATEWAY_PROXY_LISTEN_PORT = Int32.Parse(configSettings["EXTERNAL_PROXY_LISTEN_PORT"]);
                    GATEWAY_PROXY_LOGIN = configSettings["EXTERNAL_PROXY_LOGIN"];
                    GATEWAY_PROXY_PASS = configSettings["EXTERNAL_PROXY_PASS"];
                }
                DEFAULT_SEARCH_PAGE = configSettings["DEFAULT_SEARCH_PAGE"];
                DEFAULT_QUOTA = Int32.Parse(configSettings["DEFAULT_QUOTA"]);
                DEFAULT_DEPTH = Int32.Parse(configSettings["DEFAULT_DEPTH"]);
                DEFAULT_RICHNESS = configSettings["DEFAULT_RICHNESS"];
                DEFAULT_LOW_WATERMARK = DEFAULT_QUOTA / 20;
                MAXIMUM_DOWNLINK_SPEED = Int32.Parse(configSettings["MAXIMUM_DOWNLOAD_SPEED"]);
                // XXX: hardcoded for now, stub
                if (configSettings["NETWORK_STATUS"] == "offline")
                {
                    NETWORK_STATUS = (int)RCProxy.NetworkStatusCode.Offline;
                }
                else if (configSettings["NETWORK_STATUS"] == "slow")
                {
                    NETWORK_STATUS = (int)RCProxy.NetworkStatusCode.Slow;
                }
                else
                {
                    NETWORK_STATUS = (int)RCProxy.NetworkStatusCode.Online;
                }

                // print some console messages
                Console.WriteLine("LOCAL_PROXY_IP_ADDRESS: " + LOCAL_PROXY_IP_ADDRESS);
                Console.WriteLine("LOCAL_PROXY_LISTEN_PORT: " + LOCAL_PROXY_LISTEN_PORT);
                Console.WriteLine("REMOTE_PROXY_IP_ADDRESS: " + REMOTE_PROXY_IP_ADDRESS);
                Console.WriteLine("REMOTE_PROXY_LISTEN_PORT: " + REMOTE_PROXY_LISTEN_PORT);
                Console.WriteLine("EXTERNAL_PROXY_IP_ADDRESS: " + GATEWAY_PROXY_IP_ADDRESS);
                Console.WriteLine("EXTERNAL_PROXY_LISTEN_PORT: " + GATEWAY_PROXY_LISTEN_PORT);
                Console.WriteLine("EXTERNAL_PROXY_LOGIN: " + GATEWAY_PROXY_LOGIN);
				Console.WriteLine("INDEX_PATH: " + INDEX_PATH);
				Console.WriteLine("LOCAL_CACHE_PATH: " + LOCAL_CACHE_PATH);
				Console.WriteLine("LOCAL_PROXY_PATH: " + LOCAL_PROXY_PATH);
				Console.WriteLine("REMOTE_CACHE_PATH: " + REMOTE_CACHE_PATH);
				Console.WriteLine("WIKI_DUMP_FILE: " + WIKI_DUMP_FILE);
				
            }
            catch (Exception)
            {
                Console.WriteLine("Error parsing config.txt");
                Application.Exit();
            }
        }

        /// <summary>
        /// Starts the local proxy.
        /// </summary>
        public static RCLocalProxy StartLocalProxy() {
            // create the proxy
            RCLocalProxy localProxy = new RCLocalProxy(LOCAL_PROXY_IP_ADDRESS, LOCAL_PROXY_LISTEN_PORT, 
                LOCAL_PROXY_PATH, INDEX_PATH, LOCAL_CACHE_PATH, WIKI_DUMP_FILE, PACKAGE_PATH, LOGS_PATH);

            // set the remote proxy
            localProxy.SetRemoteProxy(REMOTE_PROXY_IP_ADDRESS, REMOTE_PROXY_LISTEN_PORT);

            // XXX: currently this doesn't work if the remote proxy must be reached through a firewall/gateway.
            // XXX: it would be a chain of 2 proxies anyway and needs tunneling support
            if (REMOTE_PROXY_IP_ADDRESS != LOCAL_PROXY_IP_ADDRESS)
            {
                // set the gateway proxy info and login for the local proxy
                localProxy.SetGatewayProxy(GATEWAY_PROXY_IP_ADDRESS, GATEWAY_PROXY_LISTEN_PORT,
                                           GATEWAY_PROXY_LOGIN, GATEWAY_PROXY_PASS);
            }

            // set the RC search page
            localProxy.SetRCSearchPage(DEFAULT_SEARCH_PAGE);
            localProxy.NetworkStatus = NETWORK_STATUS;
            
            // load the blacklisted domains
            localProxy.LoadBlacklist("blacklist.txt");

            // set the default depth for all requests
            LocalRequestHandler.DEFAULT_QUOTA = DEFAULT_QUOTA;
            LocalRequestHandler.DEFAULT_DEPTH = DEFAULT_DEPTH;
            LocalRequestHandler.DEFAULT_RICHNESS = DEFAULT_RICHNESS;

            // start local listener thread
            Thread localListenerThread = new Thread(new ThreadStart(localProxy.StartListener));
            localListenerThread.Name = String.Format("localListenerThread");
            localListenerThread.Start();

            // start remote requester thread
            Thread localRequesterThread = new Thread(new ThreadStart(localProxy.StartDispatcher));
            localRequesterThread.Name = String.Format("localRequesterThread");
            localRequesterThread.Start();

            // listen for cc connection
            return localProxy;
        }

        /// <summary>
        /// Start the remote proxy.
        /// </summary>
        public static RCRemoteProxy StartRemoteProxy()
        {
            // create the proxy
            RCRemoteProxy remoteProxy = new RCRemoteProxy(REMOTE_PROXY_IP_ADDRESS, REMOTE_PROXY_LISTEN_PORT, 
                REMOTE_PROXY_PATH, REMOTE_CACHE_PATH, PACKAGE_PATH, LOGS_PATH);

            /*
            // XXX: buggy...
            // XXX: currently only used if both proxies are running on the same machine
            if (REMOTE_PROXY_IP_ADDRESS == LOCAL_PROXY_IP_ADDRESS)
            {
                // set the gateway proxy info and login for the remote proxy
                remoteProxy.SetGatewayProxy(GATEWAY_PROXY_IP_ADDRESS, GATEWAY_PROXY_LISTEN_PORT,
                                            GATEWAY_PROXY_LOGIN, GATEWAY_PROXY_PASS);
            }*/

            // set the maximum downlink speed to the local proxy
            remoteProxy.MAXIMUM_DOWNLINK_BANDWIDTH = MAXIMUM_DOWNLINK_SPEED;
            remoteProxy.NetworkStatus = NETWORK_STATUS;

            // load the blacklisted domains
            remoteProxy.LoadBlacklist("blacklist.txt");

            // set the default quota, depth, watermark for each request
            RemoteRequestHandler.DEFAULT_QUOTA = DEFAULT_QUOTA;
            RemoteRequestHandler.DEFAULT_MAX_DEPTH = DEFAULT_DEPTH;
            RemoteRequestHandler.DEFAULT_RICHNESS = DEFAULT_RICHNESS;
            RemoteRequestHandler.DEFAULT_LOW_WATERMARK = DEFAULT_LOW_WATERMARK;

            // start remote listener thread
            Thread remoteListenerThread = new Thread(new ThreadStart(remoteProxy.StartListener));
            remoteListenerThread.Name = String.Format("remoteListenerThread");
            remoteListenerThread.Start();

            // listen for cc connection
            return remoteProxy;
        }
    }
}
