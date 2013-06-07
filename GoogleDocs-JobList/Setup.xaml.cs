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
using System.Windows.Media.Animation;

using GoogleDocs_JobList.AsyncWork;
using System.Net;

namespace GoogleDocs_JobList
{


    public delegate void AppSetupChangedHandler(object sender, AppSetupChangedEventArgs e);

    /// <summary>
    /// Interaction logic for Window1.xaml
    /// </summary>
    public partial class SetupWindow : Window
    {

        public event AppSetupChangedHandler SetupOptionChanged;
        
        private static DoubleAnimation hideAnimation = new DoubleAnimation(1, 0, TimeSpan.FromSeconds(.2));
        private static DoubleAnimation showAnimation = new DoubleAnimation(0, 1, TimeSpan.FromSeconds(.2));

        private string OAuthAccessCode;
        private string clientId;
        private string clientSecret;
        private App app;
        private bool rpmAccessWorked;

        public SetupWindow(string OAuthAccessCode, string rpmApiUrl, string rpmApiKey, string clientId, string clientSecret)
        {
            InitializeComponent();
            this.OAuthAccessCode = OAuthAccessCode;
            this.RpmApiUrl.Text = rpmApiUrl;
            this.RpmApiKey.Text = rpmApiKey;
            this.clientId = clientId;
            this.clientSecret = clientSecret;
            if (this.OAuthAccessCode != "")
            {
                this.AuthorizedLabel.Visibility = System.Windows.Visibility.Visible;
                this.GoogleAuthorizeButton.Content = "Deauthorize";
            }
            this.app = (App)Application.Current;
            this.app.GoogleSpreadsheetCreationStarted += app_GoogleSpreadsheetCreationStarted;
            this.app.GoogleSpreadsheetCreationComplete += app_GoogleSpreadsheetCreationComplete;
            this.app.GoogleSpreadsheetCreationProgress += app_GoogleSpreadsheetCreationProgress;
        }

        void app_GoogleSpreadsheetCreationStarted(object sender, EventArgs e)
        {
            this.DoneButton.IsEnabled = false;
            this.GoogleAuthorizeButton.Content = "Creating Data...";
        }

        void app_GoogleSpreadsheetCreationProgress(object sender, System.ComponentModel.ProgressChangedEventArgs e)
        {
            this.GoogleAuthorizeButton.Content = "Creating Data " + e.ProgressPercentage + "%";
        }

        void app_GoogleSpreadsheetCreationComplete(object sender, System.ComponentModel.RunWorkerCompletedEventArgs e)
        {
            this.AuthorizedLabel.Visibility = System.Windows.Visibility.Visible;
            this.DoneButton.IsEnabled = true;
            this.GoogleAuthorizeButton.Content = "Deauthorize";
        }

        public virtual void OnSetupOptionChanged(AppSetupChangedEventArgs e)
        {
            if (SetupOptionChanged != null)
            {
                SetupOptionChanged(this, e);
            }
        }

        private void GoogleAuthorizeButton_Click(object sender, RoutedEventArgs e)
        {
            this.loseFocus();
            if (this.OAuthAccessCode == "")
            {
                this.DoneButton.Content = "Cancel";
                this.showBrowser();

                string urlForOauth = GoogleOauthAccess.getAuthorizationURL(this.clientId, this.clientSecret);
                this.Browser.Navigate(urlForOauth);
            }
            else
            {
                this.triggerOauthUpdate("");
            }
            
        }

        private void triggerOauthUpdate(string newCode)
        {
            if (newCode == "")
            {
                this.GoogleAuthorizeButton.Content = "Authorize";
                this.AuthorizedLabel.Visibility = System.Windows.Visibility.Hidden;
            }
            else
            {
                this.GoogleAuthorizeButton.Content = "Deauthorize";
                this.AuthorizedLabel.Visibility = System.Windows.Visibility.Visible;
            }
            if (this.OAuthAccessCode != newCode)
            {
                this.OnSetupOptionChanged(
                    new AppSetupChangedEventArgs("OAuthAccessCode", newCode)
                );
            }
            this.OAuthAccessCode = newCode;
        }

        private void Browser_Navigated(object sender, System.Windows.Navigation.NavigationEventArgs e)
        {
            WebBrowser b = (WebBrowser)sender;
            mshtml.HTMLDocument doc = (mshtml.HTMLDocument)b.Document;
            string url = doc.url;
            string title = doc.title;
            if (title.Contains("Success"))
            {
                string successCode = title.Substring(title.IndexOf("=") + 1);
                this.triggerOauthUpdate(successCode); // This will trigger creating the worksheet
                this.hideBrowser();
            }
        }

        private void showBrowser()
        {
            this.hideShowGrid(this.GoogleAuthGrid, showAnimation);
            this.hideShowGrid(this.SetupOptionsGrid, hideAnimation);
            this.GoogleAuthGrid.Visibility = System.Windows.Visibility.Visible;
        }

        private void hideBrowser()
        {
            this.hideShowGrid(this.GoogleAuthGrid, hideAnimation);
            this.hideShowGrid(this.SetupOptionsGrid, showAnimation);
            this.GoogleAuthGrid.Visibility = System.Windows.Visibility.Hidden;
            this.Browser.Navigate("about:blank");
            this.DoneButton.Content = "Done";
        }

        private void hideShowGrid(Grid g, DoubleAnimation animation)
        {
            g.BeginAnimation(Grid.OpacityProperty, animation);
        }

        private void TextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            TextBox t = (TextBox)sender;
            this.OnSetupOptionChanged(
                new AppSetupChangedEventArgs(t.Name, t.Text)
            );
        }

        private void checkRPMAccess()
        {
            if (this.RpmApiKey.Text != "" && this.RpmApiUrl.Text != "")
            {
                try
                {
                    RPMSync rpmAccess = new RPMSync(this.RpmApiUrl.Text, this.RpmApiKey.Text);
                    rpmAccess.AccessCheckComplete += rpmAccess_AccessCheckComplete;
                    rpmAccess.checkRPMAccess();
                }
                catch (WebException webex)
                {
                    if (webex.Message == "Not Found")
                    {
                        this.rpmAccessWorked = false;
                        this.showRPMAccessError();
                    }
                }
            }
        }

        void rpmAccess_AccessCheckComplete(object sender, System.ComponentModel.RunWorkerCompletedEventArgs e)
        {
            this.rpmAccessWorked = ((RPMSync)sender).infoSuccessful();
            if (this.rpmAccessWorked)
            {
                this.Close();
            }
            else
            {
                this.showRPMAccessError();
            }
        }

        private void showRPMAccessError()
        {
            MessageBoxResult result = MessageBox.Show(
                "Could Not Connect to RPM", "Error",
                MessageBoxButton.OK, MessageBoxImage.Error
            );
        }

        private void DoneButton_Click(object sender, RoutedEventArgs e)
        {
            this.loseFocus();
            if (this.DoneButton.Content.ToString() == "Cancel")
            {
                this.DoneButton.Content = "Done";
                this.hideBrowser();
            }
            else
            {
                this.checkRPMAccess();
            }
        }

        private void DownloadRPMPRocessButton_Click(object sender, RoutedEventArgs e)
        {
            this.loseFocus();
            this.app.saveJobXML();
        }

        private void loseFocus()
        {
            Keyboard.Focus(this.FocusControl);
        }
    }
}
