using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Web;
using System.IO;
using System.Threading;

namespace ProductsApp.Controllers {
    public class UploadController : ApiController {
        // to lock the thread while seeking file information
        private static Dictionary<string, int> counter = new Dictionary<string, int>();

        [HttpPost]
        public async Task<HttpResponseMessage> post() {
            // check if the request contains multipart/form-data
            if (!Request.Content.IsMimeMultipartContent())
                throw new HttpResponseException(HttpStatusCode.UnsupportedMediaType);

            // grab the relative path to "App_Data"
            string tmpRoot  = HttpContext.Current.Server.MapPath("~/Tmp_Data");
            //string tmpRoot  = "F:/Tmp_Data";
            string root     = HttpContext.Current.Server.MapPath("~/Files");

            Trace.WriteLine(Request.Content.ToString());

            // write the n-chunks to the tmp folder for now
            var provider    = new MultipartFormDataStreamProvider(tmpRoot);
            try {
                var result      = await Request.Content.ReadAsMultipartAsync(provider);
                var filename    = result.FormData["resumableFilename"];
                var chunkNumber = int.Parse(result.FormData["resumableChunkNumber"]);
                var totalChunk  = int.Parse(result.FormData["resumableTotalChunks"]);

                new Thread(delegate() { 
                    // This illustrates how to get the file names.
                    foreach (MultipartFileData file in provider.FileData) {
                        // it is important to sort the filename accordingly. there might be more efficient method but i will
                        // figure this part later

                        string tmpFilename = String.Format("{0}\\{1:D7}-{2}-{3}", tmpRoot , chunkNumber, totalChunk, filename);
                        System.IO.File.Move(file.LocalFileName, tmpFilename);
                        lock (counter) {
                            if (!counter.ContainsKey(filename))
                                counter.Add(filename, 0);
                            counter[filename]++;
                        }
                    }
                }).Start();
                
                // check if all the files had been saved
                if (chunkNumber == totalChunk) {
                    new Thread(delegate() {
                        string[] files = new string[totalChunk];
                        while (true) {
                            var count = 0;
                            lock (counter) {
                                count = counter[filename];
                            }
                            if (count == totalChunk) {
                                lock (counter) {
                                    counter.Remove(filename);
                                }
                                string[] tmpFiles = System.IO.Directory.GetFiles(tmpRoot);
                                var i = 0;
                                foreach (string file in tmpFiles) {
                                    if (file.Contains(filename))
                                        files[i++] = (file);
                                } 
                                Array.Sort(files, StringComparer.InvariantCulture);
                                break;
                            } else {
                                Thread.Sleep(1000);
                            }
                        }
                        // at this point all the chunks had been written already so now we will combine them
                        var fs = new FileStream(root + "\\" + filename, FileMode.CreateNew);
                        foreach (var file in files) {
                            var buffer = System.IO.File.ReadAllBytes(file);
                            fs.Write(buffer, 0, buffer.Length);
                            //System.IO.File.Delete(file);
                        }
                        fs.Close();
                    }).Start();
                }

                // need to return for n-1 packages back.
                return Request.CreateResponse(HttpStatusCode.OK);
            } 
            catch (System.Exception e) {
                return Request.CreateErrorResponse(HttpStatusCode.InternalServerError, e);
            }
        }
    }
}
