using Feedback360.Application.Common;
using Feedback360.Application.DTOs;
using Feedback360.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Feedback360.Application.Services;

public class ResultsService
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public ResultsService(IApplicationDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<ResultsDto?> GetResultsAsync(Guid surveyId, CancellationToken ct = default)
    {
        if (_currentUser.Role == UserRole.Admin)
            throw new UnauthorizedAccessException("Admins cannot view survey results.");

        var survey = await _db.Surveys
            .Include(s => s.SubjectEmployee)
            .Include(s => s.Questions).ThenInclude(q => q.Category)
            .Include(s => s.Assignments).ThenInclude(a => a.Responses)
            .FirstOrDefaultAsync(s => s.Id == surveyId, ct);

        if (survey is null) return null;
        if (!CanViewResults(survey)) return null;

        var ratingQuestions = survey.Questions.Where(q => q.Type == QuestionType.Rating).OrderBy(q => q.Order).ToList();
        var openTextQuestions = survey.Questions.Where(q => q.Type == QuestionType.OpenText).OrderBy(q => q.Order).ToList();

        var labels = ratingQuestions.Select(q => $"[{q.Category?.Name ?? "General"}] {q.Text}").ToList();
        var series = new List<ResultsSeriesDto>
        {
            BuildSeries("Self", ReviewerType.Self, ratingQuestions, survey),
            BuildSeries("Peer", ReviewerType.Peer, ratingQuestions, survey),
            BuildSeries("Manager", ReviewerType.Manager, ratingQuestions, survey)
        };

        var commentGroups = ratingQuestions.SelectMany(q =>
        {
            return Enum.GetValues<ReviewerType>().Select(rt => new ResultsCommentGroupDto(
                rt.ToString(),
                $"[{q.Category?.Name ?? "General"}] {q.Text}",
                survey.Assignments
                    .Where(a => a.ReviewerType == rt && a.Status == AssignmentStatus.Completed)
                    .SelectMany(a => a.Responses.Where(r => r.QuestionId == q.Id && !string.IsNullOrWhiteSpace(r.Comment)))
                    .Select(r => r.Comment!)
                    .ToList()
            )).Where(g => g.Comments.Count > 0);
        }).ToList();

        var openTextGroups = openTextQuestions.SelectMany(q =>
        {
            return Enum.GetValues<ReviewerType>().Select(rt => new OpenTextGroupDto(
                rt.ToString(),
                $"[{q.Category?.Name ?? "General"}] {q.Text}",
                survey.Assignments
                    .Where(a => a.ReviewerType == rt && a.Status == AssignmentStatus.Completed)
                    .SelectMany(a => a.Responses.Where(r => r.QuestionId == q.Id && !string.IsNullOrWhiteSpace(r.OpenText)))
                    .Select(r => r.OpenText!)
                    .ToList()
            )).Where(g => g.Responses.Count > 0);
        }).ToList();

        return new ResultsDto(
            survey.Id, survey.Title, survey.SubjectEmployee.Name, survey.ResultsPublished,
            labels, series, commentGroups, openTextGroups);
    }

    public async Task<List<SurveyDto>> GetViewableSurveysAsync(CancellationToken ct = default)
    {
        if (_currentUser.Role == UserRole.Admin)
            throw new UnauthorizedAccessException("Admins cannot view survey results.");

        IQueryable<Domain.Entities.Survey> query = _db.Surveys
            .Include(s => s.SubjectEmployee)
            .Include(s => s.Assignments);

        if (_currentUser.Role == UserRole.Manager)
            query = query.Where(s => s.CreatedById == _currentUser.UserId);
        else
            query = query.Where(s => s.SubjectEmployeeId == _currentUser.UserId && s.ResultsPublished);

        var surveys = await query.OrderByDescending(s => s.CreatedAt).ToListAsync(ct);
        return surveys.Select(s => new SurveyDto(
            s.Id, s.Title, s.SubjectEmployeeId, s.SubjectEmployee.Name, s.CreatedById,
            s.Status, s.DueDate, s.ResultsPublished, s.CreatedAt,
            s.Assignments.Count, s.Assignments.Count(a => a.Status == AssignmentStatus.Completed))).ToList();
    }

    private bool CanViewResults(Domain.Entities.Survey survey)
    {
        if (_currentUser.Role == UserRole.Manager)
            return survey.CreatedById == _currentUser.UserId;

        if (_currentUser.Role == UserRole.Employee)
            return survey.SubjectEmployeeId == _currentUser.UserId && survey.ResultsPublished;

        return false;
    }

    private static ResultsSeriesDto BuildSeries(string name, ReviewerType type, List<Domain.Entities.SurveyQuestion> questions, Domain.Entities.Survey survey)
    {
        var data = questions.Select(q =>
        {
            var ratings = survey.Assignments
                .Where(a => a.ReviewerType == type && a.Status == AssignmentStatus.Completed)
                .SelectMany(a => a.Responses.Where(r => r.QuestionId == q.Id && r.Rating.HasValue))
                .Select(r => (double)r.Rating!.Value)
                .ToList();
            return ratings.Count > 0 ? ratings.Average() : (double?)null;
        }).ToList();

        return new ResultsSeriesDto(name, data);
    }
}
