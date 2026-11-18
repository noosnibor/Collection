using System.Security.Claims;

namespace Collection.Library.Service;

/// <summary>Names of the custom claims issued at login (avoids "magic strings" across the app).</summary>
public static class PortalClaims
{
    public const string UserKey = "UserKey";
    public const string FullName = "FullName";
    public const string Email = "Email";
    public const string Role = "Role";
    public const string ProfileInitials = "ProfileInitials";
    public const string Username = "Username";
    public const string LocationKey = "LocationKey";
    public const string CurrencyKey = "CurrencyKey";
    public const string CurrencyType = "CurrencyType";
    public const string CurrencyAmount = "CurrencyAmount";
    public const string Address = "Address";
    public const string Permission = "permission";
    /// <summary>"true" for the built-in System Admin that is configured on the server instead of the database.</summary>
    public const string SystemAdmin = "SystemAdmin";
}

/// <summary>
/// Per-circuit holder of the signed-in user. It is the single source of truth that the
/// custom <c>AuthenticationStateProvider</c> reads from.
/// </summary>
public class UserSessionService
{
    #region Public Members

    /// <summary>The authenticated principal, or <c>null</c> / anonymous when signed out.</summary>
    public ClaimsPrincipal? Principal { get; set; }

    /// <summary>True when a principal with an authenticated identity is present.</summary>
    public bool IsAuthenticated => Principal?.Identity?.IsAuthenticated == true;

    /// <summary>Returns the value of a claim, or <c>null</c> when it does not exist.</summary>
    public string? GetClaim(string claimType) => Principal?.FindFirst(claimType)?.Value;

    /// <summary>Signs the user out of this session.</summary>
    public void Clear() => Principal = null;

    #endregion

    #region Convenience Accessors

    public string? Username => GetClaim(PortalClaims.Username);
    public string? FullName => GetClaim(PortalClaims.FullName);
    public string? LocationKey => GetClaim(PortalClaims.LocationKey);
    public string? CurrencyKey => GetClaim(PortalClaims.CurrencyKey);
    public string? Role => GetClaim(PortalClaims.Role);

    /// <summary>True for the built-in System Admin and for any database user whose role is System Admin.</summary>
    public bool IsSystemAdmin =>
        string.Equals(GetClaim(PortalClaims.SystemAdmin), "true", StringComparison.OrdinalIgnoreCase)
        || string.Equals(Role, "System Admin", StringComparison.OrdinalIgnoreCase);

    #endregion
}
