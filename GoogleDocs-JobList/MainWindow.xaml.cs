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

using System.Collections;
using System.Diagnostics;
using System.ComponentModel;

using GoogleDocs_JobList.Properties;
using GoogleDocs_JobList.AsyncWork;

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
        private GoogleSpreadsheetAccess access;

        private string OpenGoogleSpreadsheetButtonText;
        private string SynchronizeButtonText;

        public MainWindow()
        {
            InitializeComponent();
            this.appName = (string)Settings.Default["ApplicationName"];
            this.showSetupWindow();

            this.OpenGoogleSpreadsheetButtonText = this.OpenGoogleSpreadsheet.Content.ToString();
            this.SynchronizeButtonText = this.SynchronizeStartButton.Content.ToString();
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
            Keyboard.Focus(this.FocusControl);
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

            GoogleSpreadsheetWriter googleWriter = new GoogleSpreadsheetWriter(this.access);
            googleWriter.ProgressChanged += googleWriter_ProgressChanged;
            googleWriter.WorkComplete += googleWriter_WorkComplete;
            if (googleWriter.setup()) {
                googleWriter.run();
            }
            else
            {
                this.OpenGoogleSpreadsheet.Content = this.OpenGoogleSpreadsheetButtonText;
                Process.Start(access.getSpreadsheetURL(this.appName));
            }
        }

        void googleWriter_WorkComplete(object sender, RunWorkerCompletedEventArgs e)
        {
            this.OpenGoogleSpreadsheet.Content = this.OpenGoogleSpreadsheetButtonText;
            Process.Start(access.getSpreadsheetURL(this.appName));
            Keyboard.Focus(this.FocusControl);
        }

        void googleWriter_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            int progress = e.ProgressPercentage;
            this.OpenGoogleSpreadsheet.Content = "Populating Worksheet: " + progress + "%";
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

            //this.access = new GoogleSpreadsheetAccess(
            //    this.appName, this.clientId,
            //    this.clientSecret, this.accessToken
            //);
        }

        private void SynchronizeStartButton_Click(object sender, RoutedEventArgs e)
        {
            this.SynchronizeStartButton.Content = "Synchronizing RPM...";
            this.doGoogleAuth();

            //RPMSync sync = new RPMSync(this.rpmApiUrl, this.rpmApiKey, this.access.getGoogleDocsJobs());
            //sync.ProgressChanged += sync_ProgressChanged;
            //sync.WorkComplete += sync_WorkComplete;
            //sync.run();
        }

        void sync_WorkComplete(object sender, RunWorkerCompletedEventArgs e)
        {
            this.SynchronizeStartButton.Content = this.SynchronizeButtonText;
            RPMSync sync = (RPMSync)sender;
            Process.Start(this.rpmApiUrl + "/?Page=SelectProjectByTemplate.aspx&Item=t" + sync.getJobProcessID() + "*");
            Keyboard.Focus(this.FocusControl);
        }

        void sync_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            int progress = e.ProgressPercentage;
            this.SynchronizeStartButton.Content = "Synchronizing RPM: " + progress + "%";
        }
    }
}