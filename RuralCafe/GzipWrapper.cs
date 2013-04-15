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
    public class GZipWrapper
    {
        /// <summary>
        /// Compresses a set of files into a single memorystream.
        /// </summary>
        /// <param name="fileNames"></param>
        /// <returns></returns>
        public static MemoryStream GZipCompress(LinkedList<string> fileNames)
        {
            MemoryStream ms = new MemoryStream();
            // Use the memory stream for the compressed data.
            GZipStream compressedzipStream = new GZipStream(ms, CompressionMode.Compress, true);

            foreach (string fileName in fileNames)
            {
                //Console.WriteLine("packing: " + fileName);
                FileStream infile;
                try
                {
                    // Open the file as a FileStream object.
                    infile = new FileStream(fileName, FileMode.Open, FileAccess.Read, FileShare.Read);
                    byte[] buffer = new byte[32];
                    // Read the file to ensure it is readable.
                    int bytesRead = infile.Read(buffer, 0, buffer.Length);
                    while (bytesRead != 0)
                    {
                        compressedzipStream.Write(buffer, 0, bytesRead);

                        bytesRead = infile.Read(buffer, 0, 32);
                    }
                    infile.Close();
                }
                catch (InvalidDataException)
                {
                    Console.WriteLine("Error: The file being read contains invalid data.");
                }
                catch (FileNotFoundException)
                {
                    Console.WriteLine("Error:The file specified was not found.");
                }
                catch (ArgumentException)
                {
                    Console.WriteLine("Error: path is a zero-length string, contains only white space, or contains one or more invalid characters");
                }
                catch (PathTooLongException)
                {
                    Console.WriteLine("Error: The specified path, file name, or both exceed the system-defined maximum length. For example, on Windows-based platforms, paths must be less than 248 characters, and file names must be less than 260 characters.");
                }
                catch (DirectoryNotFoundException)
                {
                    Console.WriteLine("Error: The specified path is invalid, such as being on an unmapped drive.");
                }
                catch (IOException)
                {
                    Console.WriteLine("Error: An I/O error occurred while opening the file.");
                }
                catch (UnauthorizedAccessException)
                {
                    Console.WriteLine("Error: path specified a file that is read-only, the path is a directory, or caller does not have the required permissions.");
                }
                catch (IndexOutOfRangeException)
                {
                    Console.WriteLine("Error: You must provide parameters for MyGZIP.");
                }
            }

            compressedzipStream.Close();
            // Reset the memory stream position to begin decompression.
            ms.Position = 0;
            return ms;
        }

        /*
        // version that doesn't get the expected filesize
        // and returns a filestream rather than writing to disk
        public static MemoryStream GZipDecompress(string gzipFile)
        {
            FileStream zipFileFs = new FileStream(gzipFile, FileMode.Open);
            GZipStream zipStream = new GZipStream(zipFileFs, CompressionMode.Decompress);
            //StreamReader reader = new StreamReader(zipStream, Encoding.Default);
            byte[] decompressedBuffer = new byte[1024];
            // Use the ReadAllBytesFromStream to read the stream.
            //Console.WriteLine("length of packageFs: " + packageFs.Length);
            //Console.WriteLine("length of zipStream: " + zipStream.Length);

            // create the result memorystream
            MemoryStream ms = new MemoryStream();
            
            int offset = 0;
            int read = zipStream.Read(decompressedBuffer, offset, decompressedBuffer.Length); 
            while (read != 0)
            {
                ms.Write(decompressedBuffer, offset, read);

                offset += read;
                read = zipStream.Read(decompressedBuffer, offset, decompressedBuffer.Length);
            }

            //string archive = reader.ReadToEnd();
            Console.WriteLine("Decompressed {0} bytes", offset);

            zipStream.Close();

            return ms;
        }
         */
        /// <summary>
        /// Decompresses a file.
        /// </summary>
        /// <param name="gzipFile"></param>
        /// <param name="outputFile"></param>
        /// <param name="expectedLength"></param>
        /// <returns></returns>
        public static bool GZipDecompress(string gzipFile, string outputFile, long expectedLength)
        {
            FileStream packageFs = new FileStream(gzipFile, FileMode.Open);
            GZipStream zipStream = new GZipStream(packageFs, CompressionMode.Decompress);
            //StreamReader reader = new StreamReader(zipStream, Encoding.Default);
            byte[] decompressedBuffer = new byte[expectedLength];
            // Use the ReadAllBytesFromStream to read the stream.
            //Console.WriteLine("length of packageFs: " + packageFs.Length);
            //Console.WriteLine("length of zipStream: " + zipStream.Length);

            int offset = 0;
            long remaining = expectedLength;
            while (remaining > 0)
            {
                int read = zipStream.Read(decompressedBuffer, offset, (int)remaining);
                //if (read <= 0)
                    /*
                    throw new EndOfStreamException
                        (String.Format("End of stream reached with {0} bytes left to read", remaining));*/
                remaining -= read;
                offset += read;
            }
            //string archive = reader.ReadToEnd();
            Console.WriteLine("Decompressed {0} bytes", offset);

            // create directory if it doesn't exist
            if (!Util.CreateDirectoryForFile(outputFile))
            {
                return false;
            }
            FileStream outputFileFs = Util.CreateFile(outputFile);
            if (outputFileFs == null)
            {
                return false;
            }

            outputFileFs.Write(decompressedBuffer, 0, offset);

            zipStream.Close();
            outputFileFs.Close();

            return true;
        }

        /// <summary>
        /// Decompresses a file into a memorystream.
        /// Unused.
        /// </summary>
        public static MemoryStream GZipCompress(string fileName)
        {
            MemoryStream ms = new MemoryStream();
            //Console.WriteLine("Test compression and decompression on file {0}", filename);
            FileStream infile;
            try
            {
                // Open the file as a FileStream object.
                infile = new FileStream(fileName, FileMode.Open, FileAccess.Read, FileShare.Read);
                byte[] buffer = new byte[infile.Length];
                // Read the file to ensure it is readable.
                int count = infile.Read(buffer, 0, buffer.Length);
                if (count != buffer.Length)
                {
                    infile.Close();
                    //Console.WriteLine("Test Failed: Unable to read data from file");
                    return null;
                }
                infile.Close();
                
                // Use the newly created memory stream for the compressed data.
                GZipStream compressedzipStream = new GZipStream(ms, CompressionMode.Compress, true);
                //Console.WriteLine("Compression");
                compressedzipStream.Write(buffer, 0, buffer.Length);
                // Close the stream.
                compressedzipStream.Close();
                //Console.WriteLine("Original size: {0}, Compressed size: {1}", buffer.Length, ms.Length);

                // Reset the memory stream position to begin decompression.
                ms.Position = 0;

                return ms;
                /*
                GZipStream zipStream = new GZipStream(ms, CompressionMode.Decompress);
                Console.WriteLine("Decompression");
                byte[] decompressedBuffer = new byte[buffer.Length + buffer_size];
                // Use the ReadAllBytesFromStream to read the stream.
                int totalCount = GZipWrapper.ReadAllBytesFromStream(zipStream, decompressedBuffer);
                Console.WriteLine("Decompressed {0} bytes", totalCount);

                if (!GZipWrapper.CompareData(buffer, buffer.Length, decompressedBuffer, totalCount))
                {
                    Console.WriteLine("Error. The two buffers did not compare.");
                }
                zipStream.Close();
                 */
            } // end try
            catch (InvalidDataException)
            {
                Console.WriteLine("Error: The file being read contains invalid data.");
            }
            catch (FileNotFoundException)
            {
                Console.WriteLine("Error:The file specified was not found.");
            }
            catch (ArgumentException)
            {
                Console.WriteLine("Error: path is a zero-length string, contains only white space, or contains one or more invalid characters");
            }
            catch (PathTooLongException)
            {
                Console.WriteLine("Error: The specified path, file name, or both exceed the system-defined maximum length. For example, on Windows-based platforms, paths must be less than 248 characters, and file names must be less than 260 characters.");
            }
            catch (DirectoryNotFoundException)
            {
                Console.WriteLine("Error: The specified path is invalid, such as being on an unmapped drive.");
            }
            catch (IOException)
            {
                Console.WriteLine("Error: An I/O error occurred while opening the file.");
            }
            catch (UnauthorizedAccessException)
            {
                Console.WriteLine("Error: path specified a file that is read-only, the path is a directory, or caller does not have the required permissions.");
            }
            catch (IndexOutOfRangeException)
            {
                Console.WriteLine("Error: You must provide parameters for MyGZIP.");
            }
            return ms;
        }

        /// <summary>
        /// Use this method is used to read all bytes from a stream.
        /// Unused.
        /// </summary>
        public static int ReadAllBytesFromStream(Stream stream, byte[] buffer)
        {
            int offset = 0;
            int totalCount = 0;
            while (true)
            {
                int bytesRead = stream.Read(buffer, offset, 100); // magic number 100
                if (bytesRead == 0)
                {
                    break;
                }
                offset += bytesRead;
                totalCount += bytesRead;
            }
            return totalCount;
        }
    }
}
