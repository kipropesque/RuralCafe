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
using Lucene.Net.Index;
using Lucene.Net.Documents;
using Lucene.Net.Analysis;
using Lucene.Net.Analysis.Standard;
using Lucene.Net.Search;
using Lucene.Net.QueryParsers;
using System.Net;

namespace RuralCafe
{
    /// <summary>
    /// Interacts with Lucene.Net to manage the cache index.
    /// Compatible with Lucene.Net version 2.9.1
    /// </summary>
    class IndexWrapper
    {
        public static bool EnsureIndexExists(string indexPath)
        {
            if (!Directory.Exists(indexPath))
            {
                System.IO.Directory.CreateDirectory(indexPath);
            }

            Lucene.Net.Store.FSDirectory directory = Lucene.Net.Store.FSDirectory.Open(new System.IO.DirectoryInfo(indexPath));
            if (!IndexReader.IndexExists(directory))
            {
                IndexWriter writer = new IndexWriter(directory, new StandardAnalyzer(Lucene.Net.Util.Version.LUCENE_CURRENT), true);
                writer.Close();
            }
            return true;
        }

        /// <summary>
        /// Deletes a document from our cacheindexer.
        /// To be used for cache coherence/maintainence.
        /// Unused. Untested.
        /// </summary>
        public static void DeleteDocument(string indexPath, string Uri)
        {
            Lucene.Net.Store.FSDirectory directory = Lucene.Net.Store.FSDirectory.Open(new System.IO.DirectoryInfo(indexPath));
            IndexReader indexReader = IndexReader.Open(directory, true);
            indexReader.DeleteDocuments(new Term("uri", Uri));
        }

        /// <summary>
        /// Adds a document to the index.
        /// </summary>
        /// <param name="Content">The page contents.</param>
        /// <param name="headers">HTTP response headers.</param>
        /// <param name="indexPath">Path to the Lucene index.</param>
        /// <param name="Title">Title of the page.</param>
        /// <param name="Uri">URI of the page.</param>
        public static void IndexDocument(string indexPath, string headers, string Uri, string Title, string Content)
        {
            // remove the document if it exists in the index to prevent duplicates
            //DeleteDocument(indexPath, Uri);

            IndexWriter writer = new IndexWriter(indexPath, new StandardAnalyzer(Lucene.Net.Util.Version.LUCENE_CURRENT), false);

            Document doc = new Document();
            doc.Add(new Field("uri", Uri, Field.Store.YES, Field.Index.NOT_ANALYZED));
            doc.Add(new Field("headers", headers, Field.Store.YES, Field.Index.NOT_ANALYZED));
            doc.Add(new Field("title", Title, Field.Store.YES, Field.Index.ANALYZED));
            doc.Add(new Field("content", Content, Field.Store.NO, Field.Index.ANALYZED));
            writer.AddDocument(doc);

            writer.Close();
        }

        /// <summary>
        /// Queries the index for a list of results.
        /// </summary>
        /// <param name="indexPath">Path to the Lucene index.</param>
        /// <param name="queryString">String to query the index for.</param>
        /// <returns>A List of Documents.</returns>
        public static List<Document> Query(string indexPath, string queryString)
        {
            List<Document> results = new List<Document>();

            Lucene.Net.Store.FSDirectory directory = Lucene.Net.Store.FSDirectory.Open(new System.IO.DirectoryInfo(indexPath));
            IndexSearcher searcher = new IndexSearcher(directory, true);
            QueryParser parser = new QueryParser(Lucene.Net.Util.Version.LUCENE_CURRENT, "content", new StandardAnalyzer(Lucene.Net.Util.Version.LUCENE_CURRENT));
            
            // the search function
            string searchQuery = "(" + queryString + ")";

            TopDocs topDocs = searcher.Search(parser.Parse(searchQuery), 1000);
            ScoreDoc[] hits = topDocs.scoreDocs;

            for (int i = 0; i < hits.Length; i++)
            {
                int docId = hits[i].doc;
                Document doc = searcher.Doc(docId);

                results.Add(doc);
            }
            
            searcher.Close();

            return results;
        }
    }

