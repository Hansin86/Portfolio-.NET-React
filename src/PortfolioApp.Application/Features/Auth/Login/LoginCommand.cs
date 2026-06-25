using MediatR;
using PortfolioApp.Application.Features.Auth.Common;

namespace PortfolioApp.Application.Features.Auth.Login;

/// <summary>
/// Authenticates a user by email and password (FR-02) and returns a signed JWT on success.
/// </summary>
public record LoginCommand(string Email, string Password) : IRequest<AuthResponseDto>;
