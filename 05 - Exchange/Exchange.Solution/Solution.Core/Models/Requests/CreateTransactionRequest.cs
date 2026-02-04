using Solution.Domain.Enums;

namespace Solution.Core.Models.Requests;

public class CreateTransactionRequest
{
    [JsonPropertyName("currency")]
    public Currency Currency { get; set; }

    [JsonPropertyName("foreignAmount")]
    public decimal ForeignAmount { get; set; }

    [JsonPropertyName("customerName")]
    public string CustomerName { get; set; }

    [JsonPropertyName("customerIdType")]
    public CustomerIdType CustomerIdType { get; set; }
}
