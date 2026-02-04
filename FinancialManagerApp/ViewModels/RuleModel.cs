using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FinancialManagerApp.Models
{
    public class RuleModel
    {
        public int Id { get; set; }
        public string Phrase { get; set; }
        public string Category { get; set; }
        public string SubCategory { get; set; }
        public int CategoryId { get; set; }
        public int? SubCategoryId { get; set; }
    }
}
