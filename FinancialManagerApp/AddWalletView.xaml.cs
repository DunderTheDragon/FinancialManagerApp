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

namespace FinancialManagerApp.Views
{
    public partial class AddWalletView : Window
    {
        public AddWalletView()
        {
            InitializeComponent();
        }

        // --- WŁAŚCIWOŚCI PUBLICZNE (Dostępne dla ViewModelu) ---

        // 1. Nazwa portfela (z TextBoxa x:Name="WalletNameBox")
        public string WalletName => WalletNameBox.Text;

        // 2. Czy wybrano API? (z RadioButtona x:Name="RadioAPI")
        // IsChecked jest typu bool?, więc sprawdzamy czy jest równe true
        public bool IsApi => RadioAPI.IsChecked == true;

        // 3. Dane do API (dla Revoluta)
        public string ApiClientId => ApiClientIdBox.Text;

        // Uwaga: PasswordBox przechowuje tekst w właściwości .Password, a nie .Text
        public string ApiKey => ApiKeyBox.Text;

        // 4. Dane do portfela manualnego
        public string InitialBalance => InitialBalanceBox.Text;


        // --- OBSŁUGA ZDARZEŃ ---

        // Metoda wywoływana po kliknięciu przycisku "Połącz i Utwórz"
        // (Musi być podpięta w XAML jako Click="BtnCreate_Click")
        private void BtnCreate_Click(object sender, RoutedEventArgs e)
        {
            // Ustawienie DialogResult na true informuje kod wywołujący (ViewModel),
            // że użytkownik zatwierdził formularz, a nie anulował.
            this.DialogResult = true;
            this.Close();
        }
    }
}
