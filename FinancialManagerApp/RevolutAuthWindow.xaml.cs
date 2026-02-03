using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using FinancialManagerApp.Services;
using Microsoft.Web.WebView2.Core;
using Newtonsoft.Json;

namespace FinancialManagerApp.Views
{
    public partial class RevolutAuthWindow : Window
    {
        public string CapturedAuthCode { get; private set; }

        // POPRAWKA: Dodany ukośnik na końcu, aby pasował do Twoich ustawień
        private const string REDIRECT_URI = "https://www.google.com/";
        private const string SSO_CONFIRM_URI = "https://sandbox-business.revolut.com/sso-confirm";
        private const string SSO_CONFIRM_URI_PRODUCTION = "https://business.revolut.com/sso-confirm";
        
        private DispatcherTimer _urlCheckTimer;
        private string _logFilePath;

        private void Log(string message)
        {
            try
            {
                string logMessage = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] {message}";
                System.Diagnostics.Debug.WriteLine(logMessage);
                
                // Zapis do pliku
                if (string.IsNullOrEmpty(_logFilePath))
                {
                    string logDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "FinancialManagerApp", "Logs");
                    Directory.CreateDirectory(logDir);
                    _logFilePath = Path.Combine(logDir, $"revolut_auth_{DateTime.Now:yyyyMMdd_HHmmss}.log");
                }
                
