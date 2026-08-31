using ERP.Host.Models.Auth;
using ERP.Host.Services.Auth;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace ERP.Host.Tests.Services.Auth;

/// <summary>
/// Unit tests for AuthService.
/// </summary>
public class AuthServiceTests : IDisposable
{
    private readonly Mock<UserManager<IdentityUser>> _userManagerMock;
    private readonly Mock<SignInManager<IdentityUser>> _signInManagerMock;
    private readonly Mock<JwtAuthService> _jwtAuthServiceMock;
    private readonly Mock<IHttpContextAccessor> _httpContextAccessorMock;
    private readonly AuthService _authService;
    private readonly ILogger<AuthService> _logger;

    public AuthServiceTests()
    {
        _userManagerMock = new Mock<UserManager<IdentityUser>>(
            new Mock<IUserStore<IdentityUser>>().Object,
            null!, null!, null!, null!, null!, null!, null!, null!);
        
        _signInManagerMock = new Mock<SignInManager<IdentityUser>>(
            _userManagerMock.Object,
            new Mock<IHttpContextAccessor>().Object,
            new Mock<IUserClaimsPrincipalFactory<IdentityUser>>().Object,
            null!, null!, null!, null!);
        
        _jwtAuthServiceMock = new Mock<JwtAuthService>(
            new Mock<IHttpContextAccessor>().Object,
            new Mock<IOptions<JwtSettings>>().Object);
        
        _httpContextAccessorMock = new Mock<IHttpContextAccessor>();
        _logger = NullLogger<AuthService>.Instance;
        
        _authService = new AuthService(
            _httpContextAccessorMock.Object,
            _userManagerMock.Object,
            _signInManagerMock.Object,
            _jwtAuthServiceMock.Object,
            _logger);
    }

    public void Dispose()
    {
        _userManagerMock?.Invoke();
        _signInManagerMock?.Invoke();
        _jwtAuthServiceMock?.Invoke();
    }

    [Fact]
    public void CurrentUserId_ShouldReturnJwtUserId_WhenNoHttpContext()
    {
        // Arrange
        _httpContextAccessorMock.Setup(x => x.HttpContext).Returns((HttpContext?)null);
        _jwtAuthServiceMock.Setup(x => x.CurrentUserId).Returns("jwt-user-id");

        // Act
        var userId = _authService.CurrentUserId;

        // Assert
        Assert.Equal("jwt-user-id", userId);
    }

    [Fact]
    public void CurrentUserId_ShouldReturnHttpContextUserId_WhenAvailable()
    {
        // Arrange
        var httpContext = new DefaultHttpContext();
        var claimsPrincipal = new System.Security.Claims.ClaimsPrincipal(new System.Security.Claims.ClaimsIdentity(new[]
        {
            new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.NameIdentifier, "http-user-id")
        }, "TestAuth"));
        httpContext.User = claimsPrincipal;
        
        _httpContextAccessorMock.Setup(x => x.HttpContext).Returns(httpContext);
        _jwtAuthServiceMock.Setup(x => x.CurrentUserId).Returns("jwt-user-id");

        // Act
        var userId = _authService.CurrentUserId;

