using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using GoogleDocs_JobList.Properties;
using Google.GData.Spreadsheets;
using Google.GData.Client;
using Google.GData.Documents;

namespace GoogleDocs_JobList
{
    class GoogleSpreadsheetAccess
    {
        private string applicationName;
        private OAuth2Parameters parameters;
        public SpreadsheetsService service;

        public GoogleSpreadsheetAccess(string appName, string clientId, string clientSecret, string accessToken)
        {
            this.applicationName = appName;
            this.parameters = GoogleOauthAccess.getOAuth2Parameters(clientId, clientSecret, accessToken: accessToken);

            this.service = new SpreadsheetsService(applicationName);
            this.service.RequestFactory = GoogleOauthAccess.getRequestFactory(appName, this.parameters);

        }

        public WorksheetEntry getDataWorksheet()  
        {
            WorksheetEntry found = searchForSpreadsheet(this.applicationName);
            if (found == null)
            {
                found = createSpreadSheet(this.applicationName);
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
            DocumentsService docService = new DocumentsService(this.applicationName);
            docService.RequestFactory = GoogleOauthAccess.getRequestFactory(this.applicationName, this.parameters);

            DocumentEntry entry = new DocumentEntry();
            entry.Title.Text = sheetName;
            entry.Categories.Add(DocumentEntry.SPREADSHEET_CATEGORY);

            DocumentEntry newEntry = docService.Insert(DocumentsListQuery.documentsBaseUri, entry);

            WorksheetEntry theWS  =  this.searchForSpreadsheet(entry.Title.Text);
            theWS.Rows = 1;
            return theWS;
        }

        public string getSpreadsheetURL(string sheetName)
        {
            DocumentsService docService = new DocumentsService(this.applicationName);
            docService.RequestFactory = GoogleOauthAccess.getRequestFactory(this.applicationName, this.parameters);

            Google.GData.Spreadsheets.SpreadsheetQuery query = new Google.GData.Spreadsheets.SpreadsheetQuery();

            DocumentsListQuery docQuery = new DocumentsListQuery();
            docQuery.Title = sheetName;
            //docQuery.TitleExact = true;
            
            DocumentsFeed feed = docService.Query(docQuery);
            DocumentEntry entry = (DocumentEntry) feed.Entries[0];

            return "https://docs.google.com/spreadsheet/ccc?key=" + entry.ResourceId.Replace("spreadsheet:", "");
        }
    }
}
