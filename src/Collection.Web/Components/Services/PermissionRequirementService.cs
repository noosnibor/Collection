using Microsoft.AspNetCore.Authorization;

namespace Collection.Web.Components.Services;

/// <summary>An authorisation requirement that demands a named permission claim.</summary>
public class PermissionRequirementService(string permission) : IAuthorizationRequirement
{
    /// <summary>The permission name (e.g. <c>Currency.View</c>).</summary>
    public string Permission { get; } = permission;
}
