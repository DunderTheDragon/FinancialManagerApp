using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using MySql.Data.MySqlClient;
using FinancialManagerApp.Models;

namespace FinancialManagerApp.Views
{
    public partial class LoginView : Window
    {
        // Twoje dane połączenia z bazą danych
        private string connectionString = "Server=localhost; Database=financialmanagerapp; Uid=root; Pwd=;";
        public bool Success { get; set; } = false;
        public User LoggedInUser { get; set; }
        public LoginView()
        {
            InitializeComponent();
        }

        private void btnLogin_Click(object sender, RoutedEventArgs e)
        {
            string username = txtLogin.Text;
            string password = txtPassword.Password;

            // Pobieramy prawdziwe ID użytkownika z bazy
            int realUserId = GetAuthenticatedUserId(username, password);

            if (realUserId > 0)
            {
                // Teraz przypisujemy faktyczne ID pobrane z tabeli 'uzytkownicy'
                LoggedInUser = new User { Login = username, Id = realUserId };

                this.Success = true;
                this.Close();
            }
            else
            {
                MessageBox.Show("Błędny login lub hasło.", "Błąd", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private int GetAuthenticatedUserId(string login, string password)
        {
            try
            {
                using (MySqlConnection connection = new MySqlConnection(connectionString))
                {
                    connection.Open();
                    // Zmieniamy zapytanie, aby pobrać kolumnę 'id'
                    string query = "SELECT id FROM uzytkownicy WHERE login=@login AND haslo=@password";

                    using (MySqlCommand cmd = new MySqlCommand(query, connection))
                    {
                        cmd.Parameters.AddWithValue("@login", login);
                        cmd.Parameters.AddWithValue("@password", password);

                        object result = cmd.ExecuteScalar();

                        if (result != null)
                        {
                            return Convert.ToInt32(result); // Zwraca ID z bazy
                        }
                        return -1; // Użytkownik nie istnieje
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Błąd połączenia z bazą: " + ex.Message);
                return -1;
            }
        }
        private void RegisterButton_Click(object sender, RoutedEventArgs e)
        {
            RegistrationView reg = new RegistrationView();

            this.Hide(); // Ukrywamy LoginView

            // Otwieramy rejestrację. ShowDialog() zatrzymuje ten kod tutaj.
            reg.ShowDialog();

            // Kiedy zamkniesz RegistrationView, kod ruszy dalej:
            this.Show(); // Pokazujemy LoginView ponownie. 
                         // Ponieważ to okno nie zostało zamknięte (Close), ShowDialog() w App.xaml.cs nadal "czeka".
        }

        private bool AuthenticateUser(string login, string password)
        {
            try
            {
                using (MySqlConnection connection = new MySqlConnection(connectionString))
                {
                    connection.Open();
                    // Zapytanie sprawdzające użytkownika
                    string query = "SELECT COUNT(1) FROM uzytkownicy WHERE login=@login AND haslo=@password";

                    using (MySqlCommand cmd = new MySqlCommand(query, connection))
                    {
                        cmd.Parameters.AddWithValue("@login", login);
                        cmd.Parameters.AddWithValue("@password", password);

                        int count = Convert.ToInt32(cmd.ExecuteScalar());
                        return count == 1;
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Błąd połączenia z bazą: " + ex.Message);
                return false;
            }
        }
    }
}
