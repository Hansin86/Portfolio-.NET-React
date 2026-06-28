using MediatR;
using PortfolioApp.Application.Features.Auth.Common;
using PortfolioApp.Application.Interfaces;
using PortfolioApp.Domain.Entities;
using PortfolioApp.Domain.Exceptions;
using PortfolioApp.Domain.ValueObjects;

namespace PortfolioApp.Application.Features.Auth.Register;

/// <summary>
/// Handles <see cref="RegisterCommand"/>: enforces email uniqueness, hashes the password
/// (NFR-03), persists the new user together with their portfolio, and issues a JWT.
/// </summary>
public class RegisterCommandHandler : IRequestHandler<RegisterCommand, AuthResponseDto>
{
    /// <summary>
    /// Default base display currency for a new portfolio. Users can change it later (FR-11).
    /// </summary>
    private static readonly Currency DefaultBaseCurrency = Currency.Usd;

    private readonly IUserRepository _users;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IJwtTokenGenerator _tokenGenerator;

    public RegisterCommandHandler(
        IUserRepository users,
        IPasswordHasher passwordHasher,
        IJwtTokenGenerator tokenGenerator)
    {
        _users = users;
        _passwordHasher = passwordHasher;
        _tokenGenerator = tokenGenerator;
    }

    public async Task<AuthResponseDto> Handle(RegisterCommand request, CancellationToken cancellationToken)
    {
        string email = request.Email.Trim().ToLowerInvariant();

        if (await _users.EmailExistsAsync(email, cancellationToken))
        {
            throw new EmailAlreadyInUseException(email);
        }

        DateTime now = DateTime.UtcNow;

        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = email,
            PasswordHash = _passwordHasher.Hash(request.Password),
            IsDemoTemplate = false,
            CreatedAt = now,
        };

        // Establish the one-portfolio-per-user invariant up front. Attaching it to the user
        // graph lets it persist in the same unit of work (single SaveChanges) as the user.
        user.Portfolios.Add(new Portfolio
        {
            Id = Guid.NewGuid(),
            BaseCurrency = DefaultBaseCurrency,
            CreatedAt = now,
        });

        await _users.AddAsync(user, cancellationToken);

        string token = _tokenGenerator.GenerateToken(user);
        return new AuthResponseDto(user.Id, email, token);
    }
}
