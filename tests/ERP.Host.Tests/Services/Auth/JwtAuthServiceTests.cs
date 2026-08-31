using System;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using ERP.Host.Models.Auth;
using ERP.Host.Services.Auth;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Moq;
using Xunit;

namespace ERP.Host.Tests.Services.Auth;

/// <summary>
/// Unit tests for JwtAuthService.
/// </summary>
public class JwtAuthServiceTests : IDisposable
{
    private readonly Mock<IHttpContextAccessor> _httpContextAccessorMock;
    private readonly JwtSettings _jwtSettings;
    private readonly JwtAuthService _jwtAuthService;

    public JwtAuthServiceTests()
    {
        _httpContextAccessorMock = new Mock<IHttpContextAccessor>();
        _jwtSettings = new JwtSettings
        {
            Key = "ThisIsMySuperSecretKeyForJwtTokens1234567890",
            Issuer = "ErpLight",
            Audience = "ErpLightClient",
            ExpireMinutes = 60
        };
        
        var optionsMock = new Mock<IOptions<JwtSettings>>();
        optionsMock.Setup(x => x.Value).Returns(_jwtSettings);
        
        _jwtAuthService = new JwtAuthService(_httpContextAccessorMock.Object, optionsMock.Object);
    }

    public void Dispose()
    {
        _httpContextAccessorMock?.Invoke();
    }

    [Fact]
    public void CurrentUserId_ShouldReturnNull_WhenNoHttpContext()
    {
        // Arrange
        _httpContextAccessorMock.Setup(x => x.HttpContext).Returns((HttpContext?)null);

        // Act
        var userId = _jwtAuthService.CurrentUserId;

        // Assert
        Assert.Null(userId);
    }

