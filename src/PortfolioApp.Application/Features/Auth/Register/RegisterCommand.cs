using MediatR;
using PortfolioApp.Application.Features.Auth.Common;

namespace PortfolioApp.Application.Features.Auth.Register;

/// <summary>
/// Registers a new user with an email and password (FR-01) and returns a token so the
/// client is logged in immediately on success.
/// </summary>
public record RegisterCommand(string Email, string Password) : IRequest<AuthResponseDto>;
