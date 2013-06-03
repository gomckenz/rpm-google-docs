using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel;

using Google.GData.Spreadsheets;

namespace GoogleDocs_JobList.AsyncWork
{
    class GoogleSpreadsheetWriter
    {

        public event ProgressChangedEventHandler ProgressChanged;
        public event RunWorkerCompletedEventHandler WorkComplete;

        private readonly BackgroundWorker writeToGoogleWorker = new BackgroundWorker();
        private GoogleSpreadsheetAccess access;

        public GoogleSpreadsheetWriter(GoogleSpreadsheetAccess access)
        {
            this.access = access;

            this.writeToGoogleWorker.WorkerReportsProgress = true;
            this.writeToGoogleWorker.DoWork += writeDataToGoogle;
            this.writeToGoogleWorker.RunWorkerCompleted += writingComplete;
            this.writeToGoogleWorker.ProgressChanged += writingProgressChanged;
        }

        public void run()
        {
            this.writeToGoogleWorker.RunWorkerAsync();
        }

        public bool setup()
        {
            WorksheetEntry ws = this.access.getDataWorksheet();
            if (ws.Title.Text != "Data")
            {
                ws.Title.Text = "Data";
                ws.Update();
                return true;
            }
            return false;
        }

        #region Async Events
        void writeDataToGoogle(object sender, DoWorkEventArgs e)
        {
            WorksheetEntry ws = this.access.getDataWorksheet();
            CellFeed cellFeed = this.access.service.Query(new CellQuery(ws.CellFeedLink));
            cellFeed.Insert(new CellEntry(1, 1, "JobID"));
            cellFeed.Insert(new CellEntry(1, 2, "Job Description"));
            cellFeed.Insert(new CellEntry(1, 3, "Job Location"));

            this.writeJobsToGoogleDocs(cellFeed);
        }
        void writingProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            this.ProgressChanged(sender, e);
        }
        void writingComplete(object sender, RunWorkerCompletedEventArgs e)
        {
            this.WorkComplete(sender, e);
        }
        #endregion

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
    }
}
