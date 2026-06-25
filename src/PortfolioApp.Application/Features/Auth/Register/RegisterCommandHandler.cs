using MediatR;
using PortfolioApp.Application.Features.Auth.Common;
using PortfolioApp.Application.Interfaces;
using PortfolioApp.Domain.Entities;
using PortfolioApp.Domain.Exceptions;

namespace PortfolioApp.Application.Features.Auth.Register;

/// <summary>
/// Handles <see cref="RegisterCommand"/>: enforces email uniqueness, hashes the password
/// (NFR-03), persists the new user, and issues a JWT.
/// </summary>
public class RegisterCommandHandler : IRequestHandler<RegisterCommand, AuthResponseDto>
{
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

        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = email,
            PasswordHash = _passwordHasher.Hash(request.Password),
            IsDemoTemplate = false,
            CreatedAt = DateTime.UtcNow,
        };

        await _users.AddAsync(user, cancellationToken);

        string token = _tokenGenerator.GenerateToken(user);
        return new AuthResponseDto(user.Id, email, token);
    }
}
