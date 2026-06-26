using Feedback360.Application.DTOs;
using Feedback360.Application.Services;
using Feedback360.Application.Tests.Helpers;
using Feedback360.Domain.Enums;
using Feedback360.Infrastructure.Persistence;
using FluentAssertions;

namespace Feedback360.Application.Tests.Services;

public class SurveyServiceTests : IAsyncLifetime
{
    private readonly AppDbContext _db = TestDbContextFactory.Create();
    private SurveyService _sut = null!;

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

    private SurveyService CreateSut(ICurrentUserService currentUser)
    {
        var categoryService = new CategoryService(_db, currentUser);
        return new SurveyService(_db, currentUser, categoryService);
    }

    [Fact]
    public async Task GetAllAsync_AsManager_ReturnsCreatedSurveys()
    {
        await _sut.CreateFromTemplateAsync(TestIds.TemplateId,
            new CreateSurveyFromTemplateRequest(TestIds.Employee1Id, DateTime.SpecifyKind(new DateTime(2026, 7, 1), DateTimeKind.Unspecified), "Review"));

        var result = await _sut.GetAllAsync();
        result.Should().HaveCount(1);
        result[0].DueDate!.Value.Kind.Should().Be(DateTimeKind.Utc);
    }

    [Fact]
    public async Task GetAllAsync_AsAdmin_Throws()
    {
        var sut = CreateSut(CurrentUserMock.AsAdmin());
        var act = () => sut.GetAllAsync();
        await act.Should().ThrowAsync<UnauthorizedAccessException>();
    }

    [Fact]
    public async Task CreateFromTemplateAsync_ValidRequest_CreatesDraftSurvey()
    {
        var result = await _sut.CreateFromTemplateAsync(TestIds.TemplateId,
            new CreateSurveyFromTemplateRequest(TestIds.Employee1Id, null, null));
        result.Status.Should().Be(SurveyStatus.Draft);
        result.SubjectEmployeeId.Should().Be(TestIds.Employee1Id);
    }

    [Fact]
    public async Task CreateFromTemplateAsync_NotDirectReport_Throws()
    {
        var act = () => _sut.CreateFromTemplateAsync(TestIds.TemplateId,
            new CreateSurveyFromTemplateRequest(TestIds.OtherManagerId, null, null));
        await act.Should().ThrowAsync<UnauthorizedAccessException>();
    }

    [Fact]
    public async Task AssignAsync_DraftSurvey_ActivatesWithAssignments()
    {
        var survey = await _sut.CreateFromTemplateAsync(TestIds.TemplateId,
            new CreateSurveyFromTemplateRequest(TestIds.Employee1Id, null, "Assign Test"));

        await _sut.AssignAsync(survey.Id, new AssignSurveyRequest([TestIds.Employee2Id]));

        var updated = await _db.Surveys.Include(s => s.Assignments).FirstAsync(s => s.Id == survey.Id);
        updated.Status.Should().Be(SurveyStatus.Active);
        updated.Assignments.Should().HaveCount(3);
    }

    [Fact]
    public async Task AssignAsync_AlreadyActive_Throws()
    {
        await TestData.SeedActiveSurveyAsync(_db);
        var act = () => _sut.AssignAsync(TestIds.SurveyId, new AssignSurveyRequest([]));
        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task DeleteAsync_OwnedSurvey_Removes()
    {
        var survey = await _sut.CreateFromTemplateAsync(TestIds.TemplateId,
            new CreateSurveyFromTemplateRequest(TestIds.Employee1Id, null, "Delete Test"));
        await _sut.DeleteAsync(survey.Id);
        (await _db.Surveys.FindAsync(survey.Id)).Should().BeNull();
    }

    [Fact]
    public async Task PublishResultsAsync_SetsPublishedAndClosed()
    {
        var survey = await _sut.CreateFromTemplateAsync(TestIds.TemplateId,
            new CreateSurveyFromTemplateRequest(TestIds.Employee1Id, null, "Publish Test"));
        await _sut.AssignAsync(survey.Id, new AssignSurveyRequest([TestIds.Employee2Id]));

        await _sut.PublishResultsAsync(survey.Id);

        var updated = await _db.Surveys.FindAsync(survey.Id);
        updated!.ResultsPublished.Should().BeTrue();
        updated.Status.Should().Be(SurveyStatus.Closed);
    }

    [Fact]
    public async Task GetDirectReportsAsync_ReturnsEmployees()
    {
        var result = await _sut.GetDirectReportsAsync();
        result.Should().HaveCount(2);
        result.Select(u => u.Id).Should().Contain([TestIds.Employee1Id, TestIds.Employee2Id]);
    }

    [Fact]
    public async Task GetPeerCandidatesAsync_ExcludesSubject()
    {
        var result = await _sut.GetPeerCandidatesAsync(TestIds.Employee1Id);
        result.Should().NotContain(u => u.Id == TestIds.Employee1Id);
        result.Should().NotContain(u => u.Role == UserRole.Admin);
    }

    [Fact]
    public async Task CreateAsync_CustomSurvey_PersistsQuestions()
    {
        var result = await _sut.CreateAsync(new CreateSurveyRequest("Custom", TestIds.Employee1Id, null,
        [
            new QuestionInput(QuestionType.Rating, "Custom Q", 0, TestIds.SkillsCategoryId)
        ]));
        result.Title.Should().Be("Custom");
        (await _db.SurveyQuestions.CountAsync(q => q.SurveyId == result.Id)).Should().Be(1);
    }

    [Fact]
    public async Task GetByIdAsync_AsEmployeeWithPublishedResults_ReturnsSurvey()
    {
        await TestData.SeedActiveSurveyAsync(_db);
        var survey = await _db.Surveys.FindAsync(TestIds.SurveyId);
        survey!.ResultsPublished = true;
        await _db.SaveChangesAsync();

        var employeeSut = CreateSut(CurrentUserMock.AsEmployee());
        var result = await employeeSut.GetByIdAsync(TestIds.SurveyId);
        result.Should().NotBeNull();
    }

    [Fact]
    public async Task GetManagersAsync_ReturnsActiveManagers()
    {
        var result = await _sut.GetManagersAsync();
        result.Should().HaveCountGreaterOrEqualTo(2);
    }
}
