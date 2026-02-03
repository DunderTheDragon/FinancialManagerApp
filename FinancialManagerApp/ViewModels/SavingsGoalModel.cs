using FinancialManagerApp.Core;
using System;

namespace FinancialManagerApp.Models
{
    // Zmieniamy na ViewModelBase, aby naprawić błąd braku ObservableObject
    public class SavingsGoalModel : ViewModelBase
    {
        private decimal _currentAmount;
        private decimal _targetAmount;
        private string _goalName;

        public int Id { get; set; }

        public string GoalName
        {
            get => _goalName;
            set { _goalName = value; OnPropertyChanged(); }
        }

        public decimal CurrentAmount
        {
            get => _currentAmount;
            set
            {
                _currentAmount = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(ProgressPercentage)); // Odśwież pasek postępu
            }
        }

        // Wewnątrz klasy SavingsGoalModel dodaj:
        private int? _sourceWalletId;

        public int? SourceWalletId
        {
            get => _sourceWalletId;
            set { _sourceWalletId = value; OnPropertyChanged(); }
        }

        public decimal TargetAmount
        {
            get => _targetAmount;
            set
            {
                _targetAmount = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(ProgressPercentage));
            }
        }

        public bool IsRecurring { get; set; }
        public string ContributionType { get; set; } // 'kwota' lub 'procent'
        public decimal? ContributionValue { get; set; }
        public string SourceWalletName { get; set; }

        // Obliczanie procentu postępu
        public double ProgressPercentage
        {
            get
            {
                if (TargetAmount <= 0) return 0;
                double percent = (double)(CurrentAmount / TargetAmount) * 100;
                return percent > 100 ? 100 : percent;
            }
        }

        public string ContributionInfo => IsRecurring
            ? $"Odkładasz {ContributionValue}{(ContributionType == "procent" ? "%" : " zł")} z każdego wpływu"
            : "Wpłaty manualne";
    }
}