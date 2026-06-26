using Feedback360.Infrastructure.Auth;
using FluentAssertions;

namespace Feedback360.Application.Tests.Infrastructure;

public class PasswordHasherTests
{
    private readonly PasswordHasher _hasher = new();

    [Fact]
    public void Hash_And_Verify_RoundTrip()
    {
        var hash = _hasher.Hash("TestPassword123!");
        hash.Should().NotBeNullOrEmpty();
        _hasher.Verify("TestPassword123!", hash).Should().BeTrue();
        _hasher.Verify("WrongPassword", hash).Should().BeFalse();
    }
}
