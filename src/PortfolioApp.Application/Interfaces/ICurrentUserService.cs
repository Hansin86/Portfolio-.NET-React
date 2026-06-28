namespace PortfolioApp.Application.Interfaces;

/// <summary>
/// Exposes the identity of the caller behind the current request, read from the validated
/// JWT. Implemented in the composition root over <c>IHttpContextAccessor</c>. This is the
/// read-side of per-user data scoping (FR-03): handlers use <see cref="UserId"/> to load
/// and authorize only the caller's own data.
/// </summary>
public interface ICurrentUserService
{
    /// <summary>
    /// The authenticated user's id (the JWT <c>sub</c> claim). Intended for use from
    /// endpoints protected by <c>[Authorize]</c>; throws
    /// <see cref="UnauthorizedAccessException"/> when no authenticated user is present or
    /// the claim is missing/malformed.
    /// </summary>
    Guid UserId { get; }

    /// <summary>
    /// True when the caller is operating inside an isolated demo session
    /// (JWT <c>is_demo</c> claim). Defaults to <c>false</c> when the claim is absent.
    /// </summary>
    bool IsDemo { get; }

    /// <summary>
    /// The demo session id (JWT <c>demo_session_id</c> claim) when <see cref="IsDemo"/> is
    /// true; otherwise <c>null</c>. Used to scope a request to its demo portfolio copy (FR-04).
    /// </summary>
    Guid? DemoSessionId { get; }
}
