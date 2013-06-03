using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Dynamic;
using RestSharp;
using RestSharp.Deserializers;
using RPM.ApiResults;

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
            this.client = new RestClient(this.url + "Api2.svc");
            this.key = apiKey;
        }

        public InfoResult info() {
            InfoResult result = this.sendRequest<InfoResult>("info");
            return result;
        }

        public ProcsResult Procs()
        {
            return this.sendRequest<ProcsResult>("Procs");
        }

        public ProcFormsResult ProcForms(int ProcessID = 0, string ProcessName = null, int ViewID = 0)
        {
            dynamic apiParameters = this.apiParameters();
            if (ProcessID > 0)
            {
                apiParameters.ProcessID = ProcessID;
            }
            if (ProcessName != null)
            {
                apiParameters.ProcessName = ProcessName;
            }
            if (ViewID > 0)
            {
                apiParameters.ViewID = ViewID;
            }
            return this.sendRequest<ProcFormsResult>("ProcForms", apiParameters);
        }

        internal ProcFormData ProcFormAdd(int processID, dynamic formData)
        {
            dynamic parameters = this.apiParameters();
            parameters.ProcessID = processID;
            parameters.Form = formData;
            return this.sendRequest<ProcFormData>("ProcFormAdd", parameters);
        }

        internal ProcFormData ProcForm(int processID = 0, string processName = "", int formNumber = 0, int formID = 0)
        {
            dynamic parameters = this.apiParameters();
            if (processID > 0)
            {
                parameters.ProcessID = processID;
            }
            if (processName != "")
            {
                parameters.ProcessName = processName;
            }
            if (formID > 0)
            {
                parameters.FormID = formID;
            }
            parameters.FormNumber = formNumber;
            return this.sendRequest<ProcFormData>("ProcForm", parameters);
        }

        internal ProcFormData ProcFormEdit(int processID = 0, string processName = "", dynamic formData = null)
        {
            dynamic parameters = this.apiParameters();
            if (processID > 0)
            {
                parameters.ProcessID = processID;
            }
            if (processName != "")
            {
                parameters.ProcessName = processName;
            }
            parameters.Form = formData;
            return this.sendRequest<ProcFormData>("ProcFormEdit", parameters);
        }

        /// <exception cref="RPM.ApiResults.RPMApiError">An RPM API Error occurred.</exception>
        private T sendRequest<T>(string endpoint, dynamic apiParameters = null)
        {
            if (apiParameters == null)
            {
                apiParameters = this.apiParameters();
            }

            RestRequest request = new RestRequest(endpoint, Method.POST);
            request.RequestFormat = DataFormat.Json;

            request.AddBody(apiParameters);

            RestResponse response = (RestResponse)this.client.Execute(request);

            JsonDeserializer js = new JsonDeserializer();
            response.Content = response.Content.Replace("{\"Result\":", "");
            response.Content = response.Content.Substring(0, response.Content.Length - 1);

            if (response.Content.StartsWith("{\"Error\""))
            {
                RPMApiError parsedError = js.Deserialize<RPMApiError>(response);
                throw parsedError;
            }
            T parsedResponse = js.Deserialize<T>(response);
            return parsedResponse;
        }

        private dynamic apiParameters()
        {
            dynamic apiParameters = new ExpandoObject();
            apiParameters.Key = this.key;
            return apiParameters;
        }
    }
}
