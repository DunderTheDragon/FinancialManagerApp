using FinancialManagerApp.Core;
using FinancialManagerApp.Models;
using FinancialManagerApp.Views;
using MySql.Data.MySqlClient;
using System;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace FinancialManagerApp.ViewModels
{
    // Używamy ViewModelBase, aby zachować spójność z resztą projektu
    public class GoalsViewModel : ViewModelBase
    {
        private string _connectionString = "Server=localhost; Database=financialmanagerapp; Uid=root; Pwd=;";

        public User CurrentUser { get; set; }
        public ObservableCollection<SavingsGoalModel> SavingsGoals { get; set; }

        public ICommand OpenAddGoalCommand { get; }
        public ICommand OpenEditGoalCommand { get; }
        public ICommand OpenDepositCommand { get; }
        public ICommand DeleteGoalCommand { get; }

        public GoalsViewModel(User user)
        {
            CurrentUser = user;
            SavingsGoals = new ObservableCollection<SavingsGoalModel>();

            // Inicjalizacja komend
            OpenAddGoalCommand = new RelayCommand(ExecuteOpenAddGoal);
            OpenEditGoalCommand = new RelayCommand(ExecuteOpenEditGoal);
            OpenDepositCommand = new RelayCommand(ExecuteOpenDeposit);
            DeleteGoalCommand = new RelayCommand(ExecuteDeleteGoal);

            LoadGoalsFromDb();
        }

        public void LoadGoalsFromDb()
        {
            try
            {
                using (var conn = new MySqlConnection(_connectionString))
                {
                    conn.Open();
                    // Pobieramy wszystkie kolumny, w tym te dotyczące automatyzacji
                    string query = "SELECT * FROM skarbonki WHERE id_uzytkownika = @userId";

                    using (var cmd = new MySqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@userId", CurrentUser.Id);
                        using (var reader = cmd.ExecuteReader())
                        {
                            SavingsGoals.Clear();
                            while (reader.Read())
                            {
                                SavingsGoals.Add(new SavingsGoalModel
                                {
                                    Id = reader.GetInt32("id"),
                                    GoalName = reader.GetString("nazwa"),
                                    CurrentAmount = reader.GetDecimal("kwota_aktualna"),
                                    TargetAmount = reader.GetDecimal("kwota_docelowa"),
                                    IsRecurring = reader.GetBoolean("cykliczne"),

                                    // Mapowanie nowych kolumn automatyzacji
                                    ContributionType = reader.IsDBNull(reader.GetOrdinal("typ_pobrania")) ? null : reader.GetString("typ_pobrania"),
                                    ContributionValue = reader.IsDBNull(reader.GetOrdinal("wartosc_pobrania")) ? 0 : reader.GetDecimal("wartosc_pobrania"),
                                    SourceWalletId = reader.IsDBNull(reader.GetOrdinal("id_portfela_zrodlowego")) ? (int?)null : reader.GetInt32("id_portfela_zrodlowego")
                                });
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Błąd ładowania danych skarbonek: " + ex.Message);
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
                    // Pobieramy portfele przypisane do aktualnego użytkownika
                    string query = "SELECT id, nazwa, saldo, typ FROM portfele WHERE id_uzytkownika = @uId";
                    using (var cmd = new MySqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@uId", CurrentUser.Id);
                        using (var reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                wallets.Add(new WalletModel
                                {
                                    Id = reader.GetInt32("id"),
                                    Name = reader.GetString("nazwa"),
                                    Balance = reader.GetDecimal("saldo"),
                                    Type = reader.GetString("typ")
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

        private void ExecuteOpenAddGoal(object obj)
        {
            // Pobieramy listę portfeli (załóżmy, że masz do niej dostęp lub przekaż ją z MainViewModel)
            // Jeśli nie masz listy pod ręką, możesz ją pobrać z bazy przed otwarciem okna
            var wallets = GetUserWallets();

            var addWindow = new AddGoalView(wallets);
            addWindow.Owner = Application.Current.MainWindow;

            if (addWindow.ShowDialog() == true)
            {
                var newGoal = new SavingsGoalModel
                {
                    GoalName = addWindow.GoalName,
                    TargetAmount = addWindow.TargetAmount,
                    CurrentAmount = addWindow.CurrentAmount,
                    IsRecurring = addWindow.IsRecurring,
                    ContributionType = addWindow.RecurringType,
                    ContributionValue = addWindow.RecurringValue,
                    SourceWalletId = addWindow.SelectedWalletId // NOWE POLE
                };

                if (SaveGoalToDb(newGoal))
                {
                    LoadGoalsFromDb();
                    MessageBox.Show("Skarbonka utworzona poprawnie!");
                }
            }
        }

        private bool SaveGoalToDb(SavingsGoalModel goal)
        {
            try
            {
                using (var conn = new MySqlConnection(_connectionString))
                {
                    conn.Open();
                    // Rozszerzone zapytanie o wszystkie wymagane kolumny
                    string query = @"INSERT INTO skarbonki 
                (id_uzytkownika, nazwa, kwota_aktualna, kwota_docelowa, cykliczne, typ_pobrania, wartosc_pobrania, id_portfela_zrodlowego) 
                VALUES (@uId, @name, @curr, @target, @recur, @type, @val, @wId)";

                    using (var cmd = new MySqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@uId", CurrentUser.Id);
                        cmd.Parameters.AddWithValue("@name", goal.GoalName);
                        cmd.Parameters.AddWithValue("@curr", goal.CurrentAmount);
                        cmd.Parameters.AddWithValue("@target", goal.TargetAmount);
                        cmd.Parameters.AddWithValue("@recur", goal.IsRecurring);

                        // Obsługa pól opcjonalnych (jeśli IsRecurring == false, wysyłamy NULL)
                        cmd.Parameters.AddWithValue("@type", goal.IsRecurring ? goal.ContributionType : (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@val", goal.IsRecurring ? goal.ContributionValue : (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@wId", goal.IsRecurring ? goal.SourceWalletId : (object)DBNull.Value);

                        cmd.ExecuteNonQuery();
                        return true;
                    }
                }
            }
            catch (Exception ex) { MessageBox.Show("Błąd zapisu: " + ex.Message); return false; }
        }

        private void ExecuteOpenEditGoal(object obj)
        {
            if (obj is SavingsGoalModel selectedGoal)
            {
                // Tworzymy kopię celu, aby nie zmieniać oryginału przed zapisem w bazie
                var editGoal = new SavingsGoalModel
                {
                    Id = selectedGoal.Id,
                    GoalName = selectedGoal.GoalName,
                    TargetAmount = selectedGoal.TargetAmount,
                    IsRecurring = selectedGoal.IsRecurring,
                    ContributionType = selectedGoal.ContributionType,
                    ContributionValue = selectedGoal.ContributionValue
                };

                var editWindow = new EditGoalView(editGoal);
                editWindow.Owner = Application.Current.MainWindow;

                if (editWindow.ShowDialog() == true)
                {
                    // Pobieramy dane z ComboBox (którego nie ma w bindowaniu)
                    editGoal.ContributionType = (editWindow.TypeCombo.SelectedItem as ComboBoxItem)?.Content.ToString();

                    if (UpdateGoalInDb(editGoal))
                    {
                        LoadGoalsFromDb(); // Odśwież widok
                        MessageBox.Show("Zmiany zostały zapisane.");
                    }
                }
            }
        }

        private bool UpdateGoalInDb(SavingsGoalModel goal)
        {
            try
            {
                using (var conn = new MySqlConnection(_connectionString))
                {
                    conn.Open();
                    // Zapytanie aktualizujące rekord na podstawie ID
                    string query = @"UPDATE skarbonki 
                            SET nazwa = @name, 
                                kwota_docelowa = @target, 
                                cykliczne = @recur, 
                                typ_pobrania = @type, 
                                wartosc_pobrania = @val 
                            WHERE id = @id";

                    using (var cmd = new MySqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@name", goal.GoalName);
                        cmd.Parameters.AddWithValue("@target", goal.TargetAmount);
                        cmd.Parameters.AddWithValue("@recur", goal.IsRecurring);
                        cmd.Parameters.AddWithValue("@type", goal.IsRecurring ? goal.ContributionType : (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@val", goal.IsRecurring ? goal.ContributionValue : (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@id", goal.Id);

                        cmd.ExecuteNonQuery();
                        return true;
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Błąd bazy danych: " + ex.Message);
                return false;
            }
        }
        private void ExecuteOpenDeposit(object obj)
        {
            if (obj is SavingsGoalModel selectedGoal)
            {
                // 1. Pobierz aktualne portfele użytkownika
                var wallets = GetUserWallets();

                var paymentWindow = new AddPaymentGoalView(wallets);
                paymentWindow.Owner = Application.Current.MainWindow;

                if (paymentWindow.ShowDialog() == true)
                {
                    // 2. Wykonaj transakcję w bazie
                    if (ProcessDeposit(selectedGoal, paymentWindow.Amount, paymentWindow.SelectedWallet.Id))
                    {
                        LoadGoalsFromDb(); // Odśwież widok skarbonek
                        MessageBox.Show("Wpłata zakończona sukcesem!");
                    }
                }
            }
        }

        private bool ProcessDeposit(SavingsGoalModel goal, decimal amount, int walletId)
        {
            try
            {
                using (var conn = new MySqlConnection(_connectionString))
                {
                    conn.Open();
                    using (var trans = conn.BeginTransaction()) // Transakcja gwarantuje spójność danych
                    {
                        try
                        {
                            // Odejmij z portfela
                            string subWallet = "UPDATE portfele SET saldo = saldo - @amt WHERE id = @wid";
                            // Dodaj do skarbonki
                            string addGoal = "UPDATE skarbonki SET kwota_aktualna = kwota_aktualna + @amt WHERE id = @gid";

                            using (var cmd = new MySqlCommand(subWallet, conn, trans))
                            {
                                cmd.Parameters.AddWithValue("@amt", amount);
                                cmd.Parameters.AddWithValue("@wid", walletId);
                                cmd.ExecuteNonQuery();
                            }

                            using (var cmd = new MySqlCommand(addGoal, conn, trans))
                            {
                                cmd.Parameters.AddWithValue("@amt", amount);
                                cmd.Parameters.AddWithValue("@gid", goal.Id);
                                cmd.ExecuteNonQuery();
                            }

                            trans.Commit();
                            return true;
                        }
                        catch { trans.Rollback(); throw; }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Błąd podczas wpłaty: " + ex.Message);
                return false;
            }
        }

        // Usuwanie skarbonki
        private void ExecuteDeleteGoal(object obj)
        {
            if (obj is SavingsGoalModel goalToDelete)
            {
                var result = MessageBox.Show($"Czy na pewno chcesz usunąć skarbonkę '{goalToDelete.GoalName}'?",
                                           "Potwierdzenie", MessageBoxButton.YesNo, MessageBoxImage.Question);

                if (result == MessageBoxResult.Yes)
                {
                    var wallets = GetUserWallets();
                    var refundWindow = new RefundWalletView(wallets);
                    refundWindow.Owner = Application.Current.MainWindow;

                    if (refundWindow.ShowDialog() == true)
                    {
                        if (DeleteGoalWithRefund(goalToDelete, refundWindow.SelectedWallet.Id))
                        {
                            LoadGoalsFromDb();
                            MessageBox.Show("Skarbonka została usunięta, a środki zwrócone do portfela.");
                        }
                    }
                }
            }
        }

        // 4. Metoda transakcyjna SQL
        private bool DeleteGoalWithRefund(SavingsGoalModel goal, int walletId)
        {
            try
            {
                using (var conn = new MySqlConnection(_connectionString))
                {
                    conn.Open();
                    using (var trans = conn.BeginTransaction())
                    {
                        try
                        {
                            // Zwróć środki do portfela
                            string refundQuery = "UPDATE portfele SET saldo = saldo + @amt WHERE id = @wid";
                            using (var cmd = new MySqlCommand(refundQuery, conn, trans))
                            {
                                cmd.Parameters.AddWithValue("@amt", goal.CurrentAmount);
                                cmd.Parameters.AddWithValue("@wid", walletId);
                                cmd.ExecuteNonQuery();
                            }

                            // Usuń skarbonkę
                            string deleteQuery = "DELETE FROM skarbonki WHERE id = @gid";
                            using (var cmd = new MySqlCommand(deleteQuery, conn, trans))
                            {
                                cmd.Parameters.AddWithValue("@gid", goal.Id);
                                cmd.ExecuteNonQuery();
                            }

                            trans.Commit();
                            return true;
                        }
                        catch { trans.Rollback(); throw; }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Błąd podczas usuwania: " + ex.Message);
                return false;
            }
        }
    }
}