    /* JJJ: depricated, was used to load a cache from a squid proxy log
    // XXX: untested, and values are unset.
    // method for loading the squid cache contents into RuralCafe
    public static void IndexSquidLog(string squidLogFileName)
    {
        string localProxyPath = Directory.GetCurrentDirectory() + @"\LocalProxy\";
        string luceneIndexPath = localProxyPath + @"Lucene\";
        string proxyCachePath = localProxyPath + @"Cache\";
        long totalTime = 0;
        long indexingTime = 0;
        DateTime startTotalTime = DateTime.Now;

        // setup the proxy info
        //WebProxy squidProxy = new WebProxy(Program.EXTERNAL_PROXY_IP_ADDRESS.ToString(), Program.EXTERNAL_PROXY_LISTEN_PORT);
        //squidProxy.Credentials = new NetworkCredential(Program.EXTERNAL_PROXY_LOGIN, Program.EXTERNAL_PROXY_PASS);

        // ensure the lucene index exists
        bool created;
        created = CacheIndexer.EnsureIndexExists(luceneIndexPath);
        if (!created)
        {
            Console.WriteLine("error index file couldn't be created: " + luceneIndexPath);
            return;
        }

        // iterate through the lines in the log and then grab the url and then insert it into the index
        FileInfo f;
        try
        {
            f = new FileInfo(squidLogFileName);

            if (!f.Exists)
            {
                Console.WriteLine("error file doesn't exist: " + squidLogFileName);
                return;
            }
        }
        catch (Exception e)
        {
            Console.WriteLine("problem getting file info: " + squidLogFileName);
            return;
        }

        int counter = 0;
        string line;

        // Read the file and load it
        string url = "";
        System.IO.StreamReader file = new System.IO.StreamReader(squidLogFileName);
        while((line = file.ReadLine()) != null)
        {
            // get the url 
            url = line.Trim();
            if (!(url.EndsWith("htm") || url.EndsWith("html")))
            {
                continue;
            }
            // need to make sure its translated properly to where the localproxy would expect
            string fileName = RCRequest.UriToFilePath(url);
            string cacheFileName = proxyCachePath + fileName;

            // get the page
            HttpWebRequest webRequest;
            try
            {
                // create the webRequest
                Console.WriteLine("downloading: " + url);
                webRequest = (HttpWebRequest)WebRequest.Create(url);
                webRequest.Proxy = null;
                //webRequest.Proxy = squidProxy;
                //webRequest.Timeout = WEB_REQUEST_DEFAULT_TIMEOUT;

                // setup the file
                if (!Util.CreateDirectoryForFile(cacheFileName))
                {
                    Console.WriteLine("problem creating file: " + cacheFileName);
                }
                FileStream fs = Util.CreateFile(cacheFileName);
                if (fs == null)
                {
                    Console.WriteLine("problem creating file: " + cacheFileName);
                }

                // download the page
                try
                {
                    int bytesRead = 0;
                    Byte[] buffer = new Byte[32];
                    HttpWebResponse webResponse = (HttpWebResponse)webRequest.GetResponse();

                    // Create a response stream object.
                    Stream responseStream = webResponse.GetResponseStream();

                    // Read the response into a buffer.
                    bytesRead = responseStream.Read(buffer, 0, 32);
                    while (bytesRead != 0)
                    {
                        // Write the response to the cache
                        fs.Write(buffer, 0, bytesRead);

                        // Read the next part of the response
                        bytesRead = responseStream.Read(buffer, 0, 32);
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine("stream from server to cache failed: " + e.StackTrace + " " + e.Message);
                }
                finally
                {
                    if (fs != null)
                    {
                        fs.Close();
                    }
                }

                // add the file to Lucene
                string fileExtension = "";
                int offset1 = fileName.LastIndexOf(Path.DirectorySeparatorChar.ToString());
                int offset2 = fileName.LastIndexOf(".");
                if (offset2 > offset1)
                {
                    fileExtension = fileName.Substring(offset2);
                }

                if (fileExtension.Contains("htm"))
                {
                    // get the title and content of the page
                    string document = Util.ReadFileAsString(cacheFileName);
                    string title = Util.GetPageTitle(document);
                    string content = Util.GetPageContent(document);

                    // index the document
                    Console.WriteLine("Indexing: " + url);
                    DateTime startTime = DateTime.Now;
                    IndexDocument(luceneIndexPath, "Content-Type: text/html", url, title, content);
                    DateTime endTime = DateTime.Now;
                    TimeSpan someTime = endTime - startTime;
                    indexingTime += (long)someTime.TotalMilliseconds;
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("unable to create WebRequest: " + e.StackTrace + " " + e.Message);
                continue;
            }
            counter++;
        }

        file.Close();
        DateTime endTotalTime = DateTime.Now;
        totalTime = (long)(endTotalTime - startTotalTime).TotalSeconds;
        Console.WriteLine("files downloaded and indexed: " + counter);
        Console.WriteLine("total time: " + totalTime + " seconds");
        Console.WriteLine("indexing time: " + indexingTime + " seconds");
        Console.ReadLine();
    }*/
    /*
    public void Lucene(string indexPath)
    {
        //string sIndexLocation = IndexPath();
        // check to see we do have an index
        bool created;
        EnsureIndexExists(out created, sIndexLocation);
        // if not then create the index
        if (created)
        {
            InsertIndexData(sIndexPath);
        }
    }*/
}
