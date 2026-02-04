using ErrorOr;

namespace Solution.Core.Interfaces.Services;

public interface IStatisticsService
{
    Task<ErrorOr<List<RateStatisticsResponse>>> GetRateStatisticsAsync(DateOnly startdDate, DateOnly endDate);
    Task<ErrorOr<List<TransactionStatisticsResponse>>> GetTransactionStatisticsAsync(DateOnly startDate, DateOnly endDate);
    Task<ErrorOr<SummaryStatisticsResponse>> GetSummaryAsync();
}
