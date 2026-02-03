namespace FinancialManagerApp.Models
{
    public class SubCategoryModel
    {
        public int Id { get; set; }
        public int CategoryId { get; set; }
        public string Name { get; set; } = string.Empty;
    }
}
