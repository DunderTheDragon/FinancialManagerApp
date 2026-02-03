using System.Windows;
using System.Windows.Controls;
using FinancialManagerApp.Models;

namespace FinancialManagerApp.Views
{
    // Musi być public partial class i dziedziczyć po Window
    public partial class EditGoalView : Window
    {
        public EditGoalView(SavingsGoalModel goal)
        {
            InitializeComponent(); // To połączy C# z XAML
            this.DataContext = goal;

            // Ustawienie wyboru w ComboBox na podstawie danych modelu
            if (goal.ContributionType == "procent")
            {
                TypeCombo.SelectedIndex = 1;
            }
            else
            {
                TypeCombo.SelectedIndex = 0;
            }
        }

        // Brakująca metoda obsługująca kliknięcie przycisku Zapisz
        private void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(GoalNameBox.Text))
            {
                MessageBox.Show("Nazwa celu nie może być pusta!");
                return;
            }

            this.DialogResult = true;
            this.Close();
        }
    }
}