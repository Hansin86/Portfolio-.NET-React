using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using PortfolioApp.Infrastructure.Persistence;
using Testcontainers.PostgreSql;

namespace PortfolioApp.IntegrationTests.Infrastructure;

/// <summary>
/// Boots the real API via <see cref="WebApplicationFactory{TEntryPoint}"/> against a
/// disposable PostgreSQL container (Testcontainers — Docker must be running). The DB
/// connection string and JWT settings are supplied through environment variables because
/// <c>Program.cs</c> reads them eagerly while building the host, before any
/// <c>ConfigureWebHost</c> configuration would apply.
/// </summary>
public class PortfolioApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    // 32+ byte key required for HMAC-SHA256 signing.
    private const string TestSigningKey = "integration-test-signing-key-please-change-0123456789";

    private readonly PostgreSqlContainer _database = new PostgreSqlBuilder()
        .WithImage("postgres:16-alpine")
        .Build();

    public async Task InitializeAsync()
    {
        await _database.StartAsync();

        Environment.SetEnvironmentVariable("ConnectionStrings__Default", _database.GetConnectionString());
        Environment.SetEnvironmentVariable("Jwt__Key", TestSigningKey);
        Environment.SetEnvironmentVariable("Jwt__Issuer", "PortfolioApp.Tests");
        Environment.SetEnvironmentVariable("Jwt__Audience", "PortfolioApp.Tests");

        // Accessing Services builds the host (now that the env vars are set), then apply
        // the schema to the fresh container.
        using IServiceScope scope = Services.CreateScope();
        PortfolioDbContext context = scope.ServiceProvider.GetRequiredService<PortfolioDbContext>();
        await context.Database.MigrateAsync();
    }

    async Task IAsyncLifetime.DisposeAsync()
    {
        await _database.DisposeAsync();
        await base.DisposeAsync();
    }
}
