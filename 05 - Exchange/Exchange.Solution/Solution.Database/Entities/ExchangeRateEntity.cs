namespace Solution.Database.Entities;

[Table("ExchangeRate")]
public class ExchangeRateEntity
{
    [Key]
    public int Id { get; set; }

    [Required]
    public Currency Currency { get; set; }

    [Required]
    public DateOnly Date { get; set;  }

    [Required]
    [Column(TypeName = "decimal(18,4)")]
    public decimal BuyRate { get; set; }

    [Required]
    [Column(TypeName = "decimal(18,4)")]
    public decimal SellRate { get; set; }

    [Required]
    public DateTime CreatedAt { get; set; }

    public DateTime? ModifiedAt { get; set; }
    public virtual ICollection<TransactionEntity> Transactions { get; set; }

}
