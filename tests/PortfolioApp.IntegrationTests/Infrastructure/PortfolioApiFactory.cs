using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
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
        // string is available here. Overrides are applied at the service level (rather than via
        // configuration) so they don't depend on when the host merges test configuration: in
        // particular Program.cs reads the JWT settings eagerly while building the bearer options,
        // before any injected configuration is visible.
        builder.ConfigureTestServices(services =>
        {
            ReplaceDbContext(services);
            ConfigureJwt(services);
        });
    }

    /// <summary>Repoints <see cref="PortfolioDbContext"/> at this factory's container.</summary>
    private void ReplaceDbContext(IServiceCollection services)
    {
        ServiceDescriptor options = services.Single(
            d => d.ServiceType == typeof(DbContextOptions<PortfolioDbContext>));
        services.Remove(options);

        services.AddDbContext<PortfolioDbContext>(o => o.UseNpgsql(_database.GetConnectionString()));
    }

    /// <summary>
    /// Aligns both sides of JWT auth with the test signing key: the token generator
    /// (<see cref="JwtSettings"/> via options) and the bearer validation parameters, which
    /// Program.cs builds eagerly from configuration the test host hasn't merged yet.
    /// </summary>
    private static void ConfigureJwt(IServiceCollection services)
    {
        services.Configure<JwtSettings>(s =>
        {
            s.Key = TestSigningKey;
            s.Issuer = TestIssuer;
            s.Audience = TestAudience;
        });

        services.PostConfigure<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme, options =>
        {
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                ValidIssuer = TestIssuer,
                ValidAudience = TestAudience,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(TestSigningKey)),
            };
        });
    }

    async Task IAsyncLifetime.DisposeAsync()
    {
        await _database.DisposeAsync();
        await base.DisposeAsync();
    }
}
