using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
// httpclient
using System.Net.Http;
using System.Net.Http.Headers;
// reading files
using System.IO;
using System.Threading;
// mongodb
using MongoDB.Driver;
using MongoDB.Bson;
using MongoDB.Driver.GridFS;
using MongoDB.Driver.Linq;
using MongoDB.Driver.Builders;
using System.Diagnostics;
// md5
using System.Security.Cryptography;

namespace ResumableCS {
    class Program {
        static void Main(string[] args) {
            Resumable resumable = new Resumable();
            //upload();

            // prevent premature exiting
            Console.WriteLine("\nPress any key to exit...");
            Console.ReadLine();
        }

        // running test
        static void upload() {
            var server = new MongoClient("mongodb://localhost:27017/").GetServer();
            var grid = server.GetDatabase("test").GridFS;

            // break the file into many small chunks
            var filename    = @"C:\Users\limse\Downloads\threaded.png";
            var chunks      = getChunks(filename, 10 * 1024 * 1024);
            Stopwatch stopwatch = Stopwatch.StartNew();
        
            var tasks       = new Task[chunks.Count];
            // first set up a meta file header so gridfs then pull out the object_id
            long lengh      = 4184;
            var metadata = new BsonDocument {{"filename", "threaded.png"}, {"length", lengh}, {"chunkSize", 261120},
                                             {"uploadDate", DateTime.UtcNow}, {"md5", "8eb1ce6048d14ce17dd759849c2dc0b4"}};
            //grid.Files.Insert(metadata);
            var id = metadata.GetElement("_id").ToString().Substring(4);

            for (var i = 0; i < chunks.Count; i++) {
                //var streamchunk = new MemoryStream(chunks[i]);
                var chunkMetadata = new BsonDocument { {"files_id", id}, {"n", i}, {"data", chunks[i]}};
                tasks[i] = Task.Factory.StartNew(() => writeToGrid(chunkMetadata, grid));
                //grid.Upload(streamchunk, "test.ipg");
            }
            Task.WaitAll(tasks);
            stopwatch.Stop();
            Console.WriteLine(stopwatch.ElapsedMilliseconds);
        }

        // this method will write stuff to the database
        static void writeToGrid(BsonDocument chunkMetadata, MongoGridFS grid) {
            grid.Chunks.Insert(chunkMetadata);
        }

        // to convert input from string into byte array into hash
        static string GetMd5Hash(MD5 md5Hash, string input) {
            // Convert the input string to a byte array and compute the hash. 
            byte[] data = md5Hash.ComputeHash(Encoding.UTF8.GetBytes(input));

            // Create a new Stringbuilder to collect the bytes and create a string.
            StringBuilder sBuilder = new StringBuilder();

            // Loop through each byte of the hashed data and format each one as a hexadecimal string. 
            for (int i = 0; i < data.Length; i++) {
                sBuilder.Append(data[i].ToString("x2"));
            }

            // Return the hexadecimal string. 
            return sBuilder.ToString();
        }

        /**
         * return a list of byte chunks according to chunksize
         * @param filename     path to filename
         *        chunksize    chunksize in bytes
         **/
        static List<byte[]> getChunks(string filename, int chunksize) {
           var chunks = new List<byte[]>();
           using (var stream = new FileStream(filename, FileMode.Open)) {
               var leftover = stream.Length; 
               while (leftover > 0) {
                   // calculate current chunksize
                   var currChunksize = (leftover >= chunksize) ? chunksize : leftover;
                   leftover -= currChunksize;
                   // construct one of the chunks
                   var chunk = new byte[currChunksize];
                   stream.Read(chunk, 0, (int)currChunksize);
                   chunks.Add(chunk);
               }
           }
           return chunks;
        }
    }
}
