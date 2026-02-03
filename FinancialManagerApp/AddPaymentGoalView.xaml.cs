using System.Windows;
using System.Collections.ObjectModel;
using FinancialManagerApp.Models;

namespace FinancialManagerApp.Views
{
    public partial class AddPaymentGoalView : Window
    {
        // Te właściwości teraz poprawnie odczytają dane z kontrolek o nazwach WalletCombo i AmountBox
        public WalletModel SelectedWallet => WalletCombo.SelectedItem as WalletModel;
        public decimal Amount => decimal.TryParse(AmountBox.Text, out decimal val) ? val : 0;

        public ObservableCollection<WalletModel> UserWallets { get; set; }

        public AddPaymentGoalView(ObservableCollection<WalletModel> wallets)
        {
            InitializeComponent(); // To łączy XAML z tym kodem
            UserWallets = wallets;
            this.DataContext = this;
        }

        private void BtnDeposit_Click(object sender, RoutedEventArgs e)
        {
            if (SelectedWallet == null || Amount <= 0)
            {
                MessageBox.Show("Wybierz portfel i podaj poprawną kwotę!");
                return;
            }
            this.DialogResult = true; // Zamyka okno i zwraca sukces do ViewModelu
        }
    }
}