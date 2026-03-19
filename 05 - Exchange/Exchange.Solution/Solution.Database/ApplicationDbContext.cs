namespace Solution.Database;

public sealed class ApplicationDbContext : IdentityDbContext<UserEntity, IdentityRole<Guid>, Guid>
{
    public override DbSet<UserEntity> Users { get; set; }
    public DbSet<ExchangeRateEntity> ExchangeRates { get; set; }
    public DbSet<TransactionEntity> Transactions { get; set; }

    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.ConfigureUser();

        builder.Entity<ExchangeRateEntity>(b =>
        {
            b.ToTable("ExchangeRate");

            b.HasIndex(e => new { e.Currency, e.Date }).IsUnique();
            b.Property(e => e.BuyRate).HasPrecision(18, 4);
            b.Property(e => e.SellRate).HasPrecision(18, 4);
            b.Property(e => e.Currency).HasConversion<string>();
        });

        builder.Entity<TransactionEntity>(b =>
        {
            b.ToTable("Transaction");
            b.Property(e => e.ForeignAmount).HasPrecision(18, 2);
            b.Property(e => e.HufAmount).HasPrecision(18, 2);
            b.Property(e => e.AppliedRate).HasPrecision(18, 4);
            b.Property(e => e.Type).HasConversion<string>();
            b.Property(e => e.Currency).HasConversion<string>();
            b.Property(e => e.CustomerIdType).HasConversion<string>();

            b.HasOne(e => e.ExchangeRate)
             .WithMany(e => e.Transactions)
             .HasForeignKey(e => e.ExchangeRateId)
             .OnDelete(DeleteBehavior.Restrict);
        });
    }
}