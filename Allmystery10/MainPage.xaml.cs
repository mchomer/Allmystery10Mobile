using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Windows.UI.Core;
using Windows.UI.Popups;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;
using Windows.Web.Http.Filters;

// Die Vorlage "Leere Seite" ist unter http://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409 dokumentiert.

namespace Allmystery10
{
    /// <summary>
    /// Eine leere Seite, die eigenständig verwendet oder zu der innerhalb eines Rahmens navigiert werden kann.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        private bool alreadyhandled = false;
        private static readonly Uri HomeUri = new Uri("https://www.allmystery.de", UriKind.Absolute);

        private async Task<string> registerPushChannel()
        {
            var channelOperation = await Windows.Networking.PushNotifications.PushNotificationChannelManager.CreatePushNotificationChannelForApplicationAsync();
            channelOperation.PushNotificationReceived += ChannelOperation_PushNotificationReceived;
            return channelOperation.Uri.ToString();
        }

        private void ChannelOperation_PushNotificationReceived(Windows.Networking.PushNotifications.PushNotificationChannel sender, Windows.Networking.PushNotifications.PushNotificationReceivedEventArgs e)
        {
        }

        public MainPage()
        {

            this.InitializeComponent();
            this.NavigationCacheMode = NavigationCacheMode.Required;
            SystemNavigationManager.GetForCurrentView().BackRequested += MainPage_BackRequested;
        }

        private void MainPage_BackRequested(object sender, BackRequestedEventArgs e)
        {
            if (WebViewControl.CanGoBack)
            {
                WebViewControl.GoBack();
                e.Handled = true;
            }
            else
            {
                App.Current.Exit();
                e.Handled = true;
            }

        }

        private void WebViewControl_NavigationCompleted(WebView sender, WebViewNavigationCompletedEventArgs args)
        {
            if (!args.IsSuccess)
            {
                this.showNoInternetMessageAndCloseApp();
            }
        }

        /// <summary>
        /// Wird aufgerufen, wenn diese Seite in einem Rahmen angezeigt werden soll.
        /// </summary>
        /// <param name="e">Ereignisdaten, die beschreiben, wie diese Seite erreicht wurde.
        /// Dieser Parameter wird normalerweise zum Konfigurieren der Seite verwendet.</param>
        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            WebViewControl.Navigate(HomeUri);
        }

        private async void GetBrowserCookie()
        {
            bool loggedin = false;
            try
            {
                var httpBaseProtocolFilter = new HttpBaseProtocolFilter();
                var cookieManager = httpBaseProtocolFilter.CookieManager;
                var cookieCollection = cookieManager.GetCookies(HomeUri);
                loggedin = cookieCollection.Where(c => c.Name.Equals("SESSID")).Count() > 0;
                if (loggedin)
                {
                    string channel = await registerPushChannel();
                    string[] objArray = new string[1];
                    objArray[0] = "updatePushToken({ mpn: \"" + channel + "\"})";
                    await WebViewControl.InvokeScriptAsync("eval", objArray);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
                this.showNoInternetMessageAndCloseApp();
            }

            if (loggedin == false)
            {
                if (alreadyhandled == false)
                {
                    try
                    {
                        List<string> parms = new List<string>();
                        parms.Add("$('.showSideNav').trigger('click');");
                        await WebViewControl.InvokeScriptAsync("eval", parms);
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine(ex.Message);
                        this.showNoInternetMessageAndCloseApp();
                    }
                }
            }
            alreadyhandled = true;
        }


        private void Browser_NavigationCompleted(WebView sender, WebViewNavigationCompletedEventArgs args)
        {
            if (!args.IsSuccess)
            {
                this.showNoInternetMessageAndCloseApp();
            }

        }


        /// <summary>
        /// Navigiert vorwärts im WebView-Verlauf.
        /// </summary>
        private void ForwardAppBarButton_Click(object sender, RoutedEventArgs e)
        {
            if (WebViewControl.CanGoForward)
            {
                WebViewControl.GoForward();
            }
        }

        private void WebViewControl_LoadCompleted(object sender, NavigationEventArgs e)
        {
            this.GetBrowserCookie();
        }

        private async void showNoInternetMessageAndCloseApp()
        {
            MessageDialog msg = new MessageDialog("Leider besteht keine Internetverbindung. App wird geschlossen.", "Kein Internet");
            await msg.ShowAsync();
            Debug.WriteLine("Navigation to this page failed, check your internet connection.");
            App.Current.Exit();
        }
    }

    public class PushMPN
    {
        public string mpn { get; set; }
    }
}
