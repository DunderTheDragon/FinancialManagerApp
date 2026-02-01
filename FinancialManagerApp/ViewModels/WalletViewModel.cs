using FinancialManagerApp.Core;
using FinancialManagerApp.Models;
using FinancialManagerApp.Services;
using FinancialManagerApp.Views;
using MySql.Data.MySqlClient;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;

namespace FinancialManagerApp.ViewModels
{
    public class WalletsViewModel : ViewModelBase
    {
        private string _connectionString = "Server=localhost; Database=financialmanagerapp; Uid=root; Pwd=;";

        public User CurrentUser { get; set; } // Przechowujemy zalogowanego użytkownika
        public ObservableCollection<WalletModel> Wallets { get; set; }
        public ICommand OpenAddWalletCommand { get; }
        public ICommand RefreshWalletCommand { get; }

        private readonly RevolutService _revolutService;
        private readonly TransactionSyncService _syncService;
        private DispatcherTimer _syncTimer;

        public WalletsViewModel(User user, TransactionsViewModel transactionsViewModel = null)
        {
            CurrentUser = user;
            Wallets = new ObservableCollection<WalletModel>();
            _revolutService = new RevolutService();

            var transactionsVM = transactionsViewModel ?? new TransactionsViewModel(CurrentUser);
            _syncService = new TransactionSyncService(_revolutService, transactionsVM);

            OpenAddWalletCommand = new RelayCommand(ExecuteOpenAddWallet);
            RefreshWalletCommand = new RelayCommand(ExecuteRefreshWallet);

            // Ładujemy portfele użytkownika zaraz po stworzeniu ViewModelu
            LoadWalletsFromDb();
            StartAutoSync();
        }

        public void RefreshData()
        {
            LoadWalletsFromDb();
        }

        // Metoda pobierająca portfele z bazy danych
        private void LoadWalletsFromDb()
        {
            try
            {
                using (MySqlConnection conn = new MySqlConnection(_connectionString))
                {
                    conn.Open();
                    string query = "SELECT * FROM portfele WHERE id_uzytkownika = @userId";

                    using (MySqlCommand cmd = new MySqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@userId", CurrentUser.Id);
                        using (var reader = cmd.ExecuteReader())
                        {
                            Wallets.Clear();
                            while (reader.Read())
                            {
                                Wallets.Add(new WalletModel
                                {
                                    // Pamiętaj o dodaniu Id do WalletModel, jeśli go jeszcze nie ma
                                    Name = reader.GetString("nazwa"),
                                    Type = reader.GetString("typ"),
                                    Description = reader.IsDBNull(reader.GetOrdinal("opis")) ? "" : reader.GetString("opis"),
                                    Balance = reader.GetDecimal("saldo")
                                });
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Błąd podczas ładowania portfeli: " + ex.Message);
            }
        }

        private async void ExecuteOpenAddWallet(object obj)
        {
            var addWalletWindow = new AddWalletView();

            // Wyświetl okno i czekaj na wynik
            bool? result = addWalletWindow.ShowDialog();

            if (result == true)
            {
                // Użytkownik kliknął "Utwórz"

                if (addWalletWindow.IsApi) // Wybrano REVOLUT
                {
                    try
                    {
                        var clientId = addWalletWindow.ApiClientId;
                        var privateKey = addWalletWindow.ApiKey;

                        // Wywołanie serwisu (API CALL)
                        var accounts = await _revolutService.GetAccountsAsync(clientId, privateKey);

                        // Dodaj znalezione konta do listy
                        foreach (var acc in accounts)
                        {
                            Wallets.Add(new WalletModel
                            {
                                Name = acc.Name ?? "Konto Revolut",
                                Type = "API",
                                Description = $"Waluta: {acc.Currency}",
                                Balance = acc.Balance,
                                RevolutClientId = clientId,        // ZAPISZ
                                RevolutPrivateKey = privateKey,    // ZAPISZ
                                RevolutAccountId = acc.Id          // ZAPISZ
                            });
                        }

                        MessageBox.Show($"Pobrano {accounts.Count} kont z Revolut!", "Sukces");
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Błąd połączenia z Revolut: {ex.Message}", "Błąd");
                    }
                }
                else // Wybrano MANUALNY
                {
                    if (decimal.TryParse(addWalletWindow.InitialBalance, out decimal balance))
                    {
                        var newWallet = new WalletModel
                        {
                            Name = addWalletWindow.WalletName,
                            Type = "Manualny",
                            Description = "Portfel lokalny",
                            Balance = balance
                        };

                        // ZAPIS DO BAZY
                        if (SaveWalletToDb(newWallet))
                        {
                            Wallets.Add(newWallet);
                            MessageBox.Show("Portfel dodany pomyślnie!");
                        }
                    }
                }
            }
        }
        private bool SaveWalletToDb(WalletModel wallet)
        {
            try
            {
                using (MySqlConnection conn = new MySqlConnection(_connectionString))
                {
                    conn.Open();
                    string query = "INSERT INTO portfele (id_uzytkownika, nazwa, typ, opis, saldo) " +
                                   "VALUES (@userId, @name, @type, @desc, @balance)";

                    using (MySqlCommand cmd = new MySqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@userId", CurrentUser.Id);
                        cmd.Parameters.AddWithValue("@name", wallet.Name);
                        cmd.Parameters.AddWithValue("@type", wallet.Type);
                        cmd.Parameters.AddWithValue("@desc", wallet.Description);
                        cmd.Parameters.AddWithValue("@balance", wallet.Balance);

                        cmd.ExecuteNonQuery();
                        return true;
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Błąd zapisu portfela: " + ex.Message);
                return false;
            }
        }

        // Nowa metoda do ręcznego odświeżania:
        private async void ExecuteRefreshWallet(object obj)
        {
            if (obj is WalletModel wallet && wallet.Type == "API")
            {
                try
                {
                    await _syncService.SyncWalletTransactionsAsync(wallet);
                    MessageBox.Show($"Zsynchronizowano transakcje dla {wallet.Name}", "Sukces");
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Błąd synchronizacji: {ex.Message}", "Błąd");
                }
            }
        }

        // Automatyczna synchronizacja:
        private void StartAutoSync()
        {
            _syncTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMinutes(15) // Synchronizacja co 15 minut
            };
            _syncTimer.Tick += async (s, e) => await SyncAllWallets();
            _syncTimer.Start();
        }

        private async Task SyncAllWallets()
        {
            var apiWallets = Wallets.Where(w => w.Type == "API").ToList();
            foreach (var wallet in apiWallets)
            {
                try
                {
                    await _syncService.SyncWalletTransactionsAsync(wallet);
                }
                catch (Exception ex)
                {
                    // Logowanie błędu bez przerywania synchronizacji innych portfeli
                    System.Diagnostics.Debug.WriteLine($"Błąd synchronizacji dla {wallet.Name}: {ex.Message}");
                }
            }
        }
    }
}