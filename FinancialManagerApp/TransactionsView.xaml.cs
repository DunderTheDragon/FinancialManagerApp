using FinancialManagerApp.ViewModels;
using System.Windows;
using System.Windows.Controls;

namespace FinancialManagerApp.Views // <--- DODAJ TĘ OTOCZKĘ
{
    public partial class TransactionsView : UserControl
    {
        public TransactionsView()
        {
            InitializeComponent();
            // Podpinamy zdarzenie Loaded
            this.Loaded += TransactionsView_Loaded;
        }

        private void TransactionsView_Loaded(object sender, RoutedEventArgs e)
        {
            // Sprawdzamy czy DataContext to nasz ViewModel i wywołujemy Refresh
            if (this.DataContext is TransactionsViewModel viewModel)
            {
                viewModel.Refresh();
            }
        }
    }
}