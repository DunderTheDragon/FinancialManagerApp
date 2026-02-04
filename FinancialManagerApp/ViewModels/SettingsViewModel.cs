using FinancialManagerApp.Core;
using FinancialManagerApp.Models;
using FinancialManagerApp.Services;
using FinancialManagerApp.Views;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;
using System.Windows;
using MySql.Data.MySqlClient;
using System;

namespace FinancialManagerApp.ViewModels
{
    public class SettingsViewModel : ViewModelBase
    {
        private readonly string _connectionString = "Server=localhost; Database=financialmanagerapp; Uid=root; Pwd=;";
        private readonly CategoryAssignmentService _categoryService;
        private bool _isLoading = false; // Flaga zapobiegająca zapisywaniu podczas ładowania

        public User CurrentUser { get; set; }

        // Właściwości zbindowane do checkboxów i textboxów
        private bool _autoCheckTag;
        public bool AutoCheckTag
        {
            get => _autoCheckTag;
            set 
            { 
                _autoCheckTag = value; 
                OnPropertyChanged();
                if (!_isLoading)
                    SaveSettingsToDatabase();
            }
        }

        private bool _overwriteTags;
        public bool OverwriteTags
        {
            get => _overwriteTags;
            set 
            { 
                _overwriteTags = value; 
                OnPropertyChanged();
                if (!_isLoading)
                    SaveSettingsToDatabase();
            }
        }

        private string _assignmentTolerance = "10";
        public string AssignmentTolerance
        {
            get => _assignmentTolerance;
            set 
            { 
                // Walidacja: 0-100
                if (int.TryParse(value, out int tolerance) && tolerance >= 0 && tolerance <= 100)
                {
                    _assignmentTolerance = value;
                    OnPropertyChanged();
                    if (!_isLoading)
                        SaveSettingsToDatabase();
                }
                else if (string.IsNullOrWhiteSpace(value))
                {
                    _assignmentTolerance = value;
                    OnPropertyChanged();
                }
            }
        }

        public ObservableCollection<RuleModel> UserRules { get; set; }

        public ICommand SaveSettingsCommand { get; }
        public ICommand AddRuleCommand { get; }
        public ICommand DeleteRuleCommand { get; }

        public SettingsViewModel(User user)
        {
            CurrentUser = user;
            _categoryService = new CategoryAssignmentService();
            UserRules = new ObservableCollection<RuleModel>();

            SaveSettingsCommand = new RelayCommand(ExecuteSaveSettings);
            AddRuleCommand = new RelayCommand(ExecuteAddRule);
            DeleteRuleCommand = new RelayCommand(ExecuteDeleteRule);

            LoadSettingsFromDatabase();
            LoadUserRulesFromDatabase();
        }

        /// <summary>
        /// Publiczna metoda do odświeżania danych - wywoływana przy przełączaniu widoków
        /// </summary>
        public void Refresh()
        {
            LoadSettingsFromDatabase();
            LoadUserRulesFromDatabase();
        }

