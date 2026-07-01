using Feedback360.Application.Services;
using Feedback360.Application.Tests.Helpers;
using Feedback360.Domain.Entities;
using Feedback360.Domain.Enums;
using Feedback360.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

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
    public async Task GetResultsAsync_AsManager_ReturnsCategorySummariesAndGroups()
    {
        var result = await _sut.GetResultsAsync(TestIds.SurveyId);
        result.Should().NotBeNull();
        result!.CategorySummaries.Should().ContainSingle(s => s.CategoryName == "Skills");
        result.CategoryGroups.Should().ContainSingle(g => g.CategoryName == "Skills");
        result.CategorySummaries[0].OverallAverage.Should().Be(4);
        result.CategoryGroups[0].Labels.Should().Equal("Teamwork");
    }

    [Fact]
    public async Task GetResultsAsync_CategoryGroups_GroupsMultipleCategories()
    {
        var leadershipCategoryId = Guid.Parse("aaaaaaaa-0001-0002-0002-000000000002");
        _db.QuestionCategories.Add(new QuestionCategory
        {
            Id = leadershipCategoryId,
            Name = "Leadership",
            CreatedById = TestIds.ManagerId,
            CreatedAt = DateTime.UtcNow
        });
        _db.SurveyQuestions.Add(new SurveyQuestion
        {
            Id = Guid.NewGuid(),
            SurveyId = TestIds.SurveyId,
            Order = 2,
            Type = QuestionType.Rating,
            Text = "Inspires others",
            CategoryId = leadershipCategoryId
        });
        await _db.SaveChangesAsync();

        var result = await _sut.GetResultsAsync(TestIds.SurveyId);
        result.Should().NotBeNull();
        result!.CategorySummaries.Should().HaveCount(2);
        result.CategoryGroups.Should().HaveCount(2);
        result.CategorySummaries[1].CategoryName.Should().Be("Leadership");
    }

    [Fact]
    public async Task GetResultsAsync_ReturnsTopAndBottomQuestions_RankedByPeerManagerAverage()
    {
        var survey = await _db.Surveys
            .Include(s => s.Questions)
            .Include(s => s.Assignments)
            .FirstAsync(s => s.Id == TestIds.SurveyId);

        var qHighId = Guid.NewGuid();
        var qMidId = Guid.NewGuid();
        var qLowId = Guid.NewGuid();
        var categoryId = survey.Questions.First().CategoryId;

        _db.SurveyQuestions.AddRange(
            new SurveyQuestion
            {
                Id = qHighId,
                SurveyId = TestIds.SurveyId,
                Order = 10,
                Type = QuestionType.Rating,
                Text = "High score question",
                CategoryId = categoryId
            },
            new SurveyQuestion
            {
                Id = qMidId,
                SurveyId = TestIds.SurveyId,
                Order = 11,
                Type = QuestionType.Rating,
                Text = "Mid score question",
                CategoryId = categoryId
            },
            new SurveyQuestion
            {
                Id = qLowId,
                SurveyId = TestIds.SurveyId,
                Order = 12,
                Type = QuestionType.Rating,
                Text = "Low score question",
                CategoryId = categoryId
            });

        var peerAssignment = survey.Assignments.First(a => a.ReviewerType == ReviewerType.Peer);
        var managerAssignment = survey.Assignments.First(a => a.ReviewerType == ReviewerType.Manager);
        peerAssignment.Status = AssignmentStatus.Completed;
        managerAssignment.Status = AssignmentStatus.Completed;

        _db.Responses.AddRange(
            new Response { Id = Guid.NewGuid(), AssignmentId = peerAssignment.Id, QuestionId = qHighId, Rating = 5 },
            new Response { Id = Guid.NewGuid(), AssignmentId = managerAssignment.Id, QuestionId = qHighId, Rating = 5 },
            new Response { Id = Guid.NewGuid(), AssignmentId = peerAssignment.Id, QuestionId = qMidId, Rating = 3 },
            new Response { Id = Guid.NewGuid(), AssignmentId = managerAssignment.Id, QuestionId = qMidId, Rating = 3 },
            new Response { Id = Guid.NewGuid(), AssignmentId = peerAssignment.Id, QuestionId = qLowId, Rating = 1 },
            new Response { Id = Guid.NewGuid(), AssignmentId = managerAssignment.Id, QuestionId = qLowId, Rating = 1 });

        await _db.SaveChangesAsync();

        var result = await _sut.GetResultsAsync(TestIds.SurveyId);
        result.Should().NotBeNull();
        result!.TopQuestions.Should().HaveCount(3);
        result.BottomQuestions.Should().HaveCount(3);
        result.TopQuestions[0].QuestionText.Should().Contain("High score question");
        result.TopQuestions[0].CombinedAverage.Should().Be(5);
        result.BottomQuestions[0].QuestionText.Should().Contain("Low score question");
        result.BottomQuestions[0].CombinedAverage.Should().Be(1);
    }
}
