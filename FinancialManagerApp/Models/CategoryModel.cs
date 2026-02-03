using System.Collections.ObjectModel;

namespace FinancialManagerApp.Models
{
    public class CategoryModel
    {
        public int Id { get; set; }
        public string Type { get; set; } = string.Empty; // podstawowe, osobiste, oszczednosci
        public ObservableCollection<SubCategoryModel> SubCategories { get; set; } = new ObservableCollection<SubCategoryModel>();
    }
}
