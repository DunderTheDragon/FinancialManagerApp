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

        // --- WŁAŚCIWOŚCI DO ODBIORU DANYCH Przez ViewModel ---

        public int SelectedWalletId => (WalletComboBox.SelectedValue != null) ? (int)WalletComboBox.SelectedValue : 0;

        public decimal TransactionAmount
        {
            get
            {
                decimal.TryParse(AmountBox.Text, out decimal val);
                // Jeśli wybrano "Wydatek", zwracamy wartość ujemną
                return RadioExpense.IsChecked == true ? -val : val;
            }
        }

        public string TransactionName => TransactionNameBox.Text;
        public string SelectedCategory => (CategoryComboBox.SelectedItem as ComboBoxItem)?.Content.ToString();
        public string SelectedSubCategory => (SubCategoryComboBox.SelectedItem as ComboBoxItem)?.Content.ToString();
        public DateTime SelectedDate => DateBox.SelectedDate ?? DateTime.Now;

        // --- METODY OBSŁUGI ZDARZEŃ (Naprawiają błędy kompilacji) ---

        private void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(TransactionName) || TransactionAmount == 0 || WalletComboBox.SelectedValue == null)
            {
                MessageBox.Show("Uzupełnij wszystkie wymagane pola (Portfel, Nazwa, Kwota)!", "Błąd walidacji");
                return;
            }

            this.DialogResult = true; // Zamyka okno i przesyła wynik do ViewModelu
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
            this.Close();
        }
    }
}