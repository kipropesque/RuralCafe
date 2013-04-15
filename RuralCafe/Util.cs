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
using System.Web;
using System.Net;
using System.Collections.Specialized;

namespace RuralCafe
{
    /// <summary>
    /// A set of utility functions for manipulating files and directories, 
    /// getting file extensions and mime types, and page contents.
    /// </summary>
    class Util
    {
        // map from file extension to HTTP MIME type
        private static Dictionary<string, string> _extMap = new Dictionary<string, string>();

        private static Object filesystemLock = new Object();

        /// <summary>
        /// Creates a directory for a file (locked method).
        /// JAY: this lock is probably poorly implemented.
        /// </summary>
        /// <param name="fileName">Name of the file.</param>
        /// <returns>True or false for success or failure.</returns>
        public static bool CreateDirectoryForFile(string fileName)
        {
            if (fileName == null || fileName == "")
            {
                return false;
            }

            try
            {
                lock (filesystemLock)
                {
                    int offset = fileName.LastIndexOf(Path.DirectorySeparatorChar.ToString());
                    if (offset > 0)
                    {
                        string currPath = fileName.Substring(0, offset + 1);
                        if (!Directory.Exists(currPath) &&
                            !File.Exists(currPath))
                        {
                            System.IO.Directory.CreateDirectory(currPath);
                        }
                    }
                }
            }
            catch (Exception)
            {
                return false;
            }

            return true;
        }

        public static string ReplaceXML(string somestring)
        {
            return somestring;
        }

        /// <summary>
        /// Deletes a file.
        /// </summary>
        /// <param name="fileName">Name of the file.</param>
        /// <returns>True or false for success or failure.</returns>
        public static bool DeleteFile(string fileName)
        {
            try
            {
                FileInfo f = new FileInfo(fileName);
                if (f.Exists)
                {
                    f.Delete();
                    return true;
                }
            }
            catch (Exception)
            {
                return false;
            }

            return true;
        }

        /// <summary>
        /// Creates a file.
        /// </summary>
        /// <param name="fileName">The name of the file.</param>
        /// <returns>Returns the FileStream for the created file.</returns>
        public static FileStream CreateFile(string fileName)
        {
            FileStream fs;
            try
            {
                fs = new FileStream(fileName, FileMode.Create);
                if (fs.CanSeek)
                {
                    fs.Seek(0, SeekOrigin.Begin);
                }
            }
            catch (Exception)
            {
                return null;
            }

            return fs;
        }        

        /// <summary>
        /// Helper function to get the file size on disk of a page.
        /// </summary>
        /// <param name="fileName">Name of the file.</param>
        /// <returns>-1 for non-existent file.</returns>
        public static long GetFileSize(string fileName)
        {
            FileInfo f;
            try
            {
                f = new FileInfo(fileName);

                if (!f.Exists)
                {
                    return -1;
                }

                if (f.Length >= 0)
                {
                    return f.Length;
                }
            }
            catch (Exception)
            {
                return -1;
            }

            return -1;
        }

        public static NameValueCollection ParseHtmlQuery(string requestUri)
        {
            string htmlQuery = "";
            int offset = requestUri.LastIndexOf("?");
            if (offset >= 0)
            {
                htmlQuery = requestUri.Substring(offset + 1);
            }
            return HttpUtility.ParseQueryString(htmlQuery);
        }


        /// <summary>
        /// Gets the file extension from the file name.
        /// </summary>
        /// <param name="fileName">File name to parse.</param>
        /// <returns>File extension as a string.</returns>
        public static string GetFileExtensionFromFileName(string fileName)
        {
            string fileExtension = fileName;
            
            int offset1 = fileExtension.LastIndexOf("?");
            if (offset1 >= 0)
            {
                fileExtension = fileExtension.Substring(0, offset1);
            }

            //int offset2 = fileName.LastIndexOf(Path.DirectorySeparatorChar.ToString());
            int offset2 = fileExtension.LastIndexOf(".");
            if (offset2 > 0)
            {
                fileExtension = fileName.Substring(offset2);
            }
            return fileExtension;
        }
        /// <summary>
        /// Gets the file extension from a URI.
        /// </summary>
        /// <param name="stringUri">URI to parse.</param>
        /// <returns>File extension as a string.</returns>
        public static string GetFileExtension(string stringUri)
        {
            string fileExtension = "";
            int offset1 = stringUri.LastIndexOf("/");
            int offset2 = stringUri.LastIndexOf(".");
            if (offset2 > offset1)
            {
                fileExtension = stringUri.Substring(offset2);
            }
            // throw away query terms
            fileExtension = fileExtension.Split('?')[0];
            fileExtension = fileExtension.Split('#')[0];
            return fileExtension;
        }

