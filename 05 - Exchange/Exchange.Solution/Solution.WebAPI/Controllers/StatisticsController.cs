using Solution.Core.Models.Responses;
using System.Net;

namespace Solution.Api.Controllers;

[Authorize]
public class StatisticsController(IStatisticsService statisticsService) : BaseController
{
    [HttpGet("rates")]
    [ProducesResponseType(typeof(List<RateStatisticsResponse>), (int)HttpStatusCode.OK)]
    public async Task<IActionResult> GetRateStatisticsAsync(
        [FromQuery][Required] DateOnly startDate,
        [FromQuery][Required] DateOnly endDate)
    {
        var result = await statisticsService.GetRateStatisticsAsync(startDate, endDate);
        return result.Match(
            result => Ok(result),
            errors => Problem(errors)
        );
    }

    [HttpGet("transactions")]
    [ProducesResponseType(typeof(List<TransactionStatisticsResponse>), (int)HttpStatusCode.OK)]
    public async Task<IActionResult> GetTransactionStatisticsAsync(
        [FromQuery][Required] DateOnly startDate,
        [FromQuery][Required] DateOnly endDate)
    {
        var result = await statisticsService.GetTransactionStatisticsAsync(startDate, endDate);
        return result.Match(
            result => Ok(result),
            errors => Problem(errors)
        );
    }

    [HttpGet("summary")]
    [ProducesResponseType(typeof(SummaryStatisticsResponse), (int)HttpStatusCode.OK)]
    public async Task<IActionResult> GetSummaryAsync()
    {
        var result = await statisticsService.GetSummaryAsync();
        return result.Match(
            result => Ok(result),
            errors => Problem(errors)
        );
    }
}
