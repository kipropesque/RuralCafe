using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Lucene.Net.Index;
using Lucene.Net.Documents;
using Lucene.Net.Analysis;
using Lucene.Net.Analysis.Standard;
using Lucene.Net.Search;
using Lucene.Net.QueryParsers;

namespace RuralCafe
{
    class LuceneIndex
    {
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

        private static void EnsureIndexExists(out bool created, string sIndexPath)
        {
            created = false;
            if (!IndexReader.IndexExists(sIndexPath))
            {
                IndexWriter writer = new IndexWriter(sIndexPath,
                    new StandardAnalyzer(), true);
                created = true;
                writer.Close();
            }
            created = true;
        }

        /*
        private void InsertIndexData(string sIndexPath)
        {
            IndexWriter writer = new IndexWriter(sIndexPath, new StandardAnalyzer(), false);

            //Lets insert all data - for this example I'm using 
            //some fake stuff, but you could of course easily index anything - say data from 
            //a database, generated from files/web spidering etc

            IndexDoc(writer, "About Hockey", "hockey", "Hockey is a cool sport which I really like, bla bla");
            IndexDoc(writer, "Some great players", "hockey", "Some of the great players from Sweden - well Peter Forsberg, Mats Sunding, Henrik Zetterberg");
            IndexDoc(writer, "Soccer info", "soccer", "Soccer might not be as fun as hockey but it's also pretty fun");
            IndexDoc(writer, "Players", "soccer", "From Sweden we have Zlatan Ibrahimovic and Henrik Larsson. They are the most well known soccer players");
            IndexDoc(writer, "1994", "soccer", "I remember World Cup 1994 when Sweden took the bronze. we had great players. players , bla bla");

            writer.Optimize();
            writer.Close();

        }
         */

        public static void IndexDocument(string indexPath, string Uri, string Title, string Content)
        {
            bool created;
            EnsureIndexExists(out created, indexPath);
            // if not then create the index
            if (created)
            {
                IndexWriter writer = new IndexWriter(indexPath, new StandardAnalyzer(), false);

                Document doc = new Document();
                //Title = "try";
                //Content = "pink blue orange";
                doc.Add(new Field("uri", Uri, Field.Store.YES, Field.Index.UN_TOKENIZED));
                doc.Add(new Field("title", Title, Field.Store.YES, Field.Index.TOKENIZED));
                // XXX: not sure what the difference is
                doc.Add(new Field("content", Content, Field.Store.NO, Field.Index.TOKENIZED));
                //doc.Add(new Field("content", Content));
                writer.AddDocument(doc);

                writer.Optimize();
                writer.Close();
            }
        }

        public static List<Document> Query(string indexPath, string queryString)
        {
            List<Document> results = new List<Document>();
            try
            {
                IndexSearcher searcher = new IndexSearcher(indexPath);

                QueryParser parser = new QueryParser("content", new StandardAnalyzer());
                
                // search function
                string searchQuery = "(" + queryString + ")";

                Hits hits = searcher.Search(parser.Parse(searchQuery));
                /*
                List<string> results = new List<string>();
                for (int i = 0; i < hits.Length(); i++)
                {
                    Document result = hits.Doc(i);
                
                    // add fields of the result to the storage object
                    string uri = result.Get("uri");
                    string title = result.Get("title");
                
                    QueryResult result = QueryResult(uri, title);
                    results.Add(result);
                }
                */
                for (int i = 0; i < hits.Length(); i++)
                {
                    Document result = hits.Doc(i);
                    results.Add(result);
                    // add fields of the result to the storage object
                    //string uri = result.Get("uri");
                    //string title = result.Get("title");
                    //Console.WriteLine("XYZ: " + uri + " " + title);
                }

                searcher.Close();
            }
            catch (Exception e)
            {
                // do nothing index doesn't have anything in it yet
            }

            return results;
        }
    }

    /*
    class QueryResult
    {
        public string _title;
        public string _uri;

        public QueryResult(string title, string uri)
        {
        }
    }
    */
}
