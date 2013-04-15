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
using System.Security;

using System.Web;
using Lucene.Net.Search;
using System.Collections.Specialized;
using BzReader;

namespace RuralCafe
{
    /// <summary>
    /// Request handler for requests coming in to the local proxy.
    /// </summary>
    public class LocalRequestHandler : RequestHandler
    {
        // localProxy
        //new private RCLocalProxy _proxy;

        public static int DEFAULT_QUOTA;
        public static int DEFAULT_DEPTH;
        public static string DEFAULT_RICHNESS;

        /// <summary>
        /// Constructor for a local proxy's request handler.
        /// </summary>
        /// <param name="proxy">Proxy this request handler belongs to.</param>
        /// <param name="socket">Client socket.</param>
        public LocalRequestHandler(RCLocalProxy proxy, Socket socket)
            : base(proxy, socket)
        {
            _requestId = _proxy.NextRequestId;
            _proxy.NextRequestId = _proxy.NextRequestId + 1;
            _requestTimeout = LOCAL_REQUEST_PACKAGE_DEFAULT_TIMEOUT;
        }
        /// <summary>
        /// DUMMY used for request matching.
        /// Not the cleanest implementation need to instantiate a whole object just to match
        /// </summary> 
        private LocalRequestHandler(string itemId)
        {
            /*
            if (!Util.IsValidUri(uri))
            {
                // XXX: do nothing
            }
            else
            {*/
            _rcRequest = new RCRequest(itemId);
            //}

            /* XXX: don't think the dummy needs this
            // setup the header variables
            if (IsRuralCafeLocalSearch() || IsRuralCafeRemoteRequest())
            {
                ParseRuralCafeQuery();
            }
             */
        }
        /// <summary>Destructor.</summary>
        ~LocalRequestHandler()
        {
            // cleanup stuff
        }

        /// <summary>
        /// Override Equals() for handler matching.
        /// Just calls the base class.
        /// </summary>
        public override bool Equals(object obj)
        {
            return base.Equals(obj);
        }

        /// <summary>
        /// Overriding GetHashCode() from base object.
        /// Just use the hash code of the RequestUri.
        /// </summary>        
        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        /// <summary>
        /// Checks if the request is a RuralCafe specific request.
        /// </summary>
        /// <returns>True if it is, false if not.</returns>
        private bool IsRCRequest()
        {
            if (RequestUri.StartsWith("www.ruralcafe.net") ||
                RequestUri.StartsWith("http://www.ruralcafe.net"))
            {
                return true;
            }
            return false;
        }

        /// <summary>
        /// Main logic of RuralCafe LPRequestHandler.
        /// Called by Go() in the base RequestHandler class.
        /// </summary>
        public override int HandleRequest()
        {
            if (IsRCRequest())
            {
                return ServeRCPage();
            }

            if (IsBlacklisted(RequestUri))
            {
                LogDebug("ignoring blacklisted: " + RequestUri);
                SendErrorPage(HTTP_NOT_FOUND, "ignoring blacklisted", RequestUri);
                return (int)Status.Failed;
            }

            // XXX: this function will return true if the domain is wikipedia even if the file isn't in the archive
            // XXX: ideally, this would return true or false and then if the wiki page is properly served it returns
            // XXX: Otherwise we can stream/fetch it from the cache
            if (IsInWikiCache())
            {
                return ServeWikiURI();
            }

            // XXX: not cacheable, ignore, and log it instead of streaming for now
            // XXX: we could pass through this stuff directly, but it would require bypassing all blacklist/filtering
            if ((!IsGetOrHeadHeader() || !IsCacheable()) && 
                _proxy.NetworkStatus == (int)RCProxy.NetworkStatusCode.Online)
            {
                LogDebug("streaming: " + RequestUri + " to client.");

                long bytesSent = StreamTransparently("");
                _rcRequest.FileSize = bytesSent;

                return (int)Status.Completed;
            }

            if (IsCached(_rcRequest.CacheFileName))
            {
                // Try getting the mime type from the search index
                string contentType = GetMimeType(RequestUri);
                
                // try getting the content type from the file extension
                if (contentType.Equals("text/unknown"))
                    contentType = Util.GetContentTypeOfFile(_rcRequest.CacheFileName);

                // peek at the file, major hackery...
                string peekFile = System.IO.File.ReadAllText(_rcRequest.CacheFileName);
                if (peekFile.StartsWith("HTTP/1.1 301 Moved Permanently"))
                {
                    // don't bother sending HTTP OK headers
                }
                else
                {
                    SendOkHeaders(contentType);
                }

                _rcRequest.FileSize = StreamFromCacheToClient(_rcRequest.CacheFileName);
                if (_rcRequest.FileSize < 0)
                {
                    return (int)Status.Failed;
                }
                return (int)Status.Completed;
            }

            // XXX: not sure if this should even be here, technically for a cacheable file that's not cached, this is
            // XXX: behaving like a synchronous proxy
            // XXX: this is fine if we're online, fine if we're offline since it'll fail.. but doesn't degrade gradually

            // XXX: response time could be improved here if it downloads and streams to the client at the same time
            // XXX: basically, merge the DownloadtoCache() and StreamfromcachetoClient() methods into a new third method.
            // cacheable but not cached, cache it, then send to client if there is no remote proxy
            // if online, stream to cache, then stream to client.
            if (_proxy.NetworkStatus == (int)RCProxy.NetworkStatusCode.Online)
            {
                LogDebug("streaming: " + _rcRequest.GenericWebRequest.RequestUri + " to cache and client.");
                _rcRequest.GenericWebRequest.Proxy = null;
                long bytesDownloaded = _rcRequest.DownloadToCache(true);
                try
                {
                    FileInfo f = new FileInfo(_rcRequest.CacheFileName);
                    if (bytesDownloaded > -1 && f.Exists)
                    {
                        _rcRequest.FileSize = StreamFromCacheToClient(_rcRequest.CacheFileName);
                        if (_rcRequest.FileSize < 0)
                        {
                            return (int)Status.Failed;
                        }
                        return (int)Status.Completed;
                    }
                    else
                    {
                        return (int)Status.Failed;
                    }
                }
                catch
                {
                    // do nothing
                }
                return (int)Status.Failed;
            }
            
            if (_proxy.NetworkStatus != (int)RCProxy.NetworkStatusCode.Online)
            {
                /*
                // online, just stream it
                LogDebug("streaming: " + RequestUri + " to client.");

                long bytesSent = StreamTransparently();
                _rcRequest.FileSize = bytesSent;

                return (int)Status.Completed;
            }
            else
            {*/
                // Uncached links should be redirected to trotro-user.html?t=title&a=url when the system mode is slow or offline
                // Parse parameters to get title
                NameValueCollection qscoll = Util.ParseHtmlQuery(RequestUri);
                string redirectUri = qscoll.Get("trotro");
                if (redirectUri == null)
                {
                    // XXX: temporary title
                    redirectUri = RequestUri;
                    int pos = redirectUri.LastIndexOf("/");
                    while (pos > 20)
                    {
                        redirectUri = redirectUri.Substring(0, pos);
                        pos = redirectUri.LastIndexOf("/");
                    }
                    //redirectUri = "fake title";
                }
                SendRedirect(redirectUri, RequestUri);

                return (int)Status.Completed;
            }
            return (int)Status.Failed;
        }


