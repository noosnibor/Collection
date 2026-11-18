using Collection.Library.Service;
using Microsoft.AspNetCore.Authorization;

namespace Collection.Web.Components.Services;

/// <summary>Succeeds when the signed-in user holds the permission claim being demanded.</summary>
public class PermissionAuthorizarionHandlerService : AuthorizationHandler<PermissionRequirementService>
{
    /// <inheritdoc />
    protected override Task HandleRequirementAsync(AuthorizationHandlerContext context,
                                                   PermissionRequirementService requirement)
    {
        if (context.User.HasClaim(PortalClaims.Permission, requirement.Permission))
            context.Succeed(requirement);

        return Task.CompletedTask;
    }
}
