using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

using GoogleDocs_JobList.Properties;

namespace GoogleDocs_JobList
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        public event EventHandler<Dictionary<string, JobInfo>> JobInfoReceived;
        private GoogleSpreadsheetAccess googleAccess;

        private string googleAppName = GoogleOAuthSettings.ApplicationName;
        private string googleClientId = GoogleOAuthSettings.OAuthClientId;
        private string googleClientSecret = GoogleOAuthSettings.OAuthClientSecret;
        private string googleAccessToken;
        private string googleOAuthKey;
        private string googleRefreshToken;

        private bool googleConnectionComplete = false;

        private string rpmApiUrl;
        private string rpmApiKey;

        public App()
        {
            this.loadSettings();
            this.googleAccess = new GoogleSpreadsheetAccess(this.googleAppName, this.googleClientId, this.googleClientSecret, this.googleAccessToken, this.googleRefreshToken);
            this.googleAccess.AccessTokenChanged += googleAccess_AccessTokenChanged;
            this.googleAccess.RefreshTokenChanged += googleAccess_RefreshTokenChanged;
            this.googleAccess.ConnectionComplete += googleAccess_ConnectionComplete;
            this.googleAccess.JobInfoReceived += googleAccess_JobInfoReceived;
            //this.googleAccess.connect();
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
            this.JobInfoReceived(this, e);
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
            this.rpmApiUrl = (string)Settings.Default["RpmApiUrl"];
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
    }
}
