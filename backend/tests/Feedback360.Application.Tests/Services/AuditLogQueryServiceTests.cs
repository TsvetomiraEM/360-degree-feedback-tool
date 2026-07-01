using Feedback360.Application.DTOs;
using Feedback360.Application.Services;
using Feedback360.Application.Tests.Helpers;
using Feedback360.Domain.Enums;
using Feedback360.Infrastructure.Persistence;
using FluentAssertions;

namespace Feedback360.Application.Tests.Services;

public class AuditLogQueryServiceTests : IAsyncLifetime
{
    private readonly AppDbContext _db = TestDbContextFactory.Create();
    private AuditLogQueryService _sut = null!;

    public async Task InitializeAsync()
    {
        await TestData.SeedUsersAsync(_db);
        await TestData.SeedAuditLogsAsync(_db);
        _sut = new AuditLogQueryService(_db);
    }

    public Task DisposeAsync()
    {
        _db.Dispose();
        return Task.CompletedTask;
    }

    [Fact]
    public async Task GetAsync_ReturnsPagedResults()
    {
        var result = await _sut.GetAsync(new AuditLogQuery(Page: 1, PageSize: 10));
        result.TotalCount.Should().Be(2);
        result.Items.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetAsync_FilterByAction_ReturnsMatching()
    {
        var result = await _sut.GetAsync(new AuditLogQuery(Action: AuditAction.UserCreated));
        result.Items.Should().OnlyContain(a => a.Action == AuditAction.UserCreated);
    }

    [Fact]
    public async Task GetAsync_FilterByDateRange_ReturnsMatching()
    {
        var from = DateTime.UtcNow.AddDays(-1.5);
        var result = await _sut.GetAsync(new AuditLogQuery(From: from));
        result.Items.Should().HaveCount(1);
    }

    [Fact]
    public async Task GetAsync_Paging_ReturnsSinglePage()
    {
        var result = await _sut.GetAsync(new AuditLogQuery(Page: 1, PageSize: 1));
        result.Items.Should().HaveCount(1);
        result.TotalCount.Should().Be(2);
    }
}
