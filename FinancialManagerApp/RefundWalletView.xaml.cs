using System.Windows;
using System.Collections.ObjectModel;
using FinancialManagerApp.Models;

namespace FinancialManagerApp.Views // Upewnij się, że ta przestrzeń zgadza się z x:Class w XAML!
{
    public partial class RefundWalletView : Window
    {
        // Właściwości używane przez okno
        public WalletModel SelectedWallet => WalletCombo.SelectedItem as WalletModel;
        public ObservableCollection<WalletModel> UserWallets { get; set; }

        public RefundWalletView(ObservableCollection<WalletModel> wallets)
        {
            InitializeComponent(); // Jeśli to nadal świeci na czerwono, przebuduj projekt (Build -> Rebuild Solution)
            UserWallets = wallets;
            this.DataContext = this;
        }

        // TA METODA MUSI TU BYĆ, aby błąd BtnConfirm_Click zniknął
        private void BtnConfirm_Click(object sender, RoutedEventArgs e)
        {
            if (SelectedWallet == null)
            {
                MessageBox.Show("Proszę wybrać portfel!");
                return;
            }
            this.DialogResult = true; // Zamyka okno i przesyła wynik do ViewModelu
        }
    }
}