using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using MongoDB.Driver;
using MongoDB.Bson;
using MongoDB.Driver.GridFS;
using MongoDB.Driver.Linq;
using MongoDB.Driver.Builders;
using System.Diagnostics;
using System.Security.Cryptography;

namespace ResumableCS {
    /*
     * Resumable object will handle all the upload functionality and display all the uploading status.
     * This way it works like a library, people don't have to understand how Resumable works. they just
     * need to calll the right method and let the library handles all the details work.
     */
    class Resumable {
        // these parameter might get changed as the developement
        private int chunksize;
        private string target;

        // default constructor
        public Resumable() {
            this.chunksize = 1024 * 1024;
            this.target = "http://localhost:3000/api/upload";
        }

        // modifiable contructor
        public Resumable(int chunksize, string target) {
            this.chunksize = chunksize;
            this.target = target;
        }

        // setup connections before any uploading could take place
        public void upload(string filename) {
            var server = new MongoClient("mongodb://localhost:27017/").GetServer();
            var grid = server.GetDatabase("test").GridFS;

            // break the file into many small chunks
            var chunks      = getChunks(filename);
            var metadata    = getMetadata(filename);
            //var metadata = new BsonDocument {{"filename", filename}, {"length", lengh}, {"chunkSize", 261120},
            //                                 {"uploadDate", DateTime.UtcNow}, {"md5", "8eb1ce6048d14ce17dd759849c2dc0b4"}};
            
            grid.Files.Insert(metadata);
            var id = metadata.GetElement("_id").ToString().Substring(4);

            var tasks = new Task[chunks.Count];
            Stopwatch timer = new Stopwatch();
            timer.Start();
            for (var i = 0; i < chunks.Count; i++) {
                var index = i;
                var chunkMetadata = new BsonDocument { { "files_id", id }, { "n", i }, { "data", chunks[i] } };
                tasks[i] = Task.Factory.StartNew(() => writeToGrid(chunkMetadata, grid, index));
            }
            Task.WaitAll(tasks);
            timer.Stop();
            Console.WriteLine("DONE: " + timer.ElapsedMilliseconds.ToString());
        }

        // this method will write stuff to the database
        private void writeToGrid(BsonDocument chunkMetadata, MongoGridFS grid, int index) {
            while (true) {
                var result = grid.Chunks.Insert(chunkMetadata);
                var response = result.Response.GetElement("ok").Value.AsInt32;
                if (response == 1) {
                    // do something
                    Console.WriteLine("Chunk " + index + " uploaded complete");
                    break;
                } else {
                    Task.Delay(250);
                }
            }
        }

        // return a list of array of byte according to chunksize
        private List<byte[]> getChunks(string filename) {
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

        // getting all the header metadata
        private BsonDocument getMetadata(string filename) {
            using (var stream = new FileStream(filename, FileMode.Open)) {
                var md5 = getMD5(stream);
                var length = stream.Length;
                return new BsonDocument {{"filename", filename}, {"length", length},
                    {"chunkSize", chunksize}, {"uploadDate", DateTime.UtcNow}, {"md5", md5}};
            }
        }

        // computing md5
        private string getMD5(FileStream stream) {
            using (var md5 = MD5.Create()) {
                byte[] data = md5.ComputeHash(stream);
                var res = new StringBuilder();
                for (int i = 0; i < data.Length; i++)
                    res.AppendFormat("{0:x2}", data[i]);
                return res.ToString();
            }
        }
    }
}