using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using FinancialManagerApp.Models;
using FinancialManagerApp.Services;

namespace FinancialManagerApp.Views
{
    public partial class AddTransactionView : Window
    {
        private CategoryAssignmentService _categoryService;
        private User _currentUser;
        private bool _isAutoAssigning = false;
        private ObservableCollection<CategoryModel> _categories;
        private ObservableCollection<SubCategoryModel> _subCategories;

        public AddTransactionView(User user = null)
        {
            InitializeComponent();
            _currentUser = user;
            _categoryService = new CategoryAssignmentService();
            _categories = new ObservableCollection<CategoryModel>();
            _subCategories = new ObservableCollection<SubCategoryModel>();
            
            // Dodaj obsługę zmiany typu transakcji i kwoty
            RadioIncome.Checked += (s, e) => UpdateCategoryForIncome();
            RadioExpense.Checked += (s, e) => UpdateCategoryForIncome();
            AmountBox.TextChanged += (s, e) => UpdateCategoryForIncome();
            
            LoadCategories();
        }

        private void LoadCategories()
        {
            try
            {
                var categories = _categoryService.LoadCategoriesFromDatabase();
                _categories.Clear();
                foreach (var category in categories)
                {
                    // Filtruj kategorię "przychód" (id=4) - nie pokazuj jej dla transakcji ujemnych
                    if (category.Id != 4)
                    {
                        _categories.Add(category);
                    }
                }
                
                CategoryComboBox.ItemsSource = _categories;
                
                // Sprawdź czy to przychód i ustaw kategorię automatycznie
                UpdateCategoryForIncome();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Błąd ładowania kategorii: {ex.Message}");
            }
        }

        private void UpdateCategoryForIncome()
        {
            // Jeśli wybrano przychód lub kwota jest dodatnia, ustaw kategorię "przychód" (id=4)
            bool isIncome = RadioIncome.IsChecked == true || TransactionAmount > 0;
            
            if (isIncome)
            {
                // Załaduj kategorię "przychód" z bazy
                var allCategories = _categoryService.LoadCategoriesFromDatabase();
                var incomeCategory = allCategories.FirstOrDefault(c => c.Id == 4);
                
                if (incomeCategory != null)
                {
                    // Dodaj kategorię przychód do listy (tylko dla tego widoku)
                    if (!_categories.Any(c => c.Id == 4))
                    {
                        _categories.Add(incomeCategory);
                    }
                    
                    // Ustaw kategorię przychód
                    CategoryComboBox.SelectedValue = 4;
                    
                    // Zablokuj ComboBoxy kategorii
                    CategoryComboBox.IsEnabled = false;
                    SubCategoryComboBox.IsEnabled = false;
                    
                    // Wyszarz ComboBoxy
                    CategoryComboBox.Opacity = 0.6;
                    SubCategoryComboBox.Opacity = 0.6;
                }
            }
            else
            {
                // Usuń kategorię "przychód" z listy jeśli była dodana
                var incomeCat = _categories.FirstOrDefault(c => c.Id == 4);
                if (incomeCat != null)
                {
                    _categories.Remove(incomeCat);
                }
                
                // Odblokuj ComboBoxy dla wydatków
                CategoryComboBox.IsEnabled = true;
                CategoryComboBox.Opacity = 1.0;
                SubCategoryComboBox.Opacity = 1.0;
            }
        }

        public int SelectedWalletId => (WalletComboBox.SelectedValue != null) ? (int)WalletComboBox.SelectedValue : 0;
        public string TransactionName => TransactionNameBox.Text;
        public DateTime SelectedDate => DateBox.SelectedDate ?? DateTime.Now;
        public decimal TransactionAmount => decimal.TryParse(AmountBox.Text, out decimal val) ? val : 0;

        // NAPRAWA BŁĘDU: Dodanie IsExpense
        public bool IsExpense => RadioExpense.IsChecked == true;

