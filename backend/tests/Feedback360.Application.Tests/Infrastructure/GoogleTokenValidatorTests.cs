using Feedback360.Infrastructure.Auth;
using FluentAssertions;
using Microsoft.Extensions.Configuration;

namespace Feedback360.Application.Tests.Infrastructure;

public class GoogleTokenValidatorTests
{
    [Fact]
    public async Task ValidateAsync_UnconfiguredClient_ReturnsNull()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["Google:ClientId"] = "your-google-client-id" })
            .Build();
        var sut = new GoogleTokenValidator(config);

        var result = await sut.ValidateAsync("invalid-token");
        result.Should().BeNull();
    }

    [Fact]
    public async Task ValidateAsync_MissingClient_ReturnsNull()
    {
        var config = new ConfigurationBuilder().Build();
        var sut = new GoogleTokenValidator(config);

        var result = await sut.ValidateAsync("token");
        result.Should().BeNull();
    }
}
