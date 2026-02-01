using MySql.Data.MySqlClient;
using FinancialManagerApp.Core;
using FinancialManagerApp.Models;
using FinancialManagerApp.Views;
using System;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;

namespace FinancialManagerApp.ViewModels
{
    public class TransactionsViewModel : ViewModelBase
    {
        private string _connectionString = "Server=localhost; Database=financialmanagerapp; Uid=root; Pwd=;";

        public User CurrentUser { get; set; }
        public ObservableCollection<TransactionModel> Transactions { get; set; }
        public ICommand OpenAddTransactionCommand { get; }

        private bool _onlyUnchecked;
        public bool OnlyUnchecked
        {
            get => _onlyUnchecked;
            set
            {
                _onlyUnchecked = value;
                OnPropertyChanged(); // Powiadomienie UI o zmianie
                LoadTransactionsFromDb(); // Automatyczne odświeżenie listy przy zmianie filtra 
            }
        }

        public TransactionsViewModel(User user)
        {
            CurrentUser = user;
            Transactions = new ObservableCollection<TransactionModel>();
            OpenAddTransactionCommand = new RelayCommand(ExecuteOpenAddTransaction);
            LoadTransactionsFromDb();
        }

        private void LoadTransactionsFromDb()
        {
            try
            {
                using (MySqlConnection conn = new MySqlConnection(_connectionString))
                {
                    conn.Open();
                    // JOIN pozwala wyciągnąć nazwę kategorii z innej tabeli
                    string query = @"
                SELECT t.*, p.nazwa as wallet_name, k.typ as nazwa_kategorii 
                FROM transakcje t 
                JOIN portfele p ON t.id_portfela = p.id 
                JOIN kategorie k ON t.id_kategorii = k.id 
                WHERE p.id_uzytkownika = @userId";

                    if (OnlyUnchecked) query += " AND t.checkedTag = 0";
                    query += " ORDER BY t.data_transakcji DESC";

                    using (MySqlCommand cmd = new MySqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@userId", CurrentUser.Id);
                        using (var reader = cmd.ExecuteReader())
                        {
                            Transactions.Clear();
                            while (reader.Read())
                            {
                                Transactions.Add(new TransactionModel
                                {
                                    Id = reader.GetInt32("id"),
                                    WalletName = reader.GetString("wallet_name"),
                                    Date = reader.GetDateTime("data_transakcji"),
                                    Name = reader.GetString("nazwa"),
                                    // ZMIANA: Czytamy "nazwa_kategorii", którą wyciągnął JOIN
                                    Category = reader.GetString("nazwa_kategorii"),
                                    Amount = reader.GetDecimal("kwota"),
                                    CheckedTag = reader.GetBoolean("checkedTag")
                                });
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Błąd ładowania: " + ex.Message);
            }
        }

        private void ExecuteOpenAddTransaction(object obj)
        {
            var addTransactionWindow = new AddTransactionView();

            // Ładujemy portfele i przypisujemy do ComboBoxa w oknie
            var userWallets = GetUserWallets();
            addTransactionWindow.WalletComboBox.ItemsSource = userWallets;

            if (addTransactionWindow.ShowDialog() == true)
            {
                var newTransaction = new TransactionModel
                {
                    WalletId = addTransactionWindow.SelectedWalletId,
                    // Szukamy nazwy portfela w pobranej wcześniej liście dla widoku
                    WalletName = userWallets.FirstOrDefault(w => w.Id == addTransactionWindow.SelectedWalletId)?.Name,
                    Date = addTransactionWindow.SelectedDate,
                    Name = addTransactionWindow.TransactionName,
                    Category = addTransactionWindow.SelectedCategory,
                    SubCategory = addTransactionWindow.SelectedSubCategory,
                    Amount = addTransactionWindow.TransactionAmount,
                    CheckedTag = true // Ustawienie flagi zgodnie z dokumentacją 
                };

                if (SaveTransactionToDatabase(newTransaction))
                {
                    // Teraz "Transactions" jest dostępne w tym kontekście
                    Transactions.Insert(0, newTransaction);
                    MessageBox.Show("Transakcja dodana pomyślnie!", "Sukces");
                }
            }
        }

        private bool SaveTransactionToDatabase(TransactionModel transaction)
        {
            using (var conn = new MySqlConnection(_connectionString))
            {
                conn.Open();
                using (var sqlTrans = conn.BeginTransaction())
                {
                    try
                    {
                        string insertQuery = @"INSERT INTO transakcje 
                    (id_portfela, data_transakcji, nazwa, id_kategorii, id_subkategorii, kwota, checkedTag) 
                    VALUES (@wId, @date, @name, @catId, @subCatId, @amount, @checked)";

                        using (var cmd = new MySqlCommand(insertQuery, conn, sqlTrans))
                        {
                            cmd.Parameters.AddWithValue("@wId", transaction.WalletId);
                            cmd.Parameters.AddWithValue("@date", transaction.Date);
                            cmd.Parameters.AddWithValue("@name", transaction.Name);

                            // UWAGA: Twoja baza oczekuje ID (liczb), a nie tekstu.
                            // Na tym etapie, jeśli nie masz jeszcze tabeli kategorii, 
                            // możesz przekazać tymczasowo 1 (lub zmapować kategorie na ID).
                            cmd.Parameters.AddWithValue("@catId", 1); // Tymczasowe ID kategorii
                            cmd.Parameters.AddWithValue("@subCatId", 1); // Tymczasowe ID subkategorii

                            cmd.Parameters.AddWithValue("@amount", transaction.Amount);
                            cmd.Parameters.AddWithValue("@checked", true);

                            cmd.ExecuteNonQuery();
                        }

                        // Aktualizacja salda (bez zmian)
                        string updateQuery = "UPDATE portfele SET saldo = saldo + @amount WHERE id = @wId";
                        using (var cmdUpdate = new MySqlCommand(updateQuery, conn, sqlTrans))
                        {
                            cmdUpdate.Parameters.AddWithValue("@amount", transaction.Amount);
                            cmdUpdate.Parameters.AddWithValue("@wId", transaction.WalletId);
                            cmdUpdate.ExecuteNonQuery();
                        }

                        sqlTrans.Commit();
                        return true;
                    }
                    catch (Exception ex)
                    {
                        sqlTrans.Rollback();
                        MessageBox.Show("Błąd zapisu: " + ex.Message);
                        return false;
                    }
                }
            }
        }

        private ObservableCollection<WalletModel> GetUserWallets()
        {
            var wallets = new ObservableCollection<WalletModel>();
            try
            {
                using (var conn = new MySqlConnection(_connectionString))
                {
                    conn.Open();
                    string query = "SELECT id, nazwa FROM portfele WHERE id_uzytkownika = @userId";
                    using (var cmd = new MySqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@userId", CurrentUser.Id);
                        using (var reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                wallets.Add(new WalletModel
                                {
                                    Id = reader.GetInt32("id"),
                                    Name = reader.GetString("nazwa")
                                });
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Błąd pobierania portfeli: " + ex.Message);
            }
            return wallets;
        }


    }
}