        #region RC display and handling Methods

        /// <summary>
        /// Serve the RuralCafe page.
        /// XXX: this entire group of RC display/handling methods will most likely all be obsoleted in the next major revision.
        /// </summary>
        /// <returns>Request status.</returns>
        private int ServeRCPage()
        {
            if (IsIndex())
            {
                try
                {
                    ServeRCIndexPage();
                }
                catch (Exception)
                {
                    return (int)Status.Failed;
                }
                return (int)Status.Completed;
            }

            if (IsRemoteResult())
            {
                try
                {
                    ServeRCRemoteResultPage();
                }
                catch (Exception)
                {
                    return (int)Status.Failed;
                }
                return (int)Status.Completed;
            }

            if (IsResult())
            {
                try
                {
                    ServeRCResultPage();
                }
                catch (Exception)
                {
                    return (int)Status.Failed;
                }
                return (int)Status.Completed;
            }

            if (IsQueue())
            {
                try
                {
                    ServeRCQueuePage();
                }
                catch (Exception)
                {
                    return (int)Status.Failed;
                }
                return (int)Status.Completed;
            }

            if (IsRemovePage())
            {
                try
                {
                    RemoveRequest();
                }
                catch (Exception)
                {
                    return (int)Status.Failed;
                }
                return (int)Status.Completed;
            }

            if (IsAddPage())
            {
                try
                {
                    AddRequest();
                }
                catch (Exception)
                {
                    return (int)Status.Failed;
                }
                return (int)Status.Pending;
            }

            if (IsRequestNetworkStatus())
            {
                try
                {
                    ServeNetworkStatus();
                }
                catch (Exception)
                {
                    return (int)Status.Failed;
                }
                return (int)Status.Completed;
            }

            if (IsEtaRequest())
            {
                try
                {
                    ServeETARequest();
                }
                catch (Exception)
                {
                    return (int)Status.Failed;
                }
                return (int)Status.Completed;
            }

            if (IsRCHomePage())
            {
                try
                {
                    ServeRCSearchPage(((RCLocalProxy)_proxy).RCSearchPage);
                }
                catch (Exception)
                {
                    return (int)Status.Failed;
                }
                return (int)Status.Completed;
            }

            // everything else
            string fileName = RequestUri.Replace("http://www.ruralcafe.net/", "");
            fileName = fileName.Replace('/', Path.DirectorySeparatorChar);
            fileName = ((RCLocalProxy)_proxy).UIPagesPath + fileName;
            string contentType = Util.GetContentTypeOfFile(fileName);
            SendOkHeaders(contentType);
            //LogDebug(contentType);
            long bytesSent = StreamFromCacheToClient(fileName);
            if (bytesSent > 0)
            {
                return (int)Status.Completed;
            }

            SendErrorPage(HTTP_NOT_FOUND, "page does not exist", RequestUri);
            return (int)RequestHandler.Status.Failed;
        }

        /// <summary>Checks if the request is for the RuralCafe command to get the index page.</summary>
        bool IsIndex()
        {
            if (RequestUri.StartsWith("http://www.ruralcafe.net/request/index.xml"))
            {
                return true;
            }
            return false;
        }

        /// <summary>Checks if the request is for the RuralCafe command to get the queue.</summary>
        bool IsQueue()
        {
            if (RequestUri.StartsWith("http://www.ruralcafe.net/request/queue.xml"))
            {
                return true;
            }
            return false;
        }
        bool IsRemoteResult()
        {
            if (RequestUri.StartsWith("http://www.ruralcafe.net/request/search.xml"))
            {
                return true;
            }
            return false;
        }
        /// <summary>Checks if the request is for the RuralCafe command to get the search results.</summary>
        bool IsResult()
        {
            if (RequestUri.StartsWith("http://www.ruralcafe.net/request/result.xml"))
            {
                return true;
            }
            return false;
        }

        /// <summary>Checks if the request is for the RuralCafe homepage.</summary>
        private bool IsRCHomePage()
        {
            if (RequestUri.Equals("http://www.ruralcafe.net") ||
                RequestUri.Equals("http://www.ruralcafe.net/") ||
                RequestUri.Equals("www.ruralcafe.net") ||
                RequestUri.Equals("www.ruralcafe.net/"))
            //    RequestUri.Equals("http://www.ruralcafe.net/index.html") ||
            //    RequestUri.Equals("www.ruralcafe.net/index.html"))
            {
                return true;
            }
            return false;
        }

