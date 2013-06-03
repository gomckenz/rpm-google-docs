using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Dynamic;

using System.Collections;
using System.Diagnostics;
using System.ComponentModel;

using GoogleDocs_JobList.Properties;

using Google.GData.Client;
using Google.GData.Spreadsheets;

using RPM.Api;
using RPM.ApiResults;

namespace GoogleDocs_JobList
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {

        private string googleOAuthKey;
        private string rpmApiUrl;
        private string rpmApiKey;
        private string appName;
        private string accessToken;
        private string refreshToken;
        
        private string clientId = GoogleOAuthSettings.OAuthClientId;
        private string clientSecret = GoogleOAuthSettings.OAuthClientSecret;
        private WorksheetEntry ws;
        private GoogleSpreadsheetAccess access;

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

        #region Fake Data To Generate Jobs Information
        private Dictionary<String, int> siteNames = new Dictionary<string, int>()
        {
            {"Abyssinia", 0}, {"Cook", 0}, {"Douala", 0}, {"Karakoram", 0}, {"Kunashiri", 0}, {"Lagos", 0},
            {"Louisiade", 0}, {"Netherlands", 0}, {"Suriname", 0}, {"Outer", 0}, {"Peshawar", 0},
            {"Sala", 0}, {"Velez", 0}, {"Thessaloniki", 0}, {"Tyrrhenian", 0}, {"Wetar", 0}, {"Yugoslavia", 0},
            {"Jersey", 0}, {"Marshall", 0}, {"Tokelau", 0}, {"Amami", 0}, {"Amsterdam", 0}, {"Andaman", 0},
            {"Bonin", 0}, {"Bothnia", 0},  {"Dodecanese", 0}, {"Nuuk", 0}, {"Guatemala", 0}, {"Lobamba", 0},
            {"Manipa", 0}, {"Maputo", 0}, {"North", 0}, {"Ross", 0}, {"Southern", 0}, {"Archipelago", 0},
            {"Vostok", 0}, {"Bahamas", 0}, {"Poland", 0}, {"Spain", 0}
        };

        private System.Collections.ArrayList repairActivities = new System.Collections.ArrayList
        {
            "Refurbishment", "Replacement", "Inspection", "Overhaul",
            "Repair", "Repaint",
        };

        private System.Collections.ArrayList partNames = new System.Collections.ArrayList
        {
            "Pump", "Compressor", "Pipeline", "Gasket", "Steam Trap", "Valve",
            "Transformer", "Drive Train", "Motor", "Heat Tracing"
        };
        #endregion

        private readonly BackgroundWorker writeToGoogleWorker = new BackgroundWorker();
        private readonly BackgroundWorker syncJobDataWorker = new BackgroundWorker();

        public MainWindow()
        {
            InitializeComponent();
            this.appName = (string)Settings.Default["ApplicationName"];
            this.showSetupWindow();

            this.OpenGoogleSpreadsheetButtonText = this.OpenGoogleSpreadsheet.Content.ToString();
            this.writeToGoogleWorker.DoWork += worker_DoWork;
            this.writeToGoogleWorker.RunWorkerCompleted += worker_RunWorkerCompleted;
            this.writeToGoogleWorker.WorkerReportsProgress = true;
            this.writeToGoogleWorker.ProgressChanged += worker_ProgressChanged;

            this.SynchronizeButtonText = this.SynchronizeStartButton.Content.ToString();
            this.syncJobDataWorker.WorkerReportsProgress = true;
            this.syncJobDataWorker.DoWork += syncJobDataWorker_DoWork;
            this.syncJobDataWorker.RunWorkerCompleted += syncJobDataWorker_RunWorkerCompleted;
            this.syncJobDataWorker.ProgressChanged += syncJobDataWorker_ProgressChanged;
        }

        private void SaveSetupOption(object sender, AppSetupChangedEventArgs e)
        {
            this.saveSetting(e.key, e.value);
        }

        private void saveSetting(string key, string value)
        {
            Settings.Default[key] = value;
            Settings.Default.Save();
            this.loadSettings();
        }

        private void SetupButton_Click(object sender, RoutedEventArgs e)
        {
            this.showSetupWindow(true);
        }

        private void loadSettings()
        {
            this.googleOAuthKey = (string)Settings.Default["GoogleOauthKey"];
            this.rpmApiUrl = (string)Settings.Default["RpmApiUrl"];
            this.rpmApiKey = (string)Settings.Default["RpmApiKey"];
            this.accessToken = (string)Settings.Default["OAuthAccessToken"];
            this.refreshToken= (string)Settings.Default["OAuthRefreshToken"];
        }

        private void showSetupWindow(bool forceShow = false)
        {
            this.loadSettings();
            if (this.googleOAuthKey == "" || this.rpmApiUrl == "" || this.rpmApiKey == "" || forceShow)
            {
                SetupWindow w = new SetupWindow(googleOAuthKey, rpmApiUrl, rpmApiKey, clientId, clientSecret);
                w.SetupOptionChanged += this.SaveSetupOption;
                w.ShowDialog();
            }
        }

        private void OpenGoogleSpreadsheet_Click(object sender, RoutedEventArgs e)
        {
            this.OpenGoogleSpreadsheet.Content = "Starting...";
            this.doGoogleAuth();
            if (this.ws.Title.Text == "Sheet 1")
            {
                this.ws.Title.Text = "Data";
                this.ws.Update();
                writeToGoogleWorker.RunWorkerAsync();
            }
            else
            {
                this.OpenGoogleSpreadsheet.Content = this.OpenGoogleSpreadsheetButtonText;
                Process.Start(access.getSpreadsheetURL(this.appName));
            }
        }

        private void doGoogleAuth()
        {
            if (this.accessToken == "")
            {
                GoogleOauthAccess.getAccessTokens(
                    this.clientId, this.clientSecret, this.googleOAuthKey,
                    out this.accessToken, out this.refreshToken
                );
                this.saveSetting("OAuthAccessToken", this.accessToken);
                this.saveSetting("OAuthRefreshToken", this.refreshToken);
            }
            else
            {
                this.accessToken = GoogleOauthAccess.getRefreshedAccessToken(this.clientId, this.clientSecret, this.refreshToken);
                this.saveSetting("OAuthAccessToken", this.accessToken);
            }

            this.access = new GoogleSpreadsheetAccess(
                this.appName, this.clientId,
                this.clientSecret, this.accessToken
            );

            this.ws = access.getDataWorksheet();
        }

        #region AsyncWork Forn RPM Synchronization
        private string SynchronizeButtonText;
        void syncJobDataWorker_DoWork(object sender, DoWorkEventArgs e)
        {
            ProcsResult procs = this.getAllProcs();
            ProcResult ilpProc = this.getProc("ILP-Incident Learning Process", procs);
            ProcResult externalJobs = this.getProc("External-JobInformation", procs);
            this.synchronizeJobInformation(externalJobs);
        }

        private void synchronizeJobInformation(ProcResult jobProc)
        {
            Dictionary<string, ProcForm> forms = this.byExternalJobID(jobProc.ProcessID, this.getListOfForms(jobProc.ProcessID));
            Dictionary<string, Tuple<string, string>> googleData = this.getGoogleDocsJobs();

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
                this.syncJobDataWorker.ReportProgress(current * 100 / jobCount);
            }
        }

        void syncJobDataWorker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            this.SynchronizeStartButton.Content = this.SynchronizeButtonText;
            Process.Start(this.rpmApiUrl);
        }

        void syncJobDataWorker_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            int progress = e.ProgressPercentage;
            this.SynchronizeStartButton.Content = "Synchronizing RPM: " + progress + "%";
        }
        #endregion

        private void SynchronizeStartButton_Click(object sender, RoutedEventArgs e)
        {
            this.SynchronizeStartButton.Content = "Synchronizing RPM...";
            this.doGoogleAuth();
            this.syncJobDataWorker.RunWorkerAsync();
        }

        #region RPM API Interaction
        private Boolean testRPMAPI()
        {
            RPMApi rpm = new RPMApi(this.rpmApiUrl, this.rpmApiKey);
            InfoResult info = rpm.info();
            return true;
        }

        private ProcsResult getAllProcs()
        {
            RPMApi rpm = new RPMApi(this.rpmApiUrl, this.rpmApiKey);
            ProcsResult p = rpm.Procs();
            return p;
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

        private Dictionary<string, Tuple<string, string>> getGoogleDocsJobs()
        {
            Dictionary<string, Tuple<string, string>> jobs = new Dictionary<string, Tuple<string, string>>();

            this.ws = this.access.getDataWorksheet();
            AtomLink feedLink = this.ws.Links.FindService(GDataSpreadsheetsNameTable.ListRel, null);
            ListQuery listQuery = new ListQuery(feedLink.HRef.ToString());
            ListFeed feed = this.access.service.Query(listQuery);

            foreach (ListEntry row in feed.Entries)
            {
                string key  = null;
                string col1 = null;
                string col2 = null;
                foreach (ListEntry.Custom element in row.Elements)
                {
                    if (key == null) key = element.Value;
                    else if (col1 == null) col1 = element.Value;
                    else if (col2 == null) col2 = element.Value;
                }
                jobs.Add(key, new Tuple<string,string>(col1, col2));
            }
            return jobs;
        }

        private ProcResult getProc(string procName , ProcsResult procs)
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
        #endregion

        #region AsyncWork For Loading Data into Google Docs
        private string OpenGoogleSpreadsheetButtonText;
        void worker_DoWork(object sender, DoWorkEventArgs e)
        {
            CellFeed cellFeed = access.service.Query(new CellQuery(ws.CellFeedLink));
            cellFeed.Insert(new CellEntry(1, 1, "JobID"));
            cellFeed.Insert(new CellEntry(1, 2, "Job Description"));
            cellFeed.Insert(new CellEntry(1, 3, "Job Location"));

            this.writeJobsToGoogleDocs(cellFeed);
        }
        void worker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            this.OpenGoogleSpreadsheet.Content = this.OpenGoogleSpreadsheetButtonText;
            Process.Start(access.getSpreadsheetURL(this.appName));
        }

        void worker_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            int progress = e.ProgressPercentage;
            this.OpenGoogleSpreadsheet.Content = "Populating Worksheet: " + progress + "%";
        }

        private void writeJobsToGoogleDocs(CellFeed cellFeed)
        {
            Random rand = new Random();
            List<String> names = Enumerable.ToList<String>(this.siteNames.Keys);
            int nameCount = this.siteNames.Count;
            for (int i = 0; i < 40; i++)
            {
                string randName = names[rand.Next(nameCount)];
                int count = this.siteNames[randName];
                this.writeJob(randName, count, (uint)i, cellFeed);
                this.siteNames[randName] += 1;
                this.writeToGoogleWorker.ReportProgress(i * 100 / 40);
            }
        }

        private void writeJob(string siteName, int count, uint index, CellFeed cellFeed)
        {
            Random rand = new Random();
            siteName += " " + new String('I', rand.Next(2) + 1);

            string description = this.partNames[rand.Next(partNames.Count - 1)] + " " +
                this.repairActivities[rand.Next(repairActivities.Count - 1)];

            cellFeed.Insert(new CellEntry(index + 2, 1, "JOB" + index.ToString("0000#")));
            cellFeed.Insert(new CellEntry(index + 2, 2, description));
            cellFeed.Insert(new CellEntry(index + 2, 3, siteName));
        }


        #endregion
    }
}