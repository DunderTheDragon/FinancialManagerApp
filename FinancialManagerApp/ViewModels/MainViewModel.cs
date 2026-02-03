using FinancialManagerApp.Core;
using FinancialManagerApp.Models;

namespace FinancialManagerApp.ViewModels
{
    public class MainViewModel : ViewModelBase
    {
        // Dane użytkownika
        public User CurrentUser { get; set; }

        // Aktualnie wyświetlany widok
        private object _currentView;
        public object CurrentView
        {
            get { return _currentView; }
            set { _currentView = value; OnPropertyChanged(); }
        }

        // Instancje widoków (żeby nie tworzyć ich w kółko na nowo)
        public DashboardViewModel DashboardVM { get; set; }
        public TransactionsViewModel TransactionsVM { get; set; }
        public WalletsViewModel WalletsVM { get; set; }
        public GoalsViewModel GoalsVM { get; set; }
        public SettingsViewModel SettingsVM { get; set; }

        // Komendy nawigacji
        public RelayCommand NavigateToDashboardCommand { get; set; }
        public RelayCommand NavigateToTransactionsCommand { get; set; }
        public RelayCommand NavigateToWalletsCommand { get; set; }
        public RelayCommand NavigateToGoalsCommand { get; set; }
        public RelayCommand NavigateToSettingsCommand { get; set; }

        public MainViewModel(User user)
        {
            CurrentUser = user;

            DashboardVM = new DashboardViewModel(CurrentUser);
            TransactionsVM = new TransactionsViewModel(CurrentUser);
            WalletsVM = new WalletsViewModel(CurrentUser, TransactionsVM);
            GoalsVM = new GoalsViewModel(CurrentUser);
            SettingsVM = new SettingsViewModel();

            // Domyślny widok przy starcie
            CurrentView = DashboardVM;

            // Przypisanie logiki do przycisków menu

            // Odświeża dane w Dashboardzie
            NavigateToDashboardCommand = new RelayCommand(o =>
            {
                DashboardVM.RefreshData(); // 1. Najpierw pobierz świeże dane z bazy
                CurrentView = DashboardVM; // 2. Potem pokaż widok
            });
            NavigateToTransactionsCommand = new RelayCommand(o => { CurrentView = TransactionsVM; });

            // Odświeża dane w Portfelach
            NavigateToWalletsCommand = new RelayCommand(o =>
            {
                WalletsVM.RefreshData();
                CurrentView = WalletsVM;
            });
            NavigateToGoalsCommand = new RelayCommand(o => { CurrentView = GoalsVM; });
            NavigateToSettingsCommand = new RelayCommand(o => { CurrentView = SettingsVM; });
        }
    }
}