using Feedback360.Application.DTOs;
using Feedback360.Application.Services;
using Feedback360.Application.Tests.Helpers;
using Feedback360.Domain.Enums;
using Feedback360.Infrastructure.Persistence;
using FluentAssertions;

namespace Feedback360.Application.Tests.Services;

public class TemplateServiceTests : IAsyncLifetime
{
    private readonly AppDbContext _db = TestDbContextFactory.Create();
    private TemplateService _sut = null!;

    public async Task InitializeAsync()
    {
        await TestData.SeedAllAsync(_db);
        _sut = CreateSut(CurrentUserMock.AsManager());
    }

    public Task DisposeAsync()
    {
        _db.Dispose();
        return Task.CompletedTask;
    }

    private TemplateService CreateSut(ICurrentUserService currentUser)
    {
        var categoryService = new CategoryService(_db, currentUser);
        return new TemplateService(_db, currentUser, categoryService);
    }

    [Fact]
    public async Task GetAllAsync_ReturnsOwnedTemplate()
    {
        var result = await _sut.GetAllAsync();
        result.Should().ContainSingle(t => t.Id == TestIds.TemplateId && t.IsOwner);
    }

    [Fact]
    public async Task GetByIdAsync_Existing_ReturnsTemplate()
    {
        var result = await _sut.GetByIdAsync(TestIds.TemplateId);
        result.Should().NotBeNull();
        result!.Questions.Should().HaveCount(2);
    }

    [Fact]
    public async Task CreateAsync_ValidRequest_Persists()
    {
        var result = await _sut.CreateAsync(new CreateTemplateRequest("New Template", "desc",
        [
            new QuestionInput(QuestionType.Rating, "Q1", 0, TestIds.SkillsCategoryId)
        ]));
        result.Name.Should().Be("New Template");
    }

    [Fact]
    public async Task CreateAsync_NoQuestions_Throws()
    {
        var act = () => _sut.CreateAsync(new CreateTemplateRequest("Empty", null, []));
        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task CreateAsync_OpenTextTooLong_Throws()
    {
        var act = () => _sut.CreateAsync(new CreateTemplateRequest("Long", null,
        [
            new QuestionInput(QuestionType.OpenText, new string('x', 301), 0, TestIds.SkillsCategoryId)
        ]));
        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task UpdateAsync_OwnedTemplate_Updates()
    {
        var result = await _sut.UpdateAsync(TestIds.TemplateId, new UpdateTemplateRequest("Updated", "new desc",
        [
            new QuestionInput(QuestionType.Rating, "Updated Q", 0, TestIds.SkillsCategoryId)
        ]));
        result.Name.Should().Be("Updated");
        result.Questions.Should().ContainSingle(q => q.Text == "Updated Q");
    }

    [Fact]
    public async Task UpdateAsync_NotOwned_Throws()
    {
        var otherSut = CreateSut(CurrentUserMock.AsManager(TestIds.OtherManagerId));
        var act = () => otherSut.UpdateAsync(TestIds.TemplateId, new UpdateTemplateRequest("X", null,
        [
            new QuestionInput(QuestionType.Rating, "Q", 0, TestIds.SkillsCategoryId)
        ]));
        await act.Should().ThrowAsync<KeyNotFoundException>();
    }

    [Fact]
    public async Task DeleteAsync_OwnedTemplate_Removes()
    {
        await _sut.DeleteAsync(TestIds.TemplateId);
        (await _db.SurveyTemplates.FindAsync(TestIds.TemplateId)).Should().BeNull();
    }

    [Fact]
    public async Task ShareAsync_AddsShareForOtherManager()
    {
        await _sut.ShareAsync(TestIds.TemplateId, new ShareTemplateRequest([TestIds.OtherManagerId]));
        var shares = _db.TemplateShares.Where(s => s.TemplateId == TestIds.TemplateId).ToList();
        shares.Should().ContainSingle(s => s.SharedWithManagerId == TestIds.OtherManagerId);
    }

    [Fact]
    public async Task GetAllAsync_SharedTemplate_VisibleToRecipient()
    {
        await _sut.ShareAsync(TestIds.TemplateId, new ShareTemplateRequest([TestIds.OtherManagerId]));
        var otherSut = CreateSut(CurrentUserMock.AsManager(TestIds.OtherManagerId));
        var result = await otherSut.GetAllAsync();
        result.Should().ContainSingle(t => t.IsShared && !t.IsOwner);
    }
}
