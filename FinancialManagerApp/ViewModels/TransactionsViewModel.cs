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

        // NOWOŚĆ: Komenda do wywołania z XAML
        public ICommand RefreshCommand { get; }

        private bool _onlyUnchecked;
        public bool OnlyUnchecked
        {
            get => _onlyUnchecked;
            set
            {
                _onlyUnchecked = value;
                OnPropertyChanged();
                LoadTransactionsFromDb();
            }
        }

        public TransactionsViewModel(User user)
        {
            CurrentUser = user;
            Transactions = new ObservableCollection<TransactionModel>();

            OpenAddTransactionCommand = new RelayCommand(ExecuteOpenAddTransaction);
            // Inicjalizacja komendy odświeżania
            RefreshCommand = new RelayCommand(_ => Refresh());

            Refresh(); // Pierwsze ładowanie
        }

        // Metoda publiczna, którą można wywołać przy "wejściu" w zakładkę
        public void Refresh()
        {
            LoadTransactionsFromDb();
        }

        private void LoadTransactionsFromDb()
        {
            try
            {
                using (MySqlConnection conn = new MySqlConnection(_connectionString))
                {
                    conn.Open();
                    // POPRAWKA SQL: Dodałem LEFT JOIN do subkategorii, o którym wspominałeś wcześniej
                    string query = @"
                        SELECT t.*, p.nazwa as wallet_name, k.typ as nazwa_kategorii, 
                               COALESCE(s.nazwa, 'Brak') as nazwa_subkategorii
                        FROM transakcje t 
                        JOIN portfele p ON t.id_portfela = p.id 
                        JOIN kategorie k ON t.id_kategorii = k.id 
                        LEFT JOIN subkategorie s ON t.id_subkategorii = s.id
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
                                    Date = reader.GetDateTime("data_transakcji"),
                                    Name = reader.GetString("nazwa"),
                                    WalletName = reader.GetString("wallet_name"),
                                    Category = reader.GetString("nazwa_kategorii"),
                                    SubCategory = reader.GetString("nazwa_subkategorii"), // Teraz z bazy!
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
                MessageBox.Show("Błąd ładowania transakcji: " + ex.Message);
            }
        }

        private void ExecuteOpenAddTransaction(object obj)
        {
            var addWindow = new AddTransactionView();
            var userWallets = GetUserWallets();
            addWindow.WalletComboBox.ItemsSource = userWallets;

            if (addWindow.ShowDialog() == true)
            {
                // 1. Logika znaku kwoty (używamy właściwości z AddTransactionView)
                decimal finalAmount = addWindow.IsExpense ? -addWindow.TransactionAmount : addWindow.TransactionAmount;

                var newTransaction = new TransactionModel
                {
                    WalletId = addWindow.SelectedWalletId,
                    Date = addWindow.SelectedDate,
                    Name = addWindow.TransactionName,
                    Amount = finalAmount,
                    // 2. NAPRAWA: Używamy nazw CategoryId i SubCategoryId (bez "Int")
                    CategoryId = addWindow.SelectedCategoryId,
                    SubCategoryId = addWindow.SelectedSubCategoryId,
                    CheckedTag = true
                };

                if (SaveTransactionToDatabase(newTransaction))
                {
                    LoadTransactionsFromDb();
                    MessageBox.Show("Transakcja dodana!");
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
                            // NAPRAWA: Pobieramy CategoryId zamiast CategoryIdInt
                            cmd.Parameters.AddWithValue("@catId", transaction.CategoryId);
                            cmd.Parameters.AddWithValue("@subCatId", transaction.SubCategoryId);
                            cmd.Parameters.AddWithValue("@amount", transaction.Amount);
                            cmd.Parameters.AddWithValue("@checked", transaction.CheckedTag);
                            cmd.ExecuteNonQuery();
                        }

                        // Aktualizacja salda portfela (dodajemy kwotę - jeśli ujemna, saldo spadnie)
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