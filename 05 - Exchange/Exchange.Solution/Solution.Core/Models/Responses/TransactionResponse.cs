namespace Solution.Core.Models.Responses;

public class TransactionResponse
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("type")]
    public string Type { get; set; }

    [JsonPropertyName("currency")]
    public string Currency { get; set; }

    [JsonPropertyName("foreignAmount")]
    public decimal ForeignAmount { get; set; }

    [JsonPropertyName("hufAmount")]
    public decimal HufAmount { get; set; }

    [JsonPropertyName("AppliedRate")]
    public decimal AppliedRate { get; set; }

    [JsonPropertyName("customerName")]
    public string CustomerName { get; set; }

    [JsonPropertyName("customerIdType")] 
    public string CustomerIdType { get; set; }

    [JsonPropertyName("customerIdNumber")]
    public string CustomerIdNumber { get; set; }

    [JsonPropertyName("transactionDate")]
    public DateTime TransactionDate { get; set; }

    [JsonPropertyName("processedBy")]
    public string ProcessedBy { get; set; }
}
