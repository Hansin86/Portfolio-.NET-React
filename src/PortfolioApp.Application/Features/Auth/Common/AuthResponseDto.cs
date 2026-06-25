namespace PortfolioApp.Application.Features.Auth.Common;

/// <summary>
/// Result of a successful registration or login: the authenticated user's identity plus a
/// signed JWT bearer token the client sends on subsequent requests (NFR-04).
/// </summary>
public record AuthResponseDto(Guid UserId, string Email, string Token);