        /// <summary>Checks if the request is for the RuralCafe command to check whether the network is up.</summary>
        bool IsRequestNetworkStatus()
        {
            if (RequestUri.StartsWith("http://www.ruralcafe.net/request/status"))
            {
                return true;
            }
            return false;
        }
        /// <summary>Checks if the request is for the RuralCafe command to add a URI.</summary>
        bool IsAddPage()
        {
            if (RequestUri.StartsWith("http://www.ruralcafe.net/request/add?"))
            // if (RequestUri.StartsWith("http://www.ruralcafe.net/addpage="))
            {
                return true;
            } 
            return false;
        }
        /// <summary>Checks if the request is for the RuralCafe command to remove a URI.</summary>
        bool IsRemovePage()
        {
            if (RequestUri.StartsWith("http://www.ruralcafe.net/request/remove?"))
            // if (RequestUri.StartsWith("http://www.ruralcafe.net/removepage="))
            {
                return true;
            }
            return false;
        } 
        /// <summary>Checks if the request is for the RuralCafe command to remove all URIs for a client.</summary>
        bool IsRemoveAllPage()
        {
            if (RequestUri.StartsWith("http://www.ruralcafe.net/removeall"))
            {
                return true;
            }
            return false;
        }
        /// <summary>Checks if the request is for the RuralCafe command to add a URI.</summary>
        bool IsEtaRequest()
        {
            if (RequestUri.StartsWith("http://www.ruralcafe.net/request/eta"))
            // if (RequestUri.StartsWith("http://www.ruralcafe.net/addpage="))
            {
                return true;
            }
            return false;
        }

        /// <summary>
        /// Sends the RC Index page to the client.
        /// GET request will be sent to request/index.xml?c=6&n=4&s=root where
        /// c is the number of categories, the number of <category> required
        /// n is the maximum number of items in a category, the number of <item> allowed
        /// s is the upper level category which the user want to explore (the top level category is defined as 'root')
        /// </summary>
        void ServeRCIndexPage()
        {
            // Parse parameters
            NameValueCollection qscoll = Util.ParseHtmlQuery(RequestUri);
            int numItems = Int32.Parse(qscoll.Get("n"));
            int numCategories = Int32.Parse(qscoll.Get("c"));
            string searchString = qscoll.Get("s");

            SendOkHeaders("text/xml", "Cache-Control: no-cache" + "\r\n" +
                                      "Pragma: no-cache" + "\r\n" +
                                      "Expires: -1" + "\r\n");
            // not building the xml file yet, just sending the dummy page for now since there's really no top categories query yet
            // for the cache path
            StreamFromCacheToClient(((RCLocalProxy)_proxy).UIPagesPath + "index.xml");
        }

        /// <summary>
        /// Sends the frame page to the client.
        /// xml file of reuqests in the queue, content will be displayed in frame-offline-login.html
        /// Please organize the item in chronological order (older items first)
        /// GET request will be sent to request/queue.xml?u=a01&v=24-05-2012 where
        /// u is the user id
        /// v is used to specify date (in format of dd-mm-yyyy), or month (in format of mm-yyyy), or all (v=0). 
        /// If t is set to dd-mm-yyyy or mm-yyyy, only the requests submitted during that day/month will be returned. 
        /// If v is set to 0, all requests submitted by the user should be returned.
        /// </summary>
        void ServeRCQueuePage()
        {
            // Parse parameters
            NameValueCollection qscoll = Util.ParseHtmlQuery(RequestUri);
            int userId = Int32.Parse(qscoll.Get("u"));
            string date = qscoll.Get("v");

            // parse date
            bool queryOnDate = true;
            bool queryOnDay = false;
            int day = 1;
            int month = 1;
            int year = 1;
            DateTime queryDate = new DateTime();

            if (date.Equals("0"))
            {
                queryOnDate = false;
            }
            else if ((date.Length == 7) || (date.Length == 10))
            {
                if (date.Length <= 7)
                {
                    month = Int32.Parse(date.Substring(0, 2));
                    year = Int32.Parse(date.Substring(3, 4));
                    queryDate = new DateTime(year, month, day);
                }
                else
                {
                    queryOnDay = true;
                    day = Int32.Parse(date.Substring(0, 2));
                    month = Int32.Parse(date.Substring(3, 2));
                    year = Int32.Parse(date.Substring(6, 4));
                    queryDate = new DateTime(year, month, day);
                }
            }
            else
            {
                // malformed date
            }

            // get requests for this user
            List<LocalRequestHandler> requestHandlers = ((RCLocalProxy)_proxy).GetRequests(userId);
            // in reverse chronological order
            // requestHandlers.Reverse();

            string queuePageString = "<?xml version=\"1.0\" encoding=\"UTF-8\"?>" + "<queue>";
            int i = 1;
            if (requestHandlers != null)
            {
                foreach (LocalRequestHandler requestHandler in requestHandlers)
                {
                    string itemId = System.Security.SecurityElement.Escape("" + requestHandler.ItemId);
                    string linkAnchorText = System.Security.SecurityElement.Escape(requestHandler.AnchorText);
                    string linkTarget = System.Security.SecurityElement.Escape(requestHandler.RequestUri);

                    // XXX: temporary hack to change the way the Title is being displayed
                    Uri tempUri = new Uri(linkTarget);
                    linkAnchorText = tempUri.Segments.Last();
                    if (linkAnchorText == "/")
                        if (tempUri.Segments.Length > 1)
                            linkAnchorText = tempUri.Segments[tempUri.Segments.Length - 2];
                        else
                            linkAnchorText = tempUri.Host;

                    string statusString = "";
                    DateTime requestDate = requestHandler.StartTime;

                    if (!queryOnDate || (
                        (requestDate.Year == queryDate.Year) &&
                        (requestDate.Month == queryDate.Month)) &&
                        ((queryOnDay && (requestDate.Day == queryDate.Day)) || !queryOnDay))
                    {
                        /*
                        // if its a search request, translate to the google version to get the remotely returned google results
                        if (linkAnchorText.StartsWith("http://"))
                        {
                            linkTarget = linkAnchorText;
                        }
                        else
                        {
                            linkTarget = requestHandler.RCRequest.TranslateRCSearchToGoogle();
                        }*/

                        statusString = System.Security.SecurityElement.Escape(StatusCodeToString(requestHandler.RequestStatus));

                        // build the actual element
                        queuePageString = queuePageString +
                                        "<item id=\"" + itemId + "\">" +
                                            "<title>" + linkAnchorText + "</title>" +
                                            "<url>" + linkTarget + "</url>" +
                                            "<status>" + statusString + "</status>" +
                                            "<size>" + "unknown" + "</size>" +
                                        "</item>";
                        i++;
                    }
                }
            }

            queuePageString = queuePageString + "</queue>"; //end tag
            SendOkHeaders("text/xml", "Cache-Control: no-cache" + "\r\n" +
                                      "Pragma: no-cache" + "\r\n" +
                                      "Expires: -1" + "\r\n");
            SendMessage(queuePageString);
        }

