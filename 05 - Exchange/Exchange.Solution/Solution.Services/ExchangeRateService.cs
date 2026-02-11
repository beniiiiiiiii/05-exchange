
namespace Solution.Services;

public class ExchangeRateService : IExchangeRateService
{
    private readonly ApplicationDbContext dbContext;
    private readonly IHttpContextAccessor httpContextAccessor;
    private readonly ILogger<ExchangeRateService> logger;

    public ExchangeRateService(
        ApplicationDbContext dbContext,
        IHttpContextAccessor httpContextAccessor,
        ILogger<ExchangeRateService> logger)
    {
        this.dbContext = dbContext;
        this.httpContextAccessor = httpContextAccessor;
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
}
