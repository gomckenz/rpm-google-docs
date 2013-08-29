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

        public DataListing()
        {
            InitializeComponent();
            this.DataContext = this;
            this.Jobs = new ObservableCollection<JobInfo>();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            this.app = (App)Application.Current;
            while (!this.app.settingsAreComplete())
            {
                this.app.showSetupWindow();
            }
            this.app.JobInfoReceived += app_JobInfoReceived;
            this.app.RPMSyncComplete += app_RPMSyncComplete;
            this.app.RPMSyncProgress += app_RPMSyncProgress;
            this.setupAnimation();
            this.clearLoadingMessage();
            this.loadJobsData();
        }

        private void setupAnimation()
        {
            DoubleAnimation rotate = new DoubleAnimation(0, 360, new Duration(TimeSpan.FromSeconds(.5)));
            rotate.RepeatBehavior = RepeatBehavior.Forever;
            RotateTransform rt = new RotateTransform();
            this.LoadingImage.RenderTransform = rt;
            this.LoadingImage.RenderTransformOrigin = new Point(0.5, 0.5);
            rt.BeginAnimation(RotateTransform.AngleProperty, rotate);
        }

        private void setLoadingMessage(string message)
        {
            this.LoadingImage.Visibility = Visibility.Visible;
            this.LoadingMessage.Content = message;  
        }

        private void clearLoadingMessage()
        {
            this.LoadingImage.Visibility = Visibility.Hidden;
            this.LoadingMessage.Content = "";
        }

        #region Google Data Download
        private void RefreshDataButton_Click(object sender, RoutedEventArgs e)
        {
            this.loseFocus();
            this.disableButtons();
            this.loadJobsData();
        }

        private void disableButtons()
        {
            this.RefreshDataButton.IsEnabled = false;
            this.SynchronizeButton.IsEnabled = false;
        }
        private void enableButtons()
        {
            this.RefreshDataButton.IsEnabled = true;
            this.SynchronizeButton.IsEnabled = true;
        }

        private void loadJobsData()
        {
            this.setLoadingMessage("Loading Jobs Data");
            this.app.getJobsListAsync();
        }
        void app_JobInfoReceived(object sender, Dictionary<string, JobInfo> e)
        {
            this.Jobs.Clear();
            foreach (JobInfo job in this.app.Jobs.Values)
            {
                this.Jobs.Add(job);
            }
            this.enableButtons();
            this.clearLoadingMessage();
        } 
        #endregion

        #region RPM Sync
        private void SynchronizeButton_Click(object sender, RoutedEventArgs e)
        {
            this.loseFocus();
            this.disableButtons();
            this.setLoadingMessage("Updating RPM");
            this.app.syncRPMAsync();
        }
        void app_RPMSyncComplete(object sender, int e)
        {
            this.enableButtons();
            this.clearLoadingMessage();
        }
        void app_RPMSyncProgress(object sender, System.ComponentModel.ProgressChangedEventArgs e) { } 
        #endregion

        private void JobsDataButton_Click(object sender, RoutedEventArgs e)
        {
            this.loseFocus();
            this.app.openWorksheetInBrowser();
        }

        private void GoToRPM_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                this.loseFocus();
                this.app.openRPMInBrowser();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void SetupButton_Click(object sender, RoutedEventArgs e)
        {
            this.loseFocus();
            this.app.showSetupWindow(true);
        }

        private void loseFocus()
        {
            Keyboard.Focus(this.FocusControl);
        }
    }
}
