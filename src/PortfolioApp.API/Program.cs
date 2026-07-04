using System.Text;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using PortfolioApp.API.Middleware;
using PortfolioApp.API.Services;
using PortfolioApp.Application;
using PortfolioApp.Application.Interfaces;
using PortfolioApp.Infrastructure;
using PortfolioApp.Infrastructure.Identity;
using Scalar.AspNetCore;

// Store all timestamptz values as UTC (no implicit local-time conversion).
AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", false);

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers()
    // Serialize/accept enums as their string names (e.g. "Buy"/"Sell") rather than integers,
    // so the JSON contract is self-describing.
    .AddJsonOptions(options =>
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddApplication();

// Exposes the authenticated caller (JWT claims) to Application handlers — the read-side
// of per-user data scoping (FR-03).
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer();

// Build the bearer validation parameters from JwtSettings resolved through the options system
// (bound to the "Jwt" section in AddInfrastructure). Reading the settings here — when the bearer
// options are materialised — rather than eagerly off IConfiguration while the host is still being
// built keeps validation aligned with the final, merged configuration.
builder.Services
    .AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme)
    .Configure<IOptions<JwtSettings>>((options, jwtSettingsAccessor) =>
    {
        JwtSettings jwtSettings = jwtSettingsAccessor.Value;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtSettings.Issuer,
            ValidAudience = jwtSettings.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.Key)),
        };
    });
builder.Services.AddAuthorization();

var app = builder.Build();

// Configure the HTTP request pipeline.
app.UseMiddleware<ExceptionHandlingMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();

/// <summary>
/// Exposed as a public partial class so the integration test project can reference it as
/// the entry point for <c>WebApplicationFactory&lt;Program&gt;</c>.
/// </summary>
public partial class Program;
