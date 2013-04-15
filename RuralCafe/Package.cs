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
using System.IO.Compression;

namespace RuralCafe
{
    /// <summary>
    /// Package used to transfer a group of prefetched files from the remote to local proxy
    /// </summary>
    public class Package
    {
        private LinkedList<RCRequest> _rcRequests;
        private long _contentSize;
        private long _indexSize;

        /// <summary>
        /// Constructor for a RuralCafe package.
        /// </summary>
        public Package()
        {
            _rcRequests = new LinkedList<RCRequest>();
            _contentSize = 0;
            _indexSize = 0;
        }

        /// <summary>Size of the package contents.</summary>
        public long ContentSize
        {
            set { _contentSize = value; }
            get { return _contentSize; }
        }
        /// <summary>Size of the package index.</summary>
        public long IndexSize
        {
            set { _indexSize = value; }
            get { return _indexSize; }
        }
        /// <summary>List of the RCRequests in this package.</summary>
        public LinkedList<RCRequest> RCRequests
        {
            get { return _rcRequests; }
        }

        /// <summary>
        /// Adds a RCRequest to the package given the quota limitation.
        /// Only called by the remote proxy.
        /// XXX: quota is currently simple algorithm.
        /// XXX: should be changed to check for compressed size rather than actual size.
        /// </summary>
        /// <param name="requestHandler">Calling handler for this method.</param>
        /// <param name="rcRequest">RCRequest to add.</param>
        /// <param name="quota">Quota limit.</param>
        /// <returns>Error message.</returns>
        public bool Pack(RemoteRequestHandler requestHandler, RCRequest rcRequest, ref long quota)
        {
            if (rcRequest.Uri.Contains(' '))
            {
                requestHandler.LogDebug("object contains spaces: " + rcRequest.Uri);
                return false;
            }

            if (_rcRequests.Contains(rcRequest))
            {
                requestHandler.LogDebug("object exists in package: " + rcRequest.Uri);
                return false;
            }

            rcRequest.FileSize = Util.GetFileSize(rcRequest.CacheFileName);
            if (rcRequest.FileSize <= 0)
            {
                //requestHandler.LogDebug("object has no content: " + rcRequest.Uri);
                return false;
            }

            // quota check
            if ((quota - rcRequest.FileSize) < 0)
            {
                requestHandler.LogDebug("object doesn't fit in quota: " + rcRequest.Uri);
                return false;
            }

            _rcRequests.AddLast(rcRequest);
            quota -= rcRequest.FileSize;

            //requestHandler.LogDebug("packed: " + requestHandler.RequestUri + " " + rcRequest.FileSize + " bytes - " + quota + " left");
            return true;
        }
        
        /// <summary>
        /// Adds all the requests to the package.
        /// </summary>
        /// <param name="requests"></param>
        /// <returns></returns>
        public LinkedList<RCRequest> Pack(RemoteRequestHandler requestHandler, LinkedList<RCRequest> requests, ref long quota)
        {
            LinkedList<RCRequest> addedObjects = new LinkedList<RCRequest>();
            // add files that were completed to the package
            foreach (RCRequest request in requests)
            {
                // check watermark
                //if (quota > requestHandler.DEFAULT_LOW_WATERMARK)
                //{
                // add to the package
                if (Pack(requestHandler, request, ref quota))
                {
                    //requestHandler.LogDebug("packed: " + request.Uri + " " + request.FileSize + " bytes - " + quota + " left");
                    addedObjects.AddLast(request);
                }
                //}
            }
            
            return addedObjects;
        }

