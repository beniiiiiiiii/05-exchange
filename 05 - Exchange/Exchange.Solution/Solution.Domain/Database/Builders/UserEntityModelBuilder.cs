using System;
using System.Collections.Generic;
using System.Text;

namespace Solution.Domain.Database.Builders;

internal static class UserEntityModelBuilder
{
    public static void ConfigureUser(this ModelBuilder builder)
    {
        builder.Entity<UserEntity>(entity =>
        {
            entity.ToTable("Users");

            entity.HasIndex(e => e.Id)
                  .IsUnique();
            
            entity.Property(e => e.FullName)
                .HasColumnName("FullName")
                .IsRequired()
                .HasMaxLength(200);

            entity.Property(e => e.RegisteredAtUtc)
                .HasColumnName("RegisteredAtUtc");
        });
    }
}
