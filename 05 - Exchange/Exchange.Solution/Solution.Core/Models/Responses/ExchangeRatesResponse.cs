namespace Solution.Core.Models.Response;

public class ExchangeRatesResponse
{
    [JsonPropertyName("date")]
    public DateOnly Date { get; set; }

    [JsonPropertyName("rates")]
    public List<ExchangeRateResponse> Rates { get; set; } = new();
}