        /// <summary>Serves the RuralCafe search page.</summary>
        private void ServeRCSearchPage(string pageUri)
        {
            string fileName = pageUri;
            int offset = pageUri.LastIndexOf('/');
            if (offset >= 0 && offset < (pageUri.Length - 1))
            {
                fileName = pageUri.Substring(offset + 1);
            }

            SendOkHeaders("text/html");
            StreamFromCacheToClient(((RCLocalProxy)_proxy).UIPagesPath + fileName);
        }

        /// <summary>
        /// Sends the frame page to the client.
        /// GET request will be sent to request/result.xml?n=5&p=1&s=searchstring where
        /// n is the maximum number of items per page, the number of <item> allowed in this file
        /// p is the current page number, if there are multipage pages, page number starts from 1, 2, 3...,
        /// s is the search query string
        /// </summary>
        void ServeRCResultPage()
        {
            // Parse parameters
            NameValueCollection qscoll = Util.ParseHtmlQuery(RequestUri);
            int numItemsPerPage = Int32.Parse(qscoll.Get("n"));
            int pageNumber = Int32.Parse(qscoll.Get("p"));
            string queryString = qscoll.Get("s");

            string resultsString = "<?xml version=\"1.0\" encoding=\"UTF-8\"?>";

            List<Lucene.Net.Documents.Document> tempLuceneResults = new List<Lucene.Net.Documents.Document>();
            List<Lucene.Net.Documents.Document> filteredLuceneResults = new List<Lucene.Net.Documents.Document>();
            HitCollection wikiResults = new HitCollection();
            int currentItemNumber = 0;
            if (queryString.Trim().Length > 0)
            {
                // Query the Wiki index
                wikiResults = Indexer.Search(queryString, RCLocalProxy.WikiIndices.Values, Indexer.MAX_SEARCH_HITS);

                // Query our RuralCafe index
                List<Lucene.Net.Documents.Document> luceneResults = IndexWrapper.Query(((RCLocalProxy)_proxy).IndexPath, queryString);

                // remove duplicates
                foreach (Lucene.Net.Documents.Document document in luceneResults)
                {
                    string documentUri = document.Get("uri");
                    string documentTitle = document.Get("title");

                    // ignore blacklisted domains
                    if (IsBlacklisted(documentUri))
                    {
                        continue;
                    }

                    bool exists = false;
                    foreach (Lucene.Net.Documents.Document tempDocument in tempLuceneResults)
                    {
                        string documentUri2 = tempDocument.Get("uri");
                        string documentTitle2 = tempDocument.Get("title");
                        if (String.Compare (documentUri, documentUri2) == 0 || String.Compare (documentTitle, documentTitle2)==0)
                        {
                            exists = true;
                            break;
                        }
                    }
                    if (exists == false)
                    {
                        currentItemNumber++;
                        tempLuceneResults.Add(document);
                        if ((currentItemNumber  > ((pageNumber - 1) * numItemsPerPage)) &&
                            (currentItemNumber < (pageNumber * numItemsPerPage) + 1))
                        {
                            filteredLuceneResults.Add(document);
                        }
                    }

                }
            }

            LogDebug(filteredLuceneResults.Count + " results");

            resultsString = resultsString + "<search total=\"" + currentItemNumber + "\">"; //laura: total should be the total number of results
            // Local Search Results
            for (int i = 0; i < filteredLuceneResults.Count; i++)
            {
                Lucene.Net.Documents.Document result = filteredLuceneResults.ElementAt(i);

                string uri = System.Security.SecurityElement.Escape(result.Get("uri")); // escape xml string
                string title = System.Security.SecurityElement.Escape(result.Get("title")); //escape xml string
                string displayUri = uri;
                string contentSnippet = "";

                // JAY: find content snippet here
                //contentSnippet = 
                if (uri.StartsWith("http://")) //laura: obmit http://
                    uri=uri.Substring(7);
                resultsString = resultsString +
                                "<item>" +
                                "<title>" + title + "</title>" +
                                "<url>" + uri + "</url>" +
                                "<snippet>" + contentSnippet + "</snippet>" +
                                "</item>";
            }

            resultsString = resultsString + "</search>";

            SendOkHeaders("text/xml", "Cache-Control: no-cache" + "\r\n" +
                                      "Pragma: no-cache" + "\r\n" +
                                      "Expires: -1" + "\r\n");
            SendMessage(resultsString);
        }


