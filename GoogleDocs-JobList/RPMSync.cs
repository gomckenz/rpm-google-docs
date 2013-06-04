using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;
using System.Dynamic;

using RPM.Api;
using RPM.ApiResults;

namespace GoogleDocs_JobList.AsyncWork
{
    class RPMSync
    {
        public event ProgressChangedEventHandler ProgressChanged;
        public event RunWorkerCompletedEventHandler WorkComplete;

        private readonly BackgroundWorker syncDataWorker = new BackgroundWorker();

        private Dictionary<string, Tuple<string, string>> googleData;

        private string rpmApiUrl;
        private string rpmApiKey;
        private RPMApi _api;
        private RPMApi API
        {
            get
            {
                if (_api == null)
                {
                    _api = new RPMApi(this.rpmApiUrl, this.rpmApiKey);
                }
                return _api;
            }
        }

        public RPMSync(string apiUrl, string apiKey, Dictionary<string, Tuple<string, string>> googleData)
        {
            this.rpmApiUrl = apiUrl;
            this.rpmApiKey = apiKey;
            this.googleData = googleData;

            this.syncDataWorker.WorkerReportsProgress = true;
            this.syncDataWorker.DoWork += syncData;
            this.syncDataWorker.RunWorkerCompleted += syncingComplete;
            this.syncDataWorker.ProgressChanged += syncingProgressChanged;
        }

        public void run()
        {
            this.syncDataWorker.RunWorkerAsync();
        }

        #region Async Events
        private void syncData(object sender, DoWorkEventArgs e)
        {
            ProcsResult procs = this.getAllProcs();
            ProcResult ilpProc = this.getProc("ILP-Incident Learning Process", procs);
            ProcResult externalJobs = this.getProc("External-JobInformation", procs);
            this.synchronizeJobInformation(externalJobs);
        }
        private void syncingComplete(object sender, RunWorkerCompletedEventArgs e)
        {
            this.WorkComplete(sender, e);
        }
        private void syncingProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            this.ProgressChanged(sender, e);
        }
        #endregion

        private ProcsResult getAllProcs()
        {
            RPMApi rpm = new RPMApi(this.rpmApiUrl, this.rpmApiKey);
            ProcsResult p = rpm.Procs();
            return p;
        }

        private ProcResult getProc(string procName, ProcsResult procs)
        {
            foreach (ProcResult proc in procs.Procs)
            {
                if (proc.Process == procName)
                {
                    return proc;
                }
            }
            return null;
        }

        private void synchronizeJobInformation(ProcResult jobProc)
        {
            Dictionary<string, ProcForm> forms = this.byExternalJobID(jobProc.ProcessID, this.getListOfForms(jobProc.ProcessID));
            //Dictionary<string, Tuple<string, string>> googleData = this.getGoogleDocsJobs();

            int jobCount = googleData.Count;
            int current = 0;
            foreach (string jobId in googleData.Keys)
            {
                string description = googleData[jobId].Item1;
                string location = googleData[jobId].Item2;
                if (forms.ContainsKey(jobId))
                {
                    ProcForm form = forms[jobId];
                    if (form.valueForField("Job Description") != description || form.valueForField("Job Location") != location)
                    {
                        this.updateJobForm(forms[jobId], description, location);
                    }
                }
                else
                {
                    this.createJobForm(jobProc.ProcessID, jobId, description, location);
                }
                current += 1;
                this.syncDataWorker.ReportProgress(current * 100 / jobCount);
            }
        }

        private Dictionary<string, ProcForm> byExternalJobID(int processID, List<ProcForm> list)
        {
            Dictionary<string, ProcForm> converted = new Dictionary<string, ProcForm>(list.Count);
            foreach (ProcForm form in list)
            {
                ProcFormData formData = this.API.ProcForm(processID, formID: form.FormID);
                converted.Add(formData.Form.valueForField("JobId"), formData.Form);
            }
            return converted;
        }

        private void updateJobForm(ProcForm procForm, string description, string location)
        {
            dynamic jobData = new ExpandoObject();
            jobData.FormID = procForm.FormID;
            List<object> fields = new List<object>
            {
                new
                {
                    Field = "Job Description",
                    Value = description
                },
                new
                {
                    Field = "Job Location",
                    Value = location
                }
            };
            jobData.Fields = fields;
            this.API.ProcFormEdit(formData: jobData);
        }

        private void createJobForm(int processID, string jobId, string description, string location)
        {
            dynamic jobData = new ExpandoObject();
            List<object> fields = new List<object>
            {
                new
                {
                    Field = "JobId",
                    Value = jobId
                },
                new
                {
                    Field = "Job Description",
                    Value = description
                },
                new
                {
                    Field = "Job Location",
                    Value = location
                }
            };
            jobData.Fields = fields;
            ProcFormData form = this.API.ProcFormAdd(processID, jobData);
        }

        private List<ProcForm> getListOfForms(int ProcessID)
        {
            List<ProcForm> forms = new List<ProcForm>();
            try
            {
                ProcFormsResult result = this.API.ProcForms(ProcessID);
                forms = result.Forms;
            }
            catch (RPMApiError e)
            {
                if (e.Message != "No forms")
                {
                    throw e;
                }
            }
            return forms;
        }
    }
}
