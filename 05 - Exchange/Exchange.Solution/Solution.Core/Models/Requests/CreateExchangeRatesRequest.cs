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
    public decimal gbpBuyRate { get; set; }

    [JsonPropertyName("gbpSellRate")]
    public decimal gbpSellRate { get; set; }

    [JsonPropertyName("chfBuyRate")]
    public decimal chfBuyRate { get; set; }

    [JsonPropertyName("chfSellRate")]
    public decimal chfSellRate { get; set; }
}