        void ServeRCRemoteResultPage()
        {
            if (_proxy.NetworkStatus == (int)RCProxy.NetworkStatusCode.Offline)
            {
                return;
            }

            // Parse parameters
            NameValueCollection qscoll = Util.ParseHtmlQuery(RequestUri);
            int numItemsPerPage = Int32.Parse(qscoll.Get("n"));
            int pageNumber = Int32.Parse(qscoll.Get("p"));
            string queryString = qscoll.Get("s");

            // Google search
            string googleSearchString = ConstructGoogleSearch(queryString);
            _rcRequest = new RCRequest(this, googleSearchString);

            //LogDebug("streaming: " + _rcRequest.GenericWebRequest.RequestUri + " to cache and client.");
            //_rcRequest.GenericWebRequest.Proxy = null;
            long bytesDownloaded = _rcRequest.DownloadToCache(true);
            try
            {
                FileInfo f = new FileInfo(_rcRequest.CacheFileName);
                if (bytesDownloaded > -1 && f.Exists)
                {
                    LinkedList<RCRequest> resultLinkUris = ExtractGoogleResults(_rcRequest);
                    string resultsString = "<?xml version=\"1.0\" encoding=\"UTF-8\"?>";
                    resultsString = resultsString + "<search total=\"" + resultLinkUris.Count.ToString() + "\">";
                    int currentItemNumber = 0;
                    foreach (RCRequest linkObject in resultLinkUris)
                    {
                        currentItemNumber++;
                        if ((currentItemNumber > ((pageNumber - 1) * numItemsPerPage)) &&
                            (currentItemNumber < (pageNumber * numItemsPerPage) + 1))
                        {
                            string uri = System.Security.SecurityElement.Escape(linkObject.Uri); //System.Security.SecurityElement.Escape(result.Get("uri")); // escape xml string
                            string title = System.Security.SecurityElement.Escape(linkObject.AnchorText); //System.Security.SecurityElement.Escape(result.Get("title")); //escape xml string
                            //string displayUri = uri;
                            string contentSnippet = "";

                            // XXX: find content snippet here
                            if (uri.StartsWith("http://")) //laura: obmit http://
                                uri = uri.Substring(7);
                            resultsString = resultsString +
                                            "<item>" +
                                            "<title>" + title + "</title>" +
                                            "<url>" + uri + "</url>" +
                                            "<snippet>" + contentSnippet + "</snippet>" +
                                            "</item>";
                        }
                    }

                    resultsString = resultsString + "</search>";

                    SendOkHeaders("text/xml", "Cache-Control: no-cache" + "\r\n" +
                                              "Pragma: no-cache" + "\r\n" +
                                              "Expires: -1" + "\r\n");
                    SendMessage(resultsString);
                }
                else
                {
                    // do nothing
                }
            }
            catch
            {
                // do nothing
            }
        }

        /// <summary>
        /// Translates a RuralCafe search to a Google one.
        /// </summary>
        /// <returns>Google search query.</returns>
        public string ConstructGoogleSearch(string searchString)
        {
            //string searchTerms = GetRCSearchField("textfield");
            string googleWebRequestUri = "http://www.google.com/search?hl=en&q=" +
                                        searchString.Replace(' ', '+') +
                                        "&btnG=Google+Search&aq=f&oq=";

            return googleWebRequestUri;
        }

        /*
        // XXX: obsolete, and moved from RCRequest temporarily
        /// <summary>
        /// Translates a RuralCafe search to a Google one.
        /// </summary>
        /// <returns>Google search query.</returns>
        public string TranslateRCSearchToGoogle()
        {
            NameValueCollection qscoll = Util.ParseHtmlQuery(Uri);
            //int userId = Int32.Parse(qscoll.Get("u"));
            string searchString = qscoll.Get("s");
            //string targetUri = qscoll.Get("a");
            //string refererUri = qscoll.Get("r");
            if (searchString == null)
            {
                searchString = "fake query";
            }
            //string searchTerms = GetRCSearchField("textfield");
            string googleWebRequestUri = "http://www.google.com/search?hl=en&q=" +
                                        searchString.Replace(' ', '+') +
                                        "&btnG=Google+Search&aq=f&oq=";

            return googleWebRequestUri;
        }*/


