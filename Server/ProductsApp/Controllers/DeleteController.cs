using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using System.Web;

namespace ProductsApp.Controllers {
    public class DeleteController : ApiController {

        [HttpGet]
        public HttpResponseMessage ClearAll() {
            string tmpRoot = HttpContext.Current.Server.MapPath("~/Tmp_Data");
            string root = HttpContext.Current.Server.MapPath("~/Files");
            string[] tmpFiles = System.IO.Directory.GetFiles(tmpRoot);
            foreach (var file in tmpFiles)
                System.IO.File.Delete(file);
            string[] files = System.IO.Directory.GetFiles(root);
            foreach (var file in files)
                System.IO.File.Delete(file);
            var response = new HttpResponseMessage(HttpStatusCode.Created) {
                Content = new StringContent("Cleared All")
            };
            return response;
        }

        [HttpGet]
        public HttpResponseMessage ClearOne(string filename) {
            string tmpRoot = HttpContext.Current.Server.MapPath("~/Tmp_Data");
            string root = HttpContext.Current.Server.MapPath("~/Files");
            string[] tmpFiles = System.IO.Directory.GetFiles(tmpRoot);
            string[] files = System.IO.Directory.GetFiles(root);

            List<string> filteredFiles = new List<string>();
            foreach (string file in files) {
                if (file.Contains(filename))
                    filteredFiles.Add(file);
            } 
            foreach (var file in tmpFiles)
                if (file.Contains(filename))
                    filteredFiles.Add(file);

            foreach (var file in filteredFiles)
                System.IO.File.Delete(file);

            var response = new HttpResponseMessage(HttpStatusCode.Created) {
                Content = new StringContent("Cleared " + filename)
            };
            return response;
        }
    }
}
