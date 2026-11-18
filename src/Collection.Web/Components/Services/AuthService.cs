using Collection.Library.DataTransferObject;
using Collection.Library.Model;
using Collection.Library.Repository;
using Collection.Library.Service;
using System.Globalization;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;

namespace Collection.Web.Components.Services;

#region Contract

/// <summary>Validates sign-in credentials and builds the signed-in user's claims.</summary>
public interface IAuthService
{
    /// <summary>
    /// Validates the credentials. Returns <c>(true, message)</c> on success, otherwise
    /// <c>(false, reason)</c>. The special reason <see cref="AuthService.ChangePasswordRequired"/>
    /// tells the UI to send the user to the change-password page.
    /// </summary>
    Task<(bool Success, string Message)> ValidateCredentials(LoginDataTransferObject model,
                                                             CancellationToken cancellationToken = default);
}

#endregion

public class AuthService(IUserRepository userRepository,
                         ICurrencyRepository currencyRepository,
                         IPermissionRepository permissionRepository,
                         ILocationRepository locationRepository,
                         UserSessionService userSessionService,
                         IOptions<SystemAdminOptions> systemAdminOptions,
                         ILogger<AuthService> logger) : IAuthService
{
    #region Constants

    /// <summary>Reason text returned when the user must replace a temporary password.</summary>
    public const string ChangePasswordRequired = "Change Password";

    #endregion

    #region Private Fields

    private readonly IUserRepository __userRepository = userRepository;
    private readonly ICurrencyRepository __currencyRepository = currencyRepository;
    private readonly IPermissionRepository __permissionRepository = permissionRepository;
    private readonly ILocationRepository __locationRepository = locationRepository;
    private readonly UserSessionService __userSessionService = userSessionService;
    private readonly SystemAdminOptions __systemAdmin = systemAdminOptions.Value;
    private readonly ILogger<AuthService> __logger = logger;

    #endregion

    #region Public Methods

    /// <inheritdoc />
    public async Task<(bool Success, string Message)> ValidateCredentials(LoginDataTransferObject model,
                                                                          CancellationToken cancellationToken = default)
    {
        try
        {
            if (model is null || string.IsNullOrWhiteSpace(model.fstrUsername) || string.IsNullOrEmpty(model.fstrPassword))
                return (false, "Invalid login request");

            // 0. Built-in System Admin (configured on the server, not in the database) ------
            if (IsSystemAdminLogin(model.fstrUsername))
                return await ValidateSystemAdmin(model, cancellationToken);

            // 1. Find the account ------------------------------------------------------
            var users = await __userRepository.SelectUser(username: model.fstrUsername.Trim(), cancellationToken: cancellationToken);
            var user = users.FirstOrDefault(u => string.Equals(u.fstrUsername, model.fstrUsername.Trim(), StringComparison.OrdinalIgnoreCase));

            if (user is null || string.IsNullOrWhiteSpace(user.fstrUsername))
                return (false, "Username does not exist in the system / account is not active");

            // 2. Account state (active flag and validity window) -----------------------
            var stateError = ValidateAccountState(user);
            if (stateError is not null)
                return (false, stateError);

            // 3. Password ---------------------------------------------------------------
            if (string.IsNullOrWhiteSpace(user.fstrPassword))
                return (false, "User password is not configured. Please contact administrator.");

            var passwordResult = VerifyPassword(user, model.fstrPassword);
            if (passwordResult is not null)
                return (false, passwordResult);

            // 4. Supporting data (permissions / currency / location / role) -------------
            var permissionKeys = await __permissionRepository.SelectPermissionByUsername(
                user.fstrUsername, user.fstrLocationKey ?? string.Empty, cancellationToken);

            var currency = (await __currencyRepository.SelectCurrency(currency: user.fstrCurrencyKey, cancellationToken: cancellationToken))
                .FirstOrDefault();
            if (currency is null || string.IsNullOrWhiteSpace(currency.fstrCurrencyType))
                return (false, "Currency configuration missing.");

            var location = (await __locationRepository.SelectLocation(location: user.fstrLocationKey, cancellationToken: cancellationToken))
                .FirstOrDefault();
            if (location is null || string.IsNullOrWhiteSpace(location.fstrAddress))
                return (false, "Location configuration missing.");

            var role = StaticDataService.GetRoles().FirstOrDefault(r => r.Key == user.flngRoleKey);
            if (role is null)
                return (false, "User role configuration missing.");

            // 5. Build the principal ----------------------------------------------------
            __userSessionService.Principal = BuildPrincipal(user, currency, location, role.Name!, permissionKeys);

            return (true, "Successfully logged in!");
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            // SECURITY FIX: the stack trace used to be returned to the browser. It is now only logged.
            __logger.LogError(ex, "Login failed unexpectedly for user {Username}", model?.fstrUsername);
            return (false, "Unable to sign in right now. Please try again or contact the administrator.");
        }
    }

    #endregion

    #region System Admin

    /// <summary>True when the System Admin is switched on and the typed user name is its user name.</summary>
    private bool IsSystemAdminLogin(string username) =>
        __systemAdmin.Enabled
        && !string.IsNullOrWhiteSpace(__systemAdmin.Username)
        && !string.IsNullOrWhiteSpace(__systemAdmin.PasswordHash)
        && string.Equals(username.Trim(), __systemAdmin.Username.Trim(), StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Signs in the built-in System Admin. It has every permission. The password is checked against a
    /// BCrypt hash that only exists in the server settings, and every attempt is written to the log.
    /// </summary>
    private async Task<(bool Success, string Message)> ValidateSystemAdmin(LoginDataTransferObject model,
                                                                           CancellationToken cancellationToken)
    {
        bool passwordOk;
        try { passwordOk = BCrypt.Net.BCrypt.Verify(model.fstrPassword!.Trim(), __systemAdmin.PasswordHash!.Trim()); }
        catch (Exception) { passwordOk = false; }   // a malformed hash in the settings simply never matches

        if (!passwordOk)
        {
            __logger.LogWarning("System Admin sign-in FAILED for {Username}", __systemAdmin.Username);
            return (false, "Password verification failed.");
        }

        // Work in the configured location / currency, or the first active location. On an empty database
        // there may be none yet, so the account can still sign in and create them.
        string locationKey = __systemAdmin.LocationKey?.Trim() ?? string.Empty;
        string address = string.Empty;
        string currencyKey = __systemAdmin.CurrencyKey?.Trim() ?? string.Empty;
        string currencyType = string.Empty;
        decimal currencyRate = 1m;

        try
        {
            var locations = await __locationRepository.SelectLocation(cancellationToken: cancellationToken);
            var location = locationKey.Length > 0
                ? locations.FirstOrDefault(l => string.Equals(l.fstrLocation, locationKey, StringComparison.OrdinalIgnoreCase))
                : locations.FirstOrDefault(l => l.fblnActive) ?? locations.FirstOrDefault();

            if (location is not null)
            {
                locationKey = location.fstrLocation ?? locationKey;
                address = location.fstrAddress ?? string.Empty;

                var currencies = await __currencyRepository.SelectCurrency(location: locationKey, cancellationToken: cancellationToken);
                var currency = currencyKey.Length > 0
                    ? currencies.FirstOrDefault(c => string.Equals(c.fstrCurrency, currencyKey, StringComparison.OrdinalIgnoreCase))
                    : currencies.FirstOrDefault(c => c.fcurAmount == 1m) ?? currencies.FirstOrDefault();

                if (currency is not null)
                {
                    currencyKey = currency.fstrCurrency ?? currencyKey;
                    currencyType = currency.fstrCurrencyType ?? string.Empty;
                    currencyRate = currency.fcurAmount;
                }
            }
        }
        catch (OperationCanceledException) { throw; }
        catch (Exception ex)
        {
            // Never block the break-glass account because of a data problem: it is meant to fix those.
            __logger.LogWarning(ex, "System Admin signed in without location / currency details");
        }

        var name = string.IsNullOrWhiteSpace(__systemAdmin.FullName) ? "System Administrator" : __systemAdmin.FullName.Trim();

        var claims = new List<Claim>
        {
            new(PortalClaims.UserKey, Guid.Empty.ToString()),
            new(PortalClaims.FullName, name),
            new(PortalClaims.Email, string.Empty),
            new(PortalClaims.Role, "System Admin"),
            new(PortalClaims.ProfileInitials, name[..1].ToUpperInvariant()),
            new(PortalClaims.Username, __systemAdmin.Username!.Trim()),
            new(PortalClaims.LocationKey, locationKey),
            new(PortalClaims.CurrencyKey, currencyKey),
            new(PortalClaims.CurrencyType, currencyType),
            new(PortalClaims.CurrencyAmount, currencyRate.ToString(CultureInfo.InvariantCulture)),
            new(PortalClaims.Address, address),
            new(PortalClaims.SystemAdmin, "true")
        };

        // Every permission the application knows about.
        foreach (var permission in StaticDataService.GetPermissions().Where(p => !string.IsNullOrWhiteSpace(p.Name)))
            claims.Add(new Claim(PortalClaims.Permission, permission.Name!));

        __userSessionService.Principal = new ClaimsPrincipal(new ClaimsIdentity(claims, "Custom"));
        __logger.LogWarning("System Admin signed in: {Username}", __systemAdmin.Username);

        return (true, "Successfully logged in!");
    }

    #endregion

    #region Private Helpers

    /// <summary>Checks the active flag and the start/end validity window. Returns an error or null.</summary>
    private static string? ValidateAccountState(UserModel user)
    {
        // BUG FIX: the active flag and start date were never checked.
        if (!user.fblnActive)
            return "Your account is not active. Please contact the administrator.";

        var now = DateTime.UtcNow;

        if (user.fdtmStart != default && user.fdtmStart > now)
            return "Your account is not active yet";

        if (user.fdtmEnd != default && user.fdtmEnd < now)
            return "Your account has expired";

        return null;
    }

    /// <summary>
    /// Verifies the password. Returns <c>null</c> when the user may continue, otherwise the reason.
    /// </summary>
    /// <remarks>
    /// SECURITY FIX: previously ANY account with an unchanged password could be forced into the
    /// "change password" flow by typing the literal word "password" – without knowing the real
    /// temporary password. The typed value is now compared with the stored temporary password.
    /// </remarks>
    private static string? VerifyPassword(UserModel user, string typed)
    {
        var stored = user.fstrPassword!.Trim();

        if (!user.fblnPasswordChanged)
        {
            // Temporary password – stored as plain text by the admin screen (or as a hash).
            var matches = IsBCryptHash(stored)
                ? BCrypt.Net.BCrypt.Verify(typed.Trim(), stored)
                : FixedTimeEquals(typed.Trim(), stored);

            return matches ? ChangePasswordRequired : "Password verification failed.";
        }

        return BCrypt.Net.BCrypt.Verify(typed.Trim(), stored) ? null : "Password verification failed.";
    }

    private static bool IsBCryptHash(string value) =>
        value.StartsWith("$2", StringComparison.Ordinal) && value.Length >= 59;

    /// <summary>Compares two strings in constant time so timing cannot leak the temporary password.</summary>
    private static bool FixedTimeEquals(string a, string b) =>
        CryptographicOperations.FixedTimeEquals(Encoding.UTF8.GetBytes(a), Encoding.UTF8.GetBytes(b));

    /// <summary>Creates the claims principal stored in the user session.</summary>
    private ClaimsPrincipal BuildPrincipal(UserModel user,
                                           CurrencyModel currency,
                                           LocationModel location,
                                           string roleName,
                                           IEnumerable<string> permissionKeys)
    {
        var first = (user.fstrFirstname ?? string.Empty).Trim();
        var last = (user.fstrLastname ?? string.Empty).Trim();

        var claims = new List<Claim>
        {
            new(PortalClaims.UserKey, user.flngKey.ToString()),
            new(PortalClaims.FullName, $"{first} {last}".Trim()),
            new(PortalClaims.Email, user.fstrEmailAddress ?? string.Empty),
            new(PortalClaims.Role, roleName),
            // BUG FIX: Substring(0,1) threw for an empty first name.
            new(PortalClaims.ProfileInitials, first.Length > 0 ? first[..1].ToUpperInvariant() : "?"),
            new(PortalClaims.Username, user.fstrUsername!),
            new(PortalClaims.LocationKey, user.fstrLocationKey ?? string.Empty),
            new(PortalClaims.CurrencyKey, user.fstrCurrencyKey ?? string.Empty),
            new(PortalClaims.CurrencyType, currency.fstrCurrencyType!),
            // Invariant culture so a server with a "," decimal separator doesn't corrupt the rate.
            new(PortalClaims.CurrencyAmount, currency.fcurAmount.ToString(CultureInfo.InvariantCulture)),
            new(PortalClaims.Address, location.fstrAddress!)
        };

        var known = StaticDataService.GetPermissions();

        foreach (var key in permissionKeys.Distinct())
        {
            // BUG FIX: one unknown permission key used to abort the whole login. It is now skipped.
            var permission = known.FirstOrDefault(p => p.Key.ToString() == key);
            if (permission?.Name is null)
            {
                __logger.LogWarning("Unknown permission key {Key} ignored for user {Username}", key, user.fstrUsername);
                continue;
            }

            claims.Add(new Claim(PortalClaims.Permission, permission.Name));
        }

        return new ClaimsPrincipal(new ClaimsIdentity(claims, "Custom"));
    }

    #endregion
}
