using System;
using System.Collections.Generic;
using System.Linq;
using MySql.Data.MySqlClient;
using FinancialManagerApp.Models;

namespace FinancialManagerApp.Services
{
    public class CategoryAssignmentService
    {
        private readonly string _connectionString = "Server=localhost; Database=financialmanagerapp; Uid=root; Pwd=;";

        /// <summary>
        /// Przypisuje kategorię do transakcji na podstawie reguł użytkownika, systemowych i domyślnej kategorii
        /// </summary>
        public (int categoryId, int? subCategoryId) AssignCategory(ImportedTransactionModel transaction, int userId)
        {
            // 1. Sprawdź reguły użytkownika
            var userRules = LoadUserRules(userId);
            var match = FindMatchingRule(transaction, userRules);
            if (match.HasValue)
                return match.Value;

            // 2. Sprawdź reguły systemowe
            var systemRules = LoadSystemRules();
            match = FindMatchingRule(transaction, systemRules);
            if (match.HasValue)
                return match.Value;

            // 3. Zwróć domyślną kategorię
            return GetDefaultCategory();
        }

        /// <summary>
        /// Wyszukuje dopasowanie reguły do transakcji
        /// </summary>
        private (int categoryId, int? subCategoryId)? FindMatchingRule(ImportedTransactionModel transaction, List<RuleData> rules)
        {
            // Tekst do przeszukania (case-insensitive)
            var searchText = $"{transaction.Name} {transaction.MerchantName} {transaction.Location} {transaction.OriginalDescription}".ToLowerInvariant();

            foreach (var rule in rules)
            {
                var phrase = rule.Phrase.ToLowerInvariant();
                
                // Sprawdź czy fraza zawiera się w tekście transakcji
                if (searchText.Contains(phrase))
                {
                    return (rule.CategoryId, rule.SubCategoryId);
                }
            }

            return null;
        }

