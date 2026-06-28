using System.Security.Claims;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.IdentityModel.JsonWebTokens;
using NSubstitute;
using PortfolioApp.API.Services;

namespace PortfolioApp.UnitTests.Services;

public class CurrentUserServiceTests
{
    private readonly IHttpContextAccessor _httpContextAccessor = Substitute.For<IHttpContextAccessor>();

    private CurrentUserService CreateService(params Claim[] claims)
    {
        var context = new DefaultHttpContext
        {
            User = new ClaimsPrincipal(new ClaimsIdentity(claims, authenticationType: "jwt")),
        };
        _httpContextAccessor.HttpContext.Returns(context);
        return new CurrentUserService(_httpContextAccessor);
    }

    [Fact]
    public void UserId_ReadsSubClaim()
    {
        Guid userId = Guid.NewGuid();
        CurrentUserService service = CreateService(new Claim(JwtRegisteredClaimNames.Sub, userId.ToString()));

        service.UserId.Should().Be(userId);
    }

    [Fact]
    public void UserId_FallsBackToNameIdentifier_WhenSubIsMapped()
    {
        // The JWT bearer handler maps "sub" to NameIdentifier by default (MapInboundClaims).
        Guid userId = Guid.NewGuid();
        CurrentUserService service = CreateService(new Claim(ClaimTypes.NameIdentifier, userId.ToString()));

        service.UserId.Should().Be(userId);
    }

    [Fact]
    public void UserId_WhenClaimMissing_Throws()
    {
        CurrentUserService service = CreateService();

        Action act = () => _ = service.UserId;

        act.Should().Throw<UnauthorizedAccessException>();
    }

    [Fact]
    public void UserId_WhenClaimNotAGuid_Throws()
    {
        CurrentUserService service = CreateService(new Claim(JwtRegisteredClaimNames.Sub, "not-a-guid"));

        Action act = () => _ = service.UserId;

        act.Should().Throw<UnauthorizedAccessException>();
    }

    [Fact]
    public void UserId_WhenNoHttpContext_Throws()
    {
        _httpContextAccessor.HttpContext.Returns((HttpContext?)null);
        var service = new CurrentUserService(_httpContextAccessor);

        Action act = () => _ = service.UserId;

        act.Should().Throw<UnauthorizedAccessException>();
    }

    [Fact]
    public void IsDemo_WhenClaimAbsent_IsFalse()
    {
        CurrentUserService service = CreateService();

        service.IsDemo.Should().BeFalse();
    }

    [Fact]
    public void IsDemo_WhenClaimTrue_IsTrue()
    {
        CurrentUserService service = CreateService(new Claim("is_demo", "true"));

        service.IsDemo.Should().BeTrue();
    }

    [Fact]
    public void DemoSessionId_WhenClaimPresent_IsParsed()
    {
        Guid sessionId = Guid.NewGuid();
        CurrentUserService service = CreateService(new Claim("demo_session_id", sessionId.ToString()));

        service.DemoSessionId.Should().Be(sessionId);
    }

    [Fact]
    public void DemoSessionId_WhenClaimAbsent_IsNull()
    {
        CurrentUserService service = CreateService();

        service.DemoSessionId.Should().BeNull();
    }
}
