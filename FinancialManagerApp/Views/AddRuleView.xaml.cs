using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using FinancialManagerApp.Models;
using FinancialManagerApp.Services;
using FinancialManagerApp.ViewModels;

namespace FinancialManagerApp.Views
{
    public partial class AddRuleView : Window
    {
        private readonly CategoryAssignmentService _categoryService;
        private readonly User _currentUser;
        private readonly SettingsViewModel _parentViewModel;

        public string Phrase { get; set; }
        public CategoryModel SelectedCategory { get; set; }
        public SubCategoryModel SelectedSubCategory { get; set; }
        public ObservableCollection<CategoryModel> Categories { get; set; }
        public ObservableCollection<SubCategoryModel> SubCategories { get; set; }

        public AddRuleView(User user, SettingsViewModel parentViewModel)
        {
            InitializeComponent();
            _categoryService = new CategoryAssignmentService();
            _currentUser = user;
            _parentViewModel = parentViewModel;

            Categories = new ObservableCollection<CategoryModel>();
            SubCategories = new ObservableCollection<SubCategoryModel>();

            DataContext = this;
            LoadCategories();
        }

        private void LoadCategories()
        {
            try
            {
                var categories = _categoryService.LoadCategoriesFromDatabase();
                Categories.Clear();
                foreach (var category in categories)
                {
                    Categories.Add(category);
                    // Debug: sprawdź czy subkategorie są załadowane
                    System.Diagnostics.Debug.WriteLine($"Kategoria: {category.Type}, Subkategorie: {category.SubCategories?.Count ?? 0}");
                    if (category.SubCategories != null && category.SubCategories.Count > 0)
                    {
                        foreach (var sub in category.SubCategories)
                        {
                            System.Diagnostics.Debug.WriteLine($"  - Subkategoria: {sub.Name} (Id: {sub.Id})");
                        }
                    }
                }

                CategoryComboBox.ItemsSource = Categories;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Błąd ładowania kategorii: {ex.Message}", "Błąd", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void CategoryComboBox_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (CategoryComboBox.SelectedItem is CategoryModel selectedCategory)
            {
                SelectedCategory = selectedCategory;
                
                // Pobierz kategorię z kolekcji Categories (tak jak w ImportTransactionsView)
                var category = Categories.FirstOrDefault(c => c.Id == selectedCategory.Id);
                
                if (category != null)
                {
                    // Włącz ComboBox subkategorii
                    SubCategoryComboBox.IsEnabled = true;
                    SubCategoryInfoText.Visibility = System.Windows.Visibility.Collapsed;
                    
                    // Pobierz subkategorie dla wybranej kategorii (tak jak w ImportTransactionsView.GetSubCategoriesForCategory)
                    var subCategories = category.SubCategories ?? new ObservableCollection<SubCategoryModel>();
                    
                    if (subCategories.Count > 0)
                    {
                        // Ustaw ItemsSource bezpośrednio z SubCategories kategorii
                        SubCategoryComboBox.ItemsSource = subCategories;
                        SubCategories = subCategories;
                        
                        // Wyczyść wybór
                        SubCategoryComboBox.SelectedIndex = -1;
                        SubCategoryComboBox.SelectedValue = null;
                        SelectedSubCategory = null;
                        
                        System.Diagnostics.Debug.WriteLine($"Załadowano {subCategories.Count} subkategorii dla kategorii {category.Type}");
                    }
                    else
                    {
                        // Jeśli kategoria nie ma subkategorii
                        SubCategoryComboBox.ItemsSource = null;
                        SubCategories.Clear();
                        SubCategoryComboBox.SelectedIndex = -1;
                        SubCategoryComboBox.SelectedValue = null;
                        SelectedSubCategory = null;
                        SubCategoryComboBox.IsEnabled = false;
                        
                        MessageBox.Show($"Kategoria '{category.Type}' nie ma dostępnych subkategorii. Wybierz inną kategorię.", 
                            "Brak subkategorii", MessageBoxButton.OK, MessageBoxImage.Information);
                        
                        // Wyczyść wybór kategorii
                        CategoryComboBox.SelectedIndex = -1;
                        SelectedCategory = null;
                    }
                }
            }
            else
            {
                SubCategoryComboBox.ItemsSource = null;
                SubCategories.Clear();
                SubCategoryComboBox.SelectedIndex = -1;
                SubCategoryComboBox.SelectedValue = null;
                SubCategoryComboBox.IsEnabled = false;
                SubCategoryInfoText.Visibility = System.Windows.Visibility.Visible;
                SelectedSubCategory = null;
            }
        }

        private void AddButton_Click(object sender, RoutedEventArgs e)
        {
            // Walidacja
            if (string.IsNullOrWhiteSpace(PhraseTextBox.Text))
            {
                MessageBox.Show("Fraza nie może być pusta!", "Błąd walidacji", 
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (SelectedCategory == null)
            {
                MessageBox.Show("Wybierz kategorię!", "Błąd walidacji", 
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (SelectedSubCategory == null || SelectedSubCategory.Id <= 0)
            {
                MessageBox.Show("Wybierz subkategorię!", "Błąd walidacji", 
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                // Zapisz regułę do bazy danych (subkategoria jest wymagana)
                bool success = _categoryService.SaveUserRule(
                    _currentUser.Id,
                    PhraseTextBox.Text.Trim(),
                    SelectedCategory.Id,
                    SelectedSubCategory.Id
                );

                if (success)
                {
                    // Odśwież listę reguł w SettingsViewModel
                    _parentViewModel?.LoadUserRulesFromDatabase();
                    DialogResult = true;
                    Close();
                }
                else
                {
                    MessageBox.Show("Błąd zapisu reguły do bazy danych.", "Błąd", 
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Błąd zapisu reguły: {ex.Message}", "Błąd", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void SubCategoryComboBox_Loaded(object sender, RoutedEventArgs e)
        {
            // Metoda pomocnicza do inicjalizacji ComboBox (podobnie jak w ImportTransactionsView)
            if (SubCategoryComboBox.SelectedValue != null)
            {
                var selectedId = (int)SubCategoryComboBox.SelectedValue;
                SelectedSubCategory = SubCategories.FirstOrDefault(s => s.Id == selectedId);
            }
        }

        private void SubCategoryComboBox_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (SubCategoryComboBox.SelectedItem is SubCategoryModel selectedSubCategory)
            {
                SelectedSubCategory = selectedSubCategory;
                System.Diagnostics.Debug.WriteLine($"Wybrano subkategorię: {selectedSubCategory.Name} (Id: {selectedSubCategory.Id})");
            }
            else if (SubCategoryComboBox.SelectedValue != null)
            {
                // Jeśli wybrano przez SelectedValue
                var selectedId = (int)SubCategoryComboBox.SelectedValue;
                SelectedSubCategory = SubCategories.FirstOrDefault(s => s.Id == selectedId);
                System.Diagnostics.Debug.WriteLine($"Wybrano subkategorię przez SelectedValue: Id {selectedId}");
            }
            else
            {
                SelectedSubCategory = null;
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