        /// <summary>
        /// Ładuje reguły użytkownika z bazy danych
        /// </summary>
        public List<RuleData> LoadUserRules(int userId)
        {
            var rules = new List<RuleData>();

            try
            {
                using (var conn = new MySqlConnection(_connectionString))
                {
                    conn.Open();
                    string query = @"
                        SELECT fraza, id_kategorii, id_subkategorii 
                        FROM reguly_uzytkownika 
                        WHERE id_uzytkownika = @userId";

                    using (var cmd = new MySqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@userId", userId);
                        using (var reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                rules.Add(new RuleData
                                {
                                    Phrase = reader.GetString("fraza"),
                                    CategoryId = reader.GetInt32("id_kategorii"),
                                    SubCategoryId = reader.IsDBNull(reader.GetOrdinal("id_subkategorii")) 
                                        ? (int?)null 
                                        : reader.GetInt32("id_subkategorii")
                                });
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Błąd ładowania reguł użytkownika: {ex.Message}");
            }

            return rules;
        }

        /// <summary>
        /// Ładuje reguły systemowe z bazy danych
        /// </summary>
        public List<RuleData> LoadSystemRules()
        {
            var rules = new List<RuleData>();

            try
            {
                using (var conn = new MySqlConnection(_connectionString))
                {
                    conn.Open();
                    string query = @"
                        SELECT fraza, id_kategorii, id_subkategorii 
                        FROM lista_podstawowa_tagow";

                    using (var cmd = new MySqlCommand(query, conn))
                    {
                        using (var reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                rules.Add(new RuleData
                                {
                                    Phrase = reader.GetString("fraza"),
                                    CategoryId = reader.GetInt32("id_kategorii"),
                                    SubCategoryId = reader.IsDBNull(reader.GetOrdinal("id_subkategorii")) 
                                        ? (int?)null 
                                        : reader.GetInt32("id_subkategorii")
                                });
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Błąd ładowania reguł systemowych: {ex.Message}");
            }

            return rules;
        }

        /// <summary>
        /// Ładuje wszystkie kategorie i subkategorie z bazy danych
        /// </summary>
        public List<CategoryModel> LoadCategoriesFromDatabase()
        {
            var categories = new List<CategoryModel>();

            try
            {
                using (var conn = new MySqlConnection(_connectionString))
                {
                    conn.Open();
                    
                    // Najpierw ładujemy kategorie
                    string categoriesQuery = "SELECT id, typ FROM kategorie ORDER BY id";
                    using (var cmd = new MySqlCommand(categoriesQuery, conn))
                    {
                        using (var reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                categories.Add(new CategoryModel
                                {
                                    Id = reader.GetInt32("id"),
                                    Type = reader.GetString("typ")
                                });
                            }
                        }
                    }

                    // Następnie ładujemy subkategorie dla każdej kategorii
                    string subCategoriesQuery = "SELECT id, id_kategorii, nazwa FROM subkategorie ORDER BY id_kategorii, nazwa";
                    using (var cmd = new MySqlCommand(subCategoriesQuery, conn))
                    {
                        using (var reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                var categoryId = reader.GetInt32("id_kategorii");
                                var category = categories.FirstOrDefault(c => c.Id == categoryId);
                                if (category != null)
                                {
                                    category.SubCategories.Add(new SubCategoryModel
                                    {
                                        Id = reader.GetInt32("id"),
                                        CategoryId = categoryId,
                                        Name = reader.GetString("nazwa")
                                    });
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Błąd ładowania kategorii: {ex.Message}");
            }

            return categories;
        }

        /// <summary>
        /// Zwraca domyślną kategorię (podstawowe) lub pierwszą dostępną
        /// </summary>
        private (int categoryId, int? subCategoryId) GetDefaultCategory()
        {
            try
            {
                using (var conn = new MySqlConnection(_connectionString))
                {
                    conn.Open();
                    
                    // Szukamy kategorii "podstawowe"
                    string query = "SELECT id FROM kategorie WHERE typ = 'podstawowe' LIMIT 1";
                    using (var cmd = new MySqlCommand(query, conn))
                    {
                        var result = cmd.ExecuteScalar();
                        if (result != null)
                        {
                            int categoryId = Convert.ToInt32(result);
                            
                            // Pobierz pierwszą subkategorię dla tej kategorii (lub NULL)
                            string subQuery = "SELECT id FROM subkategorie WHERE id_kategorii = @catId LIMIT 1";
                            using (var subCmd = new MySqlCommand(subQuery, conn))
                            {
                                subCmd.Parameters.AddWithValue("@catId", categoryId);
                                var subResult = subCmd.ExecuteScalar();
                                int? subCategoryId = subResult != null ? Convert.ToInt32(subResult) : (int?)null;
                                
                                return (categoryId, subCategoryId);
                            }
                        }
                    }

                    // Jeśli nie ma kategorii "podstawowe", weź pierwszą dostępną
                    string firstQuery = "SELECT id FROM kategorie ORDER BY id LIMIT 1";
                    using (var cmd = new MySqlCommand(firstQuery, conn))
                    {
                        var result = cmd.ExecuteScalar();
                        if (result != null)
                        {
                            int categoryId = Convert.ToInt32(result);
                            return (categoryId, null);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Błąd pobierania domyślnej kategorii: {ex.Message}");
            }

            // Fallback - zwróć (1, null) jeśli nic nie znaleziono
            return (1, null);
        }

        /// <summary>
        /// Zapisuje regułę użytkownika do bazy danych
        /// </summary>
        public bool SaveUserRule(int userId, string phrase, int categoryId, int? subCategoryId)
        {
            try
            {
                using (var conn = new MySqlConnection(_connectionString))
                {
                    conn.Open();
                    
                    // Sprawdź czy reguła już istnieje
                    string checkQuery = @"
                        SELECT COUNT(*) 
                        FROM reguly_uzytkownika 
                        WHERE id_uzytkownika = @userId AND fraza = @phrase";
                    
                    using (var checkCmd = new MySqlCommand(checkQuery, conn))
                    {
                        checkCmd.Parameters.AddWithValue("@userId", userId);
                        checkCmd.Parameters.AddWithValue("@phrase", phrase);
                        var count = Convert.ToInt32(checkCmd.ExecuteScalar());
                        
                        if (count > 0)
                        {
                            // Reguła już istnieje - zaktualizuj
                            string updateQuery = @"
                                UPDATE reguly_uzytkownika 
                                SET id_kategorii = @catId, id_subkategorii = @subCatId 
                                WHERE id_uzytkownika = @userId AND fraza = @phrase";
                            
                            using (var updateCmd = new MySqlCommand(updateQuery, conn))
                            {
                                updateCmd.Parameters.AddWithValue("@userId", userId);
                                updateCmd.Parameters.AddWithValue("@phrase", phrase);
                                updateCmd.Parameters.AddWithValue("@catId", categoryId);
                                updateCmd.Parameters.AddWithValue("@subCatId", subCategoryId ?? (object)DBNull.Value);
                                updateCmd.ExecuteNonQuery();
                            }
                        }
                        else
                        {
                            // Nowa reguła - wstaw
                            string insertQuery = @"
                                INSERT INTO reguly_uzytkownika (id_uzytkownika, fraza, id_kategorii, id_subkategorii) 
                                VALUES (@userId, @phrase, @catId, @subCatId)";
                            
                            using (var insertCmd = new MySqlCommand(insertQuery, conn))
                            {
                                insertCmd.Parameters.AddWithValue("@userId", userId);
                                insertCmd.Parameters.AddWithValue("@phrase", phrase);
                                insertCmd.Parameters.AddWithValue("@catId", categoryId);
                                insertCmd.Parameters.AddWithValue("@subCatId", subCategoryId ?? (object)DBNull.Value);
                                insertCmd.ExecuteNonQuery();
                            }
                        }
                    }
                    
                    return true;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Błąd zapisu reguły użytkownika: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Ekstrahuje frazę do reguły z transakcji (nazwa sklepu, lokalizacja, bez kwot i numerów)
        /// </summary>
        public string ExtractPhraseForRule(ImportedTransactionModel transaction)
        {
            // Priorytet: nazwa sklepu > lokalizacja > nazwa transakcji
            string phrase = string.Empty;

            if (!string.IsNullOrWhiteSpace(transaction.MerchantName))
            {
                phrase = transaction.MerchantName;
            }
            else if (!string.IsNullOrWhiteSpace(transaction.Location))
            {
                phrase = transaction.Location;
            }
            else if (!string.IsNullOrWhiteSpace(transaction.Name))
            {
                phrase = transaction.Name;
            }

            // Usuń numery, kody, kwoty
            phrase = System.Text.RegularExpressions.Regex.Replace(phrase, @"\d+", "").Trim();
            
            // Usuń nadmiarowe spacje
            phrase = System.Text.RegularExpressions.Regex.Replace(phrase, @"\s+", " ").Trim();

            return phrase;
        }

        /// <summary>
        /// Klasa pomocnicza do przechowywania danych reguły
        /// </summary>
        public class RuleData
        {
            public string Phrase { get; set; }
            public int CategoryId { get; set; }
            public int? SubCategoryId { get; set; }
        }
    }
}
