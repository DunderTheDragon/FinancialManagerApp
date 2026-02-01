using FinancialManagerApp.Core;
using FinancialManagerApp.Models;
using System.Collections.ObjectModel;
using System.Windows.Input;
using System.Windows;

namespace FinancialManagerApp.ViewModels
{
    public class SettingsViewModel : ViewModelBase
    {
        // Właściwości zbindowane do checkboxów i textboxów
        private bool _autoCheckTag;
        public bool AutoCheckTag
        {
            get => _autoCheckTag;
            set { _autoCheckTag = value; OnPropertyChanged(); }
        }

        private bool _overwriteTags;
        public bool OverwriteTags
        {
            get => _overwriteTags;
            set { _overwriteTags = value; OnPropertyChanged(); }
        }

        private string _assignmentTolerance = "10";
        public string AssignmentTolerance
        {
            get => _assignmentTolerance;
            set { _assignmentTolerance = value; OnPropertyChanged(); }
        }

        public ObservableCollection<RuleModel> UserRules { get; set; }

        public ICommand SaveSettingsCommand { get; }
        public ICommand AddRuleCommand { get; }

        public SettingsViewModel()
        {
            SaveSettingsCommand = new RelayCommand(ExecuteSaveSettings);
            AddRuleCommand = new RelayCommand(ExecuteAddRule);

            UserRules = new ObservableCollection<RuleModel>
            {
                new RuleModel { Phrase = "Biedronka", Category = "Jedzenie", SubCategory = "Spożywcze" },
                new RuleModel { Phrase = "Netflix", Category = "Rozrywka", SubCategory = "VOD" }
            };
        }

        private void ExecuteAddRule(object obj)
        {
            // Tu można otworzyć małe okienko dialogowe do dodania reguły
            // Na razie dodajmy pusty wiersz dla testu
            UserRules.Add(new RuleModel { Phrase = "Nowa Reguła", Category = "Inne", SubCategory = "-" });
        }

        private void ExecuteSaveSettings(object obj)
        {
            MessageBox.Show("Ustawienia zostały zapisane!", "Finansense", MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }
}