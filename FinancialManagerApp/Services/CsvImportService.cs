using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using FinancialManagerApp.Models;

namespace FinancialManagerApp.Services
{
    public class CsvImportService
    {
        /// <summary>
        /// Parsuje plik CSV i zwraca listę transakcji
        /// </summary>
        public List<ImportedTransactionModel> ParseCsvFile(string filePath)
        {
            var transactions = new List<ImportedTransactionModel>();

            if (!File.Exists(filePath))
                throw new FileNotFoundException($"Plik nie został znaleziony: {filePath}");

            var lines = File.ReadAllLines(filePath);
            if (lines.Length < 2) // Nagłówek + co najmniej jedna transakcja
                return transactions;

            // Pomijamy nagłówek (pierwsza linia)
            for (int i = 1; i < lines.Length; i++)
            {
                var line = lines[i].Trim();
                if (string.IsNullOrWhiteSpace(line))
                    continue;

                try
                {
                    var transaction = ParseCsvLine(line);
                    if (transaction != null)
                        transactions.Add(transaction);
                }
                catch (Exception ex)
                {
                    // Logowanie błędu, ale kontynuujemy parsowanie pozostałych linii
                    System.Diagnostics.Debug.WriteLine($"Błąd parsowania linii {i + 1}: {ex.Message}");
                }
            }

            return transactions;
        }

        private ImportedTransactionModel ParseCsvLine(string line)
        {
            // Parsowanie CSV z obsługą cudzysłowów
            var fields = ParseCsvFields(line);

            if (fields.Count < 6)
                return null;

            var transaction = new ImportedTransactionModel();

            // Data operacji (indeks 0)
            if (DateTime.TryParseExact(fields[0].Trim('"'), "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime operationDate))
                transaction.Date = operationDate;

            // Data waluty (indeks 1)
            if (DateTime.TryParseExact(fields[1].Trim('"'), "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime currencyDate))
                transaction.CurrencyDate = currencyDate;

            // Typ transakcji (indeks 2)
            transaction.TransactionType = fields[2].Trim('"');

            // Kwota (indeks 3) - może być z znakiem + lub -
            var amountStr = fields[3].Trim('"').Replace(",", ".");
            if (decimal.TryParse(amountStr, NumberStyles.Any, CultureInfo.InvariantCulture, out decimal amount))
                transaction.Amount = amount;

            // Waluta (indeks 4)
            transaction.Currency = fields[4].Trim('"');

            // Opis transakcji (indeks 5 i dalej - może zawierać przecinki w cudzysłowach)
            var description = string.Join(",", fields.Skip(5)).Trim('"');
            transaction.OriginalDescription = description;

            // Ekstrakcja nazwy sklepu i lokalizacji
            ExtractMerchantAndLocation(transaction, description);

            // Utworzenie skróconej nazwy transakcji
            transaction.Name = CreateShortName(transaction);

            return transaction;
        }

        /// <summary>
        /// Parsuje linię CSV z obsługą cudzysłowów
        /// </summary>
        private List<string> ParseCsvFields(string line)
        {
            var fields = new List<string>();
            var currentField = new System.Text.StringBuilder();
            bool insideQuotes = false;

            for (int i = 0; i < line.Length; i++)
            {
                char c = line[i];

                if (c == '"')
                {
                    if (insideQuotes && i + 1 < line.Length && line[i + 1] == '"')
                    {
                        // Podwójny cudzysłów - escape
                        currentField.Append('"');
                        i++; // Pomiń następny cudzysłów
                    }
                    else
                    {
                        // Początek/koniec pola w cudzysłowach
                        insideQuotes = !insideQuotes;
                    }
                }
                else if (c == ',' && !insideQuotes)
                {
                    // Koniec pola
                    fields.Add(currentField.ToString());
                    currentField.Clear();
                }
                else
                {
                    currentField.Append(c);
                }
            }

            // Dodaj ostatnie pole
            fields.Add(currentField.ToString());

            return fields;
        }

        /// <summary>
        /// Ekstrahuje nazwę sklepu i lokalizację z opisu transakcji
        /// </summary>
        private void ExtractMerchantAndLocation(ImportedTransactionModel transaction, string description)
        {
            // Szukanie wzorca "Lokalizacja: Adres: [NAZWA]"
            var locationMatch = Regex.Match(description, @"Lokalizacja:\s*Adres:\s*([^,]+)", RegexOptions.IgnoreCase);
            if (locationMatch.Success)
            {
                transaction.MerchantName = locationMatch.Groups[1].Value.Trim();
            }

            // Jeśli nie znaleziono w lokalizacji, szukamy w "Tytuł:"
            if (string.IsNullOrWhiteSpace(transaction.MerchantName))
            {
                var titleMatch = Regex.Match(description, @"Tytuł:\s*([^,]+)", RegexOptions.IgnoreCase);
                if (titleMatch.Success)
                {
                    var title = titleMatch.Groups[1].Value.Trim();
                    // Usuwamy numery i kody z tytułu
                    transaction.MerchantName = Regex.Replace(title, @"\d+", "").Trim();
                }
            }

            // Ekstrakcja miasta i kraju
            var cityMatch = Regex.Match(description, @"Miasto:\s*([^,]+)", RegexOptions.IgnoreCase);
            var countryMatch = Regex.Match(description, @"Kraj:\s*([^,]+)", RegexOptions.IgnoreCase);

            var locationParts = new List<string>();
            if (cityMatch.Success)
                locationParts.Add(cityMatch.Groups[1].Value.Trim());
            if (countryMatch.Success)
                locationParts.Add(countryMatch.Groups[1].Value.Trim());

            transaction.Location = string.Join(", ", locationParts);
        }

        /// <summary>
        /// Tworzy skróconą nazwę transakcji z nazwy sklepu lub opisu
        /// </summary>
        private string CreateShortName(ImportedTransactionModel transaction)
        {
            if (!string.IsNullOrWhiteSpace(transaction.MerchantName))
                return transaction.MerchantName;

            // Jeśli nie ma nazwy sklepu, bierzemy pierwsze 50 znaków opisu
            if (!string.IsNullOrWhiteSpace(transaction.OriginalDescription))
            {
                var shortDesc = transaction.OriginalDescription.Length > 50
                    ? transaction.OriginalDescription.Substring(0, 50) + "..."
                    : transaction.OriginalDescription;
                return shortDesc;
            }

            return transaction.TransactionType;
        }
    }
}
