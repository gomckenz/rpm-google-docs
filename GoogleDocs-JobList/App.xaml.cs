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

        private GoogleSpreadsheetAccess googleAccess;
        private RPMSync sync;

        private string googleAppName = GoogleOAuthSettings.ApplicationName;
        private string googleClientId = GoogleOAuthSettings.OAuthClientId;
        private string googleClientSecret = GoogleOAuthSettings.OAuthClientSecret;
        private string googleAccessToken;
        private string googleOAuthKey;
        private string googleRefreshToken;

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
            this.loadSettings();
            this.googleAccess = new GoogleSpreadsheetAccess(this.googleAppName, this.googleClientId, this.googleClientSecret, this.googleAccessToken, this.googleRefreshToken);
            this.googleAccess.AccessTokenChanged += googleAccess_AccessTokenChanged;
            this.googleAccess.RefreshTokenChanged += googleAccess_RefreshTokenChanged;
            this.googleAccess.ConnectionComplete += googleAccess_ConnectionComplete;
            this.googleAccess.JobInfoReceived += googleAccess_JobInfoReceived;

            this.sync = new RPMSync(this.RpmApiUrl, this.rpmApiKey);
            this.sync.WorkComplete += sync_WorkComplete;
            this.sync.ProgressChanged += sync_ProgressChanged;
        }

        public void getJobsListAsync()
        {
            if (!this.googleConnectionComplete)
            {
                this.googleAccess.ConnectionComplete += googleAccess_ConnectionCompleteDownload;
                this.googleAccess.connect();
            }
            else
            {
                this.googleAccess.getGoogleDocsJobsAsync();
            }
        }

        private void googleAccess_JobInfoReceived(object sender, Dictionary<string, JobInfo> e)
        {
            this.Jobs = e;
            this.JobInfoReceived(this, this.Jobs);
        }
        // Automatically Download the Jobs Data after connection
        private void googleAccess_ConnectionCompleteDownload(object sender, EventArgs e)
        {
            this.googleAccess.getGoogleDocsJobsAsync();
            this.googleAccess.ConnectionComplete -= googleAccess_ConnectionCompleteDownload;
        }

        private void googleAccess_ConnectionComplete(object sender, EventArgs e)
        {
            this.googleConnectionComplete = true;
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
            this.googleOAuthKey = (string)Settings.Default["GoogleOauthKey"];
            this.RpmApiUrl = (string)Settings.Default["RpmApiUrl"];
            this.rpmApiKey = (string)Settings.Default["RpmApiKey"];
            this.googleAccessToken = (string)Settings.Default["OAuthAccessToken"];
            this.googleRefreshToken = (string)Settings.Default["OAuthRefreshToken"];
        }

        private void saveSetting(string key, string value)
        {
            Settings.Default[key] = value;
            Settings.Default.Save();
            this.loadSettings();
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
            File.WriteAllText(
                "C:/External_JobInformation.xml",
                GoogleDocs_JobList.Properties.Resources.External_JobInformation,
                Encoding.UTF8
            );
        }

        public void showSetupWindow(bool forceShow = false)
        {
            this.loadSettings();
            if (this.googleOAuthKey == "" || this.RpmApiUrl == "" || this.rpmApiKey == "" || forceShow)
            {
                SetupWindow w = new SetupWindow(
                    this.googleOAuthKey, 
                    this.RpmApiUrl, this.rpmApiKey,
                    this.googleClientId,
                    this.googleClientSecret
                );
                w.SetupOptionChanged += this.SaveSetupOption;
                w.ShowDialog();
            }
        }

        private void SaveSetupOption(object sender, AppSetupChangedEventArgs e)
        {
            this.saveSetting(e.key, e.value);
        }
    }
}
