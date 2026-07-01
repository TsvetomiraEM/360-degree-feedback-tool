using Feedback360.Application.Common;
using Feedback360.Application.DTOs;
using Feedback360.Application.Services;
using Feedback360.Application.Tests.Helpers;
using Feedback360.Domain.Enums;
using Feedback360.Infrastructure.Auth;
using FluentAssertions;
using NSubstitute;

namespace Feedback360.Application.Tests.Services;

public class AuthServiceTests : IAsyncLifetime
{
    private readonly AppDbContext _db = TestDbContextFactory.Create();
    private readonly IPasswordHasher _hasher = new PasswordHasher();
    private readonly IJwtService _jwt = Substitute.For<IJwtService>();
    private readonly IGoogleTokenValidator _google = Substitute.For<IGoogleTokenValidator>();
    private AuthService _sut = null!;

    public async Task InitializeAsync()
    {
        await TestData.SeedUsersAsync(_db, _hasher);
        _jwt.GenerateAccessToken(Arg.Any<Domain.Entities.User>()).Returns("access-token");
        _jwt.GenerateRefreshToken().Returns("refresh-token");
        _sut = new AuthService(_db, _hasher, _jwt, _google);
    }

    public Task DisposeAsync()
    {
        _db.Dispose();
        return Task.CompletedTask;
    }

    [Fact]
    public async Task LoginAsync_ValidCredentials_ReturnsAuthResponse()
    {
        var result = await _sut.LoginAsync(new LoginRequest("manager@feedback360.local", "Manager123!"));
        result.Should().NotBeNull();
        result!.User.Email.Should().Be("manager@feedback360.local");
        result.AccessToken.Should().Be("access-token");
    }

    [Fact]
    public async Task LoginAsync_InvalidPassword_ReturnsNull()
    {
        var result = await _sut.LoginAsync(new LoginRequest("manager@feedback360.local", "wrong"));
        result.Should().BeNull();
    }

    [Fact]
    public async Task LoginAsync_UnknownEmail_ReturnsNull()
    {
        var result = await _sut.LoginAsync(new LoginRequest("nobody@feedback360.local", "Manager123!"));
        result.Should().BeNull();
    }

    [Fact]
    public async Task LoginAsync_InactiveUser_ReturnsNull()
    {
        var user = await _db.Users.FindAsync(TestIds.Employee1Id);
        user!.IsActive = false;
        await _db.SaveChangesAsync();

        var result = await _sut.LoginAsync(new LoginRequest("alice@feedback360.local", "Employee123!"));
        result.Should().BeNull();
    }

    [Fact]
    public async Task GoogleLoginAsync_ValidToken_ReturnsAuthResponse()
    {
        _google.ValidateAsync("token", Arg.Any<CancellationToken>())
            .Returns(new GoogleUserInfo("manager@feedback360.local", "Jane Manager"));

        var result = await _sut.GoogleLoginAsync(new GoogleLoginRequest("token"));
        result.Should().NotBeNull();
        result!.User.AuthProvider.Should().Be("google");
    }

    [Fact]
    public async Task GoogleLoginAsync_InvalidToken_ReturnsNull()
    {
        _google.ValidateAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns((GoogleUserInfo?)null);
        var result = await _sut.GoogleLoginAsync(new GoogleLoginRequest("bad"));
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetMeAsync_ExistingUser_ReturnsDto()
    {
        var result = await _sut.GetMeAsync(TestIds.ManagerId);
        result.Should().NotBeNull();
        result!.Name.Should().Be("Jane Manager");
    }

    [Fact]
    public async Task GetMeAsync_MissingUser_ReturnsNull()
    {
        var result = await _sut.GetMeAsync(Guid.NewGuid());
        result.Should().BeNull();
    }
}
