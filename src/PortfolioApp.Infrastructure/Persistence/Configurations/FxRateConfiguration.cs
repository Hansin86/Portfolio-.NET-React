using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PortfolioApp.Domain.Entities;

namespace PortfolioApp.Infrastructure.Persistence.Configurations;

public class FxRateConfiguration : IEntityTypeConfiguration<FxRate>
{
    public void Configure(EntityTypeBuilder<FxRate> builder)
    {
        builder.ToTable("FxRates");

        builder.HasKey(f => f.Id);

        builder.Property(f => f.Id)
            .HasColumnName("id");

        builder.Property(f => f.FromCurrency)
            .HasColumnName("from_currency")
            .HasMaxLength(3)
            .IsRequired();

        builder.Property(f => f.ToCurrency)
            .HasColumnName("to_currency")
            .HasMaxLength(3)
            .IsRequired();

        builder.Property(f => f.Rate)
            .HasColumnName("rate")
            .HasColumnType("decimal(18,8)")
            .IsRequired();

        builder.Property(f => f.RecordedAt)
            .HasColumnName("recorded_at")
            .HasColumnType("timestamptz")
            .IsRequired();

        builder.HasIndex(f => new { f.FromCurrency, f.ToCurrency, f.RecordedAt })
            .HasDatabaseName("IX_FxRates_FromTo_RecordedAt");
    }
}