        /*
        // XXX: obsolete
        /// <summary>Query Lucene index of local pages and serve the results.</summary>
        private void ServeRCResultsPage(string queryString)
        {
            // RuralCafe local results
            int maxLength = 150;
            string tableHeader = "<table width=\"80%\" cellpadding=5 border=0>";
            string tableFooter = "</table>";
            string pageHeader = "<html><head></head><body><center>";
            string pageFooter = "</center></body></html>";
            string resultsString = "";

            List<Lucene.Net.Documents.Document> filteredLuceneResults = new List<Lucene.Net.Documents.Document>();
            HitCollection wikiResults = new HitCollection();
            if (queryString.Trim().Length > 0)
            {
                // Query the Wiki index
                wikiResults = Indexer.Search(queryString, RCLocalProxy.WikiIndices.Values, Indexer.MAX_SEARCH_HITS);

                // Query our RuralCafe index
                List<Lucene.Net.Documents.Document> luceneResults = IndexWrapper.Query(((RCLocalProxy)_proxy).IndexPath, queryString);
				
  				//List<Lucene.Net.Documents.Document> luceneResults = new List<Lucene.Net.Documents.Document>();             
				// remove duplicates
                foreach (Lucene.Net.Documents.Document document in luceneResults)
                {
                    string documentUri = document.Get("uri");
                    string documentTitle = document.Get("title");

                    // ignore blacklisted domains
                    if (IsBlacklisted(documentUri))
                    {
                        continue;
                    }

                    bool exists = false;
                    foreach (Lucene.Net.Documents.Document filteredDocument in filteredLuceneResults)
                    {
                        string documentUri2 = filteredDocument.Get("uri");
                        string documentTitle2 = filteredDocument.Get("title");
                        if (documentUri.Equals(documentUri2) || documentTitle.Equals(documentTitle2))
                        {
                            exists = true;
                            break;
                        }
                    }
                    if (exists == false)
                    {
                        filteredLuceneResults.Add(document);
                    }

                }
            }
            LogDebug(filteredLuceneResults.Count + " results");

            SendOkHeaders("text/html");
            SendMessage(pageHeader);

            LocalRequestHandler latestRequestHandler = ((RCLocalProxy)_proxy).GetLatestRequest(_clientAddress);
            // last requested page
            if (latestRequestHandler != null)
            {
                SendMessage("<form action=\"http://www.ruralcafe.net/request\" method=\"GET\"><table width=\"80%\" cellpadding=5 border=0><tr><td><input type=\"text\" maxlength=2048 size=55 name=\"textfield\" value=\"" + latestRequestHandler.SearchTermsOrURI() + "\"><input type=\"submit\" name=\"button\" value=\"Search\"><br>" + (filteredLuceneResults.Count + wikiResults.Count) + " results" + "</td></tr></table></form>");

                //SendMessage();//: \"" + lastRequestedPage.GetSearchTerms() + "\"<br>");

                // JAY: disabled queue remote request
                //string lastUri = lastRequestedPage.RequestUri.Replace("search", "request");
                //lastUri = lastUri + "&referrer=" + RequestUri;
                //lastUri = lastUri + "&button=Queue+Request&specificity=normal&richness=low&depth=normal";
                //string lastRequestedPageLink = "<a href=\"http://www.ruralcafe.net/addpage=" + 
                //    lastUri + 
                //    "\">[QUEUE REQUEST]</a><br>";
                //SendMessage(_clientSocket, lastRequestedPageLink);

                // JAY: Disabled query suggestions
                // suggested queries
                //string relatedQueries = GetRelatedQueriesLinks(lastRequestedPage.GetSearchTerms());
                //if (!relatedQueries.Contains("href")) 
                //{
                //    relatedQueries = "none";
                //}
                //SendMessage(_clientSocket, "Suggested queries:<br>" + relatedQueries);

                // no local results
                if (wikiResults.Count == 0 && filteredLuceneResults.Count == 0)
                {
                    SendMessage(pageFooter);
                    return;
                }

                // wikipedia results
                if (wikiResults.Count > 0)
                {
                    SendMessage(tableHeader);

                    SendMessage("<tr><td>Wikipedia Results (Top 2 shown)</td></tr>");

                    int i = 0;
                    while (wikiResults.MoveNext() && i < 2)
                    {
                        PageInfo hit = (PageInfo)wikiResults.Current;
                        string uri = "http://en.wikipedia.org/wiki/" + hit.ToString().Replace(' ', '_');
                        string title = hit.ToString();
                        string displayUri = uri;
                        string contentSnippet = "";

                        if (uri.Length > maxLength)
                        {
                            displayUri = uri.Substring(0, maxLength) + "...";
                        }
                        if (title.Length > maxLength)
                        {
                            title = title.Substring(0, maxLength) + "...";
                        }
                        else if (title.Length == 0)
                        {
                            title = "No Title";
                        }

                        // JAY: find content snippet here
                        //contentSnippet = 

                        resultsString = "<tr>" +
                                        "<td><a href=\"" + uri + "\">" + title + "</a><br><font color=\"green\">" + displayUri + "</font>" +
                                        "<br>" + contentSnippet + "</td>" +
                            //"<td>" + headers + "</td>" +
                            //"<td>" + uri + "</td>" + 
                                        "</tr>";
                        SendMessage(resultsString);

                        i++;
                    }

                    SendMessage(tableFooter);

                    SendMessage("<hr>");
                }

                SendMessage(tableHeader);

                SendMessage("<tr><td>Search Results</td></tr>");

                // Local Search Results
                for (int i = 0; i < filteredLuceneResults.Count; i++)
                {
                    Lucene.Net.Documents.Document result = filteredLuceneResults.ElementAt(i);

                    string uri = result.Get("uri");
                    string title = result.Get("title");
                    string displayUri = uri;
                    string contentSnippet = "";

                    if (uri.Length > maxLength)
                    {
                        displayUri = uri.Substring(0, maxLength) + "...";
                    }
                    if (title.Length > maxLength)
                    {
                        title = title.Substring(0, maxLength) + "...";
                    }
                    else if (title.Length == 0)
                    {
                        title = "No Title";
                    }

                    // JAY: find content snippet here
                    //contentSnippet = 

                    resultsString = "<tr>" +
                                    "<td><a href=\"" + uri + "\">" + title + "</a><br><font color=\"green\">" + displayUri + "</font>" +
                                    "<br>" + contentSnippet + "</td>" +
                        //"<td>" + headers + "</td>" +
                        //"<td>" + uri + "</td>" + 
                                    "</tr>";
                    SendMessage(resultsString);
                }

                SendMessage(tableFooter);

                SendMessage(pageFooter);
            }
        }
         */

        /*
        // XXX: obsolete
        /// <summary>Helper to translate RuralCafe to Lucene query format.</summary>
        private string TranslateRuralCafeToLuceneQuery()
        {
            string searchTerms = _rcRequest.GetRCSearchField("textfield");
            searchTerms = searchTerms.Replace('+', ' ');
            return searchTerms;
        }*/

        /*
        // XXX: obsolete
        /// <summary>Gets the search terms from the search URI for display</summary>
        private string SearchTermsOrURI()
        {
            if (IsRCLocalSearch() || IsRCRemoteQuery())
            {
                return _rcRequest.GetRCSearchField("textfield").Replace('+', ' ');
            }
            return RequestUri;
        }*/

        #endregion


        #region Proxy Control Methods

        /// <summary>
        /// Client asks proxy whether the network is on.
        /// </summary>
        private void ServeNetworkStatus()
        {
            SendOkHeaders("text/html");
            if (_proxy.NetworkStatus == (int)RCProxy.NetworkStatusCode.Offline)
            {
                SendMessage("offline");
            }
            else if (_proxy.NetworkStatus == (int)RCProxy.NetworkStatusCode.Slow)
            {
                SendMessage("cached");
            }
            else 
            {
                SendMessage("online");
            }
        }
        
        #endregion
        #region Queue Management Methods

