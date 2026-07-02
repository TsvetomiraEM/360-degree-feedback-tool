using Feedback360.Application.Common;
using FluentAssertions;

namespace Feedback360.Application.Tests.Common;

public class DateTimeNormalizationTests
{
    [Fact]
    public void ToUtc_Null_ReturnsNull()
    {
        DateTimeNormalization.ToUtc(null).Should().BeNull();
    }

    [Fact]
    public void ToUtc_Utc_PreservesValue()
    {
        var utc = new DateTime(2026, 6, 30, 0, 0, 0, DateTimeKind.Utc);
        var result = DateTimeNormalization.ToUtc(utc);
        result.Should().NotBeNull();
        result!.Value.Kind.Should().Be(DateTimeKind.Utc);
        result.Value.Should().Be(utc);
    }

    [Fact]
    public void ToUtc_Unspecified_SpecifiesUtc()
    {
        var unspecified = new DateTime(2026, 6, 30, 0, 0, 0, DateTimeKind.Unspecified);
        var result = DateTimeNormalization.ToUtc(unspecified);
        result!.Value.Kind.Should().Be(DateTimeKind.Utc);
        result.Value.Should().Be(unspecified);
    }

    [Fact]
    public void ToUtc_Local_ConvertsToUniversal()
    {
        var local = new DateTime(2026, 6, 30, 12, 0, 0, DateTimeKind.Local);
        var result = DateTimeNormalization.ToUtc(local);
        result!.Value.Kind.Should().Be(DateTimeKind.Utc);
        result.Value.Should().Be(local.ToUniversalTime());
    }
}
