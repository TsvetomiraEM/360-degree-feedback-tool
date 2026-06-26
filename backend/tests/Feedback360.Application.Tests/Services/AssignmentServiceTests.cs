using Feedback360.Application.Common;
using Feedback360.Application.DTOs;
using Feedback360.Application.Services;
using Feedback360.Application.Tests.Helpers;
using Feedback360.Domain.Enums;
using Feedback360.Infrastructure.Persistence;
using FluentAssertions;

namespace Feedback360.Application.Tests.Services;

public class AssignmentServiceTests : IAsyncLifetime
{
    private readonly AppDbContext _db = TestDbContextFactory.Create();
    private AssignmentService _sut = null!;

    public async Task InitializeAsync()
    {
        await TestData.SeedAllAsync(_db);
        await TestData.SeedActiveSurveyAsync(_db);
        _sut = new AssignmentService(_db, CurrentUserMock.AsManager());
    }

    public Task DisposeAsync()
    {
        _db.Dispose();
        return Task.CompletedTask;
    }

    [Fact]
    public async Task GetMineAsync_ReturnsPendingAssignments()
    {
        var result = await _sut.GetMineAsync();
        result.Should().ContainSingle(a => a.Id == TestIds.AssignmentId && a.Status == AssignmentStatus.Pending);
    }

    [Fact]
    public async Task GetMineAsync_AsAdmin_Throws()
    {
        var sut = new AssignmentService(_db, CurrentUserMock.AsAdmin());
        var act = () => sut.GetMineAsync();
        await act.Should().ThrowAsync<UnauthorizedAccessException>();
    }

    [Fact]
    public async Task GetByIdAsync_ReturnsQuestions()
    {
        var result = await _sut.GetByIdAsync(TestIds.AssignmentId);
        result.Should().NotBeNull();
        result!.Questions.Should().HaveCount(2);
    }

    [Fact]
    public async Task SubmitAsync_ValidResponses_CompletesAssignment()
    {
        await _sut.SubmitAsync(TestIds.AssignmentId, new SubmitResponsesRequest(
        [
            new ResponseInput(TestIds.RatingQuestionId, 4, "Good", null),
            new ResponseInput(TestIds.OpenTextQuestionId, null, null, "Solid contributor")
        ]));

        var assignment = await _db.SurveyAssignments.FindAsync(TestIds.AssignmentId);
        assignment!.Status.Should().Be(AssignmentStatus.Completed);
        assignment.CompletedAt.Should().NotBeNull();
        (await _db.Responses.CountAsync(r => r.AssignmentId == TestIds.AssignmentId)).Should().Be(2);
    }

    [Fact]
    public async Task SubmitAsync_MissingRating_Throws()
    {
        var act = () => _sut.SubmitAsync(TestIds.AssignmentId, new SubmitResponsesRequest(
        [
            new ResponseInput(TestIds.RatingQuestionId, null, null, null),
            new ResponseInput(TestIds.OpenTextQuestionId, null, null, "text")
        ]));
        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task SubmitAsync_AlreadyCompleted_Throws()
    {
        await _sut.SubmitAsync(TestIds.AssignmentId, new SubmitResponsesRequest(
        [
            new ResponseInput(TestIds.RatingQuestionId, 5, null, null),
            new ResponseInput(TestIds.OpenTextQuestionId, null, null, "Done")
        ]));

        var act = () => _sut.SubmitAsync(TestIds.AssignmentId, new SubmitResponsesRequest(
        [
            new ResponseInput(TestIds.RatingQuestionId, 3, null, null),
            new ResponseInput(TestIds.OpenTextQuestionId, null, null, "Again")
        ]));
        await act.Should().ThrowAsync<InvalidOperationException>();
    }
}
