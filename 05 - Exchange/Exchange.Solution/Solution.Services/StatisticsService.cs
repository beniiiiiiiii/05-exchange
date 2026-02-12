using Solution.Core.Models.Responses;

namespace Solution.Services.Services;

public class StatisticsService : IStatisticsService
{
    private readonly ApplicationDbContext dbContext;

    public StatisticsService(ApplicationDbContext dbContext)
    {
        this.dbContext = dbContext;
    }

    public async Task<ErrorOr<List<RateStatisticsResponse>>> GetRateStatisticsAsync(
        DateOnly startDate, DateOnly endDate)
    {
        var rates = await dbContext.ExchangeRates
            .Where(r => r.Date >= startDate && r.Date <= endDate)
            .OrderBy(r => r.Date)
            .ToListAsync();

        var result = rates
            .GroupBy(r => r.Currency)
            .Select(g => new RateStatisticsResponse
            {
                Currency = g.Key.ToString(),
                DataPoints = g.Select(r => new RateDataPoint
                {
                    Date = r.Date,
                    BuyRate = r.BuyRate,
                    SellRate = r.SellRate
                }).ToList()
            })
            .ToList();

        return result;
    }

    public async Task<ErrorOr<List<TransactionStatisticsResponse>>> GetTransactionStatisticsAsync(
        DateOnly startDate, DateOnly endDate)
    {
        var startDateTime = startDate.ToDateTime(TimeOnly.MinValue);
        var endDateTime = endDate.ToDateTime(TimeOnly.MaxValue);

        var transactions = await dbContext.Transactions
            .Where(t => t.TransactionDate >= startDateTime && t.TransactionDate <= endDateTime)
            .ToListAsync();

        var result = transactions
            .GroupBy(t => t.Currency)
            .Select(g => new TransactionStatisticsResponse
            {
                Currency = g.Key.ToString(),
                TotalBuyCount = g.Count(t => t.Type == TransactionType.Buy),
                TotalSellCount = g.Count(t => t.Type == TransactionType.Sell),
                TotalBuyHufAmount = g.Where(t => t.Type == TransactionType.Buy).Sum(t => t.HufAmount),
                TotalSellHufAmount = g.Where(t => t.Type == TransactionType.Sell).Sum(t => t.HufAmount),
                DailyBreakdown = g.GroupBy(t => DateOnly.FromDateTime(t.TransactionDate))
                    .Select(d => new DailyTransactionData
                    {
                        Date = d.Key,
                        BuyCount = d.Count(t => t.Type == TransactionType.Buy),
                        SellCount = d.Count(t => t.Type == TransactionType.Sell),
                        BuyHufAmount = d.Where(t => t.Type == TransactionType.Buy).Sum(t => t.HufAmount),
                        SellHufAmount = d.Where(t => t.Type == TransactionType.Sell).Sum(t => t.HufAmount)
                    })
                    .OrderBy(d => d.Date)
                    .ToList()
            })
            .ToList();

        return result;
    }

    public async Task<ErrorOr<SummaryStatisticsResponse>> GetSummaryAsync()
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var startOfDay = today.ToDateTime(TimeOnly.MinValue);
        var endOfDay = today.ToDateTime(TimeOnly.MaxValue);

        var todayRatesCount = await dbContext.ExchangeRates.CountAsync(r => r.Date == today);

        var todayTransactions = await dbContext.Transactions
            .Where(t => t.TransactionDate >= startOfDay && t.TransactionDate <= endOfDay)
            .ToListAsync();

        var summary = new SummaryStatisticsResponse
        {
            TodayRatesSet = todayRatesCount == 3,
            TotalTransactionsToday = todayTransactions.Count,
            TotalHufVolumeToday = todayTransactions.Sum(t => t.HufAmount),
            TransactionsByCurrency = todayTransactions
                .GroupBy(t => t.Currency)
                .Select(g => new CurrencySummary
                {
                    Currency = g.Key.ToString(),
                    BuyCount = g.Count(t => t.Type == TransactionType.Buy),
                    SellCount = g.Count(t => t.Type == TransactionType.Sell),
                    TotalHufVolume = g.Sum(t => t.HufAmount)
                })
                .ToList()
        };

        return summary;
    }
}