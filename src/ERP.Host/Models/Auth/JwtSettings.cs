namespace ERP.Host.Models.Auth;

/// <summary>
/// Configuration settings for JWT authentication.
/// </summary>
public class JwtSettings
{
    /// <summary>
    /// Gets or sets the secret key for JWT token signing.
    /// Must be at least 32 characters long.
    /// </summary>
    public string Key { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the issuer of the JWT tokens.
    /// </summary>
    public string Issuer { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the audience for the JWT tokens.
    /// </summary>
    public string Audience { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the expiration time in minutes.
    /// </summary>
    public int ExpireMinutes { get; set; } = 60;
}

/// <summary>
/// User authentication request model.
/// </summary>
public class LoginRequest
{
    /// <summary>
    /// Gets or sets the email address.
    /// </summary>
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the password.
    /// </summary>
    public string Password { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets whether to remember the user.
    /// </summary>
    public bool RememberMe { get; set; }
}

/// <summary>
/// User registration request model.
/// </summary>
public class RegisterRequest
{
    /// <summary>
    /// Gets or sets the email address.
    /// </summary>
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the password.
    /// </summary>
    public string Password { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the confirm password.
    /// </summary>
    public string ConfirmPassword { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the user name.
    /// </summary>
    public string UserName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the first name.
    /// </summary>
    public string? FirstName { get; set; }

    /// <summary>
    /// Gets or sets the last name.
    /// </summary>
    public string? LastName { get; set; }
}

/// <summary>
/// Authentication response model.
/// </summary>
public class AuthResponse
{
    /// <summary>
    /// Gets or sets whether authentication was successful.
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Gets or sets the JWT token.
    /// </summary>
    public string? Token { get; set; }

    /// <summary>
    /// Gets or sets the token expiration time.
    /// </summary>
    public DateTime? Expires { get; set; }

    /// <summary>
    /// Gets or sets the user ID.
    /// </summary>
    public string? UserId { get; set; }

    /// <summary>
    /// Gets or sets the user name.
    /// </summary>
    public string? UserName { get; set; }

    /// <summary>
    /// Gets or sets the user roles.
    /// </summary>
    public IEnumerable<string>? Roles { get; set; }

    /// <summary>
    /// Gets or sets the error message if authentication failed.
    /// </summary>
    public string? ErrorMessage { get; set; }
}

/// <summary>
/// User information model.
/// </summary>
public class UserInfo
{
    /// <summary>
    /// Gets or sets the user ID.
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the user name.
    /// </summary>
    public string UserName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the email address.
    /// </summary>
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the first name.
    /// </summary>
    public string? FirstName { get; set; }

    /// <summary>
    /// Gets or sets the last name.
    /// </summary>
    public string? LastName { get; set; }

    /// <summary>
    /// Gets or sets the user roles.
    /// </summary>
    public IEnumerable<string> Roles { get; set; } = new List<string>();
}
