using System;
using System.Windows;
using FinancialManagerApp.Services;

namespace FinancialManagerApp.Views
{
    public partial class AddWalletView : Window
    {
        public AddWalletView()
        {
            InitializeComponent();
        }

        // --- ISTNIEJĄCE WŁAŚCIWOŚCI ---
        public string WalletName => WalletNameBox.Text;
        public bool IsApi => RadioAPI.IsChecked == true;
        public string ApiClientId => ApiClientIdBox.Text;
        public string ApiKey => ApiKeyBox.Text;
        public string InitialBalance => InitialBalanceBox.Text;

        // --- DODAJ TĘ NOWĄ WŁAŚCIWOŚĆ ---
        // Dzięki temu ViewModel pobierze token z pola tekstowego
        public string RefreshToken => RefreshTokenBox.Text;

        // --- METODY OBSŁUGI ZDARZEŃ ---

        private void BtnCreate_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = true;
            this.Close();
        }

        // --- TUTAJ WKLEJ SWOJĄ METODĘ ---
        private async void BtnAutoLogin_Click(object sender, RoutedEventArgs e)
        {
            string clientId = ApiClientIdBox.Text;
            string privateKey = ApiKeyBox.Text;

            if (string.IsNullOrWhiteSpace(clientId) || string.IsNullOrWhiteSpace(privateKey))
            {
                MessageBox.Show("Wprowadź najpierw Client ID i Klucz Prywatny!");
                return;
            }

            // 1. Otwórz okno przeglądarki
            // Pobieramy informację o środowisku z ComboBoxa w XAML (zakładam, że nazywa się EnvironmentCombo lub jest pierwszy na liście)
            // Jeśli nie masz nazwanego ComboBoxa, dla testów przyjmijmy true:
            bool isSandbox = true;

            // Upewnij się, że masz klasę RevolutAuthWindow stworzoną w projekcie!
            var authWindow = new RevolutAuthWindow(clientId, isSandbox);

            if (authWindow.ShowDialog() == true)
            {
                string code = authWindow.CapturedAuthCode;

                // 2. Wymień kod na Refresh Token
                try
                {
                    // Tutaj tworzysz instancję serwisu
                    var revolutService = new RevolutService();

                    // UWAGA: Ta metoda musi istnieć w RevolutService.cs (patrz Krok 3 poniżej)
                    string refreshToken = await revolutService.ExchangeAuthCodeForRefreshToken(code, clientId, privateKey);

                    // 3. Wpisz token do pola tekstowego
                    RefreshTokenBox.Text = refreshToken;
                    MessageBox.Show("Autoryzacja udana! Refresh Token pobrany.");
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Błąd wymiany tokena: " + ex.Message);
                }
            }
        }
    }
}