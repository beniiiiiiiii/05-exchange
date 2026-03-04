namespace Solution.WebAPI.Controllers;

[Authorize]
public class ExchangeRateController(IExchangeRateService exchangeRateService) : BaseController
{
    [HttpGet]
    [ProducesResponseType(typeof(List<ExchangeRateResponse>), (int)HttpStatusCode.OK)]
    public async Task<IActionResult> GetRatesAsync(
        [FromQuery] DateOnly? startDate,
        [FromQuery] DateOnly? endDate)
    {
        var result = await exchangeRateService.GetRatesHistoryAsync(startDate, endDate);
        return result.Match(
            result => Ok(result),
            errors => Problem(errors)
        );
    }

    [HttpGet("today")]
    [ProducesResponseType(typeof(ExchangeRateResponse), (int)HttpStatusCode.OK)]
    public async Task<IActionResult> GetTodayRatesAsync()
    {
        var result = await exchangeRateService.GetTodayRatesAsync();
        return result.Match(
            result => Ok(result),
            errors => Problem(errors)
        );
    }

    [HttpGet("{date}")]
    [ProducesResponseType(typeof(ExchangeRateResponse), (int)HttpStatusCode.OK)]
    public async Task<IActionResult> GetRateByDateAsync([FromRoute] DateOnly date)
    {
        var result = await exchangeRateService.GetRatesByDateAsync(date);
        return result.Match(
            result => Ok(result),
            errors => Problem(errors)
        );
    }

    [HttpGet("exists/{date}")]
    [ProducesResponseType(typeof(bool), (int)HttpStatusCode.OK)]
    public async Task<IActionResult> RatesExistAsync([FromRoute] DateOnly date)
    {
        var result = await exchangeRateService.RatesExistForDateAsync(date);
        return result.Match(
            result => Ok(result),
            errors => Problem(errors)
        );
    }

    [HttpPost]
    [ProducesResponseType(typeof(ExchangeRateResponse), (int)HttpStatusCode.Created)]
    public async Task<IActionResult> CreateExchangeRatesAsync(
        [FromBody][Required] CreateExchangeRatesRequest request)
    {
        var result = await exchangeRateService.CreateDailyRatesAsync(request);
        return result.Match(
            result => CreatedAtAction(nameof(GetRateByDateAsync), new { date = result.Date.ToString("yyyy-MM-dd") }, result),
            errors => Problem(errors)
        );
    }

    [HttpPut]
    [ProducesResponseType(typeof(ExchangeRateResponse), (int)HttpStatusCode.OK)]
    public async Task<IActionResult> UpdateRateAsync(
        [FromBody][Required] UpdateExchangeRateRequest request)
    {
        var result = await exchangeRateService.UpdateRateAsync(request);
        return result.Match(
            result => Ok(result),
            errors => Problem(errors));
    }

}
