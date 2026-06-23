using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using PortfolioApp.Infrastructure.Persistence;

namespace PortfolioApp.Infrastructure;

/// <summary>
/// Registers Infrastructure-layer services (persistence, external clients, jobs)
/// into the DI container. Called from the API composition root.
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        string connectionString = configuration.GetConnectionString("Default")
            ?? throw new InvalidOperationException(
                "Connection string 'Default' was not found.");

        services.AddDbContext<PortfolioDbContext>(options =>
            options.UseNpgsql(connectionString));

        return services;
    }
}
