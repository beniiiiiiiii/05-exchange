
using Solution.Core.Models.Requests;
using Solution.Core.Models.Responses;
using Solution.Database.Enums;
using System.Net;

namespace Solution.WebAPI.Controllers;

[Authorize]
public class TransactionController(ITransactionService transactionService) : BaseController
{
    [HttpPost("buy")]
    [ProducesResponseType(typeof(TransactionResponse), (int)HttpStatusCode.Created)]
    public async Task<IActionResult> CreateBuyTransactionAsync(
        [FromBody][Required] CreateTransactionRequest request)
    {
        var result = await transactionService.CreateBuyTransactionAsync(request);
        return result.Match(
            result => CreatedAtAction(nameof(GetTransactionByIdAsync), new { id = result.Id }, result),
            errors => Problem(errors)
        );
    }

    [HttpPost("sell")]
    [ProducesResponseType(typeof(TransactionResponse), (int)HttpStatusCode.Created)]
    public async Task<IActionResult> CreateSellTransaction(
        [FromBody][Required] CreateTransactionRequest request)
    {
        var result = await transactionService.CreateSellTransactionAsync(request);
        return result.Match(
            result => CreatedAtAction(nameof(GetTransactionByIdAsync), new { id = result.Id }, result)
            errors => Problem(errors)
        );
    }

    [HttpGet]
    [ProducesResponseType(typeof(TransactionResponse), (int)HttpStatusCode.OK)]
    public async Task<IActionResult> GetTransactionsAsync(
        [FromQuery] DateOnly? date,
        [FromQuery] Currency? currency,
        [FromQuery] TransactionType? type)
    {
        var result = await transactionService.GetTransactionAsync(date, currency, type);
        return result.Match(
            result => Ok(result),
            errors => Problem(errors)
        );
    }

    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(TransactionResponse), (int)HttpStatusCode.OK)]
    public async Task<IActionResult> GetTransactionByIdAsync([FromRoute] int id)
    {
        var result = await transactionService.GetTransactionByIdAsync(id);
        return result.Match(
            result => Ok(result),
            errors => Problem(errors)
        );
    }
}
