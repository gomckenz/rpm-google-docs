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

        private string googleOAuthKey;
        private string rpmApiUrl;
        private string rpmApiKey;
        private string clientId;
        private string clientSecret;

        private bool rpmAccessWorked;

        public SetupWindow(string googleOAuthKey, string rpmApiUrl, string rpmApiKey, string clientId, string clientSecret)
        {
            InitializeComponent();
            this.googleOAuthKey = googleOAuthKey;
            this.rpmApiUrl = rpmApiUrl;
            this.rpmApiKey = rpmApiKey;
            this.clientId = clientId;
            this.clientSecret = clientSecret;
            if (this.googleOAuthKey != "")
            {
                this.AuthorizedLabel.Visibility = System.Windows.Visibility.Visible;
                this.GoogleAuthorizeButton.Content = "Deauthorize";
            }
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
            if (this.googleOAuthKey == "")
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
            if (this.googleOAuthKey != newCode)
            {
                this.OnSetupOptionChanged(
                    new AppSetupChangedEventArgs("GoogleOauthKey", newCode)
                );
            }
            this.googleOAuthKey = newCode;
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
                this.triggerOauthUpdate(successCode);
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

        private void RpmApiUrl_Loaded(object sender, RoutedEventArgs e)
        {
            TextBox t = (TextBox)sender;
            t.Text = this.rpmApiUrl;
            this.rpmAccessWorked = false;
        }

        private void RpmApiKey_Loaded(object sender, RoutedEventArgs e)
        {
            TextBox t = (TextBox)sender;
            t.Text = this.rpmApiKey;
            this.rpmAccessWorked = false;
        }

        private void checkRPMAccess()
        {
            if (this.rpmApiKey != "" && this.rpmApiUrl != "")
            {
                RPMSync rpmAccess = new RPMSync(this.rpmApiUrl, this.rpmApiKey);
                rpmAccess.AccessCheckComplete += rpmAccess_AccessCheckComplete;
                rpmAccess.checkRPMAccess();
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
            throw new NotImplementedException();
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
            App app = (App)Application.Current;
            app.saveJobXML();
        }

        private void loseFocus()
        {
            Keyboard.Focus(this.FocusControl);
        }
    }
}
