using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using PortfolioApp.Application.Features.Auth.Common;
using PortfolioApp.IntegrationTests.Infrastructure;

namespace PortfolioApp.IntegrationTests.Auth;

/// <summary>
/// End-to-end coverage of the auth slice: registration and login driven through the real
/// HTTP pipeline (validation behaviour, controllers, exception middleware) against a real
/// PostgreSQL container.
/// </summary>
public class AuthEndpointsTests : IClassFixture<PortfolioApiFactory>
{
    private const string ValidPassword = "Sup3rSecretPass";

    private readonly HttpClient _client;

    public AuthEndpointsTests(PortfolioApiFactory factory)
    {
        _client = factory.CreateClient();
    }

    private static string UniqueEmail() => $"user-{Guid.NewGuid():N}@example.com";

    [Fact]
    public async Task Register_ThenLogin_ReturnsTokenForSameUser()
    {
        string email = UniqueEmail();

        HttpResponseMessage registerResponse = await _client.PostAsJsonAsync(
            "/auth/register", new { email, password = ValidPassword });

        registerResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        AuthResponseDto? registered = await registerResponse.Content.ReadFromJsonAsync<AuthResponseDto>();
        registered.Should().NotBeNull();
        registered!.Token.Should().NotBeNullOrWhiteSpace();
        registered.Email.Should().Be(email);
        registered.UserId.Should().NotBe(Guid.Empty);

        HttpResponseMessage loginResponse = await _client.PostAsJsonAsync(
            "/auth/login", new { email, password = ValidPassword });

        loginResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        AuthResponseDto? loggedIn = await loginResponse.Content.ReadFromJsonAsync<AuthResponseDto>();
        loggedIn.Should().NotBeNull();
        loggedIn!.Token.Should().NotBeNullOrWhiteSpace();
        loggedIn.UserId.Should().Be(registered.UserId);
    }

    [Fact]
    public async Task Register_WithDuplicateEmail_ReturnsConflict()
    {
        string email = UniqueEmail();
        await _client.PostAsJsonAsync("/auth/register", new { email, password = ValidPassword });

        HttpResponseMessage response = await _client.PostAsJsonAsync(
            "/auth/register", new { email, password = ValidPassword });

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task Register_WithWeakPassword_ReturnsBadRequest()
    {
        HttpResponseMessage response = await _client.PostAsJsonAsync(
            "/auth/register", new { email = UniqueEmail(), password = "weak" });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Login_WithWrongPassword_ReturnsUnauthorized()
    {
        string email = UniqueEmail();
        await _client.PostAsJsonAsync("/auth/register", new { email, password = ValidPassword });

        HttpResponseMessage response = await _client.PostAsJsonAsync(
            "/auth/login", new { email, password = "WrongPassword123" });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Login_WithUnknownEmail_ReturnsUnauthorized()
    {
        HttpResponseMessage response = await _client.PostAsJsonAsync(
            "/auth/login", new { email = UniqueEmail(), password = ValidPassword });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
