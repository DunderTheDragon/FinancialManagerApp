using FinancialManagerApp.Core;
using FinancialManagerApp.Models;
using FinancialManagerApp.Views;
using System;
using System.Collections.ObjectModel;
using System.Windows.Input;

namespace FinancialManagerApp.ViewModels
{
    public class GoalsViewModel : ViewModelBase
    {
        public ObservableCollection<SavingsGoalModel> SavingsGoals { get; set; }

        public ICommand OpenAddGoalCommand { get; }      // Przycisk górny
        public ICommand OpenEditGoalCommand { get; }     // Przycisk na kafelku
        public ICommand OpenDepositCommand { get; }      // Przycisk "Wpłać"

        public GoalsViewModel()
        {
            OpenAddGoalCommand = new RelayCommand(ExecuteOpenAddGoal);
            OpenEditGoalCommand = new RelayCommand(ExecuteOpenEditGoal);
            OpenDepositCommand = new RelayCommand(ExecuteOpenDeposit);

            // Dane testowe
            SavingsGoals = new ObservableCollection<SavingsGoalModel>
            {
                new SavingsGoalModel
                {
                    GoalName = "Wakacje 2026",
                    CurrentAmount = 3500,
                    TargetAmount = 8000,
                    EstimatedDate = DateTime.Now.AddMonths(5),
                    IsAutomatic = true,
                    ContributionInfo = "500 zł miesięcznie z Revolut"
                },
                new SavingsGoalModel
                {
                    GoalName = "Nowy Laptop",
                    CurrentAmount = 1200,
                    TargetAmount = 6000,
                    EstimatedDate = DateTime.Now.AddMonths(8),
                    IsAutomatic = false,
                    ContributionInfo = "Wpłaty ręczne"
                }
            };
        }

        private void ExecuteOpenAddGoal(object obj)
        {
            // Otwieramy czyste okno edycji jako "Nowy Cel"
            var goalWindow = new EditGoalView();
            goalWindow.Title = "Nowa Skarbonka"; // Nadpisujemy tytuł
            goalWindow.ShowDialog();
        }

        private void ExecuteOpenEditGoal(object obj)
        {
            // Tu logika edycji (w przyszłości przekażemy wybrany cel do konstruktora widoku)
            var goalWindow = new EditGoalView();
            goalWindow.ShowDialog();
        }

        private void ExecuteOpenDeposit(object obj)
        {
            var paymentWindow = new AddPaymentGoalView();
            paymentWindow.ShowDialog();
        }
    }
}