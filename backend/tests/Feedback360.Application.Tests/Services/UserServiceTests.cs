using Feedback360.Application.Common;
using Feedback360.Application.DTOs;
using Feedback360.Application.Services;
using Feedback360.Application.Tests.Helpers;
using Feedback360.Domain.Enums;
using Feedback360.Infrastructure.Auth;
using Feedback360.Infrastructure.Persistence;
using FluentAssertions;
using NSubstitute;

namespace Feedback360.Application.Tests.Services;

public class UserServiceTests : IAsyncLifetime
{
    private readonly AppDbContext _db = TestDbContextFactory.Create();
    private readonly IPasswordHasher _hasher = new PasswordHasher();
    private readonly IAuditLogService _auditLog = Substitute.For<IAuditLogService>();
    private UserService _sut = null!;

    public async Task InitializeAsync()
    {
        await TestData.SeedAllAsync(_db, _hasher);
        _sut = new UserService(_db, _hasher, _auditLog, CurrentUserMock.AsAdmin());
    }

    public Task DisposeAsync()
    {
        _db.Dispose();
        return Task.CompletedTask;
    }

    [Fact]
    public async Task GetAllAsync_ReturnsAllUsers()
    {
        var result = await _sut.GetAllAsync();
        result.Should().HaveCountGreaterOrEqualTo(5);
    }

    [Fact]
    public async Task GetByIdAsync_Existing_ReturnsUser()
    {
        var result = await _sut.GetByIdAsync(TestIds.ManagerId);
        result.Should().NotBeNull();
        result!.Email.Should().Be("manager@feedback360.local");
    }

    [Fact]
    public async Task CreateAsync_ValidEmployee_CreatesUser()
    {
        var result = await _sut.CreateAsync(new CreateUserRequest(
            "new@feedback360.local", "New User", UserRole.Employee, TestIds.ManagerId, "Pass123!"));
        result.Email.Should().Be("new@feedback360.local");
        await _auditLog.Received(1).LogAsync(AuditAction.UserCreated, result.Id, Arg.Any<object>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateAsync_DuplicateEmail_Throws()
    {
        var act = () => _sut.CreateAsync(new CreateUserRequest(
            "manager@feedback360.local", "Dup", UserRole.Manager, null, "Pass123!"));
        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task CreateAsync_InvalidManager_Throws()
    {
        var act = () => _sut.CreateAsync(new CreateUserRequest(
            "x@feedback360.local", "X", UserRole.Employee, TestIds.Employee1Id, "Pass123!"));
        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task UpdateAsync_ChangesName()
    {
        var result = await _sut.UpdateAsync(TestIds.Employee1Id, new UpdateUserRequest(
            "alice@feedback360.local", "Alice Updated", UserRole.Employee, TestIds.ManagerId, null));
        result.Name.Should().Be("Alice Updated");
    }

    [Fact]
    public async Task SetActiveAsync_DeactivatesUser()
    {
        await _sut.SetActiveAsync(TestIds.Employee2Id, false);
        var user = await _db.Users.FindAsync(TestIds.Employee2Id);
        user!.IsActive.Should().BeFalse();
    }

    [Fact]
    public async Task DeleteAsync_LastAdmin_Throws()
    {
        var act = () => _sut.DeleteAsync(TestIds.AdminId);
        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task DeleteAsync_EmployeeWithSurveyData_RemovesUser()
    {
        await TestData.SeedActiveSurveyAsync(_db);
        await _sut.DeleteAsync(TestIds.Employee1Id);
        (await _db.Users.FindAsync(TestIds.Employee1Id)).Should().BeNull();
        (await _db.Surveys.AnyAsync(s => s.SubjectEmployeeId == TestIds.Employee1Id)).Should().BeFalse();
    }
}
