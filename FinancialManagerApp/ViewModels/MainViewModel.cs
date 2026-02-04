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
        // Komendy nawigacji
        public RelayCommand NavigateToDashboardCommand { get; set; }
        public RelayCommand NavigateToTransactionsCommand { get; set; }
        // DODAJ TO: Ta nazwa musi się zgadzać z bindowaniem w DashboardView.xaml
        public RelayCommand ViewTransactionsCommand { get; set; }
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
            SettingsVM = new SettingsViewModel(CurrentUser);

            CurrentView = DashboardVM;

            // Przypisanie logiki nawigacji
            NavigateToDashboardCommand = new RelayCommand(o =>
            {
                DashboardVM.RefreshData();
                CurrentView = DashboardVM;
            });

            // To jest główna komenda nawigacji z menu
            NavigateToTransactionsCommand = new RelayCommand(o => 
            {
                TransactionsVM.Refresh();
                CurrentView = TransactionsVM;
            });

            // DODAJ TO: To jest komenda, której szuka Twój przycisk "Zobacz wszystkie transakcje"
            ViewTransactionsCommand = new RelayCommand(o =>
            {
                TransactionsVM.Refresh();
                CurrentView = TransactionsVM;
            });

            NavigateToWalletsCommand = new RelayCommand(o =>
            {
                WalletsVM.RefreshData();
                CurrentView = WalletsVM;
            });

            NavigateToGoalsCommand = new RelayCommand(o => 
            {
                GoalsVM.Refresh();
                CurrentView = GoalsVM;
            });
            
            NavigateToSettingsCommand = new RelayCommand(o => 
            {
                SettingsVM.Refresh();
                CurrentView = SettingsVM;
            });
        }
    }
}