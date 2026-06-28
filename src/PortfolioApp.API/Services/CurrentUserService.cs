using System.Security.Claims;
using Microsoft.IdentityModel.JsonWebTokens;
using PortfolioApp.Application.Interfaces;

namespace PortfolioApp.API.Services;

/// <summary>
/// Reads the current caller's identity from the validated JWT on the active
/// <see cref="HttpContext"/>. Registered scoped in the composition root.
/// </summary>
public class CurrentUserService : ICurrentUserService
{
    // Claims minted by the demo-session login (FR-04). Not yet issued by the token
    // generator; the accessors below degrade gracefully until that lands.
    private const string IsDemoClaim = "is_demo";
    private const string DemoSessionIdClaim = "demo_session_id";

    private readonly IHttpContextAccessor _httpContextAccessor;

    public CurrentUserService(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public Guid UserId
    {
        get
        {
            // The JWT bearer handler maps the standard "sub" claim to NameIdentifier by
            // default (MapInboundClaims), so accept either form.
            string? value = FindFirst(JwtRegisteredClaimNames.Sub)
                ?? FindFirst(ClaimTypes.NameIdentifier);

            if (!Guid.TryParse(value, out Guid userId))
            {
                throw new UnauthorizedAccessException(
                    "No authenticated user is associated with the current request.");
            }

            return userId;
        }
    }

    public bool IsDemo =>
        bool.TryParse(FindFirst(IsDemoClaim), out bool isDemo) && isDemo;

    public Guid? DemoSessionId =>
        Guid.TryParse(FindFirst(DemoSessionIdClaim), out Guid sessionId) ? sessionId : null;

    private string? FindFirst(string claimType) =>
        _httpContextAccessor.HttpContext?.User?.FindFirstValue(claimType);
}
