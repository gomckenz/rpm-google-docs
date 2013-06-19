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
using RPM.ApiResults;

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

        private const string PLACEHOLDER_URL =  "https://example.com/rpm/";
        private const string PLACEHOLDER_KEY =  "xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx";

        public SetupWindow(string OAuthAccessCode, string rpmApiUrl, string rpmApiKey, string clientId, string clientSecret)
        {
            InitializeComponent();
            this.OAuthAccessCode = OAuthAccessCode;
            this.RpmApiUrl.Text = rpmApiUrl;
            this.setTextboxPlaceholder(this.RpmApiUrl, PLACEHOLDER_URL);
            this.RpmApiKey.Text = rpmApiKey;
            this.setTextboxPlaceholder(this.RpmApiKey, PLACEHOLDER_KEY);
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
            string placeHolderText = (t.Name == "RpmApiUrl" ? PLACEHOLDER_URL : PLACEHOLDER_KEY);
            string value = t.Text;
            if (t.Text == placeHolderText)
            {
                value = "";
            }
            this.OnSetupOptionChanged(
                new AppSetupChangedEventArgs(t.Name, value)
            );
            this.setTextboxPlaceholder(t, placeHolderText);
        }

        private void setTextboxPlaceholder(TextBox t, string placeHolderText)
        {
            if (t.Text == placeHolderText || t.Text == "")
            {
                t.Text = placeHolderText;
                t.Foreground = Brushes.Gray;
            } else {
                t.Foreground = Brushes.Black;
            }
        }

        private void checkRPMAccess()
        {
            if (this.RpmApiKey.Text != PLACEHOLDER_KEY && this.RpmApiUrl.Text != PLACEHOLDER_URL)
            {
                string errorMessage = "";
                this.rpmAccessWorked = false;
                try
                {
                    RPMSync rpmAccess = new RPMSync(this.RpmApiUrl.Text, this.RpmApiKey.Text);
                    this.rpmAccessWorked = rpmAccess.infoSuccessful();
                }
                catch (WebException webex)
                {
                    errorMessage = "Could not connect to RPM: " + webex.Message;
                }
                catch (RPMApiError apiError)
                {
                    errorMessage = apiError.Message;
                }
                catch (ProcessNotFoundException pnfE)
                {
                    errorMessage = pnfE.Message;
                }
                finally
                {
                    if (errorMessage != "")
                    {
                        this.showRPMAccessError(errorMessage);
                    }
                }
            }
        }

        private void showRPMAccessError(string message = "Could Not Connect to RPM")
        {
            MessageBoxResult result = MessageBox.Show(
                message, "RPM Setup Error",
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
                this.Close();
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

        private void RpmApiUrl_GotFocus(object sender, RoutedEventArgs e)
        {
            this.RpmApiUrl.Foreground = Brushes.Black;
            if (this.RpmApiUrl.Text == PLACEHOLDER_URL)
            {
                this.RpmApiUrl.SelectAll();
            }
        }

        private void RpmApiKey_GotFocus(object sender, RoutedEventArgs e)
        {
            this.RpmApiKey.Foreground = Brushes.Black;
            if (this.RpmApiKey.Text == PLACEHOLDER_KEY)
            {
                this.RpmApiKey.SelectAll();
            }
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (this.RpmApiKey.Text == PLACEHOLDER_KEY ||
                this.RpmApiUrl.Text == PLACEHOLDER_URL ||
                this.OAuthAccessCode == ""
               )
            {
                MessageBox.Show("Setup is incomplete");
                e.Cancel = true;
                return;
            }
            this.checkRPMAccess();
            if (!this.rpmAccessWorked)
            {
                e.Cancel = true;
            }
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            this.checkRPMAccess();
        }
    }
}
