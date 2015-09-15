using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
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
        // these static variable are used to test uploading speed and file integrity
        private static bool UNIT_TEST = true;
        private static int TEST_LOOP = 1;
        private static int DELAY = 250; // how long the task/thread will sleep if connection lost
        // mongodb only allow 16MB per grid.fs.file upload request
        private static int MAX_CHUNK_SIZE = 16 * 1024 * 1024;

        // these parameter might get changed as the developement
        private int chunksize;
        private string target;
        private int simultaneousUploads;
        private string database;

        // default constructor
        public Resumable()
            : this(1024 * 1024, "mongodb://localhost:27017/", "test", 10) {
            // call modifiable constructor instead
        }

        // modifiable contructor
        public Resumable(int chunksize, string target, string database, int simultaneousUploads) {
            if (chunksize > MAX_CHUNK_SIZE)
                throw new ArgumentException("Chunksize can not exceed 16MB.");
            this.database = database;
            this.chunksize = chunksize;
            this.target = target;
            this.simultaneousUploads = simultaneousUploads;
        }

        // setup connections before any uploading could take place
        public void upload(string filename) {
            Stopwatch timer = new Stopwatch();
            timer.Start();
            List<byte[]> chunks     = null;
            MongoGridFS grid        = null;
            BsonDocument metadata   = null;
            string id               = null;
            for (var i = 0; i < TEST_LOOP; i++) {
                var server = new MongoClient(target).GetServer();
                grid = server.GetDatabase(database).GridFS;

                // break the file into many small chunks
                chunks          = getChunks(filename);
                metadata        = getMetadata(filename);

                // it is important to first write a simple header decoration then grab the id
                // to upload the chunks.
                grid.Files.Insert(metadata);
                id = metadata.GetElement("_id").ToString().Substring(4);
                multiThreadedUpload(chunks, id, grid);
            }
            timer.Stop();
            report(timer, filename, chunks);

            // check upload file integrity
            if (UNIT_TEST)
                checkFileIntegrity(grid, id, metadata);
        }

        // check if the uploaded file integrity
        private void checkFileIntegrity(MongoGridFS grid, string id, BsonDocument metadata) {
            Console.Write("Checking file upload integrity: ");
            var downloadInfo = grid.FindOneById(ObjectId.Parse(id));
            var downloadMD5 = getMD5(downloadInfo.OpenRead());
            if (metadata["md5"] == downloadMD5)
                Console.WriteLine("PASS");
            else
                Console.WriteLine("FAIL");
        }

        // display report data
        private void report(Stopwatch timer, string filename ,List<Byte[]> chunks) {
            using (var stream = new FileStream(filename, FileMode.Open)) {
                Console.WriteLine("Filesize: " + bytesToSize(stream.Length));
                Console.WriteLine("simultaneous Uploads: " + simultaneousUploads);
                Console.WriteLine("Avg Time: " + timer.ElapsedMilliseconds / TEST_LOOP + "ms");
            }
        }

        // converting storage units. mainly use to report data 
        private string bytesToSize(double size) {
            var sizes = new string[] {"Bytes", "KB", "MB", "GB", "TB"};
            if (size == 0) return "0 " + sizes[0];
            var i = Convert.ToInt32(Math.Floor(Math.Log(size) / Math.Log(1024)));
            return Math.Round(size / Math.Pow(1024, i), 2) + " " + sizes[i];
        }

        // uploading all the chunks into grid.fs.files asynchronously but we have to watch out for
        // resource competition. so we will only allow upto 'simultaneousUploads' tasks run at a time.
        // this ensure that gridfs to not overwhelm with write requests.
        public void multiThreadedUpload(List<byte[]> chunks, string id, MongoGridFS grid) {
            SemaphoreSlim max = new SemaphoreSlim(simultaneousUploads);
            var tasks = new Task[chunks.Count];
            for (var i = 0; i < chunks.Count; i++) {
                max.Wait();
                var index = i;
                var chunkMetadata = new BsonDocument { { "files_id", new ObjectId(id) }, { "n", i }, { "data", chunks[i] } };
                tasks[i] = Task.Factory.StartNew(() => writeToGrid(chunkMetadata, grid, index)
                    , TaskCreationOptions.LongRunning)
                    .ContinueWith( (task) => max.Release());
            }
            Task.WaitAll(tasks);
        }

        // this method will write stuff to the database
        private void writeToGrid(BsonDocument chunkMetadata, MongoGridFS grid, int index) {
            while (true) {
                try {
                    var result = grid.Chunks.Insert(chunkMetadata);
                    var response = result.Response.GetElement("ok").Value.AsInt32;
                    if (response == 1) {
                        Console.WriteLine("Chunk " + index + " uploaded.");
                        break;
                    } else {
                        Console.WriteLine("Chunk " + index + " corrupt upload...resume in " + DELAY + "ms");
                        Task.Delay(DELAY);
                    }
                } catch (MongoConnectionException) {
                    Console.WriteLine("Chunk " + index + " lost connection...resume in " + DELAY + "ms");
                    Task.Delay(DELAY);
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
        private string getMD5(Stream stream) {
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