        /// <summary>
        /// Queues this request.
        /// </summary>
        private void AddRequest()
        {
            // Parse parameters
            NameValueCollection qscoll = Util.ParseHtmlQuery(RequestUri);
            int userId = Int32.Parse(qscoll.Get("u"));
            string targetName = qscoll.Get("t");
            string targetUri = qscoll.Get("a");
            string refererUri = qscoll.Get("r");
            if (targetName == null)
            {
                targetName = "fake title";
            }
            if (targetUri == null)
            {
                // error
                targetUri = "";
                SendErrorPage(HTTP_NOT_FOUND, "malformed add request", "");
                return;
            }
            if (refererUri == null)
            {
                refererUri = targetUri;
            }

            _originalRequestUri = RequestUri;
            // preserve the original request status (for HandleLogRequest)
            int originalRequestStatus = _rcRequest.RequestStatus;
            _rcRequest = new RCRequest(this, targetUri, targetName, refererUri);
            _rcRequest.RequestStatus = originalRequestStatus;

            ((RCLocalProxy)_proxy).QueueRequest(userId, this);
            SendOkHeaders("text/html");
            SendMessage(RefererUri);
        }

        /// <summary>
        /// Removes the request from Ruralcafe's queue.
        /// </summary>
        private void RemoveRequest()
        {
            // Parse parameters
            NameValueCollection qscoll = Util.ParseHtmlQuery(RequestUri);
            int userId = Int32.Parse(qscoll.Get("u"));
            string itemId = qscoll.Get("i");

            LocalRequestHandler matchingRequestHandler = new LocalRequestHandler(itemId);
            ((RCLocalProxy)_proxy).DequeueRequest(userId, matchingRequestHandler);
            SendOkHeaders("text/html");
        }

        /// <summary>
        /// Gets the eta for a request in Ruralcafe's queue.
        /// </summary>
        private void ServeETARequest()
        {
            // Parse parameters
            NameValueCollection qscoll = Util.ParseHtmlQuery(RequestUri);
            int userId = Int32.Parse(qscoll.Get("u"));
            string itemId = qscoll.Get("i");

            // find the indexer of the matching request
            List<LocalRequestHandler> requestHandlers = ((RCLocalProxy)_proxy).GetRequests(userId);
            if (requestHandlers == null)
            {
                SendOkHeaders("text/html");
                SendMessage("0");
                return;
            }
            LocalRequestHandler matchingRequestHandler = new LocalRequestHandler(itemId);
            int requestIndex = requestHandlers.IndexOf(matchingRequestHandler);
            if (requestIndex < 0)
            {
                SendOkHeaders("text/html");
                SendMessage("0");
                return;
            }

            if (requestHandlers[requestIndex].RequestStatus == (int)RequestHandler.Status.Pending)
            {
                SendOkHeaders("text/html");
                SendMessage("-1");
                return;
            }
            string printableEta = requestHandlers[requestIndex].PrintableETA();
            
            SendOkHeaders("text/html");
            SendMessage(printableEta);
        }

        /// <summary>
        /// Helper method to get the ETA from the proxy this handler belongs to.
        /// </summary>
        /// <returns>ETA as a printable string.</returns>
        private string PrintableETA()
        {
            int eta = ((RCLocalProxy)_proxy).ETA(this);
            string etaString = "";
            if ((this.RequestStatus == (int)Status.Completed) ||
                (this.RequestStatus == (int)Status.Failed))
            {
                etaString = "0";
            }
            else if (eta < 60)
            {
                etaString = "< 1 min";
            }
            else
            {
                // now eta is in minutes
                eta = eta / 60;
                if (eta < 60)
                {
                    if (eta == 1)
                    {
                        etaString = "1 min";// eta.ToString() + " minute";
                    }
                    else
                    {
                        etaString = eta.ToString() + " min";
                    }
                }
                else
                {
                    // now eta is in hours
                    eta = eta / 60;
                    if (eta < 24)
                    {
                        etaString = eta.ToString() + " hours";
                    }
                    else
                    {
                        etaString = "> 1 day";
                    }
                }
            }
            //LogDebug("eta sent: " + etaString);
            return etaString;
        }

        #endregion

        /*
        #region Google Translation Fun

        private bool IsGoogleResultLink()
        {
            if (RequestUri.StartsWith("http://www.google.com/url?q="))
            {
                return true;
            }
            return false;
        }

        private void TranslateGoogleResultLink()
        {
            string newLinkUri = RequestUri.Replace("http://www.google.com/url?q=", "");
            int stopIndex = newLinkUri.IndexOf("&");
            newLinkUri = newLinkUri.Substring(0, stopIndex);
            RequestUri = newLinkUri;

            string fileName = RCRequest.UriToFilePath(RequestUri);
            HashPath = RCRequest.GetHashPath(fileName) + fileName;
            CacheFileName = Proxy.CachePath + HashPath;
        }

        #endregion*/

        #region Serve Wikipedia Page Methods

        /// <summary>
        /// Checks if the request is in the wikipedia cache.
        /// </summary>
        /// <returns>True or false if the request is in the wiki cache or not.</returns>
        private bool IsInWikiCache()
        {
            if (!((RCLocalProxy)_proxy).HasWikiIndices())
            {
                return false;
            }
            if (RequestUri.StartsWith("http://en.wikipedia.org/wiki/"))
            {
                // images aren't currently cached, just return no
                if (RequestUri.StartsWith("http://en.wikipedia.org/wiki/File:"))
                {
                    return false;
                }

                if (((RCLocalProxy)_proxy).WikiDumpPath.Equals(""))
                {
                    return false;
                }

                // XXX: need to check whether the request is actually in the cache
                return true;
            }
            return false;
        }