        /// <summary>
        /// Returns the HTTP "Content-Type" of an extension.
        /// </summary>
        /// <param name="extension">File extension.</param>
        /// <returns>Content type of the extension.</returns>
        public static string GetContentType(string extension)
        {
            if (_extMap.ContainsKey(extension))
            {
                return _extMap[extension];
            }
            // JAY: not sure how squid or whatever does this
            // content/unknown, but the problem is that after caching how do we know the content type without the extension in place
            return "text/html";
        }

        /// <summary>
        /// Tries to return the content type of a file based on its extension.
        /// Also tries to peek inside the file for HTML headers.
        /// </summary>
        /// <param name="fileName">Name of the file.</param>
        /// <returns>Content type of the file.</returns>
        public static string GetContentTypeOfFile(string fileName)
        {
            string fileExtension = GetFileExtensionFromFileName(fileName);

            /*
            if (fileExtension.StartsWith(".asp") || fileExtension.StartsWith(".php"))
            {
                return "text/html";
            }
            */

            if (!fileExtension.Equals(""))
            {
                return GetContentType(fileExtension);
            }

            // only try xhtml and html for pages without extensions
            try
            {
                // make sure file exists
                FileInfo f = new FileInfo(fileName);
                if (f.Exists)
                {
                    // read beginning of the file
                    string fileContents = ReadFileAsString(fileName).Trim();
                    if (fileContents != null)
                    {
                        if (fileContents.StartsWith("<!DOCTYPE html") ||
                            fileContents.StartsWith("<html>"))
                        {
                            return "text/html";
                        }
                    }
                }
            }
            catch (Exception)
            {
                // do nothing
            }

            return "content/unknown";
        }

        /// <summary>
        /// Checks if the URI is parseable by RuralCafe.
        /// </summary>
        /// <param name="rcRequest">A RCRequest object.</param>
        /// <returns>True or false for parseable or not.</returns>
        public static bool IsParseable(RCRequest rcRequest)
        {
            string contentType = GetContentTypeOfFile(rcRequest.CacheFileName);

            if (contentType.Contains("htm"))
            {
                return true;
            }

            return false;
        }
        
        /// <summary>
        /// Checks if the URI is valid.
        /// </summary>
        /// <param name="Uri">URI.</param>
        /// <returns>True or false for valid or not.</returns>
        public static bool IsValidUri(string Uri)
        {
            // blank
            if (Uri.Trim().Length == 0)
            {
                return false;
            }

            // malformed
            try
            {
                HttpWebRequest tempRequest = (HttpWebRequest)WebRequest.Create(Uri);
            }
            catch (Exception)
            {
                return false;
            }

            // blank
            if (Uri.Equals("http://"))
            {
                return false;
            }

            return true;
        }

        /// <summary>
        /// Reads in a file as a string for ease of parsing.
        /// </summary>
        /// <param name="fileName">Name of the file.</param>
        /// <returns>String containing the file's contents. If there is a problem return an empty string.</returns>
        public static string ReadFileAsString(string fileName)
        {
            string str = "";

            try
            {
                FileInfo f = new FileInfo(fileName);
                if (!f.Exists)
                {
                    return str;
                }

                // open the file stream
                FileStream fs = f.Open(FileMode.Open, FileAccess.Read);

                // loop and get the bytes we need if we couldn't get it in one go
                StreamReader r = new StreamReader(fs);
                string t;
                while ((t = r.ReadLine()) != null)
                {
                    // append to the string
                    str += t;
                }

                r.Close();
                fs.Close();
            }
            catch (Exception)
            {
                // do nothing
            }

            return str;
        }

