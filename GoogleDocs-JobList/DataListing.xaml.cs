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
using System.Windows.Shapes;
using System.Collections.ObjectModel;

using RPM.ApiResults;

namespace GoogleDocs_JobList
{
    /// <summary>
    /// Interaction logic for DataListing.xaml
    /// </summary>
    public partial class DataListing : Window
    {

        public ObservableCollection<JobInfo> Jobs { private set; get; }
        private Dictionary<string, JobInfo> jobsById;

        public DataListing()
        {
            InitializeComponent();
            this.DataContext = this;
            this.Jobs = new ObservableCollection<JobInfo>();
        }

        private void updateGoogleSpreadsheetData()
        {
            //GoogleSpreadsheetAccess access = new GoogleSpreadsheetAccess(
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            App app = (App)Application.Current;
            app.JobInfoReceived += app_JobInfoReceived;
            app.getJobsListAsync();
        }

        void app_JobInfoReceived(object sender, Dictionary<string, JobInfo> e)
        {
            this.jobsById = e;
            this.Jobs.Clear();
            foreach (JobInfo job in e.Values)
            {
                this.Jobs.Add(job);
            }
        }
    }
}
