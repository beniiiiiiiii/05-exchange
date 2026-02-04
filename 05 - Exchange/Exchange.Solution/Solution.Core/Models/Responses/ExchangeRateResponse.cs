namespace Solution.Core.Models.Responses;

public class ExchangeRateResponse
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("currency")]
    public string Currency { get; set; }

    [JsonPropertyName("date")]
    public DateOnly Date { get; set; }

    [JsonPropertyName("buyRate")]
    public decimal BuyRate { get; set; }

    [JsonPropertyName("sellRate")]
    public decimal SellRate { get; set; }

    [JsonPropertyName("createdAt")]
    public DateTime CreatedAt { get; set; }

    [JsonPropertyName("modifiedAt")]
    public DateTime? ModifiedAt { get; set; }
}
