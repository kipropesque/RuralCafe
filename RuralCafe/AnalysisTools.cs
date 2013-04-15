using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Threading;

namespace RuralCafe
{
    /// <summary>
    /// Tools for analyzing number of embedded objects and links on webpages.
    /// </summary>
    class AnalysisTools
    {
        /// <summary>
        /// Counts embedded objects and links on a page. 
        /// Currently only takes in simple log format of "google.txt".
        /// Unused.
        /// </summary>
        public static void CountEmbeddedObjects()
        {
            Console.WindowWidth = Console.LargestWindowWidth;
            Console.WindowHeight = Console.LargestWindowHeight;
            Console.SetWindowPosition(0, 0);

            // fill extension map
            Util.FillExtMap();

            // load Configuration Settings
            Program.LoadConfigFile();

            // start the local proxy
            //StartLocalProxy();

            // start the remote proxy
            RCRemoteProxy remoteProxy = Program.StartRemoteProxy();

            // parse the input file line by line
            // create reader & open file
            TextReader tr = new StreamReader("google.txt");

            uint linesParsed = 0;
            uint requestsMade = 0;
            string line = tr.ReadLine();
            string[] lineTokens;

            while (line != null)
            {
                linesParsed++;
                line = line.ToLower();

                lineTokens = line.Split('\t');
                // maximum number of tokens is 100
                if (lineTokens.Length >= 100 || lineTokens.Length < 19)
                {
                    Console.WriteLine("Error, too many tokens to fit in array");
                    // read the next line
                    line = tr.ReadLine();
                    continue;
                }

                // make sure that its actually a search query
                string urlRequest = lineTokens[18];
                if (urlRequest.StartsWith("http://www.google.com/search?"))
                {
                    if (urlRequest.Length > 200)
                    {
                        line = tr.ReadLine();
                        continue;
                    }

                    /* print to show that we're processing
                    if (DateTime.Now.Subtract(displayDotTimer).TotalMinutes > 0)
                    {
                        Console.Write(".");
                        displayDotTimer = DateTime.Now;
                    }*/
                    Console.WriteLine(urlRequest);

                    /*
                    // check if the query has already been processed before
                    if (IsCached(fileName))
                    {
                        // skip cached files
                        continue;
                    }
                    */

                    // process search query one at a time
                    RemoteRequestHandler requestHandler = new RemoteRequestHandler(remoteProxy, null);
                    if (Util.IsValidUri(urlRequest))
                    {
                        requestHandler.RCRequest = new RCRequest(requestHandler, urlRequest);
                        requestHandler.RCRequest.SetProxyAndTimeout(remoteProxy.GatewayProxy, RemoteRequestHandler.WEB_REQUEST_DEFAULT_TIMEOUT);
                        //requestHandler.PrefetchAnalysis("high", 1);

                        Thread.Sleep(1500);
                    }
                }
                requestsMade++;

                // stop after 1000 entries
                if (requestsMade == 1000)
                {
                    //SaveBenchmarkValues("linksOnResultsPage.out", RPRequestHandler.linksOnResultsPage);
                    //SaveBenchmarkValues("imagesOnResultsPage.out", RPRequestHandler.imagesOnResultsPage);
                    //SaveBenchmarkValues("imagesOnTargetPage.out", RPRequestHandler.imagesOnTargetPage);
                }

                // read the next line
                line = tr.ReadLine();
            }

            tr.Close();

            // keep the console open
            Console.ReadLine();
        }

        /// <summary>Helper method to save benchmark values</summary>
        private static void SaveBenchmarkValues(string fileName, List<int> values)
        {
            TextWriter tw = new StreamWriter(fileName, true);

            foreach (int number in values)
            {
                tw.Write(number + "\n");
            }

            tw.Close();
        }
    }
}
