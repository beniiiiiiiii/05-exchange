using Solution.Core.Models.Response;

namespace Solution.Core.Interfaces.Services;

public interface IExchangeRateService
{
    Task<ErrorOr<ExchangeRatesResponse>> GetRatesByDateAsync(DateOnly date);
    Task<ErrorOr<ExchangeRatesResponse>> GetTodayRatesAsync();
    Task<ErrorOr<List<ExchangeRatesResponse>>> GetRatesHistoryAsync(DateOnly? startDate, DateOnly? endDate);
    Task<ErrorOr<bool>> RatesExistForDateAsync(DateOnly date);
    Task<ErrorOr<ExchangeRatesResponse>> CreateDailyRatesAsync(CreateExchangeRatesRequest request);
    Task<ErrorOr<ExchangeRateResponse>> UpdateRateAsync(UpdateExchangeRateRequest request);
}
