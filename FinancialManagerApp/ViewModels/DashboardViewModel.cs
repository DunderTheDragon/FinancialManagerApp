using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using MySql.Data.MySqlClient;
using FinancialManagerApp.Core;
using FinancialManagerApp.Models;
using LiveCharts;
using LiveCharts.Wpf;

namespace FinancialManagerApp.ViewModels
{
    public class DashboardViewModel : ViewModelBase
    {
        private string _connectionString = "Server=localhost; Database=financialmanagerapp; Uid=root; Pwd=;";
        private int _currentUserId;

        // --- ISTNIEJĄCE WŁAŚCIWOŚCI ---
        private decimal _totalBalance;
        public decimal TotalBalance
        {
            get => _totalBalance;
            set { _totalBalance = value; OnPropertyChanged(); }
        }
        public ObservableCollection<object> LastTransactions { get; set; } = new ObservableCollection<object>();

        // --- NOWE WŁAŚCIWOŚCI DLA WYKRESÓW ---
        public SeriesCollection BarSeriesCollection { get; set; } = new SeriesCollection();
        public SeriesCollection PieSeriesCollection { get; set; } = new SeriesCollection();
        public List<string> BarLabels { get; set; } = new List<string>();
        public Func<double, string> YFormatter { get; set; } = value => value.ToString("N2") + " zł";

        // Listy dla ComboBoxów
        public ObservableCollection<WalletModel> AvailableWallets { get; set; } = new ObservableCollection<WalletModel>();

        private WalletModel _selectedWalletForChart;
        public WalletModel SelectedWalletForChart
        {
            get => _selectedWalletForChart;
            set { _selectedWalletForChart = value; OnPropertyChanged(); LoadBarChartData(); }
        }

        private string _selectedCategorizationType = "Kategoryzacja podstawowa";
        public string SelectedCategorizationType
        {
            get => _selectedCategorizationType;
            set { _selectedCategorizationType = value; OnPropertyChanged(); LoadPieChartData(); }
        }

        // Teksty statystyk
        private string _averageSpendingText;
        public string AverageSpendingText { get => _averageSpendingText; set { _averageSpendingText = value; OnPropertyChanged(); } }

        private string _comparisonToPlanText;
        public string ComparisonToPlanText { get => _comparisonToPlanText; set { _comparisonToPlanText = value; OnPropertyChanged(); } }

        public DashboardViewModel(User user)
        {
            if (user != null)
            {
                _currentUserId = user.Id;
                InitializeData();
            }
        }

        private void InitializeData()
        {
            LoadAvailableWallets();
            RefreshData();
            LoadBarChartData();
            LoadPieChartData();
        }

        public void RefreshData()
        {
            CalculateTotalBalance(_currentUserId);
            LoadLastTransactions(_currentUserId);
        }

        // --- LOGIKA PORTFELI (Dla ComboBox) ---
        private void LoadAvailableWallets()
        {
            AvailableWallets.Clear();
            AvailableWallets.Add(new WalletModel { Id = -1, Name = "Wszystkie portfele" });

            using (var conn = new MySqlConnection(_connectionString))
            {
                conn.Open();
                string query = "SELECT id, nazwa FROM portfele WHERE id_uzytkownika = @userId";
                using (var cmd = new MySqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@userId", _currentUserId);
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                            AvailableWallets.Add(new WalletModel { Id = reader.GetInt32("id"), Name = reader.GetString("nazwa") });
                    }
                }
            }
            SelectedWalletForChart = AvailableWallets[0];
        }

