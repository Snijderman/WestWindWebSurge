﻿using System;
using System.Collections.Generic;
using System.Linq;
using Westwind.Utilities;

namespace WebSurge.Support
{
   public class PostmanIntegration
   {
      public string Export(string name, List<HttpRequestData> requests, StressTesterConfiguration config, string filename = null)
      {
         var pm = new PostmanCollection();

         foreach (var request in requests)
         {
            pm.info._postman_id = Guid.NewGuid().ToString();
            if (!string.IsNullOrEmpty(name))
            {
               pm.info.name = name;
            }
            else
            {
               pm.info.name = "Collection-" + DataUtils.GenerateUniqueId(8);
            }

            pm.info.schema = "https://schema.getpostman.com/json/collection/v2.1.0/collection.json";

            var item = new Item();
            pm.item.Add(item);

            item.name = request.Url;

            var req = new Request();
            item.request = req;

            req.url = new Url();
            req.method = request.HttpVerb;

            var body = new Body();
            req.body = body;
            body.mode = "raw";
            body.raw = request.RequestContent;

            // don't copy over credentials explicitly
            //if (!string.IsNullOrEmpty(config.Username))
            //{
            //    req.auth = new Auth();
            //    req.auth.type = "ntlm";
            //    var ntlm = new List<Ntlm>();
            //    req.auth.ntlm = ntlm;
            //    ntlm.Add(new Ntlm {key = "username", value = config.Username});
            //    ntlm.Add(new Ntlm {key = "password", value = ""});
            //}

            foreach (var header in request.Headers)
            {
               req.header.Add(new Header
               { key = header.Name, name = header.Name, value = header.Value, type = "text" });
            }

            req.url.raw = request.Url;
            req.url.protocol = "http";
            req.url.host.Add(request.Host);


         }

         if (!string.IsNullOrEmpty(filename))
         {
            if (JsonSerializationUtils.SerializeToFile(pm, filename, false, true))
            {
               return "OK";
            }

            return null;
         }

         return JsonSerializationUtils.Serialize(pm, false, formatJsonOutput: true);
      }

      public RequestCollection ImportFromFile(string collectionFilename)
      {
         var collection =
             JsonSerializationUtils.DeserializeFromFile(collectionFilename, typeof(PostmanCollection), false) as
                 PostmanCollection;
         return this.Import(collection);
      }

      public RequestCollection Import(string collectionJson)
      {
         var collection =
             JsonSerializationUtils.Deserialize(collectionJson, typeof(PostmanCollection), false) as
                 PostmanCollection;
         return this.Import(collection);
      }

      public RequestCollection Import(PostmanCollection collection)
      {
         if (!(collection is PostmanCollection))
         {
            return null;
         }

         var config = new WebSurgeConfiguration();
         DataUtils.CopyObjectData(App.Configuration, config);

         var list = new List<HttpRequestData>();

         foreach (var item in collection.item)
         {
            if (item.request == null)
            {
               continue;
            }
            var req = new HttpRequestData
            {
               Url = item.request.url.raw
            };
            if (req.Url != item.name)
            {
               req.Name = item.name;
            }

            req.HttpVerb = item.request.method;

            req.RequestContent = item.request.body?.raw;

            foreach (var header in item.request.header)
            {
               req.Headers.Add(new HttpRequestHeader { Name = header.key ?? header.name, Value = header.value?.Trim() });
            }

            if (item.request.auth?.ntlm != null && item.request.auth?.ntlm.Count > 0)
            {
               req.Username = item.request.auth.ntlm.FirstOrDefault(nt => nt.key.Equals("username", StringComparison.InvariantCultureIgnoreCase))?.value;
               req.Password = item.request.auth.ntlm.FirstOrDefault(nt => nt.key.Equals("password", StringComparison.InvariantCultureIgnoreCase))?.value;
            }
            if (item.request.auth?.basic != null && item.request.auth?.basic.Count > 0)
            {
               req.Username = item.request.auth.basic.FirstOrDefault(nt => nt.key.Equals("username", StringComparison.InvariantCultureIgnoreCase))?.value;
               req.Password = item.request.auth.basic.FirstOrDefault(nt => nt.key.Equals("password", StringComparison.InvariantCultureIgnoreCase))?.value;
            }


            list.Add(req);
         }


         return new RequestCollection()
         {
            Configuration = config,
            Requests = list
         };
      }
   }

   public class RequestCollection
   {
      public WebSurgeConfiguration Configuration { get; set; }
      public List<HttpRequestData> Requests { get; set; }
   }

   public class PostmanCollection
   {
      public Info info { get; set; } = new Info();
      public List<Item> item { get; set; } = new List<Item>();
   }

   public class Info
   {
      public string _postman_id { get; set; }
      public string name { get; set; }
      public string schema { get; set; }
   }

   public class Item
   {
      public string name { get; set; }
      public Request request { get; set; }
      public object[] response { get; set; }
   }

   public class Request
   {
      public string method { get; set; }
      public List<Header> header { get; set; } = new List<Header>();
      public Url url { get; set; }
      public Auth auth { get; set; }
      public Body body { get; set; }
   }

   public class Url
   {
      public string raw { get; set; }
      public string protocol { get; set; }
      public List<string> host { get; set; } = new List<string>();
      public List<string> path { get; set; }
      public List<Query> query { get; set; }
   }

   public class Query
   {
      public string key { get; set; }
      public string value { get; set; }
   }

   public class Auth
   {
      public string type { get; set; }
      public List<AuthData> ntlm { get; set; } = new List<AuthData>();
      public List<AuthData> basic { get; set; } = new List<AuthData>();
   }

   public class AuthData
   {
      public string key { get; set; }
      public string value { get; set; }
      public string type { get; set; }
   }

   public class Body
   {
      public string mode { get; set; }
      public string raw { get; set; }
   }

   public class Header
   {
      public string key { get; set; }
      public string name { get; set; }
      public string value { get; set; }
      public string type { get; set; }
   }

}
