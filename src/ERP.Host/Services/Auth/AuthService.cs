using ERP.Host.Models.Auth;
using ERP.SharedKernel.Contracts.Auth;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;

namespace ERP.Host.Services.Auth;

/// <summary>
/// Authentication service that wraps Identity and JWT functionality.
/// </summary>
public class AuthService : IAuthService
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly UserManager<IdentityUser> _userManager;
    private readonly SignInManager<IdentityUser> _signInManager;
    private readonly JwtAuthService _jwtAuthService;
    private readonly ILogger<AuthService> _logger;

    public AuthService(
        IHttpContextAccessor httpContextAccessor,
        UserManager<IdentityUser> userManager,
        SignInManager<IdentityUser> signInManager,
        JwtAuthService jwtAuthService,
        ILogger<AuthService> logger)
    {
        _httpContextAccessor = httpContextAccessor;
        _userManager = userManager;
        _signInManager = signInManager;
        _jwtAuthService = jwtAuthService;
        _logger = logger;
    }

    /// <summary>
    /// Gets the current user's ID.
    /// </summary>
    public string? CurrentUserId => _httpContextAccessor.HttpContext?.User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
        ?? _jwtAuthService.CurrentUserId;

    /// <summary>
    /// Gets the current user's name.
    /// </summary>
    public string? CurrentUserName => _httpContextAccessor.HttpContext?.User?.FindFirst(System.Security.Claims.ClaimTypes.Name)?.Value
        ?? _jwtAuthService.CurrentUserName;

    /// <summary>
    /// Gets the current user's email.
    /// </summary>
    public string? CurrentUserEmail => _httpContextAccessor.HttpContext?.User?.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value
        ?? _jwtAuthService.CurrentUserEmail;

    /// <summary>
    /// Gets whether the current user is authenticated.
    /// </summary>
    public bool IsAuthenticated => _httpContextAccessor.HttpContext?.User?.Identity?.IsAuthenticated ?? _jwtAuthService.IsAuthenticated;

    /// <summary>
    /// Gets the current user's roles.
    /// </summary>
    public IEnumerable<string> CurrentUserRoles => _jwtAuthService.CurrentUserRoles;

    /// <summary>
    /// Checks if the current user has the specified role.
    /// </summary>
    /// <param name="role">The role to check.</param>
    /// <returns>True if the user has the role.</returns>
    public bool IsInRole(string role)
    {
        return _jwtAuthService.IsInRole(role);
    }

    /// <summary>
    /// Checks if the current user has any of the specified roles.
    /// </summary>
    /// <param name="roles">The roles to check.</param>
    /// <returns>True if the user has any of the roles.</returns>
    public bool IsInAnyRole(params string[] roles)
    {
        return _jwtAuthService.IsInAnyRole(roles);
    }

    /// <summary>
    /// Logs in a user with username and password.
    /// </summary>
    /// <param name="email">The user's email.</param>
    /// <param name="password">The user's password.</param>
    /// <param name="rememberMe">Whether to remember the user.</param>
    /// <returns>AuthResponse with the result.</returns>
    public async Task<AuthResponse> LoginAsync(string email, string password, bool rememberMe = false)
    {
        try
        {
            var result = await _signInManager.PasswordSignInAsync(email, password, rememberMe, shouldLockout: true);

            if (result.Succeeded)
            {
                var user = await _userManager.FindByEmailAsync(email);
                if (user != null)
                {
                    var roles = await _userManager.GetRolesAsync(user);
                    var token = _jwtAuthService.GenerateToken(user.Id, user.UserName ?? email, email, roles);

                    return new AuthResponse
                    {
                        Success = true,
                        Token = token,
                        UserId = user.Id,
                        UserName = user.UserName,
                        Roles = roles
                    };
                }
            }

            return new AuthResponse
            {
                Success = false,
                ErrorMessage = result.IsLockedOut ? "Account locked out" : 
                              result.IsNotAllowed ? "Account not allowed" : 
                              "Invalid username or password"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during login for user {Email}", email);
            return new AuthResponse
            {
                Success = false,
                ErrorMessage = "An error occurred during login"
            };
        }
    }

    /// <summary>
    /// Registers a new user.
    /// </summary>
    /// <param name="request">The registration request.</param>
    /// <returns>AuthResponse with the result.</returns>
    public async Task<AuthResponse> RegisterAsync(RegisterRequest request)
    {
        try
        {
            if (request.Password != request.ConfirmPassword)
            {
                return new AuthResponse
                {
                    Success = false,
                    ErrorMessage = "Passwords do not match"
                };
            }

            var user = new IdentityUser
            {
                UserName = request.UserName,
                Email = request.Email
            };

            var result = await _userManager.CreateAsync(user, request.Password);

            if (result.Succeeded)
            {
                // Add user to default role if specified
                if (!string.IsNullOrEmpty(request.UserName))
                {
                    await _userManager.AddToRoleAsync(user, "User");
                }

                var token = _jwtAuthService.GenerateToken(user.Id, user.UserName ?? request.Email, request.Email, new[] { "User" });

                return new AuthResponse
                {
                    Success = true,
                    Token = token,
                    UserId = user.Id,
                    UserName = user.UserName,
                    Roles = new[] { "User" }
                };
            }

            return new AuthResponse
            {
                Success = false,
                ErrorMessage = string.Join(", ", result.Errors.Select(e => e.Description))
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during registration for user {Email}", request.Email);
            return new AuthResponse
            {
                Success = false,
                ErrorMessage = "An error occurred during registration"
            };
        }
    }

    /// <summary>
    /// Logs out the current user.
    /// </summary>
    /// <returns>A task representing the logout operation.</returns>
    public async Task LogoutAsync()
    {
        await _signInManager.SignOutAsync();
    }

    /// <summary>
    /// Gets the current user's information.
    /// </summary>
    /// <returns>UserInfo or null if not authenticated.</returns>
    public async Task<UserInfo?> GetCurrentUserAsync()
    {
        if (!IsAuthenticated)
        {
            return null;
        }

        var userId = CurrentUserId;
        if (string.IsNullOrEmpty(userId))
        {
            return null;
        }

        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
        {
            return null;
        }

        var roles = await _userManager.GetRolesAsync(user);

        return new UserInfo
        {
            Id = user.Id,
            UserName = user.UserName ?? string.Empty,
            Email = user.Email ?? string.Empty,
            Roles = roles
        };
    }

    /// <summary>
    /// Creates a user with the specified role.
    /// </summary>
    /// <param name="email">The user's email.</param>
    /// <param name="password">The user's password.</param>
    /// <param name="userName">The user's username.</param>
    /// <param name="role">The role to assign.</param>
    /// <returns>AuthResponse with the result.</returns>
    public async Task<AuthResponse> CreateUserWithRoleAsync(string email, string password, string userName, string role)
    {
        try
        {
            var user = new IdentityUser
            {
                UserName = userName,
                Email = email
            };

            var result = await _userManager.CreateAsync(user, password);

            if (result.Succeeded)
            {
                await _userManager.AddToRoleAsync(user, role);

                var roles = await _userManager.GetRolesAsync(user);
                var token = _jwtAuthService.GenerateToken(user.Id, user.UserName ?? email, email, roles);

                return new AuthResponse
                {
                    Success = true,
                    Token = token,
                    UserId = user.Id,
                    UserName = user.UserName,
                    Roles = roles
                };
            }

            return new AuthResponse
            {
                Success = false,
                ErrorMessage = string.Join(", ", result.Errors.Select(e => e.Description))
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating user {UserName} with role {Role}", userName, role);
            return new AuthResponse
            {
                Success = false,
                ErrorMessage = "An error occurred while creating the user"
            };
        }
    }
}
