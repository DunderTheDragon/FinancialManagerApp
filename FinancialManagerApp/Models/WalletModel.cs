namespace FinancialManagerApp.Models
{
    public class WalletModel
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Type { get; set; } = "Manualny";
        public string Description { get; set; } = string.Empty;
        public decimal Balance { get; set; }

        // Nowe pola dla integracji API
        public string? RevolutClientId { get; set; }
        public string? RevolutPrivateKey { get; set; }
        public string? RevolutRefreshToken { get; set; }
        public string? RevolutAccountId { get; set; }
        public DateTime? LastSyncDate { get; set; }
    }
}