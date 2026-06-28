using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using PortfolioApp.Application.Interfaces;
using PortfolioApp.Infrastructure.Identity;
using PortfolioApp.Infrastructure.Persistence;
using PortfolioApp.Infrastructure.Repositories;

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

        services.Configure<JwtSettings>(configuration.GetSection(JwtSettings.SectionName));

        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IPortfolioRepository, PortfolioRepository>();
        services.AddSingleton<IPasswordHasher, PasswordHasher>();
        services.AddSingleton<IJwtTokenGenerator, JwtTokenGenerator>();

        return services;
    }
}
