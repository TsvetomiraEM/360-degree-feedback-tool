using Feedback360.Application.Common;
using Feedback360.Application.DTOs;
using Feedback360.Domain.Entities;
using Feedback360.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Feedback360.Application.Services;

public class AssignmentService
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public AssignmentService(IApplicationDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<List<AssignmentDto>> GetMineAsync(CancellationToken ct = default)
    {
        if (_currentUser.Role == UserRole.Admin)
            throw new UnauthorizedAccessException("Admins cannot access assignments.");

        var assignments = await _db.SurveyAssignments
            .Include(a => a.Survey).ThenInclude(s => s.SubjectEmployee)
            .Where(a => a.ReviewerId == _currentUser.UserId && a.Survey.Status == SurveyStatus.Active)
            .OrderBy(a => a.Status).ThenBy(a => a.Survey.DueDate)
            .ToListAsync(ct);

        return assignments.Select(a => new AssignmentDto(
            a.Id, a.SurveyId, a.Survey.Title, a.Survey.SubjectEmployee.Name,
            a.ReviewerType, a.Status, a.Survey.DueDate, a.CompletedAt)).ToList();
    }

    public async Task<AssignmentDetailDto?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        if (_currentUser.Role == UserRole.Admin)
            throw new UnauthorizedAccessException("Admins cannot access assignments.");

        var assignment = await _db.SurveyAssignments
            .Include(a => a.Survey).ThenInclude(s => s.SubjectEmployee)
            .Include(a => a.Survey).ThenInclude(s => s.Questions).ThenInclude(q => q.Category)
            .Include(a => a.Responses)
            .FirstOrDefaultAsync(a => a.Id == id && a.ReviewerId == _currentUser.UserId, ct);

        if (assignment is null) return null;

        return new AssignmentDetailDto(
            assignment.Id, assignment.SurveyId, assignment.Survey.Title,
            assignment.Survey.SubjectEmployee.Name, assignment.ReviewerType, assignment.Status,
            assignment.Survey.Questions.OrderBy(q => q.Order).Select(QuestionMapping.ToResponseDto).ToList(),
            assignment.Responses.Select(r => new ResponseInput(r.QuestionId, r.Rating, r.Comment, r.OpenText)).ToList());
    }

    public async Task SubmitAsync(Guid id, SubmitResponsesRequest request, CancellationToken ct = default)
    {
        if (_currentUser.Role == UserRole.Admin)
            throw new UnauthorizedAccessException("Admins cannot submit responses.");

        var assignment = await _db.SurveyAssignments
            .Include(a => a.Survey).ThenInclude(s => s.Questions)
            .FirstOrDefaultAsync(a => a.Id == id && a.ReviewerId == _currentUser.UserId, ct)
            ?? throw new KeyNotFoundException("Assignment not found.");

        if (assignment.Status == AssignmentStatus.Completed)
            throw new InvalidOperationException("Assignment already completed.");
        if (assignment.Survey.Status != SurveyStatus.Active)
            throw new InvalidOperationException("Survey is not active.");

        ValidateResponses(assignment.Survey.Questions.ToList(), request.Responses);

        var existingResponses = await _db.Responses
            .Where(r => r.AssignmentId == id)
            .ToListAsync(ct);
        _db.Responses.RemoveRange(existingResponses);

        foreach (var input in request.Responses)
        {
            _db.Responses.Add(new Response
            {
                Id = Guid.NewGuid(),
                AssignmentId = id,
                QuestionId = input.QuestionId,
                Rating = input.Rating,
                Comment = input.Comment,
                OpenText = input.OpenText
            });
        }

        assignment.Status = AssignmentStatus.Completed;
        assignment.CompletedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);
    }

    private static void ValidateResponses(List<SurveyQuestion> questions, List<ResponseInput> responses)
    {
        foreach (var question in questions)
        {
            var response = responses.FirstOrDefault(r => r.QuestionId == question.Id);
            if (response is null)
                throw new InvalidOperationException($"Missing response for question: {question.Text}");

            if (question.Type == QuestionType.Rating)
            {
                if (!response.Rating.HasValue || response.Rating < 1 || response.Rating > 5)
                    throw new InvalidOperationException($"Rating must be between 1 and 5 for: {question.Text}");
            }
            else
            {
                if (string.IsNullOrWhiteSpace(response.OpenText))
                    throw new InvalidOperationException($"Open text response required for: {question.Text}");
                if (response.OpenText.Length > 300)
                    throw new InvalidOperationException($"Open text cannot exceed 300 characters for: {question.Text}");
            }
        }
    }
}