                File.AppendAllText(_logFilePath, logMessage + Environment.NewLine);
            }
            catch
            {
                // Ignoruj błędy logowania
            }
        }

        private string _clientId;
        private string _privateKey;
        private bool _isSandbox;

        public RevolutAuthWindow(string clientId, bool isSandbox, string privateKey = null)
        {
            InitializeComponent();
            _clientId = clientId;
            _isSandbox = isSandbox;
            _privateKey = privateKey;
            Log("=== Rozpoczęcie autoryzacji Revolut ===");
            Log($"ClientId: {clientId}, IsSandbox: {isSandbox}");
            InitializeAsync(clientId, isSandbox);
        }

        private async void InitializeAsync(string clientId, bool isSandbox)
        {
            try
            {
                await webView.EnsureCoreWebView2Async();

                // POPRAWKA: Obsługa wyskakujących okien (New Window)
                // Dzięki temu popupy Revoluta otworzą się w TYM SAMYM oknie
                webView.CoreWebView2.NewWindowRequested += (sender, args) =>
                {
                    Log($"NewWindowRequested: {args.Uri}");
                    args.Handled = true; // Zablokuj otwarcie w Edge
                    webView.Source = new Uri(args.Uri); // Otwórz w naszym oknie
                };

                // Przechwytujemy wszystkie żądania HTTP, aby zobaczyć, co się dzieje
                webView.CoreWebView2.WebResourceRequested += (sender, args) =>
                {
                    string requestUrl = args.Request.Uri;
                    Log($"WebResourceRequested: {requestUrl}");
                    
                    // Jeśli żądanie jest do sso-confirm lub redirect_uri z kodem, logujemy szczegółowo
                    if (requestUrl.Contains("sso-confirm") || requestUrl.Contains("google.com"))
                    {
                        Log($"WAŻNE: WebResourceRequested do: {requestUrl}");
                        if (requestUrl.Contains("code="))
                        {
                            Log("ZNALEZIONO KOD W WebResourceRequested!");
                            StopUrlCheckTimer();
                            ExtractCodeFromUri(requestUrl);
                        }
                    }
                };

                // Funkcja do wstrzykiwania JavaScript - wywoływana przy każdym załadowaniu strony
                Func<Task> injectScript = async () =>
                {
                    try
                    {
                        // Wstrzykujemy skrypt, który będzie monitorował zmiany URL
                        string script = @"
                            (function() {
                                // Sprawdź aktualny URL od razu
                                if (window.location.href.includes('code=') && 
                                    (window.location.href.includes('sso-confirm') || window.location.href.includes('google.com'))) {
                                    window.chrome.webview.postMessage(JSON.stringify({
                                        type: 'urlChange',
                                        url: window.location.href
                                    }));
                                }
                                
                                // Monitoruj zmiany w window.location
                                let lastUrl = window.location.href;
                                const checkUrl = () => {
                                    const currentUrl = window.location.href;
                                    if (currentUrl !== lastUrl) {
                                        lastUrl = currentUrl;
                                        // Jeśli URL zawiera kod, wywołaj callback
                                        if (currentUrl.includes('code=') && 
                                            (currentUrl.includes('sso-confirm') || currentUrl.includes('google.com'))) {
                                            window.chrome.webview.postMessage(JSON.stringify({
                                                type: 'urlChange',
                                                url: currentUrl
                                            }));
                                        }
                                    }
                                };
                                
                                // Sprawdź URL co 50ms (częściej)
                                setInterval(checkUrl, 50);
                                
                                // Monitoruj również pushState i replaceState (SPA navigation)
                                const originalPushState = history.pushState;
                                const originalReplaceState = history.replaceState;
                                
                                history.pushState = function() {
                                    originalPushState.apply(history, arguments);
                                    setTimeout(checkUrl, 10);
                                };
                                
                                history.replaceState = function() {
                                    originalReplaceState.apply(history, arguments);
                                    setTimeout(checkUrl, 10);
                                };
                                
                                // Monitoruj również zmiany hash
                                window.addEventListener('hashchange', checkUrl);
                                
                                // Monitoruj również zmiany w location
                                let locationCheckInterval = setInterval(() => {
                                    checkUrl();
                                }, 50);
                            })();
                        ";
                        
                        await webView.CoreWebView2.ExecuteScriptAsync(script);
                        Log("JavaScript monitoring script injected");
                    }
                    catch (Exception ex)
                    {
                        Log($"Błąd podczas wstrzykiwania JavaScript: {ex.Message}");
                    }
                };

                // Wstrzykuj JavaScript przy każdym załadowaniu DOM
                webView.CoreWebView2.DOMContentLoaded += async (sender, args) =>
                {
                    await injectScript();
                };

                // Obsługa wiadomości z JavaScript
                webView.CoreWebView2.WebMessageReceived += (sender, args) =>
                {
                    try
                    {
                        var message = JsonConvert.DeserializeObject<dynamic>(args.TryGetWebMessageAsString());
                        if (message?.type == "urlChange")
                        {
                            string url = message.url;
                            Log($"JavaScript wykrył zmianę URL: {url}");
                            
                            if (!string.IsNullOrEmpty(url) && 
                                (url.Contains("sso-confirm") || url.StartsWith(REDIRECT_URI, StringComparison.OrdinalIgnoreCase)) &&
                                url.Contains("code="))
                            {
                                Log("Przechwycono kod z JavaScript monitoring!");
                                StopUrlCheckTimer();
                                ExtractCodeFromUri(url);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Log($"Błąd podczas przetwarzania wiadomości z JavaScript: {ex.Message}");
                    }
                };

                // Dodatkowa obsługa: SourceChanged - przechwytuje zmiany URL
                webView.CoreWebView2.SourceChanged += (sender, args) =>
                {
                    string currentUrl = webView.CoreWebView2.Source;
                    Log($"SourceChanged: {currentUrl}");
                    
                    // WAŻNE: Przechwytujemy kod z sso-confirm, zanim nastąpi przekierowanie
                    if (!string.IsNullOrEmpty(currentUrl) && 
                        (currentUrl.StartsWith(SSO_CONFIRM_URI, StringComparison.OrdinalIgnoreCase) ||
                         currentUrl.StartsWith(SSO_CONFIRM_URI_PRODUCTION, StringComparison.OrdinalIgnoreCase)))
                    {
                        if (currentUrl.Contains("code="))
                        {
                            Log("Przechwycono sso-confirm z kodem w SourceChanged!");
                            StopUrlCheckTimer();
                            ExtractCodeFromUri(currentUrl);
                        }
                    }
                    else if (!string.IsNullOrEmpty(currentUrl) && currentUrl.StartsWith(REDIRECT_URI, StringComparison.OrdinalIgnoreCase))
                    {
                        Log("Przechwycono redirect URI w SourceChanged!");
                        StopUrlCheckTimer();
                        ExtractCodeFromUri(currentUrl);
                    }
                };

                // Timer do okresowego sprawdzania URL (fallback, jeśli eventy nie zadziałają)
                _urlCheckTimer = new DispatcherTimer
                {
                    Interval = TimeSpan.FromMilliseconds(500) // Sprawdzamy co 500ms
                };
                _urlCheckTimer.Tick += UrlCheckTimer_Tick;
                _urlCheckTimer.Start();

                // Budujemy URL do logowania
                string baseUrl = isSandbox
                    ? "https://sandbox-business.revolut.com/app-confirm"
                    : "https://business.revolut.com/app-confirm";

                // Używamy Uri.EscapeDataString dla bezpieczeństwa
                string authUrl = $"{baseUrl}?client_id={clientId}&redirect_uri={Uri.EscapeDataString(REDIRECT_URI)}&response_type=code";

                Log($"Otwieranie URL autoryzacji: {authUrl}");
                webView.Source = new Uri(authUrl);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Błąd inicjalizacji WebView: {ex.Message}", "Błąd", MessageBoxButton.OK, MessageBoxImage.Error);
                this.DialogResult = false;
                this.Close();
            }
        }

        private void WebView_NavigationStarting(object sender, CoreWebView2NavigationStartingEventArgs e)
        {
            Log($"NavigationStarting: {e.Uri}");
            
            // WAŻNE: Revolut przekierowuje najpierw do sso-confirm z kodem, a dopiero potem na redirect_uri
            // Musimy przechwycić kod z sso-confirm, zanim nastąpi przekierowanie na google.com
            if (e.Uri.StartsWith(SSO_CONFIRM_URI, StringComparison.OrdinalIgnoreCase) || 
                e.Uri.StartsWith(SSO_CONFIRM_URI_PRODUCTION, StringComparison.OrdinalIgnoreCase))
            {
                Log("Przechwycono sso-confirm URI w NavigationStarting!");
                
                // Sprawdzamy od razu, czy jest kod w URL
                if (e.Uri.Contains("code="))
                {
                    Log("Znaleziono kod w sso-confirm URI w NavigationStarting!");
                    StopUrlCheckTimer();
                    // NIE anulujemy nawigacji - pozwalamy załadować, ale przechwytujemy kod
                    ExtractCodeFromUri(e.Uri);
                }
            }
            // Sprawdzamy, czy przeglądarka próbuje wejść na nasz "Redirect URI"
            else if (e.Uri.StartsWith(REDIRECT_URI, StringComparison.OrdinalIgnoreCase))
            {
                Log("Przechwycono redirect URI w NavigationStarting!");
                StopUrlCheckTimer();
                
                // ANULUJEMY nawigację, aby nie ładować google.com
                e.Cancel = true;
                Log("Anulowano nawigację do redirect URI");

                // Wywołujemy ExtractCodeFromUri, które zamknie okno po przechwyceniu kodu
                ExtractCodeFromUri(e.Uri);
            }
        }

        private void WebView_NavigationCompleted(object sender, CoreWebView2NavigationCompletedEventArgs e)
        {
            // Fallback: jeśli NavigationStarting nie zadziałał, próbujemy tutaj
            // Używamy webView.Source zamiast e.Uri, ponieważ NavigationCompletedEventArgs nie ma właściwości Uri
            string currentUri = webView.Source?.ToString() ?? webView.CoreWebView2?.Source ?? "unknown";
            Log($"NavigationCompleted: {currentUri}, IsSuccess: {e.IsSuccess}, WebErrorStatus: {e.WebErrorStatus}");
            
            if (e.IsSuccess && !string.IsNullOrEmpty(currentUri))
            {
                // WAŻNE: Przechwytujemy kod z sso-confirm
                if (currentUri.StartsWith(SSO_CONFIRM_URI, StringComparison.OrdinalIgnoreCase) ||
                    currentUri.StartsWith(SSO_CONFIRM_URI_PRODUCTION, StringComparison.OrdinalIgnoreCase))
                {
                    if (currentUri.Contains("code="))
                    {
                        Log("Przechwycono sso-confirm z kodem w NavigationCompleted!");
                        StopUrlCheckTimer();
                        ExtractCodeFromUri(currentUri);
                    }
                }
                else if (currentUri.StartsWith(REDIRECT_URI, StringComparison.OrdinalIgnoreCase))
                {
                    Log("Przechwycono redirect URI w NavigationCompleted!");
                    StopUrlCheckTimer();
                    ExtractCodeFromUri(currentUri);
                }
            }
            else if (!e.IsSuccess)
            {
                // Logowanie błędów nawigacji dla debugowania
                Log($"Navigation failed: {e.WebErrorStatus} - {currentUri}");
            }
        }

        private void UrlCheckTimer_Tick(object sender, EventArgs e)
        {
            // Fallback: okresowe sprawdzanie URL, jeśli eventy nie zadziałały
            try
            {
                if (webView.CoreWebView2 != null && !string.IsNullOrEmpty(CapturedAuthCode))
                {
                    StopUrlCheckTimer();
                    return;
                }

                string currentUrl = webView.CoreWebView2?.Source ?? webView.Source?.ToString();
                
                // Sprawdzamy zarówno sso-confirm jak i redirect URI
                if (!string.IsNullOrEmpty(currentUrl))
                {
                    if ((currentUrl.StartsWith(SSO_CONFIRM_URI, StringComparison.OrdinalIgnoreCase) ||
                         currentUrl.StartsWith(SSO_CONFIRM_URI_PRODUCTION, StringComparison.OrdinalIgnoreCase)) &&
                        currentUrl.Contains("code="))
                    {
                        Log($"Timer: Przechwycono sso-confirm z kodem: {currentUrl}");
                        StopUrlCheckTimer();
                        ExtractCodeFromUri(currentUrl);
                    }
                    else if (currentUrl.StartsWith(REDIRECT_URI, StringComparison.OrdinalIgnoreCase))
                    {
                        Log($"Timer: Przechwycono redirect URI: {currentUrl}");
                        StopUrlCheckTimer();
                        ExtractCodeFromUri(currentUrl);
                    }
                }
            }
            catch (Exception ex)
            {
                Log($"Błąd w UrlCheckTimer: {ex.Message}");
            }
        }

        private void StopUrlCheckTimer()
        {
            if (_urlCheckTimer != null && _urlCheckTimer.IsEnabled)
            {
                _urlCheckTimer.Stop();
                Log("Timer zatrzymany");
            }
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            Log("Użytkownik anulował autoryzację (przycisk Anuluj)");
            this.DialogResult = false;
            this.Close();
        }

        protected override void OnClosed(EventArgs e)
        {
            StopUrlCheckTimer();
            Log("=== Okno autoryzacji zamknięte ===");
            if (!string.IsNullOrEmpty(_logFilePath) && File.Exists(_logFilePath))
            {
                string logDir = Path.GetDirectoryName(_logFilePath);
                string message = $"Logi zapisane w:\n\n{_logFilePath}\n\nMożesz otworzyć folder:\n{logDir}";
                MessageBox.Show(message, "Lokalizacja logów", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            base.OnClosed(e);
        }

        private void ExtractCodeFromUri(string uriString)
        {
            // Zapobiegamy wielokrotnemu wywołaniu
            if (!string.IsNullOrEmpty(CapturedAuthCode))
            {
                Log("ExtractCodeFromUri już został wywołany, pomijam.");
                return;
            }

            Log($"ExtractCodeFromUri wywołane z: {uriString}");
            
            try
            {
                // Parsowanie URL
                var uri = new Uri(uriString);
                Log($"Parsowany URI: {uri}");
                Log($"Query string: {uri.Query}");
                
                var queryParams = ParseQueryString(uri.Query);
                Log($"Znalezione parametry: {string.Join(", ", queryParams.Keys)}");

                // Sprawdzamy, czy Revolut zwrócił błąd OAuth
                if (queryParams.ContainsKey("error"))
                {
                    string errorDescription = queryParams.ContainsKey("error_description") 
                        ? queryParams["error_description"] 
                        : queryParams["error"];
                    Log($"Błąd OAuth: {errorDescription}");
                    MessageBox.Show($"Błąd autoryzacji Revolut: {errorDescription}", "Błąd autoryzacji", MessageBoxButton.OK, MessageBoxImage.Error);
                    this.DialogResult = false;
                    this.Close();
                    return;
                }

                // Próbujemy wyciągnąć kod autoryzacyjny
                if (queryParams.ContainsKey("code"))
                {
                    string code = queryParams["code"];
                    Log($"Znaleziono kod autoryzacyjny: {code.Substring(0, Math.Min(20, code.Length))}...");
                    
                    if (!string.IsNullOrEmpty(code))
                    {
                        // WAŻNE: Kod z sso-confirm może nie być właściwy do wymiany
                        // Czekamy na przekierowanie na redirect_uri (google.com) z kodem
                        if (uriString.Contains("sso-confirm"))
                        {
                            Log("Kod z sso-confirm - czekam na przekierowanie na redirect_uri z właściwym kodem");
                            // NIE zamykamy okna - czekamy na przekierowanie na google.com
                            // NIE wymieniamy kodu - może to nie jest właściwy kod
                        }
                        else if (uriString.StartsWith(REDIRECT_URI, StringComparison.OrdinalIgnoreCase))
                        {
                            // Kod z redirect_uri (google.com) - to jest właściwy kod do wymiany
                            Log("Kod z redirect_uri - to jest właściwy kod do wymiany");
                            CapturedAuthCode = code;
                            
                            // WAŻNE: Jeśli mamy privateKey, wymieńmy kod natychmiast, zanim wygaśnie
                            if (!string.IsNullOrEmpty(_privateKey))
                            {
                                Log("Wymieniam kod na token natychmiast (mamy privateKey)");
                                _ = ExchangeCodeImmediately(code);
                            }
                            else
                            {
                                Log("Kod zapisany, zamykam okno z DialogResult=true");
                                this.DialogResult = true; // Sukces -> zamyka okno i wraca do AddWalletView
                                this.Close();
                            }
                        }
                        else
                        {
                            // Nieznane źródło kodu - próbujemy użyć
                            Log("Kod z nieznanego źródła - próbuję użyć");
                            CapturedAuthCode = code;
                            
                            if (!string.IsNullOrEmpty(_privateKey))
                            {
                                Log("Wymieniam kod na token natychmiast (mamy privateKey)");
                                _ = ExchangeCodeImmediately(code);
                            }
                            else
                            {
                                Log("Kod zapisany, zamykam okno z DialogResult=true");
                                this.DialogResult = true;
                                this.Close();
                            }
                        }
                    }
                    else
                    {
                        Log("Kod autoryzacyjny jest pusty");
                        MessageBox.Show("Kod autoryzacyjny jest pusty.", "Błąd", MessageBoxButton.OK, MessageBoxImage.Warning);
                        this.DialogResult = false;
                        this.Close();
                    }
                }
                else
                {
                    // Kod nie został znaleziony w URL - możliwe, że użytkownik anulował autoryzację
                    Log("Kod autoryzacyjny nie został znaleziony w URL");
                    // Nie zamykamy okna automatycznie - może użytkownik jeszcze nie kliknął "Autoryzuj"
                }
            }
            catch (Exception ex)
            {
                Log($"Błąd w ExtractCodeFromUri: {ex.Message}\n{ex.StackTrace}");
                MessageBox.Show($"Błąd parsowania kodu autoryzacyjnego: {ex.Message}\n\nURL: {uriString}\n\nLogi zapisane w: {_logFilePath}", "Błąd", MessageBoxButton.OK, MessageBoxImage.Error);
                this.DialogResult = false;
                this.Close();
            }
        }

        /// <summary>
        /// Parsuje query string z URL bez użycia System.Web.HttpUtility (niedostępne w .NET 8)
        /// </summary>
        private Dictionary<string, string> ParseQueryString(string query)
        {
            var result = new Dictionary<string, string>();
            if (string.IsNullOrWhiteSpace(query)) return result;

            // Usuwamy znak '?' z początku, jeśli istnieje
            string cleanQuery = query.TrimStart('?');
            if (string.IsNullOrWhiteSpace(cleanQuery)) return result;

            // Dzielimy parametry po '&'
            foreach (var pair in cleanQuery.Split('&'))
            {
                if (string.IsNullOrWhiteSpace(pair)) continue;

                // Dzielimy każdy parametr na klucz i wartość
                var parts = pair.Split(new[] { '=' }, 2);
                if (parts.Length == 2)
                {
                    // Dekodujemy URL-encoded wartości
                    string key = Uri.UnescapeDataString(parts[0]);
                    string value = Uri.UnescapeDataString(parts[1]);
                    result[key] = value;
                }
                else if (parts.Length == 1 && !string.IsNullOrWhiteSpace(parts[0]))
                {
                    // Parametr bez wartości
                    string key = Uri.UnescapeDataString(parts[0]);
                    result[key] = string.Empty;
                }
            }

            return result;
        }

        private async Task ExchangeCodeImmediately(string code)
        {
            try
            {
                var revolutService = new RevolutService();
                string refreshToken = await revolutService.ExchangeAuthCodeForRefreshToken(code, _clientId, _privateKey);
                
                if (!string.IsNullOrWhiteSpace(refreshToken))
                {
                    CapturedAuthCode = refreshToken; // Zapisujemy refresh token zamiast kodu
                    Log("Refresh token pobrany pomyślnie!");
                    this.DialogResult = true;
                    this.Close();
                }
                else
                {
                    Log("Błąd: Refresh token jest pusty");
                    MessageBox.Show("Nie udało się pobrać refresh token.", "Błąd", MessageBoxButton.OK, MessageBoxImage.Error);
                    this.DialogResult = false;
                    this.Close();
                }
            }
            catch (Exception ex)
            {
                Log($"Błąd wymiany kodu: {ex.Message}");
                MessageBox.Show($"Błąd wymiany kodu na token: {ex.Message}", "Błąd", MessageBoxButton.OK, MessageBoxImage.Error);
                this.DialogResult = false;
                this.Close();
            }
        }
    }
}