        /// <summary>
        /// Unpacks the package contents and indexes them.
        /// </summary>
        /// <param name="indexPath">Path to the index.</param>
        /// <param name="requestHandler">Calling handler for this method.</param>
        /// <returns>Total unpacked content size.</returns>
        public static long Unpack(LocalRequestHandler requestHandler, string indexPath)
        {
            string packageIndexSizeStr = requestHandler.RCRequest.GenericWebResponse.GetResponseHeader("Package-IndexSize");
            string packageContentSizeStr = requestHandler.RCRequest.GenericWebResponse.GetResponseHeader("Package-ContentSize");
            long packageIndexSize = Int64.Parse(packageIndexSizeStr);
            long packageContentSize = Int64.Parse(packageContentSizeStr);
            string packageFileName = requestHandler.PackageFileName;
            string unpackedPackageFileName = packageFileName.Replace(".gzip", "");

            GZipWrapper.GZipDecompress(packageFileName, unpackedPackageFileName, packageIndexSize + packageContentSize);
            FileStream packageFs = new FileStream(unpackedPackageFileName, FileMode.Open);

            // read the package index
            Byte[] packageIndexBuffer = new Byte[packageIndexSize];
            packageFs.Read(packageIndexBuffer, 0, (int)packageIndexSize);

            // split the big package file into pieces
            string[] stringSeparator = new string[] { "\r\n" };
            System.Text.UTF8Encoding enc = new System.Text.UTF8Encoding();
            string package = enc.GetString(packageIndexBuffer);
            string[] packageContentArr = package.Split(stringSeparator, StringSplitOptions.RemoveEmptyEntries);

            Byte[] bufferOverflow = new Byte[1024];
            int bufferOverflowCount = 0;
            int bytesRead = 0;
            long bytesReadOfCurrFile = 0;
            long unpackedBytes = 0;
            Byte[] buffer = new Byte[1024];
            string[] packageEntryArr;
            string currUri = "";
            long currFileSize = 0;
            foreach (string entry in packageContentArr)
            {
                stringSeparator = new string[] { " " };
                packageEntryArr = entry.Split(stringSeparator, StringSplitOptions.RemoveEmptyEntries);
                currUri = packageEntryArr[0];

                try
                {
                    currFileSize = Int64.Parse(packageEntryArr[1]);
                }
                catch (Exception e)
                {
                    requestHandler.LogDebug("problem unpacking: " + entry + " " + e.StackTrace + " " + e.Message);
                    return unpackedBytes;
                }

                if (!Util.IsValidUri(currUri))
                {
                    requestHandler.LogDebug("problem unpacking: " + currUri);
                    return unpackedBytes;
                }
                RCRequest rcRequest = new RCRequest(requestHandler, currUri);

                unpackedBytes += currFileSize;

                //requestHandler.LogDebug("unpacking: " + rcRequest.Uri + " - " + currFileSize + " bytes");

                // make sure the file doesn't already exist for indexing purposes only
                bool existed = false;
                FileInfo ftest = new FileInfo(rcRequest.CacheFileName);
                if (ftest.Exists)
                {
                    existed = true;
                }

                // try to delete the old version
                if (!Util.DeleteFile(rcRequest.CacheFileName))
                {
                    return unpackedBytes;
                }

                // create directory if it doesn't exist
                if (!Util.CreateDirectoryForFile(rcRequest.CacheFileName))
                {
                    return unpackedBytes;
                }

                // create the file if it doesn't exist
                FileStream currFileFS = Util.CreateFile(rcRequest.CacheFileName);
                if (currFileFS == null)
                {
                    return unpackedBytes;
                }

                // check for overflow from previous file, and use it up first
                if (bufferOverflowCount > 0)
                {
                    Buffer.BlockCopy(bufferOverflow, 0, buffer, 0, bufferOverflowCount);
                    bytesRead = bufferOverflowCount;
                }
                else
                {
                    bytesRead = packageFs.Read(buffer, 0, 1024);
                }

                // reset for current file
                bytesReadOfCurrFile = 0;
                while (bytesRead != 0 && bytesReadOfCurrFile < currFileSize)
                {
                    // check if we read too much
                    if (bytesReadOfCurrFile + bytesRead > currFileSize)
                    {
                        // bytes left must be less than 1024, fine to convert to Int
                        int bytesLeftOfCurrFile = ((int)(currFileSize - bytesReadOfCurrFile));
                        currFileFS.Write(buffer, 0, bytesLeftOfCurrFile);
                        // done with this file
                        bytesReadOfCurrFile = currFileSize;

                        // handle overflow
                        bufferOverflowCount = bytesRead - bytesLeftOfCurrFile;
                        Buffer.BlockCopy(buffer, bytesLeftOfCurrFile, bufferOverflow, 0, bytesRead - bytesLeftOfCurrFile);
                    }
                    else
                    {
                        // append what we read
                        currFileFS.Write(buffer, 0, bytesRead);
                        // update bytesReadOfCurrFile
                        bytesReadOfCurrFile += bytesRead;

                        bytesRead = packageFs.Read(buffer, 0, 1024);
                    }
                }

                if (bytesReadOfCurrFile != currFileSize)
                {
                    // ran out of bytes for this file
                    requestHandler.LogDebug("error, unexpected package size: " + rcRequest.CacheFileName +
                        "(" + bytesReadOfCurrFile + " / " + currFileSize + ")");
                    return unpackedBytes * -1;
                }

                currFileFS.Close();

                // add the file to Lucene
                if (Util.IsParseable(rcRequest))
                {
                    string document = Util.ReadFileAsString(rcRequest.CacheFileName);
                    string title = Util.GetPageTitle(document);
                    string content = Util.GetPageContent(document);

                    //request.LogDebug("indexing: " + rcRequest._uri);
                    if (!existed)
                    {
                        IndexWrapper.IndexDocument(indexPath, "Content-Type: text/html", rcRequest.Uri, title, content);
                    }
                }
            }
            if (packageFs != null)
            {
                packageFs.Close();
            }
            return unpackedBytes;
        }
    }
}