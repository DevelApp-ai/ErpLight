using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using ERP.Host.Models.Auth;
using ERP.SharedKernel.Contracts.Auth;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace ERP.Host.Services.Auth;

/// <summary>
/// JWT-based authentication service implementation.
/// </summary>
public class JwtAuthService : IAuthService
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly JwtSettings _jwtSettings;

    public JwtAuthService(IHttpContextAccessor httpContextAccessor, IOptions<JwtSettings> jwtSettings)
    {
        _httpContextAccessor = httpContextAccessor;
        _jwtSettings = jwtSettings.Value;
    }

    /// <summary>
    /// Gets the current user's ID from the JWT token.
    /// </summary>
    public string? CurrentUserId => GetClaim(ClaimTypes.NameIdentifier);

    /// <summary>
    /// Gets the current user's name from the JWT token.
    /// </summary>
    public string? CurrentUserName => GetClaim(ClaimTypes.Name);

    /// <summary>
    /// Gets the current user's email from the JWT token.
    /// </summary>
    public string? CurrentUserEmail => GetClaim(ClaimTypes.Email);

    /// <summary>
    /// Gets whether the current user is authenticated.
    /// </summary>
    public bool IsAuthenticated => _httpContextAccessor.HttpContext?.User?.Identity?.IsAuthenticated ?? false;

    /// <summary>
    /// Gets the current user's roles from the JWT token.
    /// </summary>
    public IEnumerable<string> CurrentUserRoles => 
        _httpContextAccessor.HttpContext?.User?.Claims
            ?.Where(c => c.Type == ClaimTypes.Role || c.Type == "role")
            ?.Select(c => c.Value)
        ?? Enumerable.Empty<string>();

    /// <summary>
    /// Checks if the current user has the specified role.
    /// </summary>
    /// <param name="role">The role to check.</param>
    /// <returns>True if the user has the role.</returns>
    public bool IsInRole(string role)
    {
        return CurrentUserRoles.Contains(role);
    }

    /// <summary>
    /// Checks if the current user has any of the specified roles.
    /// </summary>
    /// <param name="roles">The roles to check.</param>
    /// <returns>True if the user has any of the roles.</returns>
    public bool IsInAnyRole(params string[] roles)
    {
        return roles.Any(IsInRole);
    }

    /// <summary>
    /// Generates a JWT token for the specified user.
    /// </summary>
    /// <param name="userId">The user ID.</param>
    /// <param name="userName">The user name.</param>
    /// <param name="email">The user email.</param>
    /// <param name="roles">The user roles.</param>
    /// <returns>A JWT token string.</returns>
    public string GenerateToken(string userId, string userName, string email, IEnumerable<string> roles)
    {
        var tokenHandler = new JwtSecurityTokenHandler();
        var key = Encoding.ASCII.GetBytes(_jwtSettings.Key);

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.NameIdentifier, userId),
                new Claim(ClaimTypes.Name, userName),
                new Claim(ClaimTypes.Email, email),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                new Claim(JwtRegisteredClaimNames.Sub, userId),
                new Claim(JwtRegisteredClaimNames.Email, email),
            }),
            Expires = DateTime.UtcNow.AddMinutes(_jwtSettings.ExpireMinutes),
            SigningCredentials = new SigningCredentials(
                new SymmetricSecurityKey(key),
                SecurityAlgorithms.HmacSha256Signature),
            Issuer = _jwtSettings.Issuer,
            Audience = _jwtSettings.Audience
        };

        // Add roles as multiple claims
        foreach (var role in roles)
        {
            tokenDescriptor.Subject.AddClaim(new Claim(ClaimTypes.Role, role));
        }

        var token = tokenHandler.CreateToken(tokenDescriptor);
        return tokenHandler.WriteToken(token);
    }

    /// <summary>
    /// Validates a JWT token.
    /// </summary>
    /// <param name="token">The JWT token to validate.</param>
    /// <returns>True if the token is valid.</returns>
    public bool ValidateToken(string token)
    {
        var tokenHandler = new JwtSecurityTokenHandler();
        var key = Encoding.ASCII.GetBytes(_jwtSettings.Key);

        try
        {
            tokenHandler.ValidateToken(token, new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(key),
                ValidateIssuer = true,
                ValidIssuer = _jwtSettings.Issuer,
                ValidateAudience = true,
                ValidAudience = _jwtSettings.Audience,
                ValidateLifetime = true,
                ClockSkew = TimeSpan.Zero
            }, out _);

            return true;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Gets the current user's ID.
    /// </summary>
    /// <param name="claimType">The claim type.</param>
    /// <returns>The claim value or null.</returns>
    private string? GetClaim(string claimType)
    {
        return _httpContextAccessor.HttpContext?.User?.FindFirst(claimType)?.Value
            ?? _httpContextAccessor.HttpContext?.User?.FindFirst(c => c.Type.EndsWith(claimType.Split('/').Last()))?.Value;
    }

    /// <summary>
    /// Gets the authentication scheme configuration.
    /// </summary>
    /// <returns>JwtBearerOptions.</returns>
    public static JwtBearerOptions GetJwtBearerOptions(JwtSettings jwtSettings)
    {
        return new JwtBearerOptions
        {
            TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.ASCII.GetBytes(jwtSettings.Key)),
                ValidateIssuer = true,
                ValidIssuer = jwtSettings.Issuer,
                ValidateAudience = true,
                ValidAudience = jwtSettings.Audience,
                ValidateLifetime = true,
                ClockSkew = TimeSpan.Zero
            }
        };
    }
}
