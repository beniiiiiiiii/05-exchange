namespace Solution.Services;

public class ExchangeRateService : IExchangeRateService
{
    private readonly ApplicationDbContext dbContext;
    private readonly ICurrentUserProvider currentUserProvider;
    private readonly ILogger<ExchangeRateService> logger;

    public ExchangeRateService(
        ApplicationDbContext dbContext,
        ICurrentUserProvider currentUserProvider,
        ILogger<ExchangeRateService> logger)
    {
        this.dbContext = dbContext;
        this.currentUserProvider = currentUserProvider;
        this.logger = logger;
    }

    public async Task<ErrorOr<ExchangeRatesResponse>> GetRatesByDateAsync(DateOnly date)
    {
        var rates = await dbContext.ExchangeRates.Where(x => x.Date == date).OrderBy(x => x.Currency).ToListAsync();

        if (!rates.Any())
        {
            return Errors.ExchangeRate.NotFoundForDate;
        }
        return MapToResponse(date, rates);
    }

    public async Task<ErrorOr<ExchangeRatesResponse>> GetTodayRatesAsync()
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        return await GetRatesByDateAsync(today);
    }

    public async Task<ErrorOr<List<ExchangeRatesResponse>>> GetRatesHistoryAsync(DateOnly? startDate, DateOnly? endDate)
    {
        var query = dbContext.ExchangeRates.AsQueryable();

        if (startDate.HasValue)
            query = query.Where(x => x.Date >= startDate.Value);
        
        if (endDate.HasValue)
            query = query.Where(x => x.Date <= endDate.Value);

        var rates = await query.OrderByDescending(x => x.Date).ThenBy(x => x.Currency).ToListAsync();

        var grouped = rates.GroupBy(x => x.Date).Select(x => MapToResponse(x.Key, x.ToList())).ToList();

        return grouped;
    }

    public async Task<ErrorOr<bool>> RatesExistForDateAsync(DateOnly date)
    {
        var count = await dbContext.ExchangeRates
            .CountAsync(r => r.Date == date);

        return count == 3;
    }

    public async Task<ErrorOr<ExchangeRatesResponse>> CreateDailyRatesAsync(
        CreateExchangeRatesRequest request)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        if (request.Date != today)
            return Errors.ExchangeRate.OnlyCurrentDateAllowed;

        var existingRates = await dbContext.ExchangeRates
            .AnyAsync(r => r.Date == request.Date);

        if (existingRates)
            return Errors.ExchangeRate.AlreadyExistsForDate;

        var userId = GetCurrentUserId();
        var now = DateTime.UtcNow;

        var rates = new List<ExchangeRateEntity>
        {
            new() { Currency = Currency.USD, Date = request.Date, BuyRate = request.UsdBuyRate, SellRate = request.UsdSellRate, CreatedAt = now, CreatedByUserId = userId },
            new() { Currency = Currency.GBP, Date = request.Date, BuyRate = request.GbpBuyRate, SellRate = request.GbpSellRate, CreatedAt = now, CreatedByUserId = userId },
            new() { Currency = Currency.CHF, Date = request.Date, BuyRate = request.ChfBuyRate, SellRate = request.ChfSellRate, CreatedAt = now, CreatedByUserId = userId }
        };

        await dbContext.ExchangeRates.AddRangeAsync(rates);
        await dbContext.SaveChangesAsync();

        logger.LogInformation("Exchange rates created for date {Date} by user {UserId}", request.Date, userId);

        return MapToResponse(request.Date, rates);
    }

    public async Task<ErrorOr<ExchangeRateResponse>> UpdateRateAsync(
           UpdateExchangeRateRequest request)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        var rate = await dbContext.ExchangeRates
            .FirstOrDefaultAsync(r => r.Date == today && r.Currency == request.Currency);

        if (rate is null)
            return Errors.ExchangeRate.NotFoundForCurrency;

        var userId = GetCurrentUserId();

        rate.BuyRate = request.BuyRate;
        rate.SellRate = request.SellRate;
        rate.ModifiedAt = DateTime.UtcNow;
        rate.ModifiedByUserId = userId;

        await dbContext.SaveChangesAsync();

        logger.LogInformation("Exchange rate updated for {Currency} by user {UserId}", request.Currency, userId);

        return MapToResponse(rate);
    }

    private Guid GetCurrentUserId()
    {
        return currentUserProvider.GetCurrentUserId();
    }

    private static ExchangeRatesResponse MapToResponse(DateOnly date, List<ExchangeRateEntity> rates)
    {
        return new ExchangeRatesResponse
        {
            Date = date,
            Rates = rates.Select(MapToResponse).ToList()
        };
    }

    private static ExchangeRateResponse MapToResponse(ExchangeRateEntity entity)
    {
        return new ExchangeRateResponse
        {
            Id = entity.Id,
            Currency = entity.Currency.ToString(),
            Date = entity.Date,
            BuyRate = entity.BuyRate,
            SellRate = entity.SellRate,
            CreatedAt = entity.CreatedAt,
            ModifiedAt = entity.ModifiedAt
        };
    }
}

