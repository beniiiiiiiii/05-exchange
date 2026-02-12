namespace Solution.Core.Models.Requests;

public class CreateExchangeRatesRequest
{
    [JsonPropertyName("date")]
    public DateOnly Date {  get; set; }

    [JsonPropertyName("usdBuyRate")]
    public decimal UsdBuyRate { get; set; }

    [JsonPropertyName("usdSellRate")]
    public decimal UsdSellRate { get; set; }

    [JsonPropertyName("gbpBuyRate")]
    public decimal GbpBuyRate { get; set; }

    [JsonPropertyName("gbpSellRate")]
    public decimal GbpSellRate { get; set; }

    [JsonPropertyName("chfBuyRate")]
    public decimal ChfBuyRate { get; set; }

    [JsonPropertyName("chfSellRate")]
    public decimal ChfSellRate { get; set; }
}