    [Fact]
    public void CurrentUserId_ShouldReturnClaimValue_WhenHttpContextHasClaim()
    {
        // Arrange
        var httpContext = new DefaultHttpContext();
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, "test-user-id")
        };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var principal = new ClaimsPrincipal(identity);
        httpContext.User = principal;
        
        _httpContextAccessorMock.Setup(x => x.HttpContext).Returns(httpContext);

        // Act
        var userId = _jwtAuthService.CurrentUserId;

        // Assert
        Assert.Equal("test-user-id", userId);
    }

    [Fact]
    public void CurrentUserName_ShouldReturnClaimValue_WhenHttpContextHasClaim()
    {
        // Arrange
        var httpContext = new DefaultHttpContext();
        var claims = new[]
        {
            new Claim(ClaimTypes.Name, "test-username")
        };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var principal = new ClaimsPrincipal(identity);
        httpContext.User = principal;
        
        _httpContextAccessorMock.Setup(x => x.HttpContext).Returns(httpContext);

        // Act
        var userName = _jwtAuthService.CurrentUserName;

        // Assert
        Assert.Equal("test-username", userName);
    }

    [Fact]
    public void CurrentUserEmail_ShouldReturnClaimValue_WhenHttpContextHasClaim()
    {
        // Arrange
        var httpContext = new DefaultHttpContext();
        var claims = new[]
        {
            new Claim(ClaimTypes.Email, "test@email.com")
        };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var principal = new ClaimsPrincipal(identity);
        httpContext.User = principal;
        
        _httpContextAccessorMock.Setup(x => x.HttpContext).Returns(httpContext);

        // Act
        var email = _jwtAuthService.CurrentUserEmail;

        // Assert
        Assert.Equal("test@email.com", email);
    }

    [Fact]
    public void IsAuthenticated_ShouldReturnFalse_WhenNoHttpContext()
    {
        // Arrange
        _httpContextAccessorMock.Setup(x => x.HttpContext).Returns((HttpContext?)null);

        // Act
        var isAuthenticated = _jwtAuthService.IsAuthenticated;

        // Assert
        Assert.False(isAuthenticated);
    }

    [Fact]
    public void IsAuthenticated_ShouldReturnFalse_WhenUserNotAuthenticated()
    {
        // Arrange
        var httpContext = new DefaultHttpContext();
        var identity = new ClaimsIdentity("TestAuth");
        var principal = new ClaimsPrincipal(identity);
        httpContext.User = principal;
        
        _httpContextAccessorMock.Setup(x => x.HttpContext).Returns(httpContext);

        // Act
        var isAuthenticated = _jwtAuthService.IsAuthenticated;

        // Assert
        Assert.False(isAuthenticated);
    }

    [Fact]
    public void IsAuthenticated_ShouldReturnTrue_WhenUserAuthenticated()
    {
        // Arrange
        var httpContext = new DefaultHttpContext();
        var claims = new[]
        {
            new Claim(ClaimTypes.Name, "test-username")
        };
        var identity = new ClaimsIdentity(claims, "TestAuth") { IsAuthenticated = true };
        var principal = new ClaimsPrincipal(identity);
        httpContext.User = principal;
        
        _httpContextAccessorMock.Setup(x => x.HttpContext).Returns(httpContext);

        // Act
        var isAuthenticated = _jwtAuthService.IsAuthenticated;

        // Assert
        Assert.True(isAuthenticated);
    }

    [Fact]
    public void CurrentUserRoles_ShouldReturnEmpty_WhenNoRoles()
    {
        // Arrange
        var httpContext = new DefaultHttpContext();
        var claims = new[]
        {
            new Claim(ClaimTypes.Name, "test-username")
        };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var principal = new ClaimsPrincipal(identity);
        httpContext.User = principal;
        
        _httpContextAccessorMock.Setup(x => x.HttpContext).Returns(httpContext);

        // Act
        var roles = _jwtAuthService.CurrentUserRoles;

        // Assert
        Assert.Empty(roles);
    }

    [Fact]
    public void CurrentUserRoles_ShouldReturnRoles_WhenUserHasRoles()
    {
        // Arrange
        var httpContext = new DefaultHttpContext();
        var claims = new[]
        {
            new Claim(ClaimTypes.Name, "test-username"),
            new Claim(ClaimTypes.Role, "Admin"),
            new Claim(ClaimTypes.Role, "User"),
            new Claim("role", "Manager")
        };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var principal = new ClaimsPrincipal(identity);
        httpContext.User = principal;
        
        _httpContextAccessorMock.Setup(x => x.HttpContext).Returns(httpContext);

        // Act
        var roles = _jwtAuthService.CurrentUserRoles;

        // Assert
        Assert.Contains("Admin", roles);
        Assert.Contains("User", roles);
        Assert.Contains("Manager", roles);
    }

    [Fact]
    public void IsInRole_ShouldReturnFalse_WhenUserDoesNotHaveRole()
    {
        // Arrange
        var httpContext = new DefaultHttpContext();
        var claims = new[]
        {
            new Claim(ClaimTypes.Name, "test-username"),
            new Claim(ClaimTypes.Role, "User")
        };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var principal = new ClaimsPrincipal(identity);
        httpContext.User = principal;
        
        _httpContextAccessorMock.Setup(x => x.HttpContext).Returns(httpContext);

        // Act
        var isInRole = _jwtAuthService.IsInRole("Admin");

        // Assert
        Assert.False(isInRole);
    }

    [Fact]
    public void IsInRole_ShouldReturnTrue_WhenUserHasRole()
    {
        // Arrange
        var httpContext = new DefaultHttpContext();
        var claims = new[]
        {
            new Claim(ClaimTypes.Name, "test-username"),
            new Claim(ClaimTypes.Role, "Admin"),
            new Claim(ClaimTypes.Role, "User")
        };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var principal = new ClaimsPrincipal(identity);
        httpContext.User = principal;
        
        _httpContextAccessorMock.Setup(x => x.HttpContext).Returns(httpContext);

        // Act
        var isInRole = _jwtAuthService.IsInRole("Admin");

        // Assert
        Assert.True(isInRole);
    }

    [Fact]
    public void IsInAnyRole_ShouldReturnFalse_WhenUserHasNoRoles()
    {
        // Arrange
        var httpContext = new DefaultHttpContext();
        var claims = new[]
        {
            new Claim(ClaimTypes.Name, "test-username")
        };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var principal = new ClaimsPrincipal(identity);
        httpContext.User = principal;
        
        _httpContextAccessorMock.Setup(x => x.HttpContext).Returns(httpContext);

        // Act
        var isInAnyRole = _jwtAuthService.IsInAnyRole("Admin", "User");

        // Assert
        Assert.False(isInAnyRole);
    }

    [Fact]
    public void IsInAnyRole_ShouldReturnTrue_WhenUserHasAnyRole()
    {
        // Arrange
        var httpContext = new DefaultHttpContext();
        var claims = new[]
        {
            new Claim(ClaimTypes.Name, "test-username"),
            new Claim(ClaimTypes.Role, "User")
        };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var principal = new ClaimsPrincipal(identity);
        httpContext.User = principal;
        
        _httpContextAccessorMock.Setup(x => x.HttpContext).Returns(httpContext);

        // Act
        var isInAnyRole = _jwtAuthService.IsInAnyRole("Admin", "User", "Manager");

        // Assert
        Assert.True(isInAnyRole);
    }

    [Fact]
    public void GenerateToken_ShouldCreateValidJwtToken()
    {
        // Arrange
        var userId = "1";
        var userName = "testuser";
        var email = "test@email.com";
        var roles = new[] { "Admin", "User" };

        // Act
        var token = _jwtAuthService.GenerateToken(userId, userName, email, roles);

        // Assert
        Assert.NotNull(token);
        Assert.NotEmpty(token);
    }

    [Fact]
    public void GenerateToken_ShouldContainRequiredClaims()
    {
        // Arrange
        var userId = "1";
        var userName = "testuser";
        var email = "test@email.com";
        var roles = new[] { "Admin", "User" };

        // Act
        var token = _jwtAuthService.GenerateToken(userId, userName, email, roles);
        
        var tokenHandler = new JwtSecurityTokenHandler();
        var jwtToken = tokenHandler.ReadJwtToken(token);

        // Assert
        Assert.Equal(userId, jwtToken.Subject);
        Assert.Equal(userName, jwtToken.Claims.First(c => c.Type == ClaimTypes.Name).Value);
        Assert.Equal(email, jwtToken.Claims.First(c => c.Type == ClaimTypes.Email).Value);
        Assert.Equal(email, jwtToken.Claims.First(c => c.Type == JwtRegisteredClaimNames.Email).Value);
        
        // Check roles
        var roleClaims = jwtToken.Claims.Where(c => c.Type == ClaimTypes.Role).ToList();
        Assert.Equal(2, roleClaims.Count);
        Assert.Contains("Admin", roleClaims.Select(c => c.Value));
        Assert.Contains("User", roleClaims.Select(c => c.Value));
    }

    [Fact]
    public void GenerateToken_ShouldHaveCorrectIssuerAndAudience()
    {
        // Arrange
        var userId = "1";
        var userName = "testuser";
        var email = "test@email.com";
        var roles = new[] { "Admin" };

        // Act
        var token = _jwtAuthService.GenerateToken(userId, userName, email, roles);
        
        var tokenHandler = new JwtSecurityTokenHandler();
        var jwtToken = tokenHandler.ReadJwtToken(token);

        // Assert
        Assert.Equal(_jwtSettings.Issuer, jwtToken.Issuer);
        Assert.Equal(_jwtSettings.Audience, jwtToken.Audiences.First());
    }

    [Fact]
    public void GenerateToken_ShouldExpireInConfiguredTime()
    {
        // Arrange
        var userId = "1";
        var userName = "testuser";
        var email = "test@email.com";
        var roles = new[] { "Admin" };

        // Act
        var token = _jwtAuthService.GenerateToken(userId, userName, email, roles);
        
        var tokenHandler = new JwtSecurityTokenHandler();
        var jwtToken = tokenHandler.ReadJwtToken(token);

        // Assert
        var expectedExpiry = DateTime.UtcNow.AddMinutes(_jwtSettings.ExpireMinutes);
        var expiryTolerance = TimeSpan.FromSeconds(1); // Allow 1 second tolerance
        
        Assert.InRange(jwtToken.ValidTo, expectedExpiry.Add(-expiryTolerance), expectedExpiry.Add(expiryTolerance));
    }

    [Fact]
    public void ValidateToken_ShouldReturnTrue_ForValidToken()
    {
        // Arrange
        var userId = "1";
        var userName = "testuser";
        var email = "test@email.com";
        var roles = new[] { "Admin" };

        var token = _jwtAuthService.GenerateToken(userId, userName, email, roles);

        // Act
        var isValid = _jwtAuthService.ValidateToken(token);

        // Assert
        Assert.True(isValid);
    }

    [Fact]
    public void ValidateToken_ShouldReturnFalse_ForInvalidToken()
    {
        // Arrange
        var invalidToken = "invalid.token.string";

        // Act
        var isValid = _jwtAuthService.ValidateToken(invalidToken);

        // Assert
        Assert.False(isValid);
    }

    [Fact]
    public void ValidateToken_ShouldReturnFalse_ForTokenWithWrongKey()
    {
        // Arrange - Create a token with a different key
        var differentSettings = new JwtSettings
        {
            Key = "DifferentSecretKeyForJwtTokens0000000000",
            Issuer = "ErpLight",
            Audience = "ErpLightClient",
            ExpireMinutes = 60
        };
        
        var differentTokenHandler = new JwtSecurityTokenHandler();
        var differentKey = Encoding.ASCII.GetBytes(differentSettings.Key);
        
        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.NameIdentifier, "1"),
                new Claim(ClaimTypes.Name, "testuser")
            }),
            Expires = DateTime.UtcNow.AddMinutes(60),
            SigningCredentials = new SigningCredentials(
                new SymmetricSecurityKey(differentKey),
                SecurityAlgorithms.HmacSha256Signature),
            Issuer = differentSettings.Issuer,
            Audience = differentSettings.Audience
        };
        
        var differentToken = differentTokenHandler.WriteToken(differentTokenHandler.CreateToken(tokenDescriptor));

        // Act
        var isValid = _jwtAuthService.ValidateToken(differentToken);

        // Assert
        Assert.False(isValid);
    }

    [Fact]
    public void ValidateToken_ShouldReturnFalse_ForExpiredToken()
    {
        // Arrange - Create a token that expired in the past
        var differentSettings = new JwtSettings
        {
            Key = "ThisIsMySuperSecretKeyForJwtTokens1234567890",
            Issuer = "ErpLight",
            Audience = "ErpLightClient",
            ExpireMinutes = -60 // Expired 60 minutes ago
        };
        
        var tokenHandler = new JwtSecurityTokenHandler();
        var key = Encoding.ASCII.GetBytes(differentSettings.Key);
        
        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.NameIdentifier, "1"),
                new Claim(ClaimTypes.Name, "testuser")
            }),
            Expires = DateTime.UtcNow.AddMinutes(-60),
            SigningCredentials = new SigningCredentials(
                new SymmetricSecurityKey(key),
                SecurityAlgorithms.HmacSha256Signature),
            Issuer = differentSettings.Issuer,
            Audience = differentSettings.Audience
        };
        
        var expiredToken = tokenHandler.WriteToken(tokenHandler.CreateToken(tokenDescriptor));

        // Act
        var isValid = _jwtAuthService.ValidateToken(expiredToken);

        // Assert
        Assert.False(isValid);
    }

    [Fact]
    public void GetJwtBearerOptions_ShouldReturnCorrectOptions()
    {
        // Act
        var options = JwtAuthService.GetJwtBearerOptions(_jwtSettings);

        // Assert
        Assert.NotNull(options);
        Assert.NotNull(options.TokenValidationParameters);
        Assert.True(options.TokenValidationParameters.ValidateIssuerSigningKey);
        Assert.True(options.TokenValidationParameters.ValidateIssuer);
        Assert.True(options.TokenValidationParameters.ValidateAudience);
        Assert.True(options.TokenValidationParameters.ValidateLifetime);
        Assert.Equal(_jwtSettings.Issuer, options.TokenValidationParameters.ValidIssuer);
        Assert.Equal(_jwtSettings.Audience, options.TokenValidationParameters.ValidAudience);
    }
}
