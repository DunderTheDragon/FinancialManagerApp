using System;
using System.Windows;
using Microsoft.Web.WebView2.Core;

namespace FinancialManagerApp.Views
{
    public partial class RevolutAuthWindow : Window
    {
        public string CapturedAuthCode { get; private set; }

        // POPRAWKA: Dodany ukośnik na końcu, aby pasował do Twoich ustawień
        private const string REDIRECT_URI = "https://www.google.com/";

        public RevolutAuthWindow(string clientId, bool isSandbox)
        {
            InitializeComponent();
            InitializeAsync(clientId, isSandbox);
        }

        private async void InitializeAsync(string clientId, bool isSandbox)
        {
            await webView.EnsureCoreWebView2Async();

            // POPRAWKA: Obsługa wyskakujących okien (New Window)
            // Dzięki temu popupy Revoluta otworzą się w TYM SAMYM oknie
            webView.CoreWebView2.NewWindowRequested += (sender, args) =>
            {
                args.Handled = true; // Zablokuj otwarcie w Edge
                webView.Source = new Uri(args.Uri); // Otwórz w naszym oknie
            };

            // Budujemy URL do logowania
            string baseUrl = isSandbox
                ? "https://sandbox-business.revolut.com/app-confirm"
                : "https://business.revolut.com/app-confirm";

            // Używamy Uri.EscapeDataString dla bezpieczeństwa
            string authUrl = $"{baseUrl}?client_id={clientId}&redirect_uri={Uri.EscapeDataString(REDIRECT_URI)}&response_type=code";

            webView.Source = new Uri(authUrl);
        }

        private void WebView_NavigationStarting(object sender, CoreWebView2NavigationStartingEventArgs e)
        {
            // Sprawdzamy, czy przeglądarka próbuje wejść na nasz "Redirect URI"
            // Używamy StartsWith, żeby złapać też parametry po znaku ?
            if (e.Uri.StartsWith(REDIRECT_URI))
            {
                // Zatrzymujemy ładowanie strony google.com
                e.Cancel = true;

                try
                {
                    // Parsowanie URL
                    var uri = new Uri(e.Uri);
                    // Prosty sposób na wyciągnięcie parametru "code" bez System.Web
                    var query = System.Web.HttpUtility.ParseQueryString(uri.Query);
                    string code = query["code"];

                    if (!string.IsNullOrEmpty(code))
                    {
                        CapturedAuthCode = code;
                        this.DialogResult = true; // Sukces -> zamyka okno i wraca do AddWalletView
                        this.Close();
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Błąd parsowania kodu autoryzacyjnego: " + ex.Message);
                    this.DialogResult = false;
                    this.Close();
                }
            }
        }
    }
}