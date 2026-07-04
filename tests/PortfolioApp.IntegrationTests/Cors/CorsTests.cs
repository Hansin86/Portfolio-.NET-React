using System.Net.Http.Headers;
using FluentAssertions;
using PortfolioApp.IntegrationTests.Infrastructure;

namespace PortfolioApp.IntegrationTests.Cors;

/// <summary>
/// Verifies the config-driven CORS policy (F1): the SPA dev origin is allowed to preflight
/// API calls, while an unlisted origin is not echoed back an allow-origin header.
/// </summary>
public class CorsTests(PortfolioApiFactory factory) : IClassFixture<PortfolioApiFactory>
{
    // The default origin configured in appsettings.json (Cors:AllowedOrigins).
    private const string AllowedOrigin = "http://localhost:5173";

    [Fact]
    public async Task Preflight_FromAllowedOrigin_IsAllowed()
    {
        HttpClient client = factory.CreateClient();

        using var preflight = new HttpRequestMessage(HttpMethod.Options, "/auth/login");
        preflight.Headers.Add("Origin", AllowedOrigin);
        preflight.Headers.Add("Access-Control-Request-Method", "POST");
        preflight.Headers.Add("Access-Control-Request-Headers", "authorization,content-type");

        HttpResponseMessage response = await client.SendAsync(preflight);

        response.Headers.GetValues("Access-Control-Allow-Origin")
            .Should().ContainSingle().Which.Should().Be(AllowedOrigin);
    }

    [Fact]
    public async Task Preflight_FromDisallowedOrigin_IsNotAllowed()
    {
        HttpClient client = factory.CreateClient();

        using var preflight = new HttpRequestMessage(HttpMethod.Options, "/auth/login");
        preflight.Headers.Add("Origin", "https://evil.example.com");
        preflight.Headers.Add("Access-Control-Request-Method", "POST");

        HttpResponseMessage response = await client.SendAsync(preflight);

        response.Headers.Contains("Access-Control-Allow-Origin").Should().BeFalse();
    }
}
