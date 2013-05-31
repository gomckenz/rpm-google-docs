using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RestSharp;

namespace RPM.Api
{
    class RPMApi
    {
        private string url;
        private string key;
        private RestClient client;

        public RPMApi(string apiURL, string apiKey)
        {
            this.url = apiURL;
            if (!this.url.EndsWith("/"))
            {
                this.url += "/";
            }
            //this.url = this.url.Replace("localhost", "localhost.");
            this.client = new RestClient(this.url + "Api2.svc");
            this.key = apiKey;
        }

        public string info() {
            return this.sendRequest("info");
        }

        private string sendRequest(string endpoint) {
            RestRequest request = new RestRequest(endpoint, Method.POST);
            request.RequestFormat = DataFormat.Json;
            object requestBody = new {
                Key = this.key
            };
            request.AddBody(request.JsonSerializer.Serialize(requestBody));

            RestResponse response = (RestResponse)this.client.Execute(request);

            RestSharp.Serializers.JsonSerializer js = new RestSharp.Serializers.JsonSerializer();
            return response.Content;
        }
    }
}
