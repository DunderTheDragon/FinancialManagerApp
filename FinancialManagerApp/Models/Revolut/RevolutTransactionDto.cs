using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace FinancialManagerApp.Models.Revolut
{
    public class RevolutTransactionDto
    {
        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("created_at")]
        public DateTime CreatedAt { get; set; }

        [JsonProperty("completed_at")]
        public DateTime? CompletedAt { get; set; }

        // W Revolucie ujemna kwota to wydatek, dodatnia to wpływ
        // Ale czasem struktura jest inna (zależy od endpointu). 
        // Przyjmijmy uproszczenie dla transaction leg.
        [JsonProperty("amount")]
        public decimal Amount { get; set; }

        [JsonProperty("description")] // Czasem jest to 'reference' lub pole w 'merchant'
        public string Description { get; set; }
    }
}