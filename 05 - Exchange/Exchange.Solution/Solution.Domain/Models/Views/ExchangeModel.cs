namespace Solution.Domain.Models.Views;
public class ExchangeModel
{
    [Required]
    [JsonPropertyName("idNumber")]
    public string IDNumber { get; set; }

    [Required]
    [JsonPropertyName("transactionType")]
    public TransactionType TransactionType { get; set; }

    [Required]
    [JsonPropertyName("exchangeFrom")]
    public CurrencyType ExchangeFrom { get; set; }
    [Required]
    [JsonPropertyName("exchangeTo")]
    public CurrencyType ExchangeTo { get; set; }

    [Required]
    [JsonPropertyName("idType")]
    public IDType IDType { get; set; }

    [Required]
    [JsonPropertyName("amount")]
    public decimal Amount { get; set; }

    [Required]
    [JsonPropertyName("timeOfExchange")]
    public DateTime TimeOfExchange { get; set; }
}
