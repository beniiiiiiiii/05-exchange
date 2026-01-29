namespace Solution.Domain.Database.Entities;

public class ExchangeEntity
{
    public string IDNumber { get; set; }
    public TransactionType TransactionType { get; set; } = TransactionType.Buy;
    public CurrencyType ExchangeFrom { get; set; } = CurrencyType.HUF;
    public CurrencyType ExchangeTo { get; set; } = CurrencyType.USD;
    public IDType IDType { get; set; } = IDType.IDCard;
    public decimal Amount { get; set; }
    public DateTime TimeOfExchange { get; set; } = DateTime.UtcNow;
}
