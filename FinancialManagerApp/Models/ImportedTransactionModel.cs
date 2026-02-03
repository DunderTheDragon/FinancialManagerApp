using System;

namespace FinancialManagerApp.Models
{
    public class ImportedTransactionModel : TransactionModel
    {
        /// <summary>
        /// Pełny opis transakcji z pliku CSV
        /// </summary>
        public string OriginalDescription { get; set; } = string.Empty;

        /// <summary>
        /// Typ transakcji z CSV (np. "Płatność kartą", "Przelew na konto")
        /// </summary>
        public string TransactionType { get; set; } = string.Empty;

        /// <summary>
        /// Czy utworzyć regułę użytkownika dla tej transakcji
        /// </summary>
        public bool ShouldCreateRule { get; set; }

        /// <summary>
        /// Wyekstrahowana lokalizacja z opisu (miasto, kraj)
        /// </summary>
        public string Location { get; set; } = string.Empty;

        /// <summary>
        /// Nazwa sklepu/kontrahenta wyekstrahowana z opisu
        /// </summary>
        public string MerchantName { get; set; } = string.Empty;

        /// <summary>
        /// Data waluty z CSV (może różnić się od daty operacji)
        /// </summary>
        public DateTime CurrencyDate { get; set; }

        /// <summary>
        /// Waluta transakcji (np. "PLN")
        /// </summary>
        public string Currency { get; set; } = "PLN";
    }
}
