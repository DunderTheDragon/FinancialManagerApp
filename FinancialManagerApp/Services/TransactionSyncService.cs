using FinancialManagerApp.Models;
using FinancialManagerApp.Models.Revolut;
using FinancialManagerApp.Services;
using FinancialManagerApp.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FinancialManagerApp.Services
{
    public class TransactionSyncService
    {
        private readonly RevolutService _revolutService;
        private readonly TransactionsViewModel _transactionsViewModel;

        public TransactionSyncService(RevolutService revolutService, TransactionsViewModel transactionsViewModel)
        {
            _revolutService = revolutService;
            _transactionsViewModel = transactionsViewModel;
        }

        public async Task SyncWalletTransactionsAsync(WalletModel wallet)
        {
            if (wallet.Type != "API" || string.IsNullOrEmpty(wallet.RevolutClientId) ||
                string.IsNullOrEmpty(wallet.RevolutPrivateKey) || string.IsNullOrEmpty(wallet.RevolutAccountId))
            {
                return;
            }

            try
            {
                var transactions = await _revolutService.GetTransactionsForWalletAsync(
                    wallet.RevolutClientId,
                    wallet.RevolutPrivateKey,
                    wallet.RevolutRefreshToken,
                    wallet.RevolutAccountId,
                    wallet.LastSyncDate);

                // Konwersja na TransactionModel i dodanie do listy
                foreach (var revTransaction in transactions)
                {
                    var transaction = new TransactionModel
                    {
                        Date = revTransaction.CompletedAt ?? revTransaction.CreatedAt,
                        Name = revTransaction.Description ?? "Transakcja Revolut",
                        Amount = revTransaction.TotalAmount,
                        Category = "", // Można dodać automatyczne kategoryzowanie
                        SubCategory = "",
                        CheckedTag = revTransaction.CompletedAt.HasValue
                    };

                    // Dodaj do TransactionsViewModel
                    _transactionsViewModel.Transactions.Add(transaction);
                }

                // Zaktualizuj datę ostatniej synchronizacji
                wallet.LastSyncDate = DateTime.Now;
            }
            catch (Exception ex)
            {
                // Logowanie błędu
                System.Diagnostics.Debug.WriteLine($"Błąd synchronizacji dla {wallet.Name}: {ex.Message}");
            }
        }
    }
}
