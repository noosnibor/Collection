namespace Collection.Web.Components.Services;

/// <summary>
/// Settings for the built-in "break-glass" System Admin. This account is NOT stored in the database:
/// it exists only while the settings below are present on the server, so it can sign in on a brand-new
/// database, or when every database administrator is locked out.
/// </summary>
/// <remarks>
/// Set these as environment variables / Azure App Service "Application settings" (never commit the hash):
/// <code>
/// SystemAdmin__Enabled      = true
/// SystemAdmin__Username     = sysadmin
/// SystemAdmin__PasswordHash = (output of:  dotnet Collection.Web.dll --hash-password "your password")
/// </code>
/// </remarks>
public class SystemAdminOptions
{
    /// <summary>The section name in configuration.</summary>
    public const string SectionName = "SystemAdmin";

    /// <summary>The account only works when this is true. Leave it false to switch the account off.</summary>
    public bool Enabled { get; set; }

    /// <summary>The user name typed on the login page.</summary>
    public string? Username { get; set; }

    /// <summary>A BCrypt hash of the password. A plain-text password is never accepted.</summary>
    public string? PasswordHash { get; set; }

    /// <summary>Name shown in the header. Defaults to "System Administrator".</summary>
    public string? FullName { get; set; }

    /// <summary>Optional. Location this account works in; defaults to the first active location.</summary>
    public string? LocationKey { get; set; }

    /// <summary>Optional. Currency this account works in; defaults to the location's own (rate 1) currency.</summary>
    public string? CurrencyKey { get; set; }
}
