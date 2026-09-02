namespace ERP.SharedKernel.Contracts.Auth;

/// <summary>
/// Interface for authentication services that plugins can use.
/// </summary>
public interface IAuthService
{
    /// <summary>
    /// Gets the current user's ID.
    /// </summary>
    string? CurrentUserId { get; }

    /// <summary>
    /// Gets the current user's name.
    /// </summary>
    string? CurrentUserName { get; }

    /// <summary>
    /// Gets the current user's email.
    /// </summary>
    string? CurrentUserEmail { get; }

    /// <summary>
    /// Gets whether the current user is authenticated.
    /// </summary>
    bool IsAuthenticated { get; }

    /// <summary>
    /// Gets the current user's roles.
    /// </summary>
    IEnumerable<string> CurrentUserRoles { get; }

    /// <summary>
    /// Checks if the current user has the specified role.
    /// </summary>
    /// <param name="role">The role to check.</param>
    /// <returns>True if the user has the role.</returns>
    bool IsInRole(string role);

    /// <summary>
    /// Checks if the current user has any of the specified roles.
    /// </summary>
    /// <param name="roles">The roles to check.</param>
    /// <returns>True if the user has any of the roles.</returns>
    bool IsInAnyRole(params string[] roles);
}
