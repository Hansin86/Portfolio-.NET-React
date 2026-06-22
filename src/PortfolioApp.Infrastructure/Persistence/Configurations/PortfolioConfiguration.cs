using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PortfolioApp.Domain.Entities;

namespace PortfolioApp.Infrastructure.Persistence.Configurations;

public class PortfolioConfiguration : IEntityTypeConfiguration<Portfolio>
{
    public void Configure(EntityTypeBuilder<Portfolio> builder)
    {
        builder.ToTable("Portfolios");

        builder.HasKey(p => p.Id);

        builder.Property(p => p.Id)
            .HasColumnName("id");

        builder.Property(p => p.UserId)
            .HasColumnName("user_id");

        builder.Property(p => p.BaseCurrency)
            .HasColumnName("base_currency")
            .HasMaxLength(3)
            .IsRequired();

        builder.Property(p => p.CreatedAt)
            .HasColumnName("created_at")
            .HasColumnType("timestamptz")
            .HasDefaultValueSql("now()")
            .IsRequired();

        builder.HasIndex(p => p.UserId)
            .HasDatabaseName("IX_Portfolios_UserId");

        builder.HasMany(p => p.Transactions)
            .WithOne(t => t.Portfolio)
            .HasForeignKey(t => t.PortfolioId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(p => p.DemoSession)
            .WithOne(d => d.Portfolio)
            .HasForeignKey<DemoSession>(d => d.PortfolioId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
