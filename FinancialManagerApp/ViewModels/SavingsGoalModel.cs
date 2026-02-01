using System;

namespace FinancialManagerApp.Models
{
    public class SavingsGoalModel
    {
        public string GoalName { get; set; }
        public decimal CurrentAmount { get; set; }
        public decimal TargetAmount { get; set; }
        public DateTime EstimatedDate { get; set; }
        public double ProgressPercentage => TargetAmount == 0 ? 0 : (double)(CurrentAmount / TargetAmount) * 100;
        public bool IsAutomatic { get; set; }
        public string ContributionInfo { get; set; }
    }
}