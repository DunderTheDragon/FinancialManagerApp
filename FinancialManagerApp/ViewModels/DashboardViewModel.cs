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

        // --- NOWE WŁAŚCIWOŚCI DLA WYBORU MIESIĄCA ---
        public ObservableCollection<DateTime> AvailableMonths { get; set; } = new ObservableCollection<DateTime>();

        private DateTime _selectedMonth;
        public DateTime SelectedMonth
        {
            get => _selectedMonth;
            set
            {
                _selectedMonth = value;
                OnPropertyChanged();
                // Każda zmiana miesiąca odświeża wykres kołowy
                LoadPieChartData();
            }
        }

        // --- ISTNIEJĄCE WŁAŚCIWOŚCI ---
        private decimal _totalBalance;
        public decimal TotalBalance
        {
            get => _totalBalance;
            set { _totalBalance = value; OnPropertyChanged(); }
        }

        public ObservableCollection<object> LastTransactions { get; set; } = new ObservableCollection<object>();
        public SeriesCollection BarSeriesCollection { get; set; } = new SeriesCollection();
        public SeriesCollection PieSeriesCollection { get; set; } = new SeriesCollection();
        public List<string> BarLabels { get; set; } = new List<string>();
        public Func<double, string> YFormatter { get; set; } = value => value.ToString("N2") + " zł";
        public ObservableCollection<WalletModel> AvailableWallets { get; set; } = new ObservableCollection<WalletModel>();

        private WalletModel _selectedWalletForChart;
        public WalletModel SelectedWalletForChart
        {
            get => _selectedWalletForChart;
            set
            {
                _selectedWalletForChart = value;
                OnPropertyChanged();
                LoadBarChartData();
            }
        }

        private string _selectedCategorizationType = "Kategoryzacja podstawowa";
        public string SelectedCategorizationType
        {
            get => _selectedCategorizationType;
            set
            {
                _selectedCategorizationType = value;
                OnPropertyChanged();
                LoadPieChartData();
            }
        }

        private string _averageSpendingText;
        public string AverageSpendingText { get => _averageSpendingText; set { _averageSpendingText = value; OnPropertyChanged(); } }

        private string _comparisonToPlanText;
        public string ComparisonToPlanText { get => _comparisonToPlanText; set { _comparisonToPlanText = value; OnPropertyChanged(); } }

        // --- KONSTRUKTOR ---
        public DashboardViewModel(User user)
        {
            if (user != null)
            {
                _currentUserId = user.Id;

                // 1. Najpierw przygotuj listę miesięcy
                InitializeMonthPicker();

                // 2. Potem ładuj resztę danych
                LoadAvailableWallets();
                RefreshData();
                LoadPieChartData();
            }
        }

        private void InitializeMonthPicker()
        {
            AvailableMonths.Clear();
            var start = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);

            // Dodaj ostatnie 12 miesięcy do listy
            for (int i = 0; i < 12; i++)
            {
                AvailableMonths.Add(start.AddMonths(-i));
            }

            // Ustaw domyślnie bieżący miesiąc bez wyzwalania LoadPieChartData dwa razy
            _selectedMonth = AvailableMonths[0];
            OnPropertyChanged(nameof(SelectedMonth));
        }

        public void RefreshData()
        {
            CalculateTotalBalance(_currentUserId);
            LoadLastTransactions(_currentUserId);
        }

        // --- ZMODYFIKOWANA METODA WYKRESU KOŁOWEGO ---
        private void LoadPieChartData()
        {
            try
            {
                var newPieSeries = new SeriesCollection();
                Func<ChartPoint, string> labelOnChart = chartPoint => string.Format("{0:P0}", chartPoint.Participation);

                int m = SelectedMonth.Month;
                int y = SelectedMonth.Year;

                using (var conn = new MySqlConnection(_connectionString))
                {
                    conn.Open();
                    bool isBasic = string.IsNullOrWhiteSpace(SelectedCategorizationType) ||
                                   SelectedCategorizationType.IndexOf("podstawowa", StringComparison.OrdinalIgnoreCase) >= 0;

                    // 1. WYDATKI (Używamy parametrów @month i @year)
                    string catLogic = isBasic ? "k.typ" : "CONCAT(UPPER(k.typ), ' - ', COALESCE(UPPER(s.nazwa), 'INNE'))";
                    string queryExpenses = $@"
                        SELECT {catLogic} as grupa, SUM(ABS(t.kwota)) as suma 
                        FROM transakcje t 
                        JOIN kategorie k ON t.id_kategorii = k.id
                        LEFT JOIN subkategorie s ON t.id_subkategorii = s.id
                        JOIN portfele p ON t.id_portfela = p.id
                        WHERE p.id_uzytkownika = @userId 
                        AND t.kwota < 0 AND k.id IN (1, 2)
                        AND MONTH(t.data_transakcji) = @month AND YEAR(t.data_transakcji) = @year
                        GROUP BY grupa";

                    using (var cmd = new MySqlCommand(queryExpenses, conn))
                    {
                        cmd.Parameters.AddWithValue("@userId", _currentUserId);
                        cmd.Parameters.AddWithValue("@month", m);
                        cmd.Parameters.AddWithValue("@year", y);
                        using (var reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                var slice = CreatePieSlice(reader.GetString("grupa"), Convert.ToDouble(reader["suma"]));
                                slice.LabelPoint = labelOnChart;
                                slice.DataLabels = true;
                                newPieSeries.Add(slice);
                            }
                        }
                    }

                    // --- SEKCJA OSZCZĘDNOŚCI ---
                    double sumaSkarbonki = GetSumFromDb(conn, 3, m, y);
                    double balance = GetCurrentBalance(conn, m, y);

                    if (isBasic)
                    {
                        double sumaTotal = sumaSkarbonki + balance;
                        if (sumaTotal > 0)
                        {
                            var slice = CreatePieSlice("OSZCZĘDNOŚCI", sumaTotal, System.Windows.Media.Brushes.Gold);
                            slice.LabelPoint = labelOnChart;
                            slice.DataLabels = true;
                            newPieSeries.Add(slice);
                        }
                    }
                    else
                    {
                        if (sumaSkarbonki > 0)
                        {
                            var slice = CreatePieSlice("OSZCZĘDNOŚCI - SKARBONKI", sumaSkarbonki, System.Windows.Media.Brushes.Gold);
                            slice.LabelPoint = labelOnChart;
                            slice.DataLabels = true;
                            newPieSeries.Add(slice);
                        }
                        if (balance > 0)
                        {
                            var slice = CreatePieSlice("OSZCZĘDNOŚCI - POZOSTAŁO", balance, System.Windows.Media.Brushes.MediumSeaGreen);
                            slice.LabelPoint = labelOnChart;
                            slice.DataLabels = true;
                            newPieSeries.Add(slice);
                        }
                    }
                }

                if (newPieSeries.Count == 0)
                {
                    newPieSeries.Add(new PieSeries { Title = "BRAK DANYCH", Values = new ChartValues<double> { 1e-10 }, Opacity = 0 });
                }

                PieSeriesCollection = newPieSeries;
                OnPropertyChanged(nameof(PieSeriesCollection));
                UpdateComparisonStats();
            }
            catch (Exception ex) { System.Diagnostics.Debug.WriteLine("Błąd PieChart: " + ex.Message); }
        }

        // --- ZAKTUALIZOWANE METODY POMOCNICZE Z PARAMETRAMI DATY ---
        private double GetSumFromDb(MySqlConnection conn, int catId, int month, int year)
        {
            string sql = $@"SELECT SUM(ABS(t.kwota)) FROM transakcje t JOIN portfele p ON t.id_portfela = p.id 
                    WHERE p.id_uzytkownika = @userId AND t.id_kategorii = @catId
                    AND MONTH(t.data_transakcji) = @month AND YEAR(t.data_transakcji) = @year";
            using (var cmd = new MySqlCommand(sql, conn))
            {
                cmd.Parameters.AddWithValue("@userId", _currentUserId);
                cmd.Parameters.AddWithValue("@catId", catId);
                cmd.Parameters.AddWithValue("@month", month);
                cmd.Parameters.AddWithValue("@year", year);
                var res = cmd.ExecuteScalar();
                return (res != DBNull.Value) ? Convert.ToDouble(res) : 0;
            }
        }

        private double GetCurrentBalance(MySqlConnection conn, int month, int year)
        {
            string sql = @"SELECT SUM(t.kwota) FROM transakcje t JOIN portfele p ON t.id_portfela = p.id 
                    WHERE p.id_uzytkownika = @userId 
                    AND MONTH(t.data_transakcji) = @month AND YEAR(t.data_transakcji) = @year";
            using (var cmd = new MySqlCommand(sql, conn))
            {
                cmd.Parameters.AddWithValue("@userId", _currentUserId);
                cmd.Parameters.AddWithValue("@month", month);
                cmd.Parameters.AddWithValue("@year", year);
                var res = cmd.ExecuteScalar();
                double val = (res != DBNull.Value) ? Convert.ToDouble(res) : 0;
                return val > 0 ? val : 0;
            }
        }

        private PieSeries CreatePieSlice(string title, double value, System.Windows.Media.Brush fill = null)
        {
            var series = new PieSeries
            {
                Title = title.ToUpper(),
                Values = new ChartValues<double> { value },
                DataLabels = true,
                FontSize = 9
            };
            if (fill != null) series.Fill = fill;
            return series;
        }

        // Pozostałe metody (LoadAvailableWallets, LoadBarChartData, LoadLastTransactions itd.) bez zmian...
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
                                    Cat = reader.GetString("kategoria_nazwa"),
                                    Val = reader.GetDouble("suma"),
                                    Month = reader.GetInt32("miesiac")
                                });
                            }
                        }

                        if (!results.Any())
                        {
                            BarSeriesCollection = newBarSeries;
                            BarLabels = newLabels;
                            OnPropertyChanged(nameof(BarSeriesCollection));
                            OnPropertyChanged(nameof(BarLabels));
                            return;
                        }

                        newLabels = results.Select(r => (string)System.Globalization.CultureInfo.CurrentCulture.DateTimeFormat.GetMonthName((int)r.Month))
                                          .Distinct().ToList();

                        var categories = results.Select(r => (string)r.Cat).Distinct();
                        foreach (var cat in categories)
                        {
                            newBarSeries.Add(new ColumnSeries
                            {
                                Title = cat.ToUpper(),
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

        private void LoadLastTransactions(int userId)
        {
            try
            {
                using (var conn = new MySqlConnection(_connectionString))
                {
                    conn.Open();
                    string query = @"
                        SELECT t.nazwa, t.kwota, t.data_transakcji, p.nazwa as portfel_nazwa 
                        FROM transakcje t
                        JOIN portfele p ON t.id_portfela = p.id
                        WHERE p.id_uzytkownika = @userId
                        ORDER BY t.data_transakcji DESC LIMIT 5";

                    using (var cmd = new MySqlCommand(query, conn))
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
            catch (Exception ex) { System.Diagnostics.Debug.WriteLine(ex.Message); }
        }

        private void UpdateComparisonStats()
        {
            ComparisonToPlanText = "Zaoszczędzono: +150,00 zł vs cel";
            AverageSpendingText = "Średnia z 12 m-cy: 2 450,00 zł";
        }

        private void CalculateTotalBalance(int userId)
        {
            try
            {
                using (var conn = new MySqlConnection(_connectionString))
                {
                    conn.Open();
                    string query = "SELECT SUM(saldo) FROM portfele WHERE id_uzytkownika = @userId";
                    using (var cmd = new MySqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@userId", userId);
                        var result = cmd.ExecuteScalar();
                        TotalBalance = result != DBNull.Value ? Convert.ToDecimal(result) : 0;
                    }
                }
            }
            catch (Exception ex) { System.Diagnostics.Debug.WriteLine(ex.Message); }
        }
    }
}