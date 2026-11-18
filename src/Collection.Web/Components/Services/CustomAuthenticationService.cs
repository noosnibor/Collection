using Collection.Library.Service;
using Microsoft.AspNetCore.Components.Authorization;
using System.Security.Claims;

namespace Collection.Web.Components.Services;

/// <summary>
/// Authentication state provider backed by <see cref="UserSessionService"/>.
/// The signed-in principal lives in the Blazor circuit, so it disappears when the circuit ends.
/// </summary>
public class CustomAuthenticationService(UserSessionService session) : AuthenticationStateProvider
{
    #region Private Fields

    private readonly UserSessionService __session = session;

    /// <summary>The principal used for signed-out users.</summary>
    private static readonly ClaimsPrincipal __anonymous = new(new ClaimsIdentity());

    #endregion

    #region AuthenticationStateProvider

    /// <inheritdoc />
    public override Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        var principal = __session.IsAuthenticated ? __session.Principal! : __anonymous;
        return Task.FromResult(new AuthenticationState(principal));
    }

    #endregion

    #region Notifications

    /// <summary>Tells the framework a user has just signed in so authorised views refresh.</summary>
    public void NotifyUserAuthentication(ClaimsPrincipal principal)
    {
        __session.Principal = principal;
        NotifyAuthenticationStateChanged(Task.FromResult(new AuthenticationState(principal)));
    }

    /// <summary>Signs the user out and refreshes every authorised view.</summary>
    public void NotifyUserLogout()
    {
        __session.Principal = __anonymous;
        NotifyAuthenticationStateChanged(Task.FromResult(new AuthenticationState(__anonymous)));
    }

    #endregion
}
