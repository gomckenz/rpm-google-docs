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

        public SetupWindow(string googleOAuthKey, string rpmApiUrl, string rpmApiKey, string clientId, string clientSecret)
        {
            InitializeComponent();
            this.googleOAuthKey = googleOAuthKey;
            this.rpmApiUrl = rpmApiUrl;
            this.rpmApiKey = rpmApiKey;
            this.clientId = clientId;
            this.clientSecret = clientSecret;
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
            this.showBrowser();
            
            string urlForOauth = GoogleOauthAccess.getAuthorizationURL(this.clientId, this.clientSecret);
            this.Browser.Navigate(urlForOauth);
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
                this.OnSetupOptionChanged(
                    new AppSetupChangedEventArgs("GoogleOauthKey", successCode)
                );
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
        }

        private void RpmApiKey_Loaded(object sender, RoutedEventArgs e)
        {
            TextBox t = (TextBox)sender;
            t.Text = this.rpmApiKey;
        }
    }
}
