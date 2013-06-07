using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

using System.ComponentModel;
using System.Diagnostics;
using System.IO;

using GoogleDocs_JobList.Properties;
using GoogleDocs_JobList.AsyncWork;
using System.Text;
using Microsoft.Win32;

namespace GoogleDocs_JobList
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        public event EventHandler<Dictionary<string, JobInfo>> JobInfoReceived;
        public event ProgressChangedEventHandler RPMSyncProgress;
        public event EventHandler<int> RPMSyncComplete;

        public event EventHandler GoogleSpreadsheetCreationStarted;
        public event ProgressChangedEventHandler GoogleSpreadsheetCreationProgress;
        public event RunWorkerCompletedEventHandler GoogleSpreadsheetCreationComplete;

        private GoogleSpreadsheetAccess googleAccess;
        private RPMSync sync;

        private string googleAppName = GoogleOAuthSettings.ApplicationName;
        private string googleClientId = GoogleOAuthSettings.OAuthClientId;
        private string googleClientSecret = GoogleOAuthSettings.OAuthClientSecret;
        private string OAuthAccessCode;
        private string OAuthAccessToken;
        private string OAuthRefreshToken;

        public Dictionary<string, JobInfo> Jobs
        {
            get;
            private set;
        }

        private bool googleConnectionComplete = false;

        public string RpmApiUrl { get; private set; }
        private string rpmApiKey;

        public App()
        {
            this.setup();
        }

        public void setup()
        {
            this.setupGoogleAccess();
            if (this.settingsAreComplete())
            {
                this.sync = new RPMSync(this.RpmApiUrl, this.rpmApiKey);
                this.sync.WorkComplete += sync_WorkComplete;
                this.sync.ProgressChanged += sync_ProgressChanged;
            }
        }

        private void setupGoogleAccess()
        {
            this.loadSettings();
            if (this.OAuthAccessCode != "")
            {
                this.googleAccess = new GoogleSpreadsheetAccess(
                    this.googleAppName,
                    this.googleClientId,
                    this.googleClientSecret,
                    this.OAuthAccessCode,
                    this.OAuthAccessToken,
                    this.OAuthRefreshToken
                );
                this.googleAccess.JobInfoReceived += googleAccess_JobInfoReceived;
                string outAccessToken;
                string outRefreshToken;
                this.googleAccess.connect(out outAccessToken, out outRefreshToken);

                this.saveSetting("OAuthAccessToken", outAccessToken);
                this.saveSetting("OAuthRefreshToken", outRefreshToken);
                this.googleConnectionComplete = true;
            }
        }

        public void getJobsListAsync()
        {
            this.googleAccess.getGoogleDocsJobsAsync();
        }

        private void googleAccess_JobInfoReceived(object sender, Dictionary<string, JobInfo> e)
        {
            this.Jobs = e;
            this.JobInfoReceived(this, this.Jobs);
        }

        private void googleAccess_RefreshTokenChanged(object sender, string e)
        {
            this.saveSetting("OAuthRefreshToken", e);
        }

        private void googleAccess_AccessTokenChanged(object sender, string e)
        {
            this.saveSetting("OAuthAccessToken", e);
        }

        private void loadSettings()
        {
            this.RpmApiUrl = (string)Settings.Default["RpmApiUrl"];
            this.rpmApiKey = (string)Settings.Default["RpmApiKey"];
            this.OAuthAccessCode = (string)Settings.Default["OAuthAccessCode"];
            this.OAuthAccessToken = (string)Settings.Default["OAuthAccessToken"];
            if (this.OAuthAccessToken == null)
            {
                this.OAuthAccessToken = "";
            }
            this.OAuthRefreshToken = (string)Settings.Default["OAuthRefreshToken"];
            if (this.OAuthRefreshToken == null)
            {
                this.OAuthRefreshToken = "";
            }
        }

        private void saveSetting(string key, string value)
        {
            Settings.Default[key] = value;
            Settings.Default.Save();
            if (key == "OAuthAccessCode" && value != "")
            {
                this.setupGoogleAccess();
                this.createGoogleWorksheet();
            }
            this.loadSettings();
        }

        private void createGoogleWorksheet()
        {
            if (this.googleConnectionComplete)
            {
                GoogleSpreadsheetWriter writer = new GoogleSpreadsheetWriter(this.googleAccess);
                writer.ProgressChanged += writer_ProgressChanged;
                writer.WorkComplete += writer_WorkComplete;
                writer.run();
                this.GoogleSpreadsheetCreationStarted(this, null);
            }
        }

        void writer_WorkComplete(object sender, RunWorkerCompletedEventArgs e)
        {
            this.GoogleSpreadsheetCreationComplete(this, e);
        }

        void writer_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            this.GoogleSpreadsheetCreationProgress(this, e);
        }

        public void syncRPMAsync()
        {
            this.sync.run(this.Jobs);
        }

        void sync_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            this.RPMSyncProgress(sender, e);
        }

        void sync_WorkComplete(object sender, RunWorkerCompletedEventArgs e)
        {
            this.RPMSyncComplete(this, this.sync.getJobProcessID());
        }

        public void openWorksheetInBrowser()
        {
            Process.Start(this.googleAccess.getSpreadsheetURL(this.googleAppName));
        }

        internal void openRPMInBrowser()
        {
            Process.Start(this.RpmApiUrl + "/?Page=SelectProjectByTemplate.aspx&Item=t" + sync.getJobProcessID()  + "*");
        }

        public void saveJobXML()
        {
            SaveFileDialog fd = new SaveFileDialog();
            fd.FileName = "External_JobInformation";
            fd.DefaultExt = ".xml";
            fd.Filter = "XML Document (.xml)|*.xml";

            Nullable<bool> result = fd.ShowDialog();
            if (result == true)
            {
                File.WriteAllText(
                    fd.FileName,
                    GoogleDocs_JobList.Properties.Resources.External_JobInformation,
                    Encoding.UTF8
                );
            }
        }

        public void showSetupWindow(bool forceShow = false)
        {
            if (!this.settingsAreComplete() || forceShow)
            {
                SetupWindow w = new SetupWindow(
                    this.OAuthAccessToken,
                    this.RpmApiUrl, this.rpmApiKey,
                    this.googleClientId,
                    this.googleClientSecret
                );
                w.SetupOptionChanged += this.SaveSetupOption;
                w.ShowDialog();
            }
        }

        public bool settingsAreComplete()
        {
            this.loadSettings();
            return this.OAuthAccessCode != ""
                && this.RpmApiUrl != ""
                && this.rpmApiKey != "";
        }

        private void SaveSetupOption(object sender, AppSetupChangedEventArgs e)
        {
            this.saveSetting(e.key, e.value);
        }
    }
}
