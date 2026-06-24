using System.Reflection;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.DependencyInjection;

namespace PortfolioApp.Application;

/// <summary>
/// Registers Application-layer services (MediatR use cases, validators, AutoMapper
/// profiles, pipeline behaviours) into the DI container. Called from the API composition
/// root.
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        Assembly assembly = typeof(DependencyInjection).Assembly;

        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(assembly));

        services.AddAutoMapper(cfg => { }, assembly);

        services.AddValidatorsFromAssembly(assembly);

        return services;
    }
}