        /// <summary>
        /// Ładuje ustawienia użytkownika z bazy danych
        /// </summary>
        private void LoadSettingsFromDatabase()
        {
            try
            {
                _isLoading = true;

                using (var conn = new MySqlConnection(_connectionString))
                {
                    conn.Open();
                    string query = @"
                        SELECT automatyczne_tagowanie, nadpisywanie_tagow, tolerancja_procent 
                        FROM ustawienia_uzytkownika 
                        WHERE id_uzytkownika = @userId";

                    using (var cmd = new MySqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@userId", CurrentUser.Id);
                        using (var reader = cmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                AutoCheckTag = reader.GetBoolean("automatyczne_tagowanie");
                                OverwriteTags = reader.GetBoolean("nadpisywanie_tagow");
                                AssignmentTolerance = reader.GetInt32("tolerancja_procent").ToString();
                            }
                            else
                            {
                                // Domyślne wartości jeśli brak ustawień
                                AutoCheckTag = true;
                                OverwriteTags = false;
                                AssignmentTolerance = "80";
                                // Utwórz rekord z domyślnymi wartościami
                                SaveSettingsToDatabase();
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Błąd ładowania ustawień: {ex.Message}");
                // Domyślne wartości w przypadku błędu
                AutoCheckTag = true;
                OverwriteTags = false;
                AssignmentTolerance = "80";
            }
            finally
            {
                _isLoading = false;
            }
        }

        /// <summary>
        /// Zapisuje ustawienia użytkownika do bazy danych
        /// </summary>
        private void SaveSettingsToDatabase()
        {
            try
            {
                if (CurrentUser == null) return;

                using (var conn = new MySqlConnection(_connectionString))
                {
                    conn.Open();
                    
                    // INSERT ... ON DUPLICATE KEY UPDATE
                    string query = @"
                        INSERT INTO ustawienia_uzytkownika 
                        (id_uzytkownika, automatyczne_tagowanie, nadpisywanie_tagow, tolerancja_procent) 
                        VALUES (@userId, @autoTag, @overwrite, @tolerance)
                        ON DUPLICATE KEY UPDATE 
                        automatyczne_tagowanie = @autoTag,
                        nadpisywanie_tagow = @overwrite,
                        tolerancja_procent = @tolerance";

                    using (var cmd = new MySqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@userId", CurrentUser.Id);
                        cmd.Parameters.AddWithValue("@autoTag", AutoCheckTag);
                        cmd.Parameters.AddWithValue("@overwrite", OverwriteTags);
                        cmd.Parameters.AddWithValue("@tolerance", int.TryParse(AssignmentTolerance, out int tol) ? tol : 80);
                        cmd.ExecuteNonQuery();
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Błąd zapisu ustawień: {ex.Message}");
            }
        }

        /// <summary>
        /// Ładuje reguły użytkownika z bazy danych
        /// </summary>
        public void LoadUserRulesFromDatabase()
        {
            try
            {
                UserRules.Clear();

                var categories = _categoryService.LoadCategoriesFromDatabase();

                using (var conn = new MySqlConnection(_connectionString))
                {
                    conn.Open();
                    string query = @"
                        SELECT id, fraza, id_kategorii, id_subkategorii 
                        FROM reguly_uzytkownika 
                        WHERE id_uzytkownika = @userId
                        ORDER BY fraza";

                    using (var cmd = new MySqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@userId", CurrentUser.Id);
                        using (var reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                int ruleId = reader.GetInt32("id");
                                string phrase = reader.GetString("fraza");
                                int categoryId = reader.GetInt32("id_kategorii");
                                int? subCategoryId = reader.IsDBNull(reader.GetOrdinal("id_subkategorii")) 
                                    ? (int?)null 
                                    : reader.GetInt32("id_subkategorii");

                                var category = categories.FirstOrDefault(c => c.Id == categoryId);
                                string categoryName = category?.Type ?? "Nieznana";
                                
                                string subCategoryName = "Brak";
                                if (subCategoryId.HasValue && category != null)
                                {
                                    var subCategory = category.SubCategories.FirstOrDefault(s => s.Id == subCategoryId.Value);
                                    subCategoryName = subCategory?.Name ?? "Brak";
                                }

                                UserRules.Add(new RuleModel
                                {
                                    Id = ruleId,
                                    Phrase = phrase,
                                    CategoryId = categoryId,
                                    SubCategoryId = subCategoryId,
                                    Category = categoryName,
                                    SubCategory = subCategoryName
                                });
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Błąd ładowania reguł użytkownika: {ex.Message}");
                MessageBox.Show($"Błąd ładowania reguł: {ex.Message}", "Błąd", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Usuwa regułę użytkownika z bazy danych
        /// </summary>
        private void DeleteUserRule(int ruleId)
        {
            try
            {
                using (var conn = new MySqlConnection(_connectionString))
                {
                    conn.Open();
                    string query = "DELETE FROM reguly_uzytkownika WHERE id = @ruleId AND id_uzytkownika = @userId";

                    using (var cmd = new MySqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@ruleId", ruleId);
                        cmd.Parameters.AddWithValue("@userId", CurrentUser.Id);
                        cmd.ExecuteNonQuery();
                    }
                }

                // Odśwież listę reguł
                LoadUserRulesFromDatabase();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Błąd usuwania reguły: {ex.Message}");
                MessageBox.Show($"Błąd usuwania reguły: {ex.Message}", "Błąd", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ExecuteAddRule(object obj)
        {
            var addRuleWindow = new AddRuleView(CurrentUser, this);
            addRuleWindow.Owner = Application.Current.MainWindow;
            bool? result = addRuleWindow.ShowDialog();
            
            if (result == true)
            {
                // Reguła została dodana, lista została odświeżona w AddRuleView
            }
        }

        private void ExecuteDeleteRule(object obj)
        {
            if (obj is RuleModel rule)
            {
                var result = MessageBox.Show(
                    $"Czy na pewno chcesz usunąć regułę: \"{rule.Phrase}\"?",
                    "Potwierdzenie usunięcia",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question
                );

                if (result == MessageBoxResult.Yes)
                {
                    DeleteUserRule(rule.Id);
                }
            }
        }

        private void ExecuteSaveSettings(object obj)
        {
            SaveSettingsToDatabase();
            MessageBox.Show("Ustawienia zostały zapisane!", "Finansense", MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }
}