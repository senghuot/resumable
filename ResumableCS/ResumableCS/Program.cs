using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace ResumableCS {
    class Program {
        static void Main(string[] args) {
            // instantiate resumbale object
            var local = "mongodb://localhost:27017/";
            var skydrive = "mongodb://skyd-vap-mdbp1.devop.vertafore.com:27017/";
            var database = "test";
            var chunksize = 1024 * 1024; // in bytes
            var simultaneousUploads = 10;
            Resumable resumable = new Resumable(chunksize, skydrive, database, simultaneousUploads);

            // infinte loop
            var root = @"C:\Users\limse\Downloads\";
            while (true) {
                // grab the input from user
                Console.Write("Enter a filename to upload: ");
                var input = Console.ReadLine();
                input = root + input;

                // not currently support the directory, maybe we only have to extend this
                // functionality with recursion in resumble.
                if (Directory.Exists(input)) {
                    Console.WriteLine("We don't support directory yet");

                // handles a single file
                } else if (File.Exists(input)) {
                    resumable.upload(input);
                } else {
                    Console.WriteLine("Invalid input!");
                }
            }
        }
    }
}
