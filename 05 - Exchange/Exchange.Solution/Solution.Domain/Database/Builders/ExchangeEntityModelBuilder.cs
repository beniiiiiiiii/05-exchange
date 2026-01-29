namespace Solution.Domain.Database.Builders;

internal static class ExchangeEntityModelBuilder
{
    public static void ConfigureExchange(this ModelBuilder builder)
    {
        builder.Entity<ExchangeEntity>(entity =>
        {
            entity.ToTable("Exchanges");

            entity.HasKey(e => e.IDNumber);

            entity.Property(e => e.TransactionType)
                  .HasColumnName("TransactionType")
                  .IsRequired();

            entity.Property(e => e.ExchangeFrom)
                  .HasColumnName("ExchangeFrom")
                  .IsRequired();

            entity.Property(e => e.ExchangeTo)
                  .HasColumnName("ExchangeTo")
                  .IsRequired();

            entity.Property(e => e.IDType)
                  .HasColumnName("IDType")
                  .IsRequired();

            entity.Property(e => e.Amount)
                  .HasColumnName("Amount")
                  .HasColumnType("decimal(18,2)")
                  .IsRequired();

            entity.Property(e => e.TimeOfExchange)
                  .HasColumnName("TimeOfExchange")
                  .IsRequired();
        });
    }
}
