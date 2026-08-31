using System;

namespace ERP.SharedKernel.Contracts.Auth;

/// <summary>
/// Attribute to specify required permissions for a plugin feature.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true, Inherited = true)]
public class RequirePermissionAttribute : Attribute
{
    /// <summary>
    /// Gets the required permission.
    /// </summary>
    public string Permission { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="RequirePermissionAttribute"/> class.
    /// </summary>
    /// <param name="permission">The required permission.</param>
    public RequirePermissionAttribute(string permission)
    {
        Permission = permission;
    }
}

/// <summary>
/// Attribute to specify required roles for a plugin feature.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true, Inherited = true)]
public class RequireRoleAttribute : Attribute
{
    /// <summary>
    /// Gets the required roles.
    /// </summary>
    public string[] Roles { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="RequireRoleAttribute"/> class.
    /// </summary>
    /// <param name="roles">The required roles.</param>
    public RequireRoleAttribute(params string[] roles)
    {
        Roles = roles;
    }
}