        // Dopasowanie nazw do Twojego nowego modelu
        public int SelectedCategoryId => CategoryComboBox.SelectedValue != null ? (int)CategoryComboBox.SelectedValue : 0;
        public int SelectedSubCategoryId => SubCategoryComboBox.SelectedValue != null ? (int)SubCategoryComboBox.SelectedValue : 0;

        private void TransactionNameBox_LostFocus(object sender, RoutedEventArgs e)
        {
            // Jeśli to przychód, nie przypisuj kategorii automatycznie (już jest ustawiona)
            if (RadioIncome.IsChecked == true || TransactionAmount > 0)
                return;
                
            if (_isAutoAssigning || _currentUser == null || string.IsNullOrWhiteSpace(TransactionNameBox.Text))
                return;

            try
            {
                _isAutoAssigning = true;

                // Utwórz model transakcji do przypisania kategorii
                var transaction = new ImportedTransactionModel
                {
                    Name = TransactionNameBox.Text.Trim(),
                    MerchantName = TransactionNameBox.Text.Trim(),
                    Location = "",
                    OriginalDescription = "",
                    Amount = TransactionAmount // Przekaż kwotę, aby sprawdzić czy to przychód
                };

                // Przypisz kategorię automatycznie
                var (categoryId, subCategoryId) = _categoryService.AssignCategory(transaction, _currentUser.Id);
                
                if (categoryId > 0)
                {
                    // Ustaw kategorię w ComboBox
                    CategoryComboBox.SelectedValue = categoryId;
                    
                    // Subkategorie zostaną załadowane automatycznie w CategoryComboBox_SelectionChanged
                    // Teraz ustaw subkategorię jeśli została przypisana
                    if (subCategoryId.HasValue && subCategoryId.Value > 0)
                    {
                        // Poczekaj chwilę, aby subkategorie się załadowały
                        System.Windows.Threading.Dispatcher.CurrentDispatcher.BeginInvoke(
                            new Action(() =>
                            {
                                SubCategoryComboBox.SelectedValue = subCategoryId.Value;
                            }),
                            System.Windows.Threading.DispatcherPriority.Loaded);
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Błąd automatycznego przypisywania kategorii: {ex.Message}");
            }
            finally
            {
                _isAutoAssigning = false;
            }
        }

        private void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(TransactionName) || TransactionAmount <= 0)
            {
                MessageBox.Show("Podaj poprawną nazwę i kwotę!");
                return;
            }
            
            // Upewnij się, że kategoria jest ustawiona przed zapisem
            // Jeśli to przychód i kategoria nie jest ustawiona, ustaw ją teraz
            if ((RadioIncome.IsChecked == true || TransactionAmount > 0) && SelectedCategoryId == 0)
            {
                UpdateCategoryForIncome();
                
                // Sprawdź ponownie po aktualizacji
                if (SelectedCategoryId == 0)
                {
                    MessageBox.Show("Nie można ustawić kategorii dla przychodu. Sprawdź czy kategoria 'przychód' istnieje w bazie danych.", 
                        "Błąd", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
            }
            
            this.DialogResult = true;
        }

        private void CategoryComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (CategoryComboBox.SelectedItem is CategoryModel selectedCategory)
            {
                // Włącz ComboBox subkategorii
                SubCategoryComboBox.IsEnabled = true;
                
                // Załaduj subkategorie dla wybranej kategorii
                _subCategories.Clear();
                if (selectedCategory.SubCategories != null)
                {
                    foreach (var subCategory in selectedCategory.SubCategories)
                    {
                        _subCategories.Add(subCategory);
                    }
                }
                
                SubCategoryComboBox.ItemsSource = _subCategories;
                SubCategoryComboBox.SelectedIndex = -1;
            }
            else
            {
                SubCategoryComboBox.ItemsSource = null;
                SubCategoryComboBox.SelectedIndex = -1;
                SubCategoryComboBox.IsEnabled = false;
            }
        }

        private void SubCategoryComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // Metoda pomocnicza - można dodać dodatkową logikę jeśli potrzebna
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e) => this.DialogResult = false;
    }
}