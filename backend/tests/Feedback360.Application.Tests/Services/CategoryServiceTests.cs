using Feedback360.Application.DTOs;
using Feedback360.Application.Services;
using Feedback360.Application.Tests.Helpers;
using Feedback360.Domain.Enums;
using Feedback360.Infrastructure.Persistence;
using FluentAssertions;

namespace Feedback360.Application.Tests.Services;

public class CategoryServiceTests : IAsyncLifetime
{
    private readonly AppDbContext _db = TestDbContextFactory.Create();
    private CategoryService _sut = null!;

    public async Task InitializeAsync()
    {
        await TestData.SeedAllAsync(_db);
        _sut = new CategoryService(_db, CurrentUserMock.AsManager());
    }

    public Task DisposeAsync()
    {
        _db.Dispose();
        return Task.CompletedTask;
    }

    [Fact]
    public async Task GetAllAsync_AsManager_ReturnsCategories()
    {
        var result = await _sut.GetAllAsync();
        result.Should().ContainSingle(c => c.Name == "Skills");
    }

    [Fact]
    public async Task GetAllAsync_AsEmployee_Throws()
    {
        var sut = new CategoryService(_db, CurrentUserMock.AsEmployee());
        var act = () => sut.GetAllAsync();
        await act.Should().ThrowAsync<UnauthorizedAccessException>();
    }

    [Fact]
    public async Task CreateAsync_NewCategory_Persists()
    {
        var result = await _sut.CreateAsync(new CreateQuestionCategoryRequest("Innovation"));
        result.Name.Should().Be("Innovation");
        (await _db.QuestionCategories.CountAsync(c => c.Name == "Innovation")).Should().Be(1);
    }

    [Fact]
    public async Task CreateAsync_DuplicateName_ReturnsExisting()
    {
        var result = await _sut.CreateAsync(new CreateQuestionCategoryRequest("skills"));
        result.Id.Should().Be(TestIds.SkillsCategoryId);
        (await _db.QuestionCategories.CountAsync()).Should().Be(1);
    }

    [Fact]
    public async Task CreateAsync_EmptyName_Throws()
    {
        var act = () => _sut.CreateAsync(new CreateQuestionCategoryRequest("  "));
        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task ValidateCategoryIdsAsync_InvalidId_Throws()
    {
        var act = () => _sut.ValidateCategoryIdsAsync([Guid.NewGuid()]);
        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task ValidateCategoryIdsAsync_ValidIds_Succeeds()
    {
        var act = () => _sut.ValidateCategoryIdsAsync([TestIds.SkillsCategoryId]);
        await act.Should().NotThrowAsync();
    }
}
