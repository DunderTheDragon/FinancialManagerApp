using System.Windows;
using System.Windows.Controls;
using System.Collections.ObjectModel; // Potrzebne dla ObservableCollection
using FinancialManagerApp.Models;      // Potrzebne dla WalletModel

namespace FinancialManagerApp.Views
{
    public partial class AddGoalView : Window
    {
        // 1. Właściwość przechowująca listę portfeli do wyświetlenia w ComboBox
        public ObservableCollection<WalletModel> AvailableWallets { get; set; }

        // 2. Właściwości wystawione dla ViewModelu
        public string GoalName => GoalNameBox.Text;
        public decimal TargetAmount => decimal.TryParse(TargetAmountBox.Text, out decimal val) ? val : 0;
        public decimal CurrentAmount => decimal.TryParse(CurrentAmountBox.Text, out decimal val) ? val : 0;
        public bool IsRecurring => IsRecurringCheck.IsChecked == true;
        public string RecurringType => (TypeCombo.SelectedItem as ComboBoxItem)?.Content.ToString();
        public decimal RecurringValue => decimal.TryParse(ValueBox.Text, out decimal val) ? val : 0;

        // 3. NOWOŚĆ: Pobranie ID wybranego portfela źródłowego
        // Zwraca ID lub null, jeśli nic nie wybrano
        public int? SelectedWalletId => (int?)SourceWalletCombo.SelectedValue;

        // 4. Konstruktor przyjmujący listę portfeli
        public AddGoalView(ObservableCollection<WalletModel> wallets)
        {
            InitializeComponent();
            AvailableWallets = wallets;

            // Ustawiamy DataContext na to okno, aby XAML "widział" AvailableWallets
            this.DataContext = this;
        }

        private void BtnCreate_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(GoalName) || TargetAmount <= 0)
            {
                MessageBox.Show("Podaj nazwę celu i kwotę większą niż 0!");
                return;
            }

            // Walidacja portfela przy wpłatach cyklicznych
            if (IsRecurring && SelectedWalletId == null)
            {
                MessageBox.Show("Wybierz portfel źródłowy dla wpłat cyklicznych!");
                return;
            }

            this.DialogResult = true;
        }
    }
}