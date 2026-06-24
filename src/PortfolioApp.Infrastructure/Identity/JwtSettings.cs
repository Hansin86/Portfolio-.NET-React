namespace PortfolioApp.Infrastructure.Identity;

/// <summary>
/// JWT signing/issuing options, bound from the <c>Jwt</c> configuration section. The
/// signing <see cref="Key"/> is a secret and should come from user-secrets or environment
/// variables, never source control.
/// </summary>
public class JwtSettings
{
    public const string SectionName = "Jwt";

    public string Issuer { get; set; } = string.Empty;

    public string Audience { get; set; } = string.Empty;

    /// <summary>Symmetric signing key (HMAC-SHA256).</summary>
    public string Key { get; set; } = string.Empty;

    /// <summary>Token lifetime in minutes.</summary>
    public int ExpiryMinutes { get; set; } = 60;
}
