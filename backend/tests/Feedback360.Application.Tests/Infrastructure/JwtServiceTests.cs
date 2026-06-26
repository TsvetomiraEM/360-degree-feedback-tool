using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Feedback360.Domain.Entities;
using Feedback360.Domain.Enums;
using Feedback360.Infrastructure.Auth;
using FluentAssertions;
using Microsoft.Extensions.Configuration;

namespace Feedback360.Application.Tests.Infrastructure;

public class JwtServiceTests
{
    private readonly JwtService _jwtService;

    public JwtServiceTests()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:Secret"] = "dev-secret-key-change-in-production-min-32-chars!",
                ["Jwt:Issuer"] = "Feedback360",
                ["Jwt:Audience"] = "Feedback360"
            })
            .Build();
        _jwtService = new JwtService(config);
    }

    [Fact]
    public void GenerateAccessToken_ContainsExpectedClaims()
    {
        var user = new User
        {
            Id = Guid.Parse("22222222-2222-2222-2222-222222222222"),
            Email = "manager@feedback360.local",
            Name = "Jane Manager",
            Role = UserRole.Manager,
            ManagerId = null
        };

        var token = _jwtService.GenerateAccessToken(user);
        token.Should().NotBeNullOrEmpty();

        var handler = new JwtSecurityTokenHandler();
        var jwt = handler.ReadJwtToken(token);
        jwt.Claims.Should().Contain(c => c.Type == ClaimTypes.NameIdentifier && c.Value == user.Id.ToString());
        jwt.Claims.Should().Contain(c => c.Type == ClaimTypes.Email && c.Value == user.Email);
        jwt.Claims.Should().Contain(c => c.Type == ClaimTypes.Role && c.Value == nameof(UserRole.Manager));
    }

    [Fact]
    public void GenerateRefreshToken_ReturnsNonEmptyGuidString()
    {
        var token = _jwtService.GenerateRefreshToken();
        token.Should().NotBeNullOrEmpty();
        token.Length.Should().Be(32);
    }

    [Fact]
    public void ValidateRefreshToken_InvalidFormat_ReturnsNull()
    {
        _jwtService.ValidateRefreshToken("not-a-guid").Should().BeNull();
    }
}
