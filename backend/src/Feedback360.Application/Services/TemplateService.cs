using Feedback360.Application.Common;
using Feedback360.Application.DTOs;
using Feedback360.Domain.Entities;
using Feedback360.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Feedback360.Application.Services;

public class TemplateService
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUserService _currentUser;
    private readonly CategoryService _categoryService;

    public TemplateService(IApplicationDbContext db, ICurrentUserService currentUser, CategoryService categoryService)
    {
        _db = db;
        _currentUser = currentUser;
        _categoryService = categoryService;
    }

    public async Task<List<TemplateDto>> GetAllAsync(CancellationToken ct = default)
    {
        EnsureManager();
        var userId = _currentUser.UserId;
        var templates = await _db.SurveyTemplates
            .Include(t => t.Questions).ThenInclude(q => q.Category)
            .Include(t => t.CreatedBy)
            .Include(t => t.Shares)
            .Where(t => t.CreatedById == userId || t.Shares.Any(s => s.SharedWithManagerId == userId))
            .OrderByDescending(t => t.UpdatedAt)
            .ToListAsync(ct);

        return templates.Select(MapTemplate).ToList();
    }

    public async Task<TemplateDto?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        EnsureManager();
        var template = await GetAccessibleTemplate(id, ct);
        return template is null ? null : MapTemplate(template);
    }

    public async Task<TemplateDto> CreateAsync(CreateTemplateRequest request, CancellationToken ct = default)
    {
        EnsureManager();
        ValidateQuestions(request.Questions);
        await _categoryService.ValidateCategoryIdsAsync(request.Questions.Select(q => q.CategoryId), ct);

        var template = new SurveyTemplate
        {
            Id = Guid.NewGuid(),
            Name = request.Name,
            Description = request.Description,
            CreatedById = _currentUser.UserId,
            Questions = request.Questions.Select(q => new TemplateQuestion
            {
                Id = Guid.NewGuid(),
                Order = q.Order,
                Type = q.Type,
                Text = q.Text,
                CategoryId = q.CategoryId
            }).ToList()
        };

        _db.SurveyTemplates.Add(template);
        await _db.SaveChangesAsync(ct);
        return MapTemplate(await _db.SurveyTemplates.Include(t => t.Questions).ThenInclude(q => q.Category).Include(t => t.CreatedBy).Include(t => t.Shares)
            .FirstAsync(t => t.Id == template.Id, ct));
    }

    public async Task<TemplateDto> UpdateAsync(Guid id, UpdateTemplateRequest request, CancellationToken ct = default)
    {
        EnsureManager();
        ValidateQuestions(request.Questions);
        await _categoryService.ValidateCategoryIdsAsync(request.Questions.Select(q => q.CategoryId), ct);

        var template = await _db.SurveyTemplates.Include(t => t.Questions).Include(t => t.Shares)
            .FirstOrDefaultAsync(t => t.Id == id && t.CreatedById == _currentUser.UserId, ct)
            ?? throw new KeyNotFoundException("Template not found or not owned.");

        template.Name = request.Name;
        template.Description = request.Description;
        template.UpdatedAt = DateTime.UtcNow;
        _db.TemplateQuestions.RemoveRange(template.Questions);
        template.Questions = request.Questions.Select(q => new TemplateQuestion
        {
            Id = Guid.NewGuid(),
            TemplateId = template.Id,
            Order = q.Order,
            Type = q.Type,
            Text = q.Text,
            CategoryId = q.CategoryId
        }).ToList();

        await _db.SaveChangesAsync(ct);
        return MapTemplate(await _db.SurveyTemplates.Include(t => t.Questions).ThenInclude(q => q.Category).Include(t => t.CreatedBy).Include(t => t.Shares)
            .FirstAsync(t => t.Id == id, ct));
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        EnsureManager();
        var template = await _db.SurveyTemplates.FirstOrDefaultAsync(t => t.Id == id && t.CreatedById == _currentUser.UserId, ct)
            ?? throw new KeyNotFoundException("Template not found or not owned.");
        _db.SurveyTemplates.Remove(template);
        await _db.SaveChangesAsync(ct);
    }

    public async Task ShareAsync(Guid id, ShareTemplateRequest request, CancellationToken ct = default)
    {
        EnsureManager();
        var template = await _db.SurveyTemplates.Include(t => t.Shares)
            .FirstOrDefaultAsync(t => t.Id == id && t.CreatedById == _currentUser.UserId, ct)
            ?? throw new KeyNotFoundException("Template not found or not owned.");

        foreach (var managerId in request.ManagerIds.Distinct())
        {
            if (managerId == _currentUser.UserId) continue;
            var manager = await _db.Users.FirstOrDefaultAsync(u => u.Id == managerId && u.Role == UserRole.Manager, ct);
            if (manager is null) continue;
            if (template.Shares.Any(s => s.SharedWithManagerId == managerId)) continue;

            template.Shares.Add(new TemplateShare
            {
                Id = Guid.NewGuid(),
                TemplateId = id,
                SharedWithManagerId = managerId
            });
        }

        await _db.SaveChangesAsync(ct);
    }

    private async Task<SurveyTemplate?> GetAccessibleTemplate(Guid id, CancellationToken ct)
    {
        var userId = _currentUser.UserId;
        return await _db.SurveyTemplates
            .Include(t => t.Questions).ThenInclude(q => q.Category)
            .Include(t => t.CreatedBy)
            .Include(t => t.Shares)
            .FirstOrDefaultAsync(t => t.Id == id && (t.CreatedById == userId || t.Shares.Any(s => s.SharedWithManagerId == userId)), ct);
    }

    private TemplateDto MapTemplate(SurveyTemplate t) => new(
        t.Id, t.Name, t.Description, t.CreatedById, t.CreatedBy.Name, t.CreatedAt,
        t.Questions.OrderBy(q => q.Order).Select(QuestionMapping.ToQuestionInput).ToList(),
        t.CreatedById == _currentUser.UserId,
        t.Shares.Any(s => s.SharedWithManagerId == _currentUser.UserId));

    private static void ValidateQuestions(List<QuestionInput> questions)
    {
        if (questions.Count == 0) throw new InvalidOperationException("At least one question is required.");
        foreach (var q in questions)
        {
            if (string.IsNullOrWhiteSpace(q.Text)) throw new InvalidOperationException("Question text is required.");
            if (q.CategoryId == Guid.Empty) throw new InvalidOperationException("Each question must have a category.");
            if (q.Type == QuestionType.OpenText && q.Text.Length > 300)
                throw new InvalidOperationException("Question text cannot exceed 300 characters for open text type.");
        }
    }

    private void EnsureManager()
    {
        if (_currentUser.Role != UserRole.Manager)
            throw new UnauthorizedAccessException("Only managers can access templates.");
    }
}
