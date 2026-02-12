namespace Solution.Core.Interfaces.Services;

public interface ITransactionService
{
    Task<ErrorOr<TransactionResponse>> CreateBuyTransactionAsync(CreateTransactionRequest request);
    Task<ErrorOr<TransactionResponse>> CreateSellTransactionAsync(CreateTransactionRequest request);
    Task<ErrorOr<TransactionListResponse>> GetTransactionsAsync(DateOnly? date, Currency? currency, TransactionType? type);
    Task<ErrorOr<TransactionResponse>> GetTransactionByIdAsync(int id);
}
