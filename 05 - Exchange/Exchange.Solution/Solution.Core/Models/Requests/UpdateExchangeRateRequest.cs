namespace Solution.Core.Models.Requests;

public class UpdateExchangeRateRequest
{
    [JsonPropertyName("currency")]
    public Currency Currency { get; set; }

    [JsonPropertyName("buyRate")]
    public decimal BuyRate { get; set; }

    [JsonPropertyName("sellRate")]
    public decimal SellRate { get; set; }
}