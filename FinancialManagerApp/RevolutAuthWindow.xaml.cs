using System;
using System.Windows;
using Microsoft.Web.WebView2.Core;

namespace FinancialManagerApp.Views
{
    public partial class RevolutAuthWindow : Window
    {
        public string CapturedAuthCode { get; private set; }

        // Ten adres musi być TAKI SAM jak w ustawieniach Revolut Business (Redirect URI)
        private const string REDIRECT_URI = "https://www.google.com";

        public RevolutAuthWindow(string clientId, bool isSandbox)
        {
            InitializeComponent();
            InitializeAsync(clientId, isSandbox);
        }

        private async void InitializeAsync(string clientId, bool isSandbox)
        {
            await webView.EnsureCoreWebView2Async();

            // Budujemy URL do logowania
            string baseUrl = isSandbox
                ? "https://sandbox-business.revolut.com/app-confirm"
                : "https://business.revolut.com/app-confirm";

            // response_type=code -> chcemy otrzymać kod autoryzacyjny
            string authUrl = $"{baseUrl}?client_id={clientId}&redirect_uri={REDIRECT_URI}&response_type=code";

            webView.Source = new Uri(authUrl);
        }

        private void WebView_NavigationStarting(object sender, CoreWebView2NavigationStartingEventArgs e)
        {
            // Sprawdzamy, czy przeglądarka próbuje wejść na nasz "Redirect URI" (np. google.com)
            if (e.Uri.StartsWith(REDIRECT_URI))
            {
                // Zatrzymujemy ładowanie strony google.com
                e.Cancel = true;

                // Parsujemy URL, żeby wyciągnąć "?code=..."
                try
                {
                    var uri = new Uri(e.Uri);
                    var query = System.Web.HttpUtility.ParseQueryString(uri.Query);
                    string code = query["code"];

                    if (!string.IsNullOrEmpty(code))
                    {
                        CapturedAuthCode = code;
                        this.DialogResult = true; // Sukces
                        this.Close();
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Błąd parsowania kodu: " + ex.Message);
                    this.DialogResult = false;
                    this.Close();
                }
            }
        }
    }
}