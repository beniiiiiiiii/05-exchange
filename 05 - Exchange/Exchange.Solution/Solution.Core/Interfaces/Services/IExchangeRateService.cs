namespace Solution.Core.Interfaces.Services;

public interface IExchangeRateService
{
    Task<ErrorOr<ExchangeRateResponse>> GetRatesByDateAsync(DateOnly date);
    Task<ErrorOr<ExchangeRateResponse>> GetTodayRatesAsync();
    Task<ErrorOr<List<ExchangeRateResponse>>> GetRateHistoryAsync(DateOnly? startDate, DateOnly? endDate);
    Task<ErrorOr<bool>> RatesExistForDateAsync(DateOnly date);
    Task<ErrorOr<ExchangeRateResponse>> CreateDailyRatesAsync(CreateExchangeRatesRequest request);
    Task<ErrorOr<ExchangeRateResponse>> UpdateRateAsync(UpdateExchangeRateRequest request);
}
