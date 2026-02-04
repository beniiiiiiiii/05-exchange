namespace Solution.Core.Models.Responses;

public class RateStatisticResponse
{
    [JsonPropertyName("currency")]
    public string Currency { get; set; }

    [JsonPropertyName("dataPoints")]
    public List<RateDataPoint> DataPoints { get; set; } = new();
}
public class RateDataPoint
{
    [JsonPropertyName("date")]
    public DateOnly Date { get ; set; }

    [JsonPropertyName("buyRate")]
    public decimal BuyRate { get; set; }

    [JsonPropertyName("sellRate")]
    public decimal SellRate { get; set; }
}

public class TransactionStatisticsResponse
{
    [JsonPropertyName("currency")]
    public string Currency { get; set; }

    [JsonPropertyName("totalBuyCount")]
    public int TotalBuyCount { get; set; }

    [JsonPropertyName("totalSellCount")]
    public int TotalSellCount { get; set; }

    [JsonPropertyName("totalBuyHufAmount")]
    public decimal TotalBuyHufAmount { get; set; }

    [JsonPropertyName("totalSellHufAmount")]
    public decimal TotalSellHufAmount { get; set; }

    [JsonPropertyName("dailyBreakdown")]
    public List<DailyTransactionData> DailyBreakdown { get; set; } = new();
}

public class DailyTransactionData
{
    [JsonPropertyName("date")]
    public DateOnly Date { get; set; }

    [JsonPropertyName("buyCount")]
    public int BuyCount { get; set; }

    [JsonPropertyName("sellCount")]
    public int SellCount { get; set; }

    [JsonPropertyName("buyHufAmount")]
    public decimal BuyHufAmount { get; set; }

    [JsonPropertyName("sellHufAmount")]
    public decimal SellHufAmount { get; set; }
}

public class SummaryStatisticsResponse
{
    [JsonPropertyName("todayRatesSet")]
    public bool TodayRatesSet { get; set; }

    [JsonPropertyName("totalTransactionsToday")]
    public int TotalTransactionsToday { get; set; }

    [JsonPropertyName("totalHufVolumeToday")]
    public decimal TotalHufVolumeToday { get; set; }

    [JsonPropertyName("transactionsByCurrency")]
    public List<CurrencySummary> TransactionsByCurrency { get; set; } = new();
}

public class CurrencySummary
{
    [JsonPropertyName("currency")]
    public string Currency { get; set; }

    [JsonPropertyName("buyCount")]
    public int BuyCount { get; set; }

    [JsonPropertyName("sellCount")]
    public int SellCount { get; set; }

    [JsonPropertyName("totalHufVolume")]
    public decimal TotalHufVolume { get; set; }
}