using MediatR;
using PortfolioApp.Application.Features.Auth.Common;
using PortfolioApp.Application.Interfaces;
using PortfolioApp.Domain.Entities;
using PortfolioApp.Domain.Exceptions;

namespace PortfolioApp.Application.Features.Auth.Login;

/// <summary>
/// Handles <see cref="LoginCommand"/>: locates the user by email, verifies the password
/// hash (NFR-03), and issues a JWT. A missing user and a wrong password both raise the same
/// <see cref="InvalidCredentialsException"/> so the two cases are indistinguishable.
/// </summary>
public class LoginCommandHandler : IRequestHandler<LoginCommand, AuthResponseDto>
{
    private readonly IUserRepository _users;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IJwtTokenGenerator _tokenGenerator;

    public LoginCommandHandler(
        IUserRepository users,
        IPasswordHasher passwordHasher,
        IJwtTokenGenerator tokenGenerator)
    {
        _users = users;
        _passwordHasher = passwordHasher;
        _tokenGenerator = tokenGenerator;
    }

    public async Task<AuthResponseDto> Handle(LoginCommand request, CancellationToken cancellationToken)
    {
        string email = request.Email.Trim().ToLowerInvariant();
        User? user = await _users.GetByEmailAsync(email, cancellationToken);

        if (user is null || !_passwordHasher.Verify(request.Password, user.PasswordHash))
        {
            throw new InvalidCredentialsException();
        }

        string token = _tokenGenerator.GenerateToken(user);
        return new AuthResponseDto(user.Id, user.Email!, token);
    }
}
