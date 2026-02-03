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
using System.Collections.Generic;

namespace FinancialManagerApp.ViewModels
{
    public class WalletsViewModel : ViewModelBase
    {
        private string _connectionString = "Server=localhost; Database=financialmanagerapp; Uid=root; Pwd=;";

        public User CurrentUser { get; set; } // Przechowujemy zalogowanego użytkownika
        public ObservableCollection<WalletModel> Wallets { get; set; }
        public ICommand OpenAddWalletCommand { get; }
        public ICommand RefreshWalletCommand { get; }
        public ICommand OpenEditWalletCommand { get; }
        public ICommand DeleteWalletCommand { get; }
        public ICommand ImportTransactionsCommand { get; }

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
            OpenEditWalletCommand = new RelayCommand(ExecuteOpenEditWallet);
            DeleteWalletCommand = new RelayCommand(ExecuteDeleteWallet);
            ImportTransactionsCommand = new RelayCommand(ExecuteImportTransactions);

            // Ładujemy portfele użytkownika zaraz po stworzeniu ViewModelu
            LoadWalletsFromDb();
            StartAutoSync();
        }

        public void RefreshData()
        {
            LoadWalletsFromDb();
        }

        private void ExecuteOpenEditWallet(object obj)
        {
            if (obj is WalletModel selectedWallet)
            {
                // Tworzymy okno i przekazujemy mu zaznaczony portfel
                var editWindow = new EditWalletView(selectedWallet);

                if (editWindow.ShowDialog() == true)
                {
                    // Pobieramy zmodyfikowane dane z okna
                    var updatedWallet = editWindow.EditedWallet;

                    if (UpdateWalletInDatabase(updatedWallet))
                    {
                        LoadWalletsFromDb(); // Odświeżamy listę z bazy po udanej edycji
                        MessageBox.Show("Zmiany zostały zapisane pomyślnie!", "Sukces");
                    }
                }
            }
        }

