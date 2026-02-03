using System.Windows;
using System.Windows.Controls;
using System.Collections.ObjectModel;
using FinancialManagerApp.Models;

namespace FinancialManagerApp.Views
{
    public partial class AddGoalView : Window
    {
        public ObservableCollection<WalletModel> AvailableWallets { get; set; }

        public string GoalName => GoalNameBox.Text;
        public decimal TargetAmount => decimal.TryParse(TargetAmountBox.Text, out decimal val) ? val : 0;
        public decimal CurrentAmount => decimal.TryParse(CurrentAmountBox.Text, out decimal val) ? val : 0;
        public bool IsRecurring => IsRecurringCheck.IsChecked == true;

        // Zmieniono: Zawsze zwraca "procent" dla bazy danych
        public string RecurringType => "procent";

        public decimal RecurringValue => decimal.TryParse(ValueBox.Text, out decimal val) ? val : 0;
        public int? SelectedWalletId => (int?)SourceWalletCombo.SelectedValue;

        public AddGoalView(ObservableCollection<WalletModel> wallets)
        {
            InitializeComponent();
            AvailableWallets = wallets;
            this.DataContext = this;
        }

        private void BtnCreate_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(GoalName) || TargetAmount <= 0)
            {
                MessageBox.Show("Podaj nazwę celu i kwotę większą niż 0!");
                return;
            }

            if (IsRecurring)
            {
                if (SelectedWalletId == null)
                {
                    MessageBox.Show("Wybierz portfel źródłowy dla wpłat automatycznych!");
                    return;
                }

                if (RecurringValue <= 0 || RecurringValue > 100)
                {
                    MessageBox.Show("Wpisz poprawną wartość procentową (1-100%)!");
                    return;
                }
            }

            this.DialogResult = true;
        }
    }
}