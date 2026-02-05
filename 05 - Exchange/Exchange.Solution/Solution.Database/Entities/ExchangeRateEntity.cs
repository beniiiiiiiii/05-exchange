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

    public DateTime? UpdatedAt { get; set; }

    [Required]
    public string CreatedByUserId { get; set; }
    public string? ModifiedByUserId { get; set; }

    public virtual UserEntity CreatedByUser { get; set; }
    public virtual UserEntity? ModifiedByUser { get; set; }
    public virtual ICollection<TransactionEntity> Transactions { get; set; }

}
