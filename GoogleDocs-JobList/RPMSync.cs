﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;
using System.Dynamic;

using RPM.Api;
using RPM.ApiResults;
using GoogleDocs_JobList;

namespace GoogleDocs_JobList.AsyncWork
{
    class RPMSync
    {
        public event RunWorkerCompletedEventHandler WorkComplete;
        public event ProgressChangedEventHandler ProgressChanged;

        private readonly BackgroundWorker syncDataWorker = new BackgroundWorker();
        private readonly BackgroundWorker rpmInfoCheckWorker = new BackgroundWorker();

        private Dictionary<string, JobInfo> googleData;

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

        private InfoResult info;
        private ProcResult jobProcess;

        public RPMSync(string apiUrl, string apiKey)
        {
            this.rpmApiUrl = apiUrl;
            this.rpmApiKey = apiKey;

            this.syncDataWorker.WorkerReportsProgress = true;
            this.syncDataWorker.DoWork += syncData;
            this.syncDataWorker.RunWorkerCompleted += syncingComplete;
            this.syncDataWorker.ProgressChanged += syncingProgressChanged;

            this.checkRPMAccess();

            if (this.infoSuccessful())
            {
                ProcsResult procs = this.getAllProcs();
                this.jobProcess = this.getProc("External-JobInformation", procs);
                if (this.jobProcess == null)
                {
                    throw new ProcessNotFoundException("The RPM Process \"External-JobInformation\" was not Found.");
                }
                if (!this.jobProcess.Enabled)
                {
                    RPMApiError error = new RPMApiError();
                    error.Error = new Dictionary<string,string>{
						{"Message", "The RPM Process \"External-JobInformation\" is not enabled."}
					};
                    throw error;
                }
            }
        }

        public void run(Dictionary<string, JobInfo> newData)
        {
            this.googleData = newData;
            this.syncDataWorker.RunWorkerAsync();
        }

        #region Async Events
        private void syncData(object sender, DoWorkEventArgs e)
        {
            this.synchronizeJobInformation(this.jobProcess);
        }
        private void syncingComplete(object sender, RunWorkerCompletedEventArgs e)
        {
            this.WorkComplete(this, e);
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
            int jobCount = googleData.Count;
            int current = 0;
            foreach (string jobId in googleData.Keys)
            {
                string description = googleData[jobId].Description;
                string location = googleData[jobId].Location;
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
                forms.Remove(jobId);
                this.syncDataWorker.ReportProgress(current * 100 / jobCount);
            }
            foreach (string deletedJobId in forms.Keys)
            {
                ProcForm form = forms[deletedJobId];
                this.updateJobForm(form, form.valueForField("Job Description"), form.valueForField("Job Location"), true);
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

        private void updateJobForm(ProcForm procForm, string description, string location, bool deleted = false)
        {
            object jobData = new
            {
                FormID = procForm.FormID,
                Fields = this.prepareJobData(description, location, deleted: deleted)
            };
            this.API.ProcFormEdit(formData: jobData);
        }

        private void createJobForm(int processID, string jobId, string description, string location)
        {
            object jobData = new
            {
                Fields = this.prepareJobData(description, location, jobId)
            };
            ProcFormData form = this.API.ProcFormAdd(processID, jobData);
        }

        private List<object> prepareJobData(string description, string location, string jobId ="", bool deleted = false)
        {
            List<object> data = new List<object>
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
                },
                new
                {
                    Field = "deleted",
                    Value = deleted ? "Yes" : "No"
                }
            };
            if (jobId != "")
            {
                data.Add(new{
                    Field = "JobId",
                    Value = jobId
                });
            }
            return data;
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

        public void checkRPMAccess()
        {
            this.info = this.API.info();
        }

        public bool infoSuccessful()
        {
            return this.info.User != "";
        }

        public int getJobProcessID()
        {
            return this.jobProcess.ProcessID;
        }
    }
}
