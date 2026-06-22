using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PortfolioApp.Domain.Entities;
using PortfolioApp.Domain.Enums;

namespace PortfolioApp.Infrastructure.Persistence.Configurations;

public class AssetConfiguration : IEntityTypeConfiguration<Asset>
{
    public void Configure(EntityTypeBuilder<Asset> builder)
    {
        builder.ToTable("Assets");

        builder.HasKey(a => a.Id);

        builder.Property(a => a.Id)
            .HasColumnName("id");

        builder.Property(a => a.Ticker)
            .HasColumnName("ticker")
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(a => a.Name)
            .HasColumnName("name")
            .HasMaxLength(256)
            .IsRequired();

        builder.Property(a => a.AssetType)
            .HasColumnName("asset_type")
            .HasMaxLength(10)
            .IsRequired()
            .HasConversion(
                v => v == AssetType.Stock ? "STOCK" : "ETF",
                v => v == "STOCK" ? AssetType.Stock : AssetType.Etf);

        builder.Property(a => a.QuoteCurrency)
            .HasColumnName("quote_currency")
            .HasMaxLength(3)
            .IsRequired();

        builder.HasIndex(a => a.Ticker)
            .IsUnique()
            .HasDatabaseName("IX_Assets_Ticker");

        builder.HasMany(a => a.Transactions)
            .WithOne(t => t.Asset)
            .HasForeignKey(t => t.AssetId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(a => a.PriceSnapshots)
            .WithOne(p => p.Asset)
            .HasForeignKey(p => p.AssetId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
