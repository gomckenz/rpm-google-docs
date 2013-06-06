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

using System.Windows.Media.Animation;
using System.Diagnostics;



using RPM.ApiResults;

namespace GoogleDocs_JobList
{
    /// <summary>
    /// Interaction logic for DataListing.xaml
    /// </summary>
    public partial class DataListing : Window
    {

        public ObservableCollection<JobInfo> Jobs { private set; get; }
        App app;
        private string SynchronizeButtonText;

        public DataListing()
        {
            InitializeComponent();
            this.SynchronizeButtonText = this.SynchronizeButton.Content.ToString();
            this.DataContext = this;
            this.Jobs = new ObservableCollection<JobInfo>();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            this.app = (App)Application.Current;
            this.app.JobInfoReceived += app_JobInfoReceived;
            this.app.RPMSyncComplete += app_RPMSyncComplete;
            this.app.RPMSyncProgress += app_RPMSyncProgress;
            this.loadJobsData();
        }

        private void setLoadingMessage(string message)
        {
            this.LoadingMessage.Content = message;  
        }

        private void clearLoadingMessage()
        {
            this.LoadingMessage.Content = "";
        }

        #region Google Data Download
        private void RefreshDataButton_Click(object sender, RoutedEventArgs e)
        {
            this.loadJobsData();
        }
        private void loadJobsData()
        {
            this.RefreshDataButton.IsEnabled = false;
            this.setLoadingMessage("Loading External Data");
            this.app.getJobsListAsync();
        }
        void app_JobInfoReceived(object sender, Dictionary<string, JobInfo> e)
        {
            this.Jobs.Clear();
            foreach (JobInfo job in this.app.Jobs.Values)
            {
                this.Jobs.Add(job);
            }
            this.RefreshDataButton.IsEnabled = true;
            this.clearLoadingMessage();
        } 
        #endregion

        #region RPM Sync
        private void SynchronizeButton_Click(object sender, RoutedEventArgs e)
        {
            this.SynchronizeButton.IsEnabled = false;
            Keyboard.Focus(this.FocusControl);
            this.setLoadingMessage("Updating RPM...");
            this.app.syncRPMAsync();
        }
        void app_RPMSyncComplete(object sender, int e)
        {
            this.SynchronizeButton.IsEnabled = true;
            this.SynchronizeButton.Content = this.SynchronizeButtonText;
            Keyboard.Focus(this.FocusControl);
            this.clearLoadingMessage();
        }
        void app_RPMSyncProgress(object sender, System.ComponentModel.ProgressChangedEventArgs e) { } 
        #endregion

        private void JobsDataButton_Click(object sender, RoutedEventArgs e)
        {
            this.app.openWorksheetInBrowser();
        }

        private void GoToRPM_Click(object sender, RoutedEventArgs e)
        {
            this.app.openRPMInBrowser();
        }

        private void SetupJobsXmlButton_Click(object sender, RoutedEventArgs e)
        {
            this.app.saveJobXML();
        }

        private void SetupButton_Click(object sender, RoutedEventArgs e)
        {
            Keyboard.Focus(this.FocusControl);
            this.app.showSetupWindow(true);
        }
    }
}
