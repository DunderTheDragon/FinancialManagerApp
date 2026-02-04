using MySql.Data.MySqlClient;
using FinancialManagerApp.Core;
using FinancialManagerApp.Models;
using FinancialManagerApp.Views;
using FinancialManagerApp.Services;
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
        public ICommand DeleteTransactionCommand { get; }
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
            DeleteTransactionCommand = new RelayCommand(ExecuteDeleteTransaction);
            RefreshCommand = new RelayCommand(_ => Refresh());

            Refresh();
        }

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
                    string query = @"
                        SELECT t.id, t.id_portfela as wallet_id, t.id_kategorii as category_id, 
                               t.id_subkategorii as subcategory_id,
                               t.data_transakcji, t.nazwa, t.kwota, t.checkedTag,
                               p.nazwa as wallet_name, k.typ as nazwa_kategorii, 
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
                                int walletIdOrdinal = reader.GetOrdinal("wallet_id");
                                int categoryIdOrdinal = reader.GetOrdinal("category_id");
                                int subCategoryIdOrdinal = reader.GetOrdinal("subcategory_id");
                                
                                int walletId = reader.IsDBNull(walletIdOrdinal) ? 0 : reader.GetInt32(walletIdOrdinal);
                                int categoryId = reader.IsDBNull(categoryIdOrdinal) ? 0 : reader.GetInt32(categoryIdOrdinal);
                                int subCategoryId = reader.IsDBNull(subCategoryIdOrdinal) ? 0 : reader.GetInt32(subCategoryIdOrdinal);
                                
                                Transactions.Add(new TransactionModel
                                {
                                    Id = reader.GetInt32("id"),
                                    WalletId = walletId,
                                    Date = reader.GetDateTime("data_transakcji"),
                                    Name = reader.GetString("nazwa"),
                                    WalletName = reader.GetString("wallet_name"),
                                    Category = reader.GetString("nazwa_kategorii"),
                                    SubCategory = reader.GetString("nazwa_subkategorii"),
                                    CategoryId = categoryId,
                                    SubCategoryId = subCategoryId,
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
            var addWindow = new AddTransactionView(CurrentUser);
            var userWallets = GetUserWallets();
            addWindow.WalletComboBox.ItemsSource = userWallets;

            if (addWindow.ShowDialog() == true)
            {
                decimal finalAmount = addWindow.IsExpense ? -addWindow.TransactionAmount : addWindow.TransactionAmount;

                var newTransaction = new TransactionModel
                {
                    WalletId = addWindow.SelectedWalletId,
                    Date = addWindow.SelectedDate,
                    Name = addWindow.TransactionName,
                    Amount = finalAmount,
                    CategoryId = addWindow.SelectedCategoryId > 0 ? addWindow.SelectedCategoryId : (finalAmount > 0 ? 4 : 0),
                    SubCategoryId = addWindow.SelectedSubCategoryId,
                    CheckedTag = true
                };
                
                if (newTransaction.CategoryId == 0 && finalAmount < 0)
                {
                    var transactionForAssign = new ImportedTransactionModel
                    {
                        Name = newTransaction.Name,
                        MerchantName = newTransaction.Name,
                        Location = "",
                        OriginalDescription = "",
                        Amount = newTransaction.Amount
                    };
                    var categoryService = new CategoryAssignmentService();
                    var categoryResult = categoryService.AssignCategory(transactionForAssign, CurrentUser.Id);
                    newTransaction.CategoryId = categoryResult.categoryId;
                    newTransaction.SubCategoryId = categoryResult.subCategoryId ?? 0;
                }

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
                            
                            // Jeśli to przychód (kwota dodatnia) i kategoria nie jest ustawiona, ustaw kategorię "przychód" (id=4)
                            int categoryId = transaction.CategoryId;
                            if (transaction.Amount > 0 && categoryId == 0)
                            {
                                categoryId = 4; // Kategoria "przychód"
                            }
                            
                            // Upewnij się, że kategoria jest ustawiona (nie może być 0)
                            if (categoryId == 0)
                            {
                                throw new Exception("Kategoria nie może być pusta. Ustaw kategorię przed zapisem transakcji.");
                            }
                            
                            cmd.Parameters.AddWithValue("@catId", categoryId);
                            // Jeśli SubCategoryId = 0 lub null, zapisz NULL (foreign key constraint)
                            cmd.Parameters.AddWithValue("@subCatId", transaction.SubCategoryId > 0 ? (object)transaction.SubCategoryId : DBNull.Value);
                            cmd.Parameters.AddWithValue("@amount", transaction.Amount);
                            cmd.Parameters.AddWithValue("@checked", transaction.CheckedTag);
                            cmd.ExecuteNonQuery(); // <-- TUTAJ BYŁ BŁĄD, poprawiono na ExecuteNonQuery
                        }

                        // 2. LOGIKA AUTO-OSZCZĘDZANIA (Wywoływana PRZED aktualizacją salda, tylko dla wpływów)
                        // Najpierw sprawdzamy skarbonki i obliczamy ile trzeba odjąć
                        decimal totalAutoSavings = 0;
                        if (transaction.Amount > 0)
                        {
                            totalAutoSavings = HandleAutoSavings(conn, sqlTrans, transaction.WalletId, transaction.Amount);
                        }

                        // 3. Aktualizacja salda portfela głównego (po odjęciu auto-oszczędności)
                        // Saldo = kwota transakcji - auto-oszczędności
                        decimal finalWalletAmount = transaction.Amount - totalAutoSavings;
                        string updateQuery = "UPDATE portfele SET saldo = saldo + @amount WHERE id = @wId";
                        using (var cmdUpdate = new MySqlCommand(updateQuery, conn, sqlTrans))
                        {
                            cmdUpdate.Parameters.AddWithValue("@amount", finalWalletAmount);
                            cmdUpdate.Parameters.AddWithValue("@wId", transaction.WalletId);
                            cmdUpdate.ExecuteNonQuery();
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

        /// <summary>
        /// Obsługuje automatyczne oszczędzanie do skarbonek cyklicznych przy wpływach
        /// Zwraca łączną kwotę, która została odłożona do skarbonek
        /// </summary>
        private decimal HandleAutoSavings(MySqlConnection conn, MySqlTransaction sqlTrans, int walletId, decimal incomeAmount)
        {
            decimal totalSaved = 0;
            
            // 1. Pobierz aktywne reguły (skarbonki cykliczne) dla tego portfela
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
                        string typeValue = reader.GetString("typ_pobrania");
                        decimal value = reader.GetDecimal("wartosc_pobrania");
                        
                        // Debugowanie - sprawdź wartości z bazy
                        System.Diagnostics.Debug.WriteLine($"Skarbonka: {reader.GetString("nazwa")}, Typ: {typeValue}, Wartość: {value}");
                        
                        activeRules.Add(new
                        {
                            Id = reader.GetInt32("id"),
                            Name = reader.GetString("nazwa"),
                            Type = typeValue,
                            Value = value
                        });
                    }
                }
            }

            // Jeśli nie ma aktywnych skarbonek, zwróć 0
            if (activeRules.Count == 0)
            {
                return 0;
            }

            // 2. Dla każdej reguły oblicz kwotę i wykonaj transfer
            foreach (var rule in activeRules)
            {
                decimal amountToSave = 0;
                
                // Oblicz kwotę do oszczędzenia
                // Sprawdź typ - użyj porównania case-insensitive
                string ruleType = rule.Type?.ToString().ToLower() ?? "";
                
                if (ruleType == "procent")
                {
                    // Procent od wpływu: incomeAmount * (wartosc_pobrania / 100)
                    // Jeśli wartosc_pobrania = 5, to 5% = 0.05
                    // Przykład: 1000 * (5 / 100) = 1000 * 0.05 = 50
                    decimal percentage = rule.Value / 100m; // Użyj 'm' aby wymusić typ decimal
                    amountToSave = incomeAmount * percentage;
                    
                    // Debugowanie
                    System.Diagnostics.Debug.WriteLine($"Auto-oszczędzanie: {rule.Name}, Typ: '{rule.Type}', Wartość: {rule.Value}, Procent: {percentage}, Wpływ: {incomeAmount}, Kwota do oszczędzenia: {amountToSave}");
                }
                else if (ruleType == "kwota")
                {
                    // Stała kwota
                    amountToSave = rule.Value;
                    System.Diagnostics.Debug.WriteLine($"Auto-oszczędzanie: {rule.Name}, Typ: '{rule.Type}', Kwota stała: {amountToSave}");
                }
                else
                {
                    // Nieznany typ - pomiń
                    System.Diagnostics.Debug.WriteLine($"Auto-oszczędzanie: {rule.Name}, Nieznany typ: '{rule.Type}', Pomijam");
                    continue;
                }

                // Upewnij się, że kwota nie przekracza dostępnej kwoty
                if (amountToSave > incomeAmount)
                {
                    amountToSave = incomeAmount;
                }

                if (amountToSave > 0)
                {
                    // A. Dodaj do skarbonki (zwiększ kwota_aktualna)
                    string addGoal = "UPDATE skarbonki SET kwota_aktualna = kwota_aktualna + @amt WHERE id = @gid";
                    using (var cmd = new MySqlCommand(addGoal, conn, sqlTrans))
                    {
                        cmd.Parameters.AddWithValue("@amt", amountToSave);
                        cmd.Parameters.AddWithValue("@gid", rule.Id);
                        cmd.ExecuteNonQuery();
                    }

                    // B. Zaloguj transakcję jako "OSZCZĘDNOŚCI - SKARBONKI" (Kategoria 3)
                    // To jest transakcja ujemna (wydatek z portfela na skarbonkę)
                    string logTrans = @"INSERT INTO transakcje (id_portfela, id_kategorii, id_subkategorii, kwota, nazwa, data_transakcji, checkedTag) 
                                VALUES (@wid, 3, NULL, @amtNeg, @desc, @date, 1)";
                    using (var cmd = new MySqlCommand(logTrans, conn, sqlTrans))
                    {
                        cmd.Parameters.AddWithValue("@wid", walletId);
                        cmd.Parameters.AddWithValue("@amtNeg", -amountToSave); // Kwota ujemna, bo zabieramy z portfela
                        cmd.Parameters.AddWithValue("@desc", $"AUTO-OSZCZĘDZANIE: {rule.Name.ToUpper()}");
                        cmd.Parameters.AddWithValue("@date", DateTime.Now);
                        cmd.ExecuteNonQuery();
                    }

                    totalSaved += amountToSave;
                }
            }

            return totalSaved;
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

        /// <summary>
        /// Usuwa transakcję z odwróceniem konsekwencji:
        /// - Odwraca kwotę na portfelu (dodaje jeśli była ujemna, odejmuje jeśli była dodatnia)
        /// - Jeśli transakcja dotyczy skarbonki, wycofuje kwotę ze skarbonki
        /// </summary>
        private void ExecuteDeleteTransaction(object obj)
        {
            if (obj is TransactionModel transaction)
            {
                // Debugowanie przed usunięciem
                System.Diagnostics.Debug.WriteLine($"Przed usunięciem: ID={transaction.Id}, Nazwa={transaction.Name}, Kwota={transaction.Amount}, WalletId={transaction.WalletId}");
                
                var result = MessageBox.Show(
                    $"Czy na pewno chcesz usunąć transakcję '{transaction.Name}' o wartości {transaction.Amount:N2} zł?",
                    "Potwierdzenie usunięcia",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (result == MessageBoxResult.Yes)
                {
                    if (DeleteTransactionFromDatabase(transaction))
                    {
                        LoadTransactionsFromDb();
                        MessageBox.Show("Transakcja została usunięta, a konsekwencje zostały odwrócone.");
                    }
                }
            }
        }

        /// <summary>
        /// Usuwa transakcję z bazy danych i odwraca jej konsekwencje
        /// </summary>
        private bool DeleteTransactionFromDatabase(TransactionModel transaction)
        {
            try
            {
                using (var conn = new MySqlConnection(_connectionString))
                {
                    conn.Open();
                    using (var sqlTrans = conn.BeginTransaction())
                    {
                        try
                        {
                            // 1. Sprawdź, czy transakcja dotyczy skarbonki
                            bool isSavingsTransaction = transaction.Name.Contains("SKARBONKA") || 
                                                         transaction.Name.Contains("AUTO-OSZCZĘDZANIE");
                            
                            if (isSavingsTransaction)
                            {
                                // Wycofaj kwotę ze skarbonki
                                // Nazwa transakcji ma format: "AUTO-OSZCZĘDZANIE: NAZWA_SKARBONKI" lub "SKARBONKA: NAZWA_SKARBONKI"
                                string goalName = ExtractGoalNameFromTransaction(transaction.Name);
                                
                                if (!string.IsNullOrEmpty(goalName))
                                {
                                    // Znajdź skarbonkę po nazwie
                                    // WAŻNE: Najpierw odczytujemy dane do zmiennych lokalnych, zamykamy DataReader, a potem wykonujemy UPDATE
                                    int goalId = 0;
                                    decimal currentAmount = 0;
                                    bool goalFound = false;
                                    
                                    string findGoalQuery = "SELECT id, kwota_aktualna FROM skarbonki WHERE nazwa = @name AND id_uzytkownika = @userId";
                                    using (var cmd = new MySqlCommand(findGoalQuery, conn, sqlTrans))
                                    {
                                        cmd.Parameters.AddWithValue("@name", goalName);
                                        cmd.Parameters.AddWithValue("@userId", CurrentUser.Id);
                                        
                                        using (var reader = cmd.ExecuteReader())
                                        {
                                            if (reader.Read())
                                            {
                                                goalId = reader.GetInt32("id");
                                                currentAmount = reader.GetDecimal("kwota_aktualna");
                                                goalFound = true;
                                            }
                                        }
                                    } // DataReader jest teraz zamknięty
                                    
                                    // Teraz możemy bezpiecznie wykonać UPDATE na tym samym połączeniu
                                    if (goalFound)
                                    {
                                        // Kwota transakcji jest ujemna (bo to wydatek), więc aby wycofać, musimy odjąć wartość bezwzględną
                                        decimal amountToRefund = Math.Abs(transaction.Amount);
                                        
                                        // Sprawdź, czy nie przekroczymy 0 (nie można mieć ujemnej kwoty w skarbonce)
                                        decimal newAmount = currentAmount - amountToRefund;
                                        if (newAmount < 0)
                                        {
                                            newAmount = 0;
                                        }
                                        
                                        // Zaktualizuj kwotę w skarbonce
                                        string updateGoalQuery = "UPDATE skarbonki SET kwota_aktualna = @newAmount WHERE id = @goalId";
                                        using (var cmdUpdate = new MySqlCommand(updateGoalQuery, conn, sqlTrans))
                                        {
                                            cmdUpdate.Parameters.AddWithValue("@newAmount", newAmount);
                                            cmdUpdate.Parameters.AddWithValue("@goalId", goalId);
                                            cmdUpdate.ExecuteNonQuery();
                                        }
                                    }
                                }
                            }

                            // 2. Odwróć kwotę na portfelu
                            // Logika odwracania:
                            // - Przy zapisie: saldo = saldo + transaction.Amount
                            //   * Jeśli transaction.Amount = +100 (przychód), to saldo + 100 (dodaje)
                            //   * Jeśli transaction.Amount = -100 (wydatek), to saldo + (-100) = saldo - 100 (odejmuje)
                            // - Przy usuwaniu: musimy odwrócić efekt, więc:
                            //   * Jeśli transaction.Amount = +100 (przychód), to musimy odjąć 100 → reversedAmount = -100
                            //   * Jeśli transaction.Amount = -100 (wydatek), to musimy dodać 100 → reversedAmount = +100
                            // Odwrócenie: reversedAmount = -transaction.Amount
                            
                            // WAŻNE: Sprawdzamy rzeczywistą kwotę z transakcji
                            decimal transactionAmount = transaction.Amount;
                            decimal reversedAmount = -transactionAmount; // Odwróć znak
                            
                            // Debugowanie - szczegółowe
                            System.Diagnostics.Debug.WriteLine($"=== USUWANIE TRANSAKCJI ===");
                            System.Diagnostics.Debug.WriteLine($"ID transakcji: {transaction.Id}");
                            System.Diagnostics.Debug.WriteLine($"Nazwa: {transaction.Name}");
                            System.Diagnostics.Debug.WriteLine($"Kwota transakcji (z bazy): {transactionAmount}");
                            System.Diagnostics.Debug.WriteLine($"Odwrócona kwota (do zastosowania): {reversedAmount}");
                            System.Diagnostics.Debug.WriteLine($"WalletId: {transaction.WalletId}");
                            System.Diagnostics.Debug.WriteLine($"Operacja: saldo = saldo + {reversedAmount}");
                            
                            // Aktualizacja salda: saldo = saldo + reversedAmount
                            // Jeśli reversedAmount = +100, to saldo + 100 (dodaje)
                            // Jeśli reversedAmount = -100, to saldo + (-100) = saldo - 100 (odejmuje)
                            string updateWalletQuery = "UPDATE portfele SET saldo = saldo + @amount WHERE id = @walletId";
                            using (var cmd = new MySqlCommand(updateWalletQuery, conn, sqlTrans))
                            {
                                cmd.Parameters.AddWithValue("@amount", reversedAmount);
                                cmd.Parameters.AddWithValue("@walletId", transaction.WalletId);
                                int rowsAffected = cmd.ExecuteNonQuery();
                                System.Diagnostics.Debug.WriteLine($"Zaktualizowano saldo portfela. Wpływ na saldo: {reversedAmount}, Wierszy zmienionych: {rowsAffected}");
                            }

                            // 3. Usuń transakcję z bazy
                            string deleteQuery = "DELETE FROM transakcje WHERE id = @transactionId";
                            using (var cmd = new MySqlCommand(deleteQuery, conn, sqlTrans))
                            {
                                cmd.Parameters.AddWithValue("@transactionId", transaction.Id);
                                cmd.ExecuteNonQuery();
                            }

                            sqlTrans.Commit();
                            return true;
                        }
                        catch (Exception ex)
                        {
                            sqlTrans.Rollback();
                            MessageBox.Show($"Błąd usuwania transakcji: {ex.Message}", "Błąd", MessageBoxButton.OK, MessageBoxImage.Error);
                            return false;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Błąd usuwania transakcji: {ex.Message}", "Błąd", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }
        }

        /// <summary>
        /// Wyciąga nazwę skarbonki z nazwy transakcji
        /// Format: "AUTO-OSZCZĘDZANIE: NAZWA" lub "SKARBONKA: NAZWA"
        /// </summary>
        private string ExtractGoalNameFromTransaction(string transactionName)
        {
            if (string.IsNullOrEmpty(transactionName))
                return null;

            // Sprawdź format "AUTO-OSZCZĘDZANIE: NAZWA"
            if (transactionName.StartsWith("AUTO-OSZCZĘDZANIE:", StringComparison.OrdinalIgnoreCase))
            {
                return transactionName.Substring("AUTO-OSZCZĘDZANIE:".Length).Trim();
            }
            
            // Sprawdź format "SKARBONKA: NAZWA"
            if (transactionName.StartsWith("SKARBONKA:", StringComparison.OrdinalIgnoreCase))
            {
                return transactionName.Substring("SKARBONKA:".Length).Trim();
            }

            return null;
        }

    }
}