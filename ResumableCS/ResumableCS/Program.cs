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
            var chunksize = 10 * 1024 * 1024;
            var target = "http://localhost:3000/api/upload";
            Resumable resumable = new Resumable(chunksize, target);

            // infinte loop till user type "quit" 
            var exitKeyword = "quit";
            while (true) {
                // grab the input from user
                Console.Write("Enter a filename to upload: ");
                var input = Console.ReadLine();
                input = @"C:\Users\limse\Downloads\oversize.pdf";
                // exit if the input matches the exitKeyword
                if (input == exitKeyword)
                    return;
                
                // not currently support the directory, maybe we only have to extend this
                // functionality with recursion in resumble.
                else if (Directory.Exists(input)) {
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