        // Metoda zapisu zmian w bazie MySQL
        private bool UpdateWalletInDatabase(WalletModel wallet)
        {
            try
            {
                using (var conn = new MySqlConnection(_connectionString))
                {
                    conn.Open();
                    string query = "UPDATE portfele SET nazwa = @name, opis = @desc, saldo = @balance WHERE id = @id";

                    using (var cmd = new MySqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@name", wallet.Name);
                        cmd.Parameters.AddWithValue("@desc", wallet.Description);
                        cmd.Parameters.AddWithValue("@balance", wallet.Balance);
                        cmd.Parameters.AddWithValue("@id", wallet.Id);

                        cmd.ExecuteNonQuery();
                        return true;
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Błąd aktualizacji bazy danych: " + ex.Message, "Błąd");
                return false;
            }
        }

        // Usuwanie portfelu
        private void ExecuteDeleteWallet(object obj)
        {
            if (obj is WalletModel selectedWallet)
            {
                // Wyświetlamy ostrzeżenie przed skasowaniem danych
                var result = MessageBox.Show(
                    $"Czy na pewno chcesz bezpowrotnie usunąć portfel '{selectedWallet.Name}'? \n\n" +
                    "UWAGA: Spowoduje to również usunięcie całej historii transakcji dla tego portfela!",
                    "Potwierdzenie usunięcia",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning);

                if (result == MessageBoxResult.Yes)
                {
                    if (DeleteWalletFromDb(selectedWallet.Id))
                    {
                        // Usuwamy z widoku tylko jeśli baza danych potwierdziła sukces
                        Wallets.Remove(selectedWallet);
                        MessageBox.Show("Portfel został pomyślnie usunięty.");
                    }
                }
            }
        }

        private bool DeleteWalletFromDb(int walletId)
        {
            try
            {
                using (var conn = new MySqlConnection(_connectionString))
                {
                    conn.Open();
                    // Zapytanie SQL usuwające konkretny rekord po ID
                    string query = "DELETE FROM portfele WHERE id = @id";
                    using (var cmd = new MySqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@id", walletId);
                        cmd.ExecuteNonQuery();
                        return true;
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Błąd podczas usuwania portfela z bazy: " + ex.Message, "Błąd");
                return false;
            }
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
                                var walletType = reader.GetString("typ");
                                // Normalizuj typ z bazy (może być "manualny" lub "api") do formatu "Manualny" lub "API"
                                if (walletType.Equals("manualny", StringComparison.OrdinalIgnoreCase))
                                    walletType = "Manualny";
                                else if (walletType.Equals("api", StringComparison.OrdinalIgnoreCase))
                                    walletType = "API";

                                Wallets.Add(new WalletModel
                                {
                                    // Pamiętaj o dodaniu Id do WalletModel, jeśli go jeszcze nie ma
                                    Id = reader.GetInt32("id"),
                                    Name = reader.GetString("nazwa"),
                                    Type = walletType,
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

            if (addWalletWindow.ShowDialog() == true)
            {
                // Pobieramy opis wpisany przez użytkownika
                string userDescription = addWalletWindow.WalletDescription;

                if (addWalletWindow.IsApi) // Wybrano REVOLUT
                {
                    try
                    {
                        var clientId = addWalletWindow.ApiClientId;
                        var privateKey = addWalletWindow.ApiKey;
                        var refreshToken = addWalletWindow.RefreshToken;

                        var accounts = await _revolutService.GetAccountsAsync(clientId, privateKey, refreshToken);

                        foreach (var acc in accounts)
                        {
                            var apiWallet = new WalletModel
                            {
                                Name = acc.Name,
                                Type = "API",
                                Description = userDescription, // Ustawiamy opis wpisany w oknie
                                Balance = acc.Balance,
                                RevolutClientId = clientId,
                                RevolutPrivateKey = privateKey,
                                RevolutRefreshToken = refreshToken,
                                RevolutAccountId = acc.Id
                            };

                            // Zapisujemy każde konto API do bazy, aby opis trafił do MySQL
                            if (SaveWalletToDb(apiWallet))
                            {
                                Wallets.Add(apiWallet);
                            }
                        }

                        MessageBox.Show($"Pobrano i zapisano {accounts.Count} kont z Revolut!", "Sukces");
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
                            Description = userDescription, // Pobieramy opis z okna zamiast "Portfel lokalny"
                            Balance = balance
                        };

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

        /// <summary>
        /// Otwiera dialog importu transakcji z pliku CSV
        /// </summary>
        private void ExecuteImportTransactions(object obj)
        {
            if (obj is not WalletModel wallet || wallet.Type != "Manualny")
            {
                MessageBox.Show("Import transakcji jest dostępny tylko dla portfeli manualnych.", "Informacja", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            try
            {
                // Otwórz dialog wyboru pliku CSV
                var openFileDialog = new Microsoft.Win32.OpenFileDialog
                {
                    Filter = "Pliki CSV (*.csv)|*.csv|Wszystkie pliki (*.*)|*.*",
                    Title = "Wybierz plik CSV do importu"
                };

                if (openFileDialog.ShowDialog() == true)
                {
                    // Parsuj plik CSV
                    var csvService = new Services.CsvImportService();
                    var importedTransactions = csvService.ParseCsvFile(openFileDialog.FileName);

                    if (importedTransactions.Count == 0)
                    {
                        MessageBox.Show("Nie znaleziono transakcji w pliku CSV.", "Informacja", MessageBoxButton.OK, MessageBoxImage.Information);
                        return;
                    }

                    // Konwertuj na ObservableCollection
                    var transactionsCollection = new System.Collections.ObjectModel.ObservableCollection<Models.ImportedTransactionModel>(importedTransactions);

                    // Utwórz ViewModel i okno importu
                    var importViewModel = new ViewModels.ImportTransactionsViewModel(CurrentUser, wallet.Id, transactionsCollection);
                    var importWindow = new Views.ImportTransactionsView(importViewModel);
                    importWindow.Owner = Application.Current.MainWindow;

                    // Otwórz okno
                    if (importWindow.ShowDialog() == true)
                    {
                        // Odśwież listę portfeli (saldo mogło się zmienić)
                        LoadWalletsFromDb();
                        MessageBox.Show("Transakcje zostały pomyślnie zaimportowane!", "Sukces", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Błąd importu transakcji: {ex.Message}", "Błąd", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}