        // Assert
        Assert.Equal("http-user-id", userId);
    }

    [Fact]
    public void CurrentUserName_ShouldReturnJwtUserName_WhenNoHttpContext()
    {
        // Arrange
        _httpContextAccessorMock.Setup(x => x.HttpContext).Returns((HttpContext?)null);
        _jwtAuthServiceMock.Setup(x => x.CurrentUserName).Returns("jwt-username");

        // Act
        var userName = _authService.CurrentUserName;

        // Assert
        Assert.Equal("jwt-username", userName);
    }

    [Fact]
    public void CurrentUserEmail_ShouldReturnJwtUserEmail_WhenNoHttpContext()
    {
        // Arrange
        _httpContextAccessorMock.Setup(x => x.HttpContext).Returns((HttpContext?)null);
        _jwtAuthServiceMock.Setup(x => x.CurrentUserEmail).Returns("jwt@email.com");

        // Act
        var email = _authService.CurrentUserEmail;

        // Assert
        Assert.Equal("jwt@email.com", email);
    }

    [Fact]
    public void IsAuthenticated_ShouldReturnFalse_WhenNoHttpContextAndJwtNotAuthenticated()
    {
        // Arrange
        _httpContextAccessorMock.Setup(x => x.HttpContext).Returns((HttpContext?)null);
        _jwtAuthServiceMock.Setup(x => x.IsAuthenticated).Returns(false);

        // Act
        var isAuthenticated = _authService.IsAuthenticated;

        // Assert
        Assert.False(isAuthenticated);
    }

    [Fact]
    public void IsAuthenticated_ShouldReturnTrue_WhenHttpContextAuthenticated()
    {
        // Arrange
        var httpContext = new DefaultHttpContext();
        var claimsPrincipal = new System.Security.Claims.ClaimsPrincipal(new System.Security.Claims.ClaimsIdentity(new[]
        {
            new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.Name, "testuser")
        }, "TestAuth") { IsAuthenticated = true });
        httpContext.User = claimsPrincipal;
        
        _httpContextAccessorMock.Setup(x => x.HttpContext).Returns(httpContext);
        _jwtAuthServiceMock.Setup(x => x.IsAuthenticated).Returns(false);

        // Act
        var isAuthenticated = _authService.IsAuthenticated;

        // Assert
        Assert.True(isAuthenticated);
    }

    [Fact]
    public void CurrentUserRoles_ShouldReturnJwtRoles()
    {
        // Arrange
        _jwtAuthServiceMock.Setup(x => x.CurrentUserRoles).Returns(new[] { "Admin", "User" });

        // Act
        var roles = _authService.CurrentUserRoles;

        // Assert
        Assert.Contains("Admin", roles);
        Assert.Contains("User", roles);
    }

    [Fact]
    public void IsInRole_ShouldReturnTrue_WhenUserHasRole()
    {
        // Arrange
        _jwtAuthServiceMock.Setup(x => x.IsInRole("Admin")).Returns(true);

        // Act
        var isInRole = _authService.IsInRole("Admin");

        // Assert
        Assert.True(isInRole);
    }

    [Fact]
    public void IsInRole_ShouldReturnFalse_WhenUserDoesNotHaveRole()
    {
        // Arrange
        _jwtAuthServiceMock.Setup(x => x.IsInRole("Admin")).Returns(false);

        // Act
        var isInRole = _authService.IsInRole("Admin");

        // Assert
        Assert.False(isInRole);
    }

    [Fact]
    public void IsInAnyRole_ShouldReturnTrue_WhenUserHasAnyRole()
    {
        // Arrange
        _jwtAuthServiceMock.Setup(x => x.IsInAnyRole(new[] { "Admin", "User" })).Returns(true);

        // Act
        var isInAnyRole = _authService.IsInAnyRole("Admin", "User");

        // Assert
        Assert.True(isInAnyRole);
    }

    [Fact]
    public void IsInAnyRole_ShouldReturnFalse_WhenUserHasNoRoles()
    {
        // Arrange
        _jwtAuthServiceMock.Setup(x => x.IsInAnyRole(new[] { "Admin", "User" })).Returns(false);

        // Act
        var isInAnyRole = _authService.IsInAnyRole("Admin", "User");

        // Assert
        Assert.False(isInAnyRole);
    }

    [Fact]
    public async Task LoginAsync_ShouldReturnSuccess_WhenCredentialsAreValid()
    {
        // Arrange
        var email = "test@example.com";
        var password = "password123";
        var user = new IdentityUser { Id = "1", UserName = "testuser", Email = email };
        var roles = new[] { "User" };
        var token = "test-token";

        _signInManagerMock.Setup(x => x.PasswordSignInAsync(email, password, false, true))
            .ReturnsAsync(SignInResult.Success);
        
        _userManagerMock.Setup(x => x.FindByEmailAsync(email))
            .ReturnsAsync(user);
        
        _userManagerMock.Setup(x => x.GetRolesAsync(user))
            .ReturnsAsync(roles);
        
        _jwtAuthServiceMock.Setup(x => x.GenerateToken(user.Id, user.UserName, email, roles))
            .Returns(token);

        // Act
        var result = await _authService.LoginAsync(email, password, false);

        // Assert
        Assert.True(result.Success);
        Assert.Equal(token, result.Token);
        Assert.Equal(user.Id, result.UserId);
        Assert.Equal(user.UserName, result.UserName);
        Assert.Equal(roles, result.Roles);
    }

    [Fact]
    public async Task LoginAsync_ShouldReturnFailure_WhenCredentialsAreInvalid()
    {
        // Arrange
        var email = "test@example.com";
        var password = "wrongpassword";

        _signInManagerMock.Setup(x => x.PasswordSignInAsync(email, password, false, true))
            .ReturnsAsync(SignInResult.Failed);

        // Act
        var result = await _authService.LoginAsync(email, password, false);

        // Assert
        Assert.False(result.Success);
        Assert.Equal("Invalid username or password", result.ErrorMessage);
    }

    [Fact]
    public async Task LoginAsync_ShouldReturnLockedOut_WhenAccountIsLocked()
    {
        // Arrange
        var email = "test@example.com";
        var password = "password123";

        _signInManagerMock.Setup(x => x.PasswordSignInAsync(email, password, false, true))
            .ReturnsAsync(SignInResult.LockedOut);

        // Act
        var result = await _authService.LoginAsync(email, password, false);

        // Assert
        Assert.False(result.Success);
        Assert.Equal("Account locked out", result.ErrorMessage);
    }

    [Fact]
    public async Task LoginAsync_ShouldReturnNotAllowed_WhenAccountNotAllowed()
    {
        // Arrange
        var email = "test@example.com";
        var password = "password123";

        _signInManagerMock.Setup(x => x.PasswordSignInAsync(email, password, false, true))
            .ReturnsAsync(SignInResult.NotAllowed);

        // Act
        var result = await _authService.LoginAsync(email, password, false);

        // Assert
        Assert.False(result.Success);
        Assert.Equal("Account not allowed", result.ErrorMessage);
    }

    [Fact]
    public async Task RegisterAsync_ShouldReturnSuccess_WhenRegistrationSucceeds()
    {
        // Arrange
        var request = new RegisterRequest
        {
            Email = "newuser@example.com",
            Password = "Password123!",
            ConfirmPassword = "Password123!",
            UserName = "newuser"
        };

        var user = new IdentityUser { Id = "2", UserName = "newuser", Email = request.Email };
        var token = "new-token";

        _userManagerMock.Setup(x => x.CreateAsync(It.IsAny<IdentityUser>(), request.Password))
            .ReturnsAsync(IdentityResult.Success);
        
        _userManagerMock.Setup(x => x.AddToRoleAsync(user, "User"))
            .ReturnsAsync(IdentityResult.Success);
        
        _userManagerMock.Setup(x => x.GetRolesAsync(user))
            .ReturnsAsync(new[] { "User" });
        
        _jwtAuthServiceMock.Setup(x => x.GenerateToken(user.Id, user.UserName, request.Email, new[] { "User" }))
            .Returns(token);

        // Act
        var result = await _authService.RegisterAsync(request);

        // Assert
        Assert.True(result.Success);
        Assert.Equal(token, result.Token);
        Assert.Equal(user.Id, result.UserId);
        Assert.Equal(user.UserName, result.UserName);
    }

    [Fact]
    public async Task RegisterAsync_ShouldReturnFailure_WhenPasswordsDoNotMatch()
    {
        // Arrange
        var request = new RegisterRequest
        {
            Email = "newuser@example.com",
            Password = "Password123!",
            ConfirmPassword = "DifferentPassword123!",
            UserName = "newuser"
        };

        // Act
        var result = await _authService.RegisterAsync(request);

        // Assert
        Assert.False(result.Success);
        Assert.Equal("Passwords do not match", result.ErrorMessage);
    }

    [Fact]
    public async Task RegisterAsync_ShouldReturnFailure_WhenRegistrationFails()
    {
        // Arrange
        var request = new RegisterRequest
        {
            Email = "newuser@example.com",
            Password = "Password123!",
            ConfirmPassword = "Password123!",
            UserName = "newuser"
        };

        var errors = new[] { new IdentityError { Description = "Password too weak" } };
        
        _userManagerMock.Setup(x => x.CreateAsync(It.IsAny<IdentityUser>(), request.Password))
            .ReturnsAsync(IdentityResult.Failed(errors));

        // Act
        var result = await _authService.RegisterAsync(request);

        // Assert
        Assert.False(result.Success);
        Assert.Equal("Password too weak", result.ErrorMessage);
    }

    [Fact]
    public async Task LogoutAsync_ShouldCallSignOut()
    {
        // Act
        await _authService.LogoutAsync();

        // Assert
        _signInManagerMock.Verify(x => x.SignOutAsync(), Times.Once);
    }

    [Fact]
    public async Task GetCurrentUserAsync_ShouldReturnNull_WhenNotAuthenticated()
    {
        // Arrange
        _httpContextAccessorMock.Setup(x => x.HttpContext).Returns((HttpContext?)null);
        _jwtAuthServiceMock.Setup(x => x.IsAuthenticated).Returns(false);

        // Act
        var userInfo = await _authService.GetCurrentUserAsync();

        // Assert
        Assert.Null(userInfo);
    }

    [Fact]
    public async Task GetCurrentUserAsync_ShouldReturnUserInfo_WhenAuthenticated()
    {
        // Arrange
        var userId = "1";
        var user = new IdentityUser { Id = userId, UserName = "testuser", Email = "test@email.com" };
        var roles = new[] { "Admin", "User" };

        _httpContextAccessorMock.Setup(x => x.HttpContext).Returns((HttpContext?)null);
        _jwtAuthServiceMock.Setup(x => x.IsAuthenticated).Returns(true);
        _jwtAuthServiceMock.Setup(x => x.CurrentUserId).Returns(userId);
        
        _userManagerMock.Setup(x => x.FindByIdAsync(userId))
            .ReturnsAsync(user);
        
        _userManagerMock.Setup(x => x.GetRolesAsync(user))
            .ReturnsAsync(roles);

        // Act
        var userInfo = await _authService.GetCurrentUserAsync();

        // Assert
        Assert.NotNull(userInfo);
        Assert.Equal(userId, userInfo.Id);
        Assert.Equal("testuser", userInfo.UserName);
        Assert.Equal("test@email.com", userInfo.Email);
        Assert.Equal(roles, userInfo.Roles);
    }

    [Fact]
    public async Task CreateUserWithRoleAsync_ShouldReturnSuccess_WhenUserCreated()
    {
        // Arrange
        var email = "admin@example.com";
        var password = "Admin123!";
        var userName = "adminuser";
        var role = "Admin";

        var user = new IdentityUser { Id = "3", UserName = userName, Email = email };
        var token = "admin-token";
        var roles = new[] { role };

        _userManagerMock.Setup(x => x.CreateAsync(It.IsAny<IdentityUser>(), password))
            .ReturnsAsync(IdentityResult.Success);
        
        _userManagerMock.Setup(x => x.AddToRoleAsync(user, role))
            .ReturnsAsync(IdentityResult.Success);
        
        _userManagerMock.Setup(x => x.GetRolesAsync(user))
            .ReturnsAsync(roles);
        
        _jwtAuthServiceMock.Setup(x => x.GenerateToken(user.Id, user.UserName, email, roles))
            .Returns(token);

        // Act
        var result = await _authService.CreateUserWithRoleAsync(email, password, userName, role);

        // Assert
        Assert.True(result.Success);
        Assert.Equal(token, result.Token);
        Assert.Equal(user.Id, result.UserId);
        Assert.Equal(user.UserName, result.UserName);
    }

    [Fact]
    public async Task CreateUserWithRoleAsync_ShouldReturnFailure_WhenUserCreationFails()
    {
        // Arrange
        var email = "admin@example.com";
        var password = "Admin123!";
        var userName = "adminuser";
        var role = "Admin";

        var errors = new[] { new IdentityError { Description = "Invalid email" } };
        
        _userManagerMock.Setup(x => x.CreateAsync(It.IsAny<IdentityUser>(), password))
            .ReturnsAsync(IdentityResult.Failed(errors));

        // Act
        var result = await _authService.CreateUserWithRoleAsync(email, password, userName, role);

        // Assert
        Assert.False(result.Success);
        Assert.Equal("Invalid email", result.ErrorMessage);
    }
}
