using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using System.Threading.Tasks;
// using MongoDB to store data instead of file system
using MongoDB.Bson;
using MongoDB.Driver;

namespace ProductsApp.Controllers {
    public class MongoController : ApiController {
        protected static int x = 10;
        protected static IMongoClient client      = new MongoClient("mongodb://localhost:27017");
        protected static IMongoDatabase database  = client.GetDatabase("restaurants");


    }
}
