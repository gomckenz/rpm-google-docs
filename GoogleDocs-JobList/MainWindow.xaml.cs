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

using System.Diagnostics;

using GoogleDocs_JobList.Properties;
using Google.GData.Spreadsheets;
using System.Collections;

using System.ComponentModel;

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
        // To get a clientId and Secret register an application at: https://code.google.com/apis/console
        private string clientId = (string)Settings.Default["GoogleAPIClientId"];
        private string clientSecret = (string)Settings.Default["GoogleAPIClientSecret"];
        private WorksheetEntry ws;
        private GoogleSpreadsheetAccess access;

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

        private readonly BackgroundWorker worker = new BackgroundWorker();


        public MainWindow()
        {
            InitializeComponent();
            this.appName = (string)Settings.Default["ApplicationName"];
            this.showSetupWindow();
            this.OpenSpreadsheetText = this.OpenGoogleSpreadsheet.Content.ToString();
            this.worker.DoWork += worker_DoWork;
            this.worker.RunWorkerCompleted += worker_RunWorkerCompleted;
            this.worker.WorkerReportsProgress = true;
            this.worker.ProgressChanged += worker_ProgressChanged;
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
            if (this.ws.Title.Text == "Sheet 1")
            {
                this.ws.Title.Text = "Data";
                this.ws.Update();
                worker.RunWorkerAsync();
            }
            else
            {
                this.OpenGoogleSpreadsheet.Content = this.OpenSpreadsheetText;
                Process.Start(access.getSpreadsheetURL(this.appName));
            }
            
        }

        

        private void SynchronizeStart_Click(object sender, RoutedEventArgs e)
        {
            //CellFeed cellFeed = access.service.Query(new CellQuery(ws.CellFeedLink));

        }
        #region AsyncWork For Loading Data into Google Docs
        private string OpenSpreadsheetText;
        void worker_DoWork(object sender, DoWorkEventArgs e)
        {
            CellFeed cellFeed = access.service.Query(new CellQuery(ws.CellFeedLink));
            cellFeed.Insert(new CellEntry(1, 1, "JobID"));
            cellFeed.Insert(new CellEntry(1, 2, "Job Description"));
            cellFeed.Insert(new CellEntry(1, 3, "Job Location"));

            this.loadJobs(cellFeed);
        }
        void worker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            this.OpenGoogleSpreadsheet.Content = this.OpenSpreadsheetText;
            Process.Start(access.getSpreadsheetURL(this.appName));
        }

        void worker_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            int progress = e.ProgressPercentage;
            this.OpenGoogleSpreadsheet.Content = "Populating Worksheet: " + progress + "%";
        }

        private void loadJobs(CellFeed cellFeed)
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
                this.worker.ReportProgress(i * 100 / 40);
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