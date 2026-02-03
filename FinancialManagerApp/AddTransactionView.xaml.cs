using System;
using System.Windows;
using System.Windows.Controls;

namespace FinancialManagerApp.Views
{
    public partial class AddTransactionView : Window
    {
        public AddTransactionView()
        {
            InitializeComponent();
        }

        public int SelectedWalletId => (WalletComboBox.SelectedValue != null) ? (int)WalletComboBox.SelectedValue : 0;
        public string TransactionName => TransactionNameBox.Text;
        public DateTime SelectedDate => DateBox.SelectedDate ?? DateTime.Now;
        public decimal TransactionAmount => decimal.TryParse(AmountBox.Text, out decimal val) ? val : 0;

        // NAPRAWA BŁĘDU: Dodanie IsExpense
        public bool IsExpense => RadioExpense.IsChecked == true;

        // Dopasowanie nazw do Twojego nowego modelu
        public int SelectedCategoryId => CategoryComboBox.SelectedIndex + 1;
        public int SelectedSubCategoryId => SubCategoryComboBox.SelectedIndex + 1;

        private void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(TransactionName) || TransactionAmount <= 0)
            {
                MessageBox.Show("Podaj poprawną nazwę i kwotę!");
                return;
            }
            this.DialogResult = true;
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e) => this.DialogResult = false;
    }
}