        /*
        /// <summary>
        /// Decompress a bz2 file and return the memorystream.
        /// </summary>
        /// <param name="fileName">Name of the file to decompress.</param>
        /// <returns>The file contents in a memorystream.</returns>
        public static MemoryStream BZ2DecompressFile(string fileName)
        {
            MemoryStream ms = new MemoryStream();
            FileStream bzipFileFs = new FileStream(fileName, FileMode.Open);
            ICSharpCode.SharpZipLib.BZip2.BZip2.Decompress(bzipFileFs, ms);
            return ms;
        }*/
        public static byte[] GetBytes(string str)
        {
            byte[] bytes = new byte[str.Length * sizeof(char)];
            System.Buffer.BlockCopy(str.ToCharArray(), 0, bytes, 0, bytes.Length);
            return bytes;
        }

        public static string GetString(byte[] bytes)
        {
            char[] chars = new char[bytes.Length / sizeof(char)];
            System.Buffer.BlockCopy(bytes, 0, chars, 0, bytes.Length);
            return new string(chars);
        }

        /// <summary>
        /// Gets the contents of the page. (dummy, output = input)
        /// </summary>
        /// <param name="pageContent">Page content.</param>
        /// <returns>String with the page content.</returns>
        public static string GetPageContent(string pageContent)
        {
            return pageContent;
        }
        
        /// <summary>
        /// Gets the title from a page.
        /// </summary>
        /// <param name="pageContent">Page content.</param>
        /// <returns>String containing the page title.</returns>
        public static string GetPageTitle(string pageContent)
        {
            int index1 = pageContent.IndexOf("<title>");
            int index2 = pageContent.IndexOf("</title>");
            // make sure the title tag exists
            if (index1 < 0 || index2 < 0)
            {
                return "";
            }
            // make sure there's something between the two indices
            index1 += "<title>".Length;
            if (index1 >= index2)
            {
                return "";
            }

            string title = pageContent.Substring(index1, index2 - index1);

            return title;
        }

        /// <summary>
        /// Strips a string of all HTML tags.
        /// </summary>
        /// <param name="source">Page content.</param>
        /// <returns>String containing the stripped text.</returns>
        public static string StripTagsCharArray(string source)
        {
            char[] array = new char[source.Length];
            int arrayIndex = 0;
            bool inside = false;

            for (int i = 0; i < source.Length; i++)
            {
                char let = source[i];
                if (let == '<')
                {
                    inside = true;
                    continue;
                }
                if (let == '>')
                {
                    inside = false;
                    continue;
                }
                if (!inside)
                {
                    array[arrayIndex] = let;
                    arrayIndex++;
                }
            }
            return new string(array, 0, arrayIndex);
        }

        /// <summary>
        /// Adds a key value pair to the extension to content-type map.
        /// </summary>
        /// <param name="k">Key.</param>
        /// <param name="v">Value.</param>
        private static void SetSuffix(string k, string v)
        {
            _extMap.Add(k, v);
        }

        /// <summary>
        /// Fills in the extension to content-type map with static mappings.
        /// </summary>
        public static void FillExtMap()
        {
            SetSuffix("", "content/unknown");
            SetSuffix(".uu", "application/octet-stream");
            SetSuffix(".exe", "application/octet-stream");
            SetSuffix(".ps", "application/postscript");
            SetSuffix(".zip", "application/zip");
            SetSuffix(".sh", "application/x-shar");
            SetSuffix(".tar", "application/x-tar");
            SetSuffix(".snd", "audio/basic");
            SetSuffix(".au", "audio/basic");
            SetSuffix(".wav", "audio/x-wav");
            SetSuffix(".gif", "image/gif");
            SetSuffix(".jpg", "image/jpeg");
            SetSuffix(".jpeg", "image/jpeg");
            SetSuffix(".png", "image/png");
            SetSuffix(".htm", "text/html");
            SetSuffix(".html", "text/html");
            SetSuffix(".text", "text/plain");
            SetSuffix(".c", "text/plain");
            SetSuffix(".cc", "text/plain");
            SetSuffix(".c++", "text/plain");
            SetSuffix(".h", "text/plain");
            SetSuffix(".pl", "text/plain");
            SetSuffix(".txt", "text/plain");
            SetSuffix(".java", "text/plain");
            SetSuffix(".xml", "text/xml");
            SetSuffix(".js", "application/javascript");
            SetSuffix(".css", "text/css");
            SetSuffix(".ico", "image/x-icon");
            SetSuffix(".asp", "text/html");
            SetSuffix(".php", "text/html");
        }
    }
}