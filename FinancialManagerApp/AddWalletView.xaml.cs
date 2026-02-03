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
            this.MaxHeight = SystemParameters.WorkArea.Height * 0.9;
        }

        // --- WŁAŚCIWOŚCI ---
        public string WalletName => WalletNameBox.Text;
        public bool IsApi => RadioAPI.IsChecked == true;
        public string ApiClientId => ApiClientIdBox.Text;
        public string ApiKey => ApiKeyBox.Text;
        public string InitialBalance => InitialBalanceBox.Text;
        public string RefreshToken => RefreshTokenBox.Text;
        public string WalletDescription => WalletDescriptionBox.Text;

        // --- METODY OBSŁUGI ZDARZEŃ ---

        private void BtnCreate_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = true;
            this.Close();
        }

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
            // Przekazujemy privateKey, aby móc wymienić kod natychmiast po przechwyceniu
            var authWindow = new RevolutAuthWindow(clientId, isSandbox, privateKey);

            if (authWindow.ShowDialog() == true)
            {
                string token = authWindow.CapturedAuthCode;

                // Sprawdzamy, czy token został poprawnie pobrany
                if (string.IsNullOrWhiteSpace(token))
                {
                    MessageBox.Show("Nie udało się pobrać tokena. Spróbuj ponownie.", "Błąd", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // Jeśli privateKey był przekazany, token został już wymieniony w RevolutAuthWindow
                // W przeciwnym razie, token to kod autoryzacyjny, który trzeba wymienić
                if (token.StartsWith("MzEzNThhMmIt") || token.Length < 100)
                {
                    // To wygląda na kod autoryzacyjny, wymieńmy go
                    try
                    {
                        var revolutService = new RevolutService();
                        string refreshToken = await revolutService.ExchangeAuthCodeForRefreshToken(token, clientId, privateKey);

                        if (string.IsNullOrWhiteSpace(refreshToken))
                        {
                            MessageBox.Show("Nie udało się pobrać refresh token. Sprawdź dane logowania.", "Błąd", MessageBoxButton.OK, MessageBoxImage.Error);
                            return;
                        }

                        RefreshTokenBox.Text = refreshToken;
                        MessageBox.Show("Autoryzacja udana! Refresh Token pobrany.", "Sukces", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Błąd wymiany tokena: {ex.Message}\n\nSzczegóły: {ex.GetType().Name}", "Błąd", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
                else
                {
                    // To jest już refresh token
                    RefreshTokenBox.Text = token;
                    MessageBox.Show("Autoryzacja udana! Refresh Token pobrany.", "Sukces", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            else
            {
                // Użytkownik anulował autoryzację lub wystąpił błąd
                // Nie wyświetlamy komunikatu, ponieważ RevolutAuthWindow już to obsłużył
            }
        }
    }
}