using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PortfolioApp.Domain.Entities;

namespace PortfolioApp.Infrastructure.Persistence.Configurations;

public class DemoSessionConfiguration : IEntityTypeConfiguration<DemoSession>
{
    public void Configure(EntityTypeBuilder<DemoSession> builder)
    {
        builder.ToTable("DemoSessions");

        builder.HasKey(d => d.Id);

        builder.Property(d => d.Id)
            .HasColumnName("id");

        builder.Property(d => d.PortfolioId)
            .HasColumnName("portfolio_id")
            .IsRequired();

        builder.Property(d => d.ExpiresAt)
            .HasColumnName("expires_at")
            .HasColumnType("timestamptz")
            .IsRequired();

        builder.Property(d => d.CreatedAt)
            .HasColumnName("created_at")
            .HasColumnType("timestamptz")
            .HasDefaultValueSql("now()")
            .IsRequired();

        // Used by the Hangfire cleanup job to find expired sessions.
        builder.HasIndex(d => d.ExpiresAt)
            .HasDatabaseName("IX_DemoSessions_ExpiresAt");

        // Relationship configured from PortfolioConfiguration (one-to-one, cascade).
    }
}
