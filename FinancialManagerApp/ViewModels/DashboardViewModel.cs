using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using MySql.Data.MySqlClient;
using FinancialManagerApp.Core;
using FinancialManagerApp.Models;

namespace FinancialManagerApp.ViewModels
{
    public class DashboardViewModel : ViewModelBase
    {
        private string _connectionString = "Server=localhost; Database=financialmanagerapp; Uid=root; Pwd=;";

        private int _currentUserId;

        private decimal _totalBalance;
        public decimal TotalBalance
        {
            get => _totalBalance;
            set { _totalBalance = value; OnPropertyChanged(); }
        }

        // Kolekcja dla 5 ostatnich transakcji 
        public ObservableCollection<object> LastTransactions { get; set; } = new ObservableCollection<object>();

        public DashboardViewModel(User user)
        {
            if (user != null)
            {
                _currentUserId = user.Id;
                RefreshData();
            }
        }

        public void RefreshData()
        {
            CalculateTotalBalance(_currentUserId);
            LoadLastTransactions(_currentUserId);
        }

        private void CalculateTotalBalance(int userId)
        {
            try
            {
                using (MySqlConnection conn = new MySqlConnection(_connectionString))
                {
                    conn.Open();
                    // Sumuje saldo wszystkich aktywnych kont przypisanych do użytkownika 
                    string query = "SELECT SUM(saldo) FROM portfele WHERE id_uzytkownika = @userId";

                    using (MySqlCommand cmd = new MySqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@userId", userId);
                        var result = cmd.ExecuteScalar();
                        TotalBalance = result != DBNull.Value ? Convert.ToDecimal(result) : 0;
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Błąd salda: " + ex.Message);
            }
        }

        private void LoadLastTransactions(int userId)
        {
            try
            {
                using (MySqlConnection conn = new MySqlConnection(_connectionString))
                {
                    conn.Open();
                    // Pobiera 5 ostatnich transakcji ze wszystkich portfeli użytkownika 
                    // Łączymy z tabelą portfele, aby pobrać nazwę źródła 
                    string query = @"
                        SELECT t.nazwa, t.kwota, t.data_transakcji, p.nazwa as portfel_nazwa 
                        FROM transakcje t
                        JOIN portfele p ON t.id_portfela = p.id
                        WHERE p.id_uzytkownika = @userId
                        ORDER BY t.data_transakcji DESC
                        LIMIT 5";

                    using (MySqlCommand cmd = new MySqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@userId", userId);
                        using (var reader = cmd.ExecuteReader())
                        {
                            LastTransactions.Clear();
                            while (reader.Read())
                            {
                                LastTransactions.Add(new
                                {
                                    Title = reader.GetString("nazwa"),
                                    Amount = reader.GetDecimal("kwota"),
                                    Date = reader.GetDateTime("data_transakcji"),
                                    Wallet = reader.GetString("portfel_nazwa")
                                });
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Błąd transakcji: " + ex.Message);
            }
        }
    }
}