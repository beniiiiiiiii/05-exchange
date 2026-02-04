namespace Solution.Core.Interfaces.Services;

public interface ITransactionService
{
    Task<ErrorOr<TransactionResponse>> CreateBuyTransactionAsync(CreateTransactionRequest request);
    Task<ErrorOr<TransactionResponse>> CreateSellTransactionAsynx(CreateTransactionRequest request);
    Task<ErrorOr<TransactionResponse>> GetTransactionAsync(DateOnly? date, Currency? currency, TransactionType? type);
    Task<ErrorOr<TransactionResponse>> GetTransactionByIdAsync(int id);
}
