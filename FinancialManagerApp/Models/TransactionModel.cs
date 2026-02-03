namespace FinancialManagerApp.Models
{
    public class TransactionModel
    {
        public int Id { get; set; }
        public int WalletId { get; set; }
        public string WalletName { get; set; }
        public DateTime Date { get; set; } = DateTime.Now;
        public string Name { get; set; }
        // Właściwości tekstowe (do wyświetlania w tabeli)
        public string Category { get; set; }
        public string SubCategory { get; set; }

        // --- DODAJ TE POLA DO ZAPISU DO BAZY ---
        public int CategoryId { get; set; } // Mapuje na id_kategorii
        public int SubCategoryId { get; set; } // Mapuje na id_subkategorii
        public decimal Amount { get; set; }

        // Zmieniliśmy nazwę z IsConfirmed na CheckedTag
        public bool CheckedTag { get; set; }
    }
}