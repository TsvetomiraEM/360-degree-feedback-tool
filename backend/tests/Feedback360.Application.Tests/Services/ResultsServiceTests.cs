using Feedback360.Application.Services;
using Feedback360.Application.Tests.Helpers;
using Feedback360.Domain.Entities;
using Feedback360.Domain.Enums;
using Feedback360.Infrastructure.Persistence;
using FluentAssertions;

namespace Feedback360.Application.Tests.Services;

public class ResultsServiceTests : IAsyncLifetime
{
    private readonly AppDbContext _db = TestDbContextFactory.Create();
    private ResultsService _sut = null!;

    public async Task InitializeAsync()
    {
        await TestData.SeedAllAsync(_db);
        await SeedCompletedSurveyAsync();
        _sut = new ResultsService(_db, CurrentUserMock.AsManager());
    }

    public Task DisposeAsync()
    {
        _db.Dispose();
        return Task.CompletedTask;
    }

    private async Task SeedCompletedSurveyAsync()
    {
        var survey = await TestData.SeedActiveSurveyAsync(_db);
        var assignment = survey.Assignments.First();
        assignment.Status = AssignmentStatus.Completed;
        assignment.CompletedAt = DateTime.UtcNow;
        _db.Responses.AddRange(
            new Response
            {
                Id = Guid.NewGuid(),
                AssignmentId = assignment.Id,
                QuestionId = TestIds.RatingQuestionId,
                Rating = 4,
                Comment = "Great teamwork"
            },
            new Response
            {
                Id = Guid.NewGuid(),
                AssignmentId = assignment.Id,
                QuestionId = TestIds.OpenTextQuestionId,
                OpenText = "Keep it up"
            });
        survey.ResultsPublished = true;
        survey.Status = SurveyStatus.Closed;
        await _db.SaveChangesAsync();
    }

    [Fact]
    public async Task GetResultsAsync_AsManager_ReturnsAggregatedResults()
    {
        var result = await _sut.GetResultsAsync(TestIds.SurveyId);
        result.Should().NotBeNull();
        result!.Labels.Should().HaveCount(1);
        result.Series.Should().HaveCount(3);
        result.CommentGroups.Should().NotBeEmpty();
        result.OpenTextGroups.Should().NotBeEmpty();
        result.CategoryGroups.Should().ContainSingle(g => g.CategoryName == "Skills");
        result.CategoryGroups[0].Labels.Should().Equal("Teamwork");
        result.CategoryGroups[0].CommentGroups.Should().NotBeEmpty();
        result.CategoryGroups[0].OpenTextGroups.Should().NotBeEmpty();
    }

    [Fact]
    public async Task GetResultsAsync_CategoryGroups_GroupsMultipleCategories()
    {
        _db.QuestionCategories.Add(new QuestionCategory
        {
            Id = TestIds.LeadershipCategoryId,
            Name = "Leadership",
            CreatedById = TestIds.ManagerId,
            CreatedAt = DateTime.UtcNow
        });
        var leadershipQuestionId = Guid.NewGuid();
        _db.SurveyQuestions.Add(new SurveyQuestion
        {
            Id = leadershipQuestionId,
            SurveyId = TestIds.SurveyId,
            Order = 2,
            Type = QuestionType.Rating,
            Text = "Inspires others",
            CategoryId = TestIds.LeadershipCategoryId
        });
        await _db.SaveChangesAsync();

        var result = await _sut.GetResultsAsync(TestIds.SurveyId);
        result.Should().NotBeNull();
        result!.CategoryGroups.Should().HaveCount(2);
        result.CategoryGroups[0].CategoryName.Should().Be("Skills");
        result.CategoryGroups[1].CategoryName.Should().Be("Leadership");
        result.CategoryGroups[1].Labels.Should().Equal("Inspires others");
    }

    [Fact]
    public async Task GetResultsAsync_AsEmployee_Unpublished_ReturnsNull()
    {
        var survey = await _db.Surveys.FindAsync(TestIds.SurveyId);
        survey!.ResultsPublished = false;
        await _db.SaveChangesAsync();

        var sut = new ResultsService(_db, CurrentUserMock.AsEmployee());
        var result = await sut.GetResultsAsync(TestIds.SurveyId);
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetResultsAsync_AsEmployee_Published_ReturnsResults()
    {
        var sut = new ResultsService(_db, CurrentUserMock.AsEmployee());
        var result = await sut.GetResultsAsync(TestIds.SurveyId);
        result.Should().NotBeNull();
    }

    [Fact]
    public async Task GetViewableSurveysAsync_AsManager_ReturnsCreatedSurveys()
    {
        var result = await _sut.GetViewableSurveysAsync();
        result.Should().ContainSingle(s => s.Id == TestIds.SurveyId);
    }

    [Fact]
    public async Task GetViewableSurveysAsync_AsEmployee_OnlyPublishedOwn()
    {
        var sut = new ResultsService(_db, CurrentUserMock.AsEmployee());
        var result = await sut.GetViewableSurveysAsync();
        result.Should().ContainSingle(s => s.SubjectEmployeeId == TestIds.Employee1Id);
    }

    [Fact]
    public async Task GetResultsAsync_AsAdmin_Throws()
    {
        var sut = new ResultsService(_db, CurrentUserMock.AsAdmin());
        var act = () => sut.GetResultsAsync(TestIds.SurveyId);
        await act.Should().ThrowAsync<UnauthorizedAccessException>();
    }
}
