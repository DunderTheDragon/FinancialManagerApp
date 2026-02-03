using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using FinancialManagerApp.Core;
using FinancialManagerApp.Models;
using FinancialManagerApp.Services;
using MySql.Data.MySqlClient;

namespace FinancialManagerApp.ViewModels
{
    public class ImportTransactionsViewModel : ViewModelBase
    {
        private readonly string _connectionString = "Server=localhost; Database=financialmanagerapp; Uid=root; Pwd=;";
        private readonly CategoryAssignmentService _categoryService;

        public User CurrentUser { get; set; }
        public int WalletId { get; set; }
        public ObservableCollection<ImportedTransactionModel> ImportedTransactions { get; set; }
        public ObservableCollection<CategoryModel> Categories { get; set; }
        
        public ICommand SaveTransactionsCommand { get; }
        public ICommand CancelCommand { get; }

        public ImportTransactionsViewModel(User user, int walletId, ObservableCollection<ImportedTransactionModel> transactions)
        {
            CurrentUser = user;
            WalletId = walletId;
            ImportedTransactions = transactions;
            Categories = new ObservableCollection<CategoryModel>();
            _categoryService = new CategoryAssignmentService();

            SaveTransactionsCommand = new RelayCommand(ExecuteSaveTransactions);
            CancelCommand = new RelayCommand(ExecuteCancel);

            LoadCategories();
            AssignCategories();
        }

        /// <summary>
        /// Ładuje wszystkie kategorie i subkategorie z bazy danych
        /// </summary>
        private void LoadCategories()
        {
            try
            {
                var categories = _categoryService.LoadCategoriesFromDatabase();
                Categories.Clear();
                foreach (var category in categories)
                {
                    Categories.Add(category);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Błąd ładowania kategorii: {ex.Message}", "Błąd", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Automatycznie przypisuje kategorie do wszystkich transakcji
        /// </summary>
        private void AssignCategories()
        {
            foreach (var transaction in ImportedTransactions)
            {
                var (categoryId, subCategoryId) = _categoryService.AssignCategory(transaction, CurrentUser.Id);
                transaction.CategoryId = categoryId;
                transaction.SubCategoryId = subCategoryId ?? 0;

                // Ustawienie nazw kategorii do wyświetlenia
                var category = Categories.FirstOrDefault(c => c.Id == categoryId);
                if (category != null)
                {
                    transaction.Category = category.Type;
                    
                    if (subCategoryId.HasValue)
                    {
                        var subCategory = category.SubCategories.FirstOrDefault(s => s.Id == subCategoryId.Value);
                        transaction.SubCategory = subCategory?.Name ?? "Brak";
                    }
                    else
                    {
                        transaction.SubCategory = "Brak";
                    }
                }
            }
        }

        /// <summary>
        /// Zapisuje transakcje do bazy danych
        /// </summary>
        private void ExecuteSaveTransactions(object parameter)
        {
            try
            {
                using (var conn = new MySqlConnection(_connectionString))
                {
                    conn.Open();
                    using (var transaction = conn.BeginTransaction())
                    {
                        try
                        {
                            int savedCount = 0;
                            int rulesCreated = 0;

                            foreach (var importedTransaction in ImportedTransactions)
                            {
                                // Utworzenie reguły użytkownika jeśli zaznaczono checkbox
                                if (importedTransaction.ShouldCreateRule)
                                {
                                    string phrase = _categoryService.ExtractPhraseForRule(importedTransaction);
                                    if (!string.IsNullOrWhiteSpace(phrase))
                                    {
                                        bool ruleSaved = _categoryService.SaveUserRule(
                                            CurrentUser.Id,
                                            phrase,
                                            importedTransaction.CategoryId,
                                            importedTransaction.SubCategoryId > 0 ? importedTransaction.SubCategoryId : (int?)null
                                        );
                                        if (ruleSaved)
                                            rulesCreated++;
                                    }
                                }

                                // Zapis transakcji
                                string insertQuery = @"
                                    INSERT INTO transakcje 
                                    (id_portfela, data_transakcji, nazwa, id_kategorii, id_subkategorii, kwota, checkedTag) 
                                    VALUES (@wId, @date, @name, @catId, @subCatId, @amount, @checked)";

                                using (var cmd = new MySqlCommand(insertQuery, conn, transaction))
                                {
                                    cmd.Parameters.AddWithValue("@wId", WalletId);
                                    cmd.Parameters.AddWithValue("@date", importedTransaction.Date);
                                    cmd.Parameters.AddWithValue("@name", importedTransaction.Name);
                                    cmd.Parameters.AddWithValue("@catId", importedTransaction.CategoryId);
                                    cmd.Parameters.AddWithValue("@subCatId", importedTransaction.SubCategoryId > 0 ? importedTransaction.SubCategoryId : (object)DBNull.Value);
                                    cmd.Parameters.AddWithValue("@amount", importedTransaction.Amount);
                                    cmd.Parameters.AddWithValue("@checked", importedTransaction.CheckedTag);
                                    cmd.ExecuteNonQuery();
                                }

                                savedCount++;
                            }

                            // Aktualizacja salda portfela
                            decimal totalAmount = ImportedTransactions.Sum(t => t.Amount);
                            string updateWalletQuery = "UPDATE portfele SET saldo = saldo + @amount WHERE id = @walletId";
                            using (var cmd = new MySqlCommand(updateWalletQuery, conn, transaction))
                            {
                                cmd.Parameters.AddWithValue("@amount", totalAmount);
                                cmd.Parameters.AddWithValue("@walletId", WalletId);
                                cmd.ExecuteNonQuery();
                            }

                            transaction.Commit();

                            MessageBox.Show(
                                $"Zapisano {savedCount} transakcji.\n" +
                                (rulesCreated > 0 ? $"Utworzono {rulesCreated} reguł użytkownika." : ""),
                                "Sukces",
                                MessageBoxButton.OK,
                                MessageBoxImage.Information
                            );

                            // Zamknij okno z wynikiem true
                            if (Application.Current.Windows.OfType<Views.ImportTransactionsView>().FirstOrDefault() is Views.ImportTransactionsView window)
                            {
                                window.DialogResult = true;
                                window.Close();
                            }
                        }
                        catch (Exception ex)
                        {
                            transaction.Rollback();
                            throw;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Błąd zapisu transakcji: {ex.Message}", "Błąd", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Anuluje import
        /// </summary>
        private void ExecuteCancel(object parameter)
        {
            if (Application.Current.Windows.OfType<Views.ImportTransactionsView>().FirstOrDefault() is Views.ImportTransactionsView window)
            {
                window.DialogResult = false;
                window.Close();
            }
        }

        /// <summary>
        /// Pobiera subkategorie dla danej kategorii
        /// </summary>
        public ObservableCollection<SubCategoryModel> GetSubCategoriesForCategory(int categoryId)
        {
            var category = Categories.FirstOrDefault(c => c.Id == categoryId);
            return category?.SubCategories ?? new ObservableCollection<SubCategoryModel>();
        }
    }
}
