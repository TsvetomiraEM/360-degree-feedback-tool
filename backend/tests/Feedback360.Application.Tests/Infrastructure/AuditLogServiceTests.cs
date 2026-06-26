using Feedback360.Application.Tests.Helpers;
using Feedback360.Domain.Enums;
using Feedback360.Infrastructure.Auth;
using FluentAssertions;
using Microsoft.Extensions.Configuration;

namespace Feedback360.Application.Tests.Infrastructure;

public class AuditLogServiceTests : IAsyncLifetime
{
    private readonly AppDbContext _db = TestDbContextFactory.Create();
    private AuditLogService _sut = null!;

    public async Task InitializeAsync()
    {
        await TestData.SeedUsersAsync(_db);
        _sut = new AuditLogService(_db, CurrentUserMock.AsAdmin());
    }

    public Task DisposeAsync()
    {
        _db.Dispose();
        return Task.CompletedTask;
    }

    [Fact]
    public async Task LogAsync_PersistsEntry()
    {
        await _sut.LogAsync(AuditAction.UserCreated, TestIds.Employee1Id, new { email = "test@test.com" });

        var logs = await _db.AuditLogs.ToListAsync();
        logs.Should().ContainSingle(l => l.Action == AuditAction.UserCreated && l.TargetUserId == TestIds.Employee1Id);
    }
}
