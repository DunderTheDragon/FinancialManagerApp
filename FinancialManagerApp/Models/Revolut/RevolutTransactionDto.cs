using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;

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

        // Revolut zwraca transakcje w częściach ("legs"). 
        // Musimy je pobrać, aby znać kwotę.
        [JsonProperty("legs")]
        public List<RevolutTransactionLeg> Legs { get; set; }

        [JsonProperty("reference")]
        public string Reference { get; set; }

        [JsonProperty("merchant")]
        public RevolutMerchant Merchant { get; set; }

        // Właściwość pomocnicza, która wyciąga kwotę i opis dla naszej aplikacji
        public decimal TotalAmount => Legs?.FirstOrDefault()?.Amount ?? 0;
        public string Description => !string.IsNullOrEmpty(Reference) ? Reference : (Merchant?.Name ?? "Transakcja Revolut");
    }

    public class RevolutTransactionLeg
    {
        [JsonProperty("leg_id")]
        public string LegId { get; set; }

        [JsonProperty("amount")]
        public decimal Amount { get; set; }

        [JsonProperty("currency")]
        public string Currency { get; set; }

        [JsonProperty("description")]
        public string Description { get; set; }
    }

    public class RevolutMerchant
    {
        [JsonProperty("name")]
        public string Name { get; set; }
        [JsonProperty("city")]
        public string City { get; set; }
    }
}