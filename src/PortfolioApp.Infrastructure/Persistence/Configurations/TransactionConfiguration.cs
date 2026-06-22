using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PortfolioApp.Domain.Entities;
using PortfolioApp.Domain.Enums;

namespace PortfolioApp.Infrastructure.Persistence.Configurations;

public class TransactionConfiguration : IEntityTypeConfiguration<Transaction>
{
    public void Configure(EntityTypeBuilder<Transaction> builder)
    {
        builder.ToTable("Transactions");

        builder.HasKey(t => t.Id);

        builder.Property(t => t.Id)
            .HasColumnName("id");

        builder.Property(t => t.PortfolioId)
            .HasColumnName("portfolio_id")
            .IsRequired();

        builder.Property(t => t.AssetId)
            .HasColumnName("asset_id")
            .IsRequired();

        builder.Property(t => t.Type)
            .HasColumnName("type")
            .HasMaxLength(4)
            .IsRequired()
            .HasConversion(
                v => v == TransactionType.Buy ? "BUY" : "SELL",
                v => v == "BUY" ? TransactionType.Buy : TransactionType.Sell);

        builder.Property(t => t.Quantity)
            .HasColumnName("quantity")
            .HasColumnType("decimal(18,8)")
            .IsRequired();

        builder.Property(t => t.PricePerUnit)
            .HasColumnName("price_per_unit")
            .HasColumnType("decimal(18,8)")
            .IsRequired();

        builder.Property(t => t.Currency)
            .HasColumnName("currency")
            .HasMaxLength(3)
            .IsRequired();

        builder.Property(t => t.TransactionDate)
            .HasColumnName("transaction_date")
            .HasColumnType("date")
            .IsRequired();

        builder.Property(t => t.CreatedAt)
            .HasColumnName("created_at")
            .HasColumnType("timestamptz")
            .HasDefaultValueSql("now()")
            .IsRequired();

        builder.HasIndex(t => t.PortfolioId)
            .HasDatabaseName("IX_Transactions_PortfolioId");

        builder.HasIndex(t => t.AssetId)
            .HasDatabaseName("IX_Transactions_AssetId");

        builder.HasIndex(t => new { t.PortfolioId, t.AssetId })
            .HasDatabaseName("IX_Transactions_PortfolioId_AssetId");

        builder.HasIndex(t => t.TransactionDate)
            .HasDatabaseName("IX_Transactions_TransactionDate");

        // FK relationships configured from Portfolio/Asset configurations.
    }
}
