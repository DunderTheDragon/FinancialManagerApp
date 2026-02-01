using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace FinancialManagerApp.Models.Revolut
{
    public class RevolutAccountDto
    {
        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("balance")]
        public decimal Balance { get; set; }

        [JsonProperty("currency")]
        public string Currency { get; set; }
    }
}