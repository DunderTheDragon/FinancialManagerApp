using System.Linq;
using System.Windows;
using System.Windows.Controls;
using FinancialManagerApp.Models;
using FinancialManagerApp.ViewModels;

namespace FinancialManagerApp.Views
{
    public partial class ImportTransactionsView : Window
    {
        public ImportTransactionsViewModel ViewModel { get; private set; }

        public ImportTransactionsView(ImportTransactionsViewModel viewModel)
        {
            InitializeComponent();
            ViewModel = viewModel;
            DataContext = viewModel;
        }

        private void CategoryComboBox_Loaded(object sender, RoutedEventArgs e)
        {
            if (sender is ComboBox categoryCombo)
            {
                var row = DataGridRow.GetRowContainingElement(categoryCombo);
                if (row?.DataContext is ImportedTransactionModel transaction)
                {
                    // Ustaw wybraną kategorię
                    categoryCombo.SelectedValue = transaction.CategoryId;
                }
            }
        }

        private void CategoryComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (sender is ComboBox categoryCombo && categoryCombo.SelectedItem is CategoryModel selectedCategory)
            {
                // Znajdź wiersz DataGrid dla tej komórki
                var row = DataGridRow.GetRowContainingElement(categoryCombo);
                if (row?.DataContext is ImportedTransactionModel transaction)
                {
                    // Zaktualizuj kategorię w transakcji
                    transaction.CategoryId = selectedCategory.Id;
                    transaction.Category = selectedCategory.Type;

                    // Zaktualizuj subkategorie w ComboBoxie subkategorii
                    var subCategoryCombo = FindSubCategoryComboBox(row);
                    if (subCategoryCombo != null)
                    {
                        var subCategories = ViewModel.GetSubCategoriesForCategory(selectedCategory.Id);
                        subCategoryCombo.ItemsSource = subCategories;
                        
                        // Jeśli nie ma subkategorii lub obecna subkategoria nie należy do nowej kategorii
                        if (subCategories.Count == 0 || !subCategories.Any(s => s.Id == transaction.SubCategoryId))
                        {
                            transaction.SubCategoryId = 0;
                            transaction.SubCategory = "Brak";
                            subCategoryCombo.SelectedValue = null;
                        }
                        else
                        {
                            // Zachowaj wybraną subkategorię jeśli należy do nowej kategorii
                            subCategoryCombo.SelectedValue = transaction.SubCategoryId;
                        }
                    }
                }
            }
        }

        private void SubCategoryComboBox_Loaded(object sender, RoutedEventArgs e)
        {
            if (sender is ComboBox subCategoryCombo)
            {
                var row = DataGridRow.GetRowContainingElement(subCategoryCombo);
                if (row?.DataContext is ImportedTransactionModel transaction)
                {
                    // Załaduj subkategorie dla kategorii tej transakcji
                    var subCategories = ViewModel.GetSubCategoriesForCategory(transaction.CategoryId);
                    subCategoryCombo.ItemsSource = subCategories;
                    
                    // Ustaw wybraną subkategorię jeśli istnieje
                    if (transaction.SubCategoryId > 0)
                    {
                        subCategoryCombo.SelectedValue = transaction.SubCategoryId;
                    }
                }
            }
        }

        private void SubCategoryComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (sender is ComboBox subCategoryCombo)
            {
                var row = DataGridRow.GetRowContainingElement(subCategoryCombo);
                if (row?.DataContext is ImportedTransactionModel transaction)
                {
                    if (subCategoryCombo.SelectedItem is SubCategoryModel selectedSubCategory)
                    {
                        transaction.SubCategoryId = selectedSubCategory.Id;
                        transaction.SubCategory = selectedSubCategory.Name;
                    }
                    else
                    {
                        // Jeśli wyczyszczono wybór
                        transaction.SubCategoryId = 0;
                        transaction.SubCategory = "Brak";
                    }
                }
            }
        }

        private ComboBox FindSubCategoryComboBox(DataGridRow row)
        {
            // Znajdź ComboBox subkategorii w wierszu
            return FindVisualChild<ComboBox>(row, "SubCategoryComboBox");
        }

        private T FindVisualChild<T>(DependencyObject parent, string name = null) where T : DependencyObject
        {
            for (int i = 0; i < System.Windows.Media.VisualTreeHelper.GetChildrenCount(parent); i++)
            {
                var child = System.Windows.Media.VisualTreeHelper.GetChild(parent, i);
                if (child is T result)
                {
                    if (string.IsNullOrEmpty(name) || (child is FrameworkElement fe && fe.Name == name))
                        return result;
                }

                var childOfChild = FindVisualChild<T>(child, name);
                if (childOfChild != null)
                    return childOfChild;
            }
            return null;
        }
    }

    // Klasa pomocnicza do znajdowania wiersza DataGrid
    public static class DataGridRowExtensions
    {
        public static DataGridRow GetRowContainingElement(DependencyObject element)
        {
            while (element != null)
            {
                if (element is DataGridRow row)
                    return row;
                element = System.Windows.Media.VisualTreeHelper.GetParent(element);
            }
            return null;
        }
    }
}
