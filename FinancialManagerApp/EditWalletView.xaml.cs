using System.Windows;
using FinancialManagerApp.Models;

namespace FinancialManagerApp.Views
{
    public partial class EditWalletView : Window
    {
        public WalletModel EditedWallet { get; private set; }

        public EditWalletView(WalletModel wallet)
        {
            InitializeComponent();
            // Tworzymy kopię obiektu, aby zmiany nie były widoczne na liście głównej 
            // dopóki użytkownik nie kliknie "Zapisz"
            EditedWallet = new WalletModel
            {
                Id = wallet.Id,
                Name = wallet.Name,
                Type = wallet.Type,
                Description = wallet.Description,
                Balance = wallet.Balance
            };
            this.DataContext = EditedWallet;
        }

        private void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(EditedWallet.Name))
            {
                MessageBox.Show("Nazwa portfela nie może być pusta.");
                return;
            }
            this.DialogResult = true;
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
        }
    }
}