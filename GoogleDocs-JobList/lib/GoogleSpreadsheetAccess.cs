using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.ComponentModel;

using GoogleDocs_JobList.Properties;
using Google.GData.Spreadsheets;
using Google.GData.Client;
using Google.GData.Documents;

namespace GoogleDocs_JobList
{
    class GoogleSpreadsheetAccess
    {
        private BackgroundWorker ConnectWorker = new BackgroundWorker();
        private BackgroundWorker DownloadDataWorker = new BackgroundWorker();

        public event EventHandler ConnectionComplete;
        public event EventHandler<string> AccessTokenChanged;
        public event EventHandler<string> RefreshTokenChanged;
        public event EventHandler<Dictionary<string, JobInfo>> JobInfoReceived;

        private OAuth2Parameters parameters;
        public SpreadsheetsService service;

        private string googleAppName;
        private string googleClientId = GoogleOAuthSettings.OAuthClientId;
        private string googleClientSecret = GoogleOAuthSettings.OAuthClientSecret;
        private string googleAccessToken;
        private string googleOAuthKey;
        private string googleRefreshToken;

        private Dictionary<string, JobInfo> googleDocsJobs;

        private string rpmApiUrl;
        private string rpmApiKey;

        public GoogleSpreadsheetAccess(string appName, string clientId, string clientSecret, string accessToken, string refreshToken)
        {
            
            this.googleAppName = appName;
            this.googleClientId = clientId;
            this.googleClientSecret = clientSecret;
            this.googleAccessToken = accessToken;
            this.googleRefreshToken = refreshToken;

            this.parameters = GoogleOauthAccess.getOAuth2Parameters(clientId, clientSecret, accessToken: accessToken);

            this.service = new SpreadsheetsService(appName);
            this.service.RequestFactory = GoogleOauthAccess.getRequestFactory(appName, this.parameters);

            this.ConnectWorker.DoWork += ConnectWorker_DoWork;
            this.ConnectWorker.RunWorkerCompleted += ConnectWorker_RunWorkerCompleted;
            
            this.DownloadDataWorker.DoWork += DownloadDataWorker_DoWork;
            this.DownloadDataWorker.RunWorkerCompleted += DownloadDataWorker_RunWorkerCompleted;
        }

        #region Connection Code
        public void connect()
        {
            this.ConnectWorker.RunWorkerAsync();
        }

        void ConnectWorker_DoWork(object sender, DoWorkEventArgs e)
        {
            if (this.googleAccessToken == "")
            {
                GoogleOauthAccess.getAccessTokens(
                    this.googleClientId, this.googleClientSecret, this.googleOAuthKey,
                    out this.googleAccessToken, out this.googleRefreshToken
                );
            }
            else
            {
                this.googleAccessToken = GoogleOauthAccess.getRefreshedAccessToken(
                    this.googleClientId, this.googleClientSecret, this.googleRefreshToken
                );
            }
        }

        void ConnectWorker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            this.AccessTokenChanged(this, this.googleAccessToken);
            this.RefreshTokenChanged(this, this.googleRefreshToken);
            this.ConnectionComplete(this, null);
        } 
        #endregion

        

        public WorksheetEntry getDataWorksheet()
        {
            WorksheetEntry found = searchForSpreadsheet(this.googleAppName);
            if (found == null)
            {
                found = createSpreadSheet(this.googleAppName);
            }
            return found;
        }

        private WorksheetEntry searchForSpreadsheet(string sheetName)
        {
            Google.GData.Spreadsheets.SpreadsheetQuery query = new Google.GData.Spreadsheets.SpreadsheetQuery();
            SpreadsheetFeed feed = service.Query(query);
            foreach (SpreadsheetEntry entry in feed.Entries)
            {
                if (entry.Title.Text == sheetName)
                {
                    return (WorksheetEntry)entry.Worksheets.Entries[0];
                }
            }
            return null;
        }

        private WorksheetEntry createSpreadSheet(string sheetName)
        {
            DocumentsService docService = new DocumentsService(this.googleAppName);
            docService.RequestFactory = GoogleOauthAccess.getRequestFactory(this.googleAppName, this.parameters);

            DocumentEntry entry = new DocumentEntry();
            entry.Title.Text = sheetName;
            entry.Categories.Add(DocumentEntry.SPREADSHEET_CATEGORY);

            DocumentEntry newEntry = docService.Insert(DocumentsListQuery.documentsBaseUri, entry);

            return this.searchForSpreadsheet(entry.Title.Text);
        }

        public string getSpreadsheetURL(string sheetName)
        {
            DocumentsService docService = new DocumentsService(this.googleAppName);
            docService.RequestFactory = GoogleOauthAccess.getRequestFactory(this.googleAppName, this.parameters);

            Google.GData.Spreadsheets.SpreadsheetQuery query = new Google.GData.Spreadsheets.SpreadsheetQuery();

            DocumentsListQuery docQuery = new DocumentsListQuery();
            docQuery.Title = sheetName;

            DocumentsFeed feed = docService.Query(docQuery);
            DocumentEntry entry = (DocumentEntry)feed.Entries[0];

            return "https://docs.google.com/spreadsheet/ccc?key=" + entry.ResourceId.Replace("spreadsheet:", "");
        }

        #region Data Download Code
        private Dictionary<string, JobInfo> getGoogleDocsJobs()
        {
            Dictionary<string, JobInfo> jobs = new Dictionary<string, JobInfo>();

            WorksheetEntry ws = this.getDataWorksheet();
            AtomLink feedLink = ws.Links.FindService(GDataSpreadsheetsNameTable.ListRel, null);
            ListQuery listQuery = new ListQuery(feedLink.HRef.ToString());
            ListFeed feed = this.service.Query(listQuery);

            foreach (ListEntry row in feed.Entries)
            {
                string key = null;
                string col1 = null;
                string col2 = null;
                foreach (ListEntry.Custom element in row.Elements)
                {
                    if (key == null) key = element.Value;
                    else if (col1 == null) col1 = element.Value;
                    else if (col2 == null) col2 = element.Value;
                }
                jobs.Add(key, new JobInfo(key, col1, col2));
            }
            return jobs;
        }

        public void getGoogleDocsJobsAsync()
        {
            this.DownloadDataWorker.RunWorkerAsync();
        }

        void DownloadDataWorker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            this.JobInfoReceived(this, this.googleDocsJobs);
        }

        void DownloadDataWorker_DoWork(object sender, DoWorkEventArgs e)
        {
            this.googleDocsJobs = this.getGoogleDocsJobs();
        } 
        #endregion

    }
}
