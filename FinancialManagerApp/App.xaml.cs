using FinancialManagerApp.ViewModels;
using FinancialManagerApp.Views;
using System.Configuration;
using System.Data;
using System.Windows;

namespace FinancialManagerApp
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            // Blokujemy automatyczne zamykanie aplikacji
            this.ShutdownMode = ShutdownMode.OnExplicitShutdown;

            LoginView loginWindow = new LoginView();

            // Otwieramy okno logowania. Kod stoi tutaj, dopóki loginWindow nie zostanie ZAMKNIĘTE (Close)
            loginWindow.ShowDialog();

            // Sprawdzamy, czy użytkownik się zalogował (używając flagi Success)
            if (loginWindow.Success)
            {
                MainWindow main = new MainWindow();
                this.MainWindow = main;
                main.DataContext = new MainViewModel(loginWindow.LoggedInUser);
                // Teraz pozwalamy na zamykanie apki wraz z MainWindow
                this.ShutdownMode = ShutdownMode.OnMainWindowClose;
                main.Show();
            }
            else
            {
                // Jeśli okno zamknięto (X) bez logowania - wyłączamy apkę
                Shutdown();
            }
        }
    }

}
