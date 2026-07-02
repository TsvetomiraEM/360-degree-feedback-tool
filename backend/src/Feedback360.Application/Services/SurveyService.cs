using Feedback360.Application.Common;
using Feedback360.Application.DTOs;
using Feedback360.Domain.Entities;
using Feedback360.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Feedback360.Application.Services;

public class SurveyService
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUserService _currentUser;
    private readonly CategoryService _categoryService;

    public SurveyService(IApplicationDbContext db, ICurrentUserService currentUser, CategoryService categoryService)
    {
        _db = db;
        _currentUser = currentUser;
        _categoryService = categoryService;
    }

    public async Task<List<SurveyDto>> GetAllAsync(CancellationToken ct = default)
    {
        if (_currentUser.Role == UserRole.Admin)
            throw new UnauthorizedAccessException("Admins cannot access surveys.");

        IQueryable<Survey> query = _db.Surveys.Include(s => s.SubjectEmployee).Include(s => s.Assignments);

        if (_currentUser.Role == UserRole.Manager)
            query = query.Where(s => s.CreatedById == _currentUser.UserId);
        else
            query = query.Where(s => s.Assignments.Any(a => a.ReviewerId == _currentUser.UserId));

        var surveys = await query.OrderByDescending(s => s.CreatedAt).ToListAsync(ct);
        return surveys.Select(MapSurvey).ToList();
    }

    public async Task<SurveyDetailDto?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        if (_currentUser.Role == UserRole.Admin)
            throw new UnauthorizedAccessException("Admins cannot access surveys.");

        var survey = await _db.Surveys
            .Include(s => s.SubjectEmployee)
            .Include(s => s.Questions).ThenInclude(q => q.Category)
            .Include(s => s.Assignments).ThenInclude(a => a.Reviewer)
            .FirstOrDefaultAsync(s => s.Id == id, ct);

        if (survey is null || !CanAccessSurvey(survey)) return null;

        return new SurveyDetailDto(
            survey.Id, survey.Title, survey.SubjectEmployeeId, survey.SubjectEmployee.Name,
            survey.Status, survey.DueDate, survey.ResultsPublished,
            survey.Questions.OrderBy(q => q.Order).Select(QuestionMapping.ToQuestionInput).ToList(),
            survey.Assignments.Select(a => new AssignmentDto(
                a.Id, a.SurveyId, survey.Title, survey.SubjectEmployee.Name,
                a.ReviewerType, a.Status, survey.DueDate, a.CompletedAt)).ToList());
    }

    public async Task<SurveyDto> CreateAsync(CreateSurveyRequest request, CancellationToken ct = default)
    {
        EnsureManager();
        await ValidateDirectReport(request.SubjectEmployeeId, ct);
        ValidateQuestions(request.Questions);
        await _categoryService.ValidateCategoryIdsAsync(request.Questions.Select(q => q.CategoryId), ct);

        var survey = new Survey
        {
            Id = Guid.NewGuid(),
            Title = request.Title,
            SubjectEmployeeId = request.SubjectEmployeeId,
            CreatedById = _currentUser.UserId,
            Status = SurveyStatus.Draft,
            DueDate = DateTimeNormalization.ToUtc(request.DueDate),
            Questions = request.Questions.Select(q => new SurveyQuestion
            {
                Id = Guid.NewGuid(),
                Order = q.Order,
                Type = q.Type,
                Text = q.Text,
                CategoryId = q.CategoryId
            }).ToList()
        };

        _db.Surveys.Add(survey);
        await _db.SaveChangesAsync(ct);
        return MapSurvey(await _db.Surveys.Include(s => s.SubjectEmployee).Include(s => s.Assignments).FirstAsync(s => s.Id == survey.Id, ct));
    }

    public async Task<SurveyDto> CreateFromTemplateAsync(Guid templateId, CreateSurveyFromTemplateRequest request, CancellationToken ct = default)
    {
        EnsureManager();
        await ValidateDirectReport(request.SubjectEmployeeId, ct);

        var userId = _currentUser.UserId;
        var template = await _db.SurveyTemplates.Include(t => t.Questions)
            .FirstOrDefaultAsync(t => t.Id == templateId && (t.CreatedById == userId || t.Shares.Any(s => s.SharedWithManagerId == userId)), ct)
            ?? throw new KeyNotFoundException("Template not found.");

        var survey = new Survey
        {
            Id = Guid.NewGuid(),
            Title = request.Title ?? $"{template.Name} - 360 Review",
            TemplateId = templateId,
            SubjectEmployeeId = request.SubjectEmployeeId,
            CreatedById = userId,
            Status = SurveyStatus.Draft,
            DueDate = DateTimeNormalization.ToUtc(request.DueDate),
            Questions = template.Questions.Select(q => new SurveyQuestion
            {
                Id = Guid.NewGuid(),
                Order = q.Order,
                Type = q.Type,
                Text = q.Text,
                CategoryId = q.CategoryId
            }).ToList()
        };

        _db.Surveys.Add(survey);
        await _db.SaveChangesAsync(ct);
        return MapSurvey(await _db.Surveys.Include(s => s.SubjectEmployee).Include(s => s.Assignments).FirstAsync(s => s.Id == survey.Id, ct));
    }

    public async Task AssignAsync(Guid id, AssignSurveyRequest request, CancellationToken ct = default)
    {
        EnsureManager();
        var survey = await _db.Surveys.Include(s => s.SubjectEmployee)
            .FirstOrDefaultAsync(s => s.Id == id && s.CreatedById == _currentUser.UserId, ct)
            ?? throw new KeyNotFoundException("Survey not found.");

        if (survey.Status != SurveyStatus.Draft)
            throw new InvalidOperationException("Can only assign peers on draft surveys.");

        var existingAssignments = await _db.SurveyAssignments
            .Where(a => a.SurveyId == id)
            .ToListAsync(ct);

        void AddAssignment(Guid reviewerId, ReviewerType reviewerType)
        {
            if (existingAssignments.Any(a => a.ReviewerType == reviewerType && reviewerType != ReviewerType.Peer)) return;
            if (reviewerType == ReviewerType.Peer && existingAssignments.Any(a => a.ReviewerId == reviewerId)) return;

            var assignment = new SurveyAssignment
            {
                Id = Guid.NewGuid(),
                SurveyId = id,
                ReviewerId = reviewerId,
                ReviewerType = reviewerType
            };
            _db.SurveyAssignments.Add(assignment);
            existingAssignments.Add(assignment);
        }

        AddAssignment(survey.SubjectEmployeeId, ReviewerType.Self);
        AddAssignment(_currentUser.UserId, ReviewerType.Manager);

        foreach (var peerId in request.PeerIds.Distinct())
        {
            if (peerId == survey.SubjectEmployeeId || peerId == _currentUser.UserId) continue;

            var peer = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == peerId && u.IsActive, ct);
            if (peer is null) continue;

            AddAssignment(peerId, ReviewerType.Peer);
        }

        survey.Status = SurveyStatus.Active;
        await _db.SaveChangesAsync(ct);
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        EnsureManager();
        var survey = await _db.Surveys.FirstOrDefaultAsync(s => s.Id == id && s.CreatedById == _currentUser.UserId, ct)
            ?? throw new KeyNotFoundException("Survey not found.");
        _db.Surveys.Remove(survey);
        await _db.SaveChangesAsync(ct);
    }

    public async Task PublishResultsAsync(Guid id, CancellationToken ct = default)
    {
        if (_currentUser.Role == UserRole.Admin)
            throw new UnauthorizedAccessException("Admins cannot publish results.");

        EnsureManager();
        var survey = await _db.Surveys.FirstOrDefaultAsync(s => s.Id == id && s.CreatedById == _currentUser.UserId, ct)
            ?? throw new KeyNotFoundException("Survey not found.");

        survey.ResultsPublished = true;
        survey.Status = SurveyStatus.Closed;
        await _db.SaveChangesAsync(ct);
    }

    public async Task<List<UserDto>> GetDirectReportsAsync(CancellationToken ct = default)
    {
        EnsureManager();
        var reports = await _db.Users.Where(u => u.ManagerId == _currentUser.UserId && u.IsActive)
            .OrderBy(u => u.Name).ToListAsync(ct);
        return reports.Select(AuthService.MapUser).ToList();
    }

    public async Task<List<UserDto>> GetPeerCandidatesAsync(Guid employeeId, CancellationToken ct = default)
    {
        EnsureManager();
        await ValidateDirectReport(employeeId, ct);
        var users = await _db.Users.Where(u => u.IsActive && u.Id != employeeId && u.Role != UserRole.Admin)
            .OrderBy(u => u.Name).ToListAsync(ct);
        return users.Select(AuthService.MapUser).ToList();
    }

    public async Task<List<UserDto>> GetManagersAsync(CancellationToken ct = default)
    {
        EnsureManager();
        var managers = await _db.Users.Where(u => u.IsActive && u.Role == UserRole.Manager)
            .OrderBy(u => u.Name).ToListAsync(ct);
        return managers.Select(AuthService.MapUser).ToList();
    }

    private bool CanAccessSurvey(Survey survey) =>
        _currentUser.Role == UserRole.Manager && survey.CreatedById == _currentUser.UserId
        || survey.Assignments.Any(a => a.ReviewerId == _currentUser.UserId)
        || _currentUser.Role == UserRole.Employee && survey.SubjectEmployeeId == _currentUser.UserId && survey.ResultsPublished;

    private SurveyDto MapSurvey(Survey s) => new(
        s.Id, s.Title, s.SubjectEmployeeId, s.SubjectEmployee.Name, s.CreatedById,
        s.Status, s.DueDate, s.ResultsPublished, s.CreatedAt,
        s.Assignments.Count, s.Assignments.Count(a => a.Status == AssignmentStatus.Completed));

    private async Task ValidateDirectReport(Guid employeeId, CancellationToken ct)
    {
        var employee = await _db.Users.FirstOrDefaultAsync(u => u.Id == employeeId, ct)
            ?? throw new KeyNotFoundException("Employee not found.");
        if (employee.ManagerId != _currentUser.UserId)
            throw new UnauthorizedAccessException("Employee is not a direct report.");
    }

    private static void ValidateQuestions(List<QuestionInput> questions)
    {
        if (questions.Count == 0) throw new InvalidOperationException("At least one question is required.");
        foreach (var q in questions)
        {
            if (string.IsNullOrWhiteSpace(q.Text)) throw new InvalidOperationException("Question text is required.");
            if (q.CategoryId == Guid.Empty) throw new InvalidOperationException("Each question must have a category.");
        }
    }

    private void EnsureManager()
    {
        if (_currentUser.Role != UserRole.Manager)
            throw new UnauthorizedAccessException("Only managers can manage surveys.");
    }
}