        /// <summary>
        /// Gets called to render a URI.
        /// </summary>
        /// <param name="e">Request parameters.</param>
        private void ServeWikiURLRenderPage(UrlRequestedEventArgs e)
        {
            string response = "Not found";
            string redirect = String.Empty;

            PageInfo page = null;

            if (page == null ||
                !e.Url.Equals(page.Name, StringComparison.InvariantCultureIgnoreCase))
            {
                HitCollection hits = Indexer.Search(e.Url, RCLocalProxy.WikiIndices.Values, 1);

                page = null;

                if (hits.Count > 0)
                {
                    page = hits[0];
                }
            }

            if (page != null)
            {
                response = page.GetFormattedContent();
                redirect = page.RedirectToTopic;
            }

            e.Redirect = !String.IsNullOrEmpty(redirect);
            e.RedirectTarget = redirect;
            e.Response = response;
        }

        /// <summary>
        /// Serves a Wikipedia page using the Wiki renderer.
        /// </summary>
        /// <returns>Status of the handler.</returns>
        private int ServeWikiURI()
        {
            string response = String.Empty;
            string redirectUrl = String.Empty;

            try
            {
                Uri uri = new Uri(RequestUri);
                // XXX: not sure if we need to Decode again
                UrlRequestedEventArgs urea = new UrlRequestedEventArgs(HttpUtility.UrlDecode(uri.AbsolutePath.Substring(6)));

                ServeWikiURLRenderPage(urea);

                redirectUrl = urea.Redirect ? urea.RedirectTarget : String.Empty;
                response = urea.Redirect ? "302 Moved" : urea.Response;

                byte[] sendBuf = Encoding.UTF8.GetBytes(response);
                SendWikiHeader("HTTP/1.1", sendBuf.Length, redirectUrl, _clientSocket);
                _clientSocket.Send(sendBuf);
            }
            catch (Exception)
            {
                return (int)Status.Failed;
            }
            return (int)Status.Completed;
        }

        /// <summary>
        /// Generates the URL for the given request term.
        /// </summary>
        /// <param name="term">The request term to generate the URL for.</param>
        /// <returns>The URL.</returns>
        public string GenerateUrl(string term)
        {
            return String.Format("http://en.wikipedia.org/wiki/{0}", HttpUtility.UrlEncode(term));
        }

        /// <summary>
        /// Sends HTTP header to the client for the wiki URI.
        /// </summary>
        /// <param name="httpVersion">Http version string</param>
        /// <param name="bytesCount">The number of bytes in the response stream</param>
        /// <param name="statusCode">HTTP status code</param>
        /// <param name="socket">The socket where to write to</param>
        public void SendWikiHeader(string httpVersion, int bytesCount, string redirectLocation, Socket socket)
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

        # endregion


        /// <summary>
        /// Stream the file from the cache to the client.
        /// </summary>
        /// <param name="decompress">Try to decompress then serve this file.</param>
        /// <param name="fileName">Name of the file to stream to the client.</param>
        /// <returns>Bytes streamed from the cache to the client.</returns>
        protected long StreamFromCacheToClient(string fileName)
        {
            long bytesSent = 0;

            // make sure the file exists.
            FileInfo f;
            try
            {
                int offset = fileName.LastIndexOf("?"); // laura: check get parameters
                string htmlQuery="";
                if (offset >= 0)
                {
                    htmlQuery = fileName.Substring(offset + 1);
                    fileName = fileName.Substring(0, offset);
                }
                f = new FileInfo(fileName);
                if (!f.Exists)
                {
                    LogDebug("error file doesn't exist: " + fileName);
                    return -1;
                }
            }
            catch (Exception e)
            {
                LogDebug("problem getting file info: " + fileName + " " + e.Message);
                return -1;
            }

            FileStream fs = null;
            try
            {
                /*
                // XXX: obsoleted
                // XXX: waste of computation, not a good tradeoff
                if (decompress)
                {
                    MemoryStream decompressedMs = Util.BZ2DecompressFile(fileName);
                    int streamLength = (int)decompressedMs.Length;
                    //byte[] decompressionBuf = new byte[length];
                    //decompressedMs.Read(decompressionBuf, 0, length);
                    _clientSocket.Send(decompressedMs.GetBuffer(), streamLength, 0); // XXX: this is an ugly hack, but max filesize is 32MB
                    bytesSent = streamLength;
                }
                else
                {*/
                    int offset = 0;
                    byte[] buffer = new byte[32]; // magic number 32
                    fs = f.Open(FileMode.Open, FileAccess.Read);
                    int bytesRead = fs.Read(buffer, 0, 32);
                    while (bytesRead > 0)
                    {
                        _clientSocket.Send(buffer, bytesRead, 0);
                        bytesSent += bytesRead;

                        bytesRead = fs.Read(buffer, 0, 32);

                        offset += bytesRead;
                    }
                //}
            }
            catch (Exception e)
            {
                SendErrorPage(HTTP_NOT_FOUND, "problem serving from RuralCafe cache: ", e.Message);
                bytesSent = -1;
            }
            finally
            {
                if (fs != null)
                {
                    fs.Close();
                }

            }
            return bytesSent;
        }

        /// <summary>
        /// Helper to get the mime type from the URI
        /// </summary>
        /// <param name="url"></param>
        /// <returns></returns>
        string GetMimeType(string uri)
        {
            List<Lucene.Net.Documents.Document> filteredResults = new List<Lucene.Net.Documents.Document>();

            string queryString = uri;
            if (queryString.Trim().Length > 0)
            {
                List<Lucene.Net.Documents.Document> results = IndexWrapper.Query(((RCLocalProxy)_proxy).IndexPath, queryString);

                string headerToken = "Content-Type:";
                // remove duplicates
                foreach (Lucene.Net.Documents.Document document in results)
                {
                    string documentUri = document.Get("uri");
                    if (documentUri.Equals(uri)) {
                        string headers = document.Get("headers");
                        // extract mime type
                        int i = headers.IndexOf(headerToken);
                        if (i < 0)
                            continue;
                        string chunk = headers.Substring(i + headerToken.Length);
                        int i2 = chunk.IndexOf("\r\n");
                        if (i2 < 0)
                            continue;
                        string mimeType = chunk.Substring(0, i2).Trim();
                        return mimeType;
                    }
                }
            }

            return "text/unknown";
        }
    }
}
