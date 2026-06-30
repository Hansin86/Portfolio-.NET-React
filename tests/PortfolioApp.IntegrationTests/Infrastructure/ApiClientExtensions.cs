using System.Net.Http.Headers;
using System.Net.Http.Json;
using PortfolioApp.Application.Features.Auth.Common;

namespace PortfolioApp.IntegrationTests.Infrastructure;

/// <summary>
/// Test helpers for obtaining HTTP clients that carry a valid bearer token, so protected
/// endpoints can be exercised end-to-end.
/// </summary>
public static class ApiClientExtensions
{
    private const string ValidPassword = "Sup3rSecretPass";

    /// <summary>
    /// Registers a fresh user (each gets their own bootstrapped portfolio) and returns a
    /// client with the resulting JWT attached as a bearer token.
    /// </summary>
    public static async Task<HttpClient> CreateAuthenticatedClientAsync(this PortfolioApiFactory factory)
    {
        HttpClient client = factory.CreateClient();
        string email = $"user-{Guid.NewGuid():N}@example.com";

        HttpResponseMessage response = await client.PostAsJsonAsync(
            "/auth/register", new { email, password = ValidPassword });
        response.EnsureSuccessStatusCode();

        AuthResponseDto auth = (await response.Content.ReadFromJsonAsync<AuthResponseDto>())!;
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth.Token);

        return client;
    }
}
