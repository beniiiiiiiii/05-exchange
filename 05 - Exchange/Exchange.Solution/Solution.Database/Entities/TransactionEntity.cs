namespace Solution.Database.Entities;

[Table("Transaction")]
public class TransactionEntity
{
    [Key]
    public int Id { get; set; }

    [Required]
    public TransactionType Type { get; set; }

    [Required]
    public Currency Currency { get; set; }

    [Required]
    [Column(TypeName = "decimal(18,2)")]
    public decimal ForeignAmount { get; set; }

    [Required]
    [Column(TypeName = "decimal(18,2)")]
    public decimal HufAmount { get; set; }

    [Required]
    [Column(TypeName = "decimal(18,4)")]
    public decimal AppliedRate { get; set; }

    [Required]
    [MaxLength(100)]
    public string CustomerName { get; set; }

    [Required]
    public CustomerIdType CustomerIdType { get; set; }

    [Required]
    [MaxLength(50)]
    public string CustomerIdNumber { get; set; }

    [Required]
    public DateTime TransactionDate { get; set; }

    [Required]
    public Guid ProcessedByUserId { get; set; }

    public int ExchangeRateId { get; set; }

    public virtual UserEntity ProcessedByUser { get; set; }
    public virtual ExchangeRateEntity  ExchangeRate { get; set; }
}
