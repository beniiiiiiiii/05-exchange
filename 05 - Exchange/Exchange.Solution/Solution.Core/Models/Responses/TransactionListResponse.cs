namespace Solution.Core.Models.Response;

public class TransactionListResponse
{
    [JsonPropertyName("transactions")]
    public List<TransactionResponse> Transactions { get; set; } = new();

    [JsonPropertyName("totalCount")]
    public int TotalCount { get; set; }
}