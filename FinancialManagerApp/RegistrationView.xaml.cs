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

namespace FinancialManagerApp.Views
{
    public partial class RegistrationView : Window
    {
        // Twój Connection String (dostosuj do swojej bazy)
        private string connectionString = "server=localhost; database=financialmanagerapp; uid=root; pwd=;";

        public RegistrationView()
        {
            InitializeComponent();
        }

        private void btnRegister_Click(object sender, RoutedEventArgs e)
        {
            string login = txtRegisterLogin.Text;
            string pass = txtRegisterPassword.Password;
            string confirmPass = txtConfirmPassword.Password;

            // Podstawowa walidacja
            if (string.IsNullOrWhiteSpace(login) || string.IsNullOrWhiteSpace(pass))
            {
                MessageBox.Show("Uzupełnij login i hasło!");
                return;
            }

            if (pass != confirmPass)
            {
                MessageBox.Show("Hasła nie są takie same!");
                return;
            }

            try
            {
                using (MySqlConnection conn = new MySqlConnection(connectionString))
                {
                    conn.Open();

                    // Zapytanie INSERT bez id (AI) i bez data_rejestracji (Default)
                    string query = "INSERT INTO uzytkownicy (login, haslo) VALUES (@login, @haslo)";

                    MySqlCommand cmd = new MySqlCommand(query, conn);
                    cmd.Parameters.AddWithValue("@login", login);
                    cmd.Parameters.AddWithValue("@haslo", pass); 

                    cmd.ExecuteNonQuery();

                    MessageBox.Show("Konto utworzone!");
                    this.Close(); // Zamknięcie okna rejestracji i powrót do logowania
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Błąd podczas rejestracji: " + ex.Message);
            }
        }

        private void btnBackToLogin_Click(object sender, RoutedEventArgs e)
        {
            this.Close(); // Po prostu zamknij, a LoginView się pojawi
        }
    }
}
