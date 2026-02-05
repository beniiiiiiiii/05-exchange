namespace Solution.Core.Interfaces.Services;

public interface IStatisticsService
{
    Task<ErrorOr<List<RateStatisticResponse>>> GetRateStatisticsAsync(DateOnly startdDate, DateOnly endDate);
    Task<ErrorOr<List<TransactionStatisticsResponse>>> GetTransactionStatisticsAsync(DateOnly startDate, DateOnly endDate);
    Task<ErrorOr<SummaryStatisticsResponse>> GetSummaryAsync();
}