        // --- WYKRES SŁUPKOWY (Wydatki wg kategorii - ostatnie miesiące) ---
        private void LoadBarChartData()
        {
            try
            {
                var newBarSeries = new SeriesCollection();
                var newLabels = new List<string>();

                using (var conn = new MySqlConnection(_connectionString))
                {
                    conn.Open();
                    string walletFilter = SelectedWalletForChart?.Id != -1 ? "AND t.id_portfela = @walletId" : "";

                    // DODANO: t.kwota < 0 (filtrujemy tylko wydatki)
                    string query = $@"
                        SELECT k.typ as kategoria_nazwa, MONTH(t.data_transakcji) as miesiac, SUM(ABS(t.kwota)) as suma 
                        FROM transakcje t
                        JOIN kategorie k ON t.id_kategorii = k.id
                        JOIN portfele p ON t.id_portfela = p.id
                        WHERE p.id_uzytkownika = @userId AND t.kwota < 0 
                        {walletFilter}
                        GROUP BY k.typ, MONTH(t.data_transakcji)
                        ORDER BY miesiac ASC";

                    using (var cmd = new MySqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@userId", _currentUserId);
                        if (SelectedWalletForChart?.Id != -1) cmd.Parameters.AddWithValue("@walletId", SelectedWalletForChart.Id);

                        var results = new List<dynamic>();
                        using (var reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                results.Add(new
                                {
                                    Cat = reader.GetString("kategoria_nazwa"), // Pobieramy 'podstawowe', 'osobiste' itd.
                                    Val = reader.GetDouble("suma"),
                                    Month = reader.GetInt32("miesiac")
                                });
                            }
                        }

                        if (!results.Any())
                        {
                            BarSeriesCollection = newBarSeries;
                            BarLabels = newLabels;
                            return;
                        }

                        var monthNames = results.Select(r => (string)System.Globalization.CultureInfo.CurrentCulture.DateTimeFormat.GetMonthName((int)r.Month))
                                               .Distinct().ToList();
                        newLabels.AddRange(monthNames);

                        var categories = results.Select(r => (string)r.Cat).Distinct();
                        foreach (var cat in categories)
                        {
                            newBarSeries.Add(new ColumnSeries
                            {
                                Title = cat,
                                Values = new ChartValues<double>(results.Where(r => (string)r.Cat == cat).Select(r => (double)r.Val))
                            });
                        }
                    }
                }
                BarSeriesCollection = newBarSeries;
                BarLabels = newLabels;
                OnPropertyChanged(nameof(BarSeriesCollection));
                OnPropertyChanged(nameof(BarLabels));
            }
            catch (Exception ex) { System.Diagnostics.Debug.WriteLine(ex.Message); }
        }

        // --- WYKRES KOŁOWY (Udział wydatków + Oszczędności) ---
        private void LoadPieChartData()
        {
            try
            {
                var newPieSeries = new SeriesCollection();
                using (var conn = new MySqlConnection(_connectionString))
                {
                    conn.Open();

                    // 1. LOGIKA WYBORU GRUPOWANIA
                    bool isBasic = SelectedCategorizationType.Contains("Podstawowa");
                    string catLogic = isBasic ? "k.typ" : "s.nazwa";

                    // 2. ZAPYTANIE O WYDATKI (kwota < 0)
                    string query = $@"
                SELECT {catLogic} as grupa, SUM(ABS(t.kwota)) as suma 
                FROM transakcje t 
                JOIN kategorie k ON t.id_kategorii = k.id
                LEFT JOIN subkategorie s ON t.id_subkategorii = s.id
                JOIN portfele p ON t.id_portfela = p.id
                WHERE p.id_uzytkownika = @userId 
                AND t.kwota < 0 
                AND MONTH(t.data_transakcji) = MONTH(NOW())
                AND YEAR(t.data_transakcji) = YEAR(NOW())
                GROUP BY grupa";

                    using (var cmd = new MySqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@userId", _currentUserId);
                        using (var reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                string label = reader.IsDBNull(reader.GetOrdinal("grupa")) ? "Inne" : reader.GetString("grupa");

                                newPieSeries.Add(new PieSeries
                                {
                                    Title = label.ToUpper(),
                                    Values = new ChartValues<double> { reader.GetDouble("suma") },
                                    DataLabels = true,
                                    FontSize = 8,
                                    // ZMIANA: Wyświetlanie procentów zamiast kwoty
                                    LabelPoint = chartPoint => string.Format("{0:P1}", chartPoint.Participation)
                                });
                            }
                        }
                    }

                    // 3. SEKCJA OSZCZĘDNOŚCI / SKARBONEK
                    if (isBasic)
                    {
                        string savingsQuery = @"
                    SELECT SUM(t.kwota) FROM transakcje t
                    JOIN portfele p ON t.id_portfela = p.id
                    WHERE p.id_uzytkownika = @userId 
                    AND MONTH(t.data_transakcji) = MONTH(NOW())";

                        using (var cmd = new MySqlCommand(savingsQuery, conn))
                        {
                            cmd.Parameters.AddWithValue("@userId", _currentUserId);
                            var res = cmd.ExecuteScalar();
                            double balance = (res != DBNull.Value) ? Convert.ToDouble(res) : 0;

                            if (balance > 0)
                            {
                                newPieSeries.Add(new PieSeries
                                {
                                    Title = "OSZCZĘDNOŚCI",
                                    Values = new ChartValues<double> { balance },
                                    Fill = System.Windows.Media.Brushes.MediumSeaGreen,
                                    DataLabels = true,
                                    FontSize = 8,
                                    // ZMIANA: Ujednolicenie formatu na procentowy (P1)
                                    LabelPoint = chartPoint => string.Format("{0:P1}", chartPoint.Participation)
                                });
                            }
                        }
                    }
                    else
                    {
                        string goalsQuery = @"
                    SELECT t.nazwa as cel, SUM(ABS(t.kwota)) as suma 
                    FROM transakcje t
                    JOIN portfele p ON t.id_portfela = p.id
                    WHERE p.id_uzytkownika = @userId 
                    AND t.id_subkategorii = 5
                    AND MONTH(t.data_transakcji) = MONTH(NOW())
                    GROUP BY t.nazwa";

                        using (var cmd = new MySqlCommand(goalsQuery, conn))
                        {
                            cmd.Parameters.AddWithValue("@userId", _currentUserId);
                            using (var reader = cmd.ExecuteReader())
                            {
                                while (reader.Read())
                                {
                                    newPieSeries.Add(new PieSeries
                                    {
                                        Title = "SKARBONKA: " + reader.GetString("cel").ToUpper(),
                                        Values = new ChartValues<double> { reader.GetDouble("suma") },
                                        DataLabels = true,
                                        FontSize = 8,
                                        // ZMIANA: Ujednolicenie formatu na procentowy (P1)
                                        LabelPoint = chartPoint => string.Format("{0:P1}", chartPoint.Participation)
                                    });
                                }
                            }
                        }
                    }
                }

                PieSeriesCollection = newPieSeries;
                OnPropertyChanged(nameof(PieSeriesCollection));
                UpdateComparisonStats();
            }
            catch (Exception ex) { System.Diagnostics.Debug.WriteLine("Błąd LoadPieChartData: " + ex.Message); }
        }

        private void UpdateComparisonStats()
        {
            // Przykładowa logika obliczeń porównawczych (można rozbudować o SQL)
            ComparisonToPlanText = "Zaoszczędzono: +150,00 zł vs cel";
            AverageSpendingText = "Średnia z 12 m-cy: 2 450,00 zł";
        }

        private void CalculateTotalBalance(int userId)
        {
            try
            {
                using (MySqlConnection conn = new MySqlConnection(_connectionString))
                {
                    conn.Open();
                    // Sumuje saldo wszystkich aktywnych kont przypisanych do użytkownika 
                    string query = "SELECT SUM(saldo) FROM portfele WHERE id_uzytkownika = @userId";

                    using (MySqlCommand cmd = new MySqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@userId", userId);
                        var result = cmd.ExecuteScalar();
                        TotalBalance = result != DBNull.Value ? Convert.ToDecimal(result) : 0;
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Błąd salda: " + ex.Message);
            }
        }

        private void LoadLastTransactions(int userId)
        {
            try
            {
                using (MySqlConnection conn = new MySqlConnection(_connectionString))
                {
                    conn.Open();
                    // Pobiera 5 ostatnich transakcji ze wszystkich portfeli użytkownika 
                    // Łączymy z tabelą portfele, aby pobrać nazwę źródła 
                    string query = @"
                        SELECT t.nazwa, t.kwota, t.data_transakcji, p.nazwa as portfel_nazwa 
                        FROM transakcje t
                        JOIN portfele p ON t.id_portfela = p.id
                        WHERE p.id_uzytkownika = @userId
                        ORDER BY t.data_transakcji DESC
                        LIMIT 5";

                    using (MySqlCommand cmd = new MySqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@userId", userId);
                        using (var reader = cmd.ExecuteReader())
                        {
                            LastTransactions.Clear();
                            while (reader.Read())
                            {
                                LastTransactions.Add(new
                                {
                                    Title = reader.GetString("nazwa"),
                                    Amount = reader.GetDecimal("kwota"),
                                    Date = reader.GetDateTime("data_transakcji"),
                                    Wallet = reader.GetString("portfel_nazwa")
                                });
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Błąd transakcji: " + ex.Message);
            }
        }
    }
}