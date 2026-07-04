using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using PortfolioApp.Infrastructure.Identity;
using PortfolioApp.Infrastructure.Persistence;
using Testcontainers.PostgreSql;

namespace PortfolioApp.IntegrationTests.Infrastructure;

/// <summary>
/// Boots the real API via <see cref="WebApplicationFactory{TEntryPoint}"/> against a
/// disposable PostgreSQL container (Testcontainers — Docker must be running). The container
/// connection string and JWT settings are injected per-host in <see cref="ConfigureWebHost"/>,
/// so each factory is fully self-contained and instances can run in parallel without sharing
/// process-global state.
/// </summary>
public class PortfolioApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    // 32+ byte key required for HMAC-SHA256 signing.
    private const string TestSigningKey = "integration-test-signing-key-please-change-0123456789";
    private const string TestIssuer = "PortfolioApp.Tests";
    private const string TestAudience = "PortfolioApp.Tests";

    private readonly PostgreSqlContainer _database = new PostgreSqlBuilder()
        .WithImage("postgres:16-alpine")
        .WithCleanUp(true)
        .Build();

    public async Task InitializeAsync()
    {
        await _database.StartAsync();

        // Accessing Services builds the host (applying the overrides below), then migrate the
        // fresh container to the current schema.
        using IServiceScope scope = Services.CreateScope();
        PortfolioDbContext context = scope.ServiceProvider.GetRequiredService<PortfolioDbContext>();
        await context.Database.MigrateAsync();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        // The container is started by InitializeAsync before the host is built, so its connection
        // string is available here. It must be supplied via UseSetting (not ConfigureAppConfiguration):
        // AddInfrastructure reads GetConnectionString("Default") during Program.cs service
        // registration, which runs BEFORE builder.Build() — the point at which ConfigureAppConfiguration
        // callbacks are applied. UseSetting lands in the host configuration that CreateBuilder reads
        // up front, so AddInfrastructure resolves it like production would. Without this, startup
        // throws "Connection string 'Default' was not found." in environments (e.g. CI) that lack the
        // user-secrets fallback used locally.
        builder.UseSetting("ConnectionStrings:Default", _database.GetConnectionString());

        builder.ConfigureTestServices(services =>
        {
            // The app builds bearer validation from JwtSettings via the options system, so
            // overriding the settings here aligns both token issuance and validation with the
            // test signing key.
            services.Configure<JwtSettings>(s =>
            {
                s.Key = TestSigningKey;
                s.Issuer = TestIssuer;
                s.Audience = TestAudience;
            });
        });
    }

    async Task IAsyncLifetime.DisposeAsync()
    {
        await _database.DisposeAsync();
        await base.DisposeAsync();
    }
}
