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
                        // 1. Wstawienie głównej transakcji
                        string insertQuery = @"INSERT INTO transakcje 
                    (id_portfela, data_transakcji, nazwa, id_kategorii, id_subkategorii, kwota, checkedTag) 
                    VALUES (@wId, @date, @name, @catId, @subCatId, @amount, @checked)";

                        using (var cmd = new MySqlCommand(insertQuery, conn, sqlTrans))
                        {
                            cmd.Parameters.AddWithValue("@wId", transaction.WalletId);
                            cmd.Parameters.AddWithValue("@date", transaction.Date);
                            cmd.Parameters.AddWithValue("@name", transaction.Name);
                            cmd.Parameters.AddWithValue("@catId", transaction.CategoryId);
                            cmd.Parameters.AddWithValue("@subCatId", transaction.SubCategoryId);
                            cmd.Parameters.AddWithValue("@amount", transaction.Amount);
                            cmd.Parameters.AddWithValue("@checked", transaction.CheckedTag);
                            cmd.ExecuteNonQuery(); // <-- TUTAJ BYŁ BŁĄD, poprawiono na ExecuteNonQuery
                        }

                        // 2. Aktualizacja salda portfela głównego
                        string updateQuery = "UPDATE portfele SET saldo = saldo + @amount WHERE id = @wId";
                        using (var cmdUpdate = new MySqlCommand(updateQuery, conn, sqlTrans))
                        {
                            cmdUpdate.Parameters.AddWithValue("@amount", transaction.Amount);
                            cmdUpdate.Parameters.AddWithValue("@wId", transaction.WalletId);
                            cmdUpdate.ExecuteNonQuery(); // <-- TUTAJ RÓWNIEŻ poprawiono
                        }

                        // 3. LOGIKA AUTO-OSZCZĘDZANIA (Wywoływana tylko dla wpływów)
                        if (transaction.Amount > 0)
                        {
                            HandleAutoSavings(conn, sqlTrans, transaction.WalletId, transaction.Amount);
                        }

                        sqlTrans.Commit();
                        return true;
                    }
                    catch (Exception ex)
                    {
                        sqlTrans.Rollback();
                        MessageBox.Show("Błąd zapisu i auto-oszczędzania: " + ex.Message);
                        return false;
                    }
                }
            }
        }

        private void HandleAutoSavings(MySqlConnection conn, MySqlTransaction sqlTrans, int walletId, decimal incomeAmount)
        {
            // 1. Pobierz aktywne reguły dla tego portfela
            string getRulesQuery = @"SELECT id, nazwa, typ_pobrania, wartosc_pobrania 
                             FROM skarbonki 
                             WHERE id_portfela_zrodlowego = @wId AND cykliczne = 1";

            var activeRules = new List<dynamic>();
            using (var cmd = new MySqlCommand(getRulesQuery, conn, sqlTrans))
            {
                cmd.Parameters.AddWithValue("@wId", walletId);
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        activeRules.Add(new
                        {
                            Id = reader.GetInt32("id"),
                            Name = reader.GetString("nazwa"),
                            Type = reader.GetString("typ_pobrania"),
                            Value = reader.GetDecimal("wartosc_pobrania")
                        });
                    }
                }
            }

            // 2. Dla każdej reguły oblicz kwotę i wykonaj transfer
            foreach (var rule in activeRules)
            {
                decimal amountToSave = 0;
                if (rule.Type == "procent")
                    amountToSave = incomeAmount * (rule.Value / 100);
                else
                    amountToSave = rule.Value;

                if (amountToSave > 0)
                {
                    // A. Odejmij z portfela (zmniejsz saldo o kwotę oszczędności)
                    string subWallet = "UPDATE portfele SET saldo = saldo - @amt WHERE id = @wid";
                    using (var cmd = new MySqlCommand(subWallet, conn, sqlTrans))
                    {
                        cmd.Parameters.AddWithValue("@amt", amountToSave);
                        cmd.Parameters.AddWithValue("@wid", walletId);
                        cmd.ExecuteNonQuery();
                    }

                    // B. Dodaj do skarbonki
                    string addGoal = "UPDATE skarbonki SET kwota_aktualna = kwota_aktualna + @amt WHERE id = @gid";
                    using (var cmd = new MySqlCommand(addGoal, conn, sqlTrans))
                    {
                        cmd.Parameters.AddWithValue("@amt", amountToSave);
                        cmd.Parameters.AddWithValue("@gid", rule.Id);
                        cmd.ExecuteNonQuery();
                    }

                    // C. Zaloguj transakcję jako "OSZCZĘDNOŚCI - SKARBONKI" (Kategoria 3)
                    // Dzięki temu wykres kołowy, który wcześniej robiliśmy, od razu to pokaże!
                    string logTrans = @"INSERT INTO transakcje (id_portfela, id_kategorii, kwota, nazwa, data_transakcji, checkedTag) 
                                VALUES (@wid, 3, @amtNeg, @desc, NOW(), 1)";
                    using (var cmd = new MySqlCommand(logTrans, conn, sqlTrans))
                    {
                        cmd.Parameters.AddWithValue("@wid", walletId);
                        cmd.Parameters.AddWithValue("@amtNeg", -amountToSave); // Kwota ujemna, bo zabieramy z portfela
                        cmd.Parameters.AddWithValue("@desc", $"AUTO-OSZCZĘDZANIE: {rule.Name.ToUpper()}");
                        cmd.ExecuteNonQuery();
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