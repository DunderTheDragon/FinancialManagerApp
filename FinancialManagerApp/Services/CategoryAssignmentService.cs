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
            // Jeśli kwota jest dodatnia (przychód), automatycznie przypisz kategorię "przychód" (id=4)
            if (transaction.Amount > 0)
            {
                return (4, null); // Kategoria "przychód" nie ma subkategorii
            }

            // Pobierz tolerancję z ustawień użytkownika
            int tolerance = LoadUserSettings(userId).TolerancePercent;

            // 1. Sprawdź reguły użytkownika
            var userRules = LoadUserRules(userId);
            var match = FindMatchingRule(transaction, userRules, tolerance);
            if (match.HasValue)
                return match.Value;

            // 2. Sprawdź reguły systemowe
            var systemRules = LoadSystemRules();
            match = FindMatchingRule(transaction, systemRules, tolerance);
            if (match.HasValue)
                return match.Value;

            // 3. Zwróć domyślną kategorię (1, 1)
            return GetDefaultCategory();
        }

        /// <summary>
        /// Wyszukuje dopasowanie reguły do transakcji używając algorytmu Levenshteina z tolerancją
        /// </summary>
        private (int categoryId, int? subCategoryId)? FindMatchingRule(ImportedTransactionModel transaction, List<RuleData> rules, int tolerancePercent)
        {
            // Tekst do przeszukania (case-insensitive)
            var searchText = $"{transaction.Name} {transaction.MerchantName} {transaction.Location} {transaction.OriginalDescription}".ToLowerInvariant().Trim();

            if (string.IsNullOrWhiteSpace(searchText))
                return null;

            // Lista dopasowań z procentem podobieństwa
            var matches = new List<(RuleData rule, double similarity)>();

            foreach (var rule in rules)
            {
                var phrase = rule.Phrase.ToLowerInvariant().Trim();
                
                if (string.IsNullOrWhiteSpace(phrase))
                    continue;

                // Oblicz podobieństwo dla całej frazy
                double similarity = CalculateSimilarityPercentage(searchText, phrase);
                
                // Sprawdź również czy fraza zawiera się w tekście (dla dokładnych dopasowań)
                if (searchText.Contains(phrase))
                {
                    similarity = 100.0; // Dokładne dopasowanie
                }
                else
                {
                    // Sprawdź podobieństwo dla każdego słowa w frazie
                    var phraseWords = phrase.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                    if (phraseWords.Length > 0)
                    {
                        double maxWordSimilarity = 0;
                        foreach (var word in phraseWords)
                        {
                            if (word.Length < 3) continue; // Pomijaj bardzo krótkie słowa
                            
                            // Sprawdź czy słowo jest w tekście
                            if (searchText.Contains(word))
                            {
                                maxWordSimilarity = Math.Max(maxWordSimilarity, 100.0);
                            }
                            else
                            {
                                // Oblicz podobieństwo dla każdego słowa w tekście
                                var textWords = searchText.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                                foreach (var textWord in textWords)
                                {
                                    if (textWord.Length < 3) continue;
                                    double wordSimilarity = CalculateSimilarityPercentage(textWord, word);
                                    maxWordSimilarity = Math.Max(maxWordSimilarity, wordSimilarity);
                                }
                            }
                        }
                        similarity = Math.Max(similarity, maxWordSimilarity);
                    }
                }

                matches.Add((rule, similarity));
            }

            // Sortuj od najbardziej dopasowanej do najmniej
            matches = matches.OrderByDescending(m => m.similarity).ToList();

            // Weź najbardziej dopasowaną regułę i sprawdź czy spełnia tolerancję
            if (matches.Count > 0 && matches[0].similarity >= tolerancePercent)
            {
                return (matches[0].rule.CategoryId, matches[0].rule.SubCategoryId);
            }

            return null;
        }

        /// <summary>
        /// Oblicza odległość Levenshteina między dwoma stringami
        /// </summary>
        private int CalculateLevenshteinDistance(string s1, string s2)
        {
            if (string.IsNullOrEmpty(s1))
                return string.IsNullOrEmpty(s2) ? 0 : s2.Length;

            if (string.IsNullOrEmpty(s2))
                return s1.Length;

            int n = s1.Length;
            int m = s2.Length;
            int[,] d = new int[n + 1, m + 1];

            // Inicjalizacja
            for (int i = 0; i <= n; i++)
                d[i, 0] = i;

            for (int j = 0; j <= m; j++)
                d[0, j] = j;

            // Obliczanie odległości
            for (int i = 1; i <= n; i++)
            {
                for (int j = 1; j <= m; j++)
                {
                    int cost = (s2[j - 1] == s1[i - 1]) ? 0 : 1;

                    d[i, j] = Math.Min(
                        Math.Min(d[i - 1, j] + 1, d[i, j - 1] + 1),
                        d[i - 1, j - 1] + cost
                    );
                }
            }

            return d[n, m];
        }

        /// <summary>
        /// Oblicza procent podobieństwa (0-100) między dwoma stringami używając algorytmu Levenshteina
        /// </summary>
        private double CalculateSimilarityPercentage(string s1, string s2)
        {
            if (string.IsNullOrEmpty(s1) && string.IsNullOrEmpty(s2))
                return 100.0;

            if (string.IsNullOrEmpty(s1) || string.IsNullOrEmpty(s2))
                return 0.0;

            int maxLength = Math.Max(s1.Length, s2.Length);
            if (maxLength == 0)
                return 100.0;

            int distance = CalculateLevenshteinDistance(s1, s2);
            double similarity = ((maxLength - distance) / (double)maxLength) * 100.0;

            return Math.Max(0.0, Math.Min(100.0, similarity));
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
        /// Zwraca domyślną kategorię (1, 1)
        /// </summary>
        private (int categoryId, int? subCategoryId) GetDefaultCategory()
        {
            // Zgodnie z wymaganiami: domyślna kategoria to id_kategorii = 1, id_subkategorii = 1
            return (1, 1);
        }

        /// <summary>
        /// Ładuje ustawienia użytkownika z bazy danych
        /// </summary>
        public UserSettings LoadUserSettings(int userId)
        {
            try
            {
                using (var conn = new MySqlConnection(_connectionString))
                {
                    conn.Open();
                    string query = @"
                        SELECT automatyczne_tagowanie, nadpisywanie_tagow, tolerancja_procent 
                        FROM ustawienia_uzytkownika 
                        WHERE id_uzytkownika = @userId";

                    using (var cmd = new MySqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@userId", userId);
                        using (var reader = cmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                return new UserSettings
                                {
                                    AutoCheckTag = reader.GetBoolean("automatyczne_tagowanie"),
                                    OverwriteTags = reader.GetBoolean("nadpisywanie_tagow"),
                                    TolerancePercent = reader.GetInt32("tolerancja_procent")
                                };
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Błąd ładowania ustawień użytkownika: {ex.Message}");
            }

            // Domyślne wartości
            return new UserSettings
            {
                AutoCheckTag = true,
                OverwriteTags = false,
                TolerancePercent = 80
            };
        }

        /// <summary>
        /// Klasa pomocnicza do przechowywania ustawień użytkownika
        /// </summary>
        public class UserSettings
        {
            public bool AutoCheckTag { get; set; }
            public bool OverwriteTags { get; set; }
            public int TolerancePercent { get; set; }
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
