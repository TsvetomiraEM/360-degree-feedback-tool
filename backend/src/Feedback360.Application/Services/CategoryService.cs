using Feedback360.Application.Common;
using Feedback360.Application.DTOs;
using Feedback360.Domain.Entities;
using Feedback360.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Feedback360.Application.Services;

public class CategoryService
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public CategoryService(IApplicationDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<List<QuestionCategoryDto>> GetAllAsync(CancellationToken ct = default)
    {
        EnsureManager();
        return await _db.QuestionCategories
            .Include(c => c.CreatedBy)
            .OrderBy(c => c.Name)
            .Select(c => new QuestionCategoryDto(c.Id, c.Name, c.CreatedBy.Name, c.CreatedAt))
            .ToListAsync(ct);
    }

    public async Task<QuestionCategoryDto> CreateAsync(CreateQuestionCategoryRequest request, CancellationToken ct = default)
    {
        EnsureManager();
        var name = request.Name.Trim();
        if (string.IsNullOrWhiteSpace(name))
            throw new InvalidOperationException("Category name is required.");
        if (name.Length > 100)
            throw new InvalidOperationException("Category name cannot exceed 100 characters.");

        var existing = await _db.QuestionCategories
            .Include(c => c.CreatedBy)
            .FirstOrDefaultAsync(c => c.Name.ToLower() == name.ToLower(), ct);
        if (existing is not null)
            return new QuestionCategoryDto(existing.Id, existing.Name, existing.CreatedBy.Name, existing.CreatedAt);

        var category = new QuestionCategory
        {
            Id = Guid.NewGuid(),
            Name = name,
            CreatedById = _currentUser.UserId,
            CreatedAt = DateTime.UtcNow
        };

        _db.QuestionCategories.Add(category);
        await _db.SaveChangesAsync(ct);

        var creator = await _db.Users.FirstAsync(u => u.Id == _currentUser.UserId, ct);
        return new QuestionCategoryDto(category.Id, category.Name, creator.Name, category.CreatedAt);
    }

    public async Task ValidateCategoryIdsAsync(IEnumerable<Guid> categoryIds, CancellationToken ct = default)
    {
        var ids = categoryIds.Distinct().ToList();
        if (ids.Count == 0) return;

        var found = await _db.QuestionCategories.CountAsync(c => ids.Contains(c.Id), ct);
        if (found != ids.Count)
            throw new InvalidOperationException("One or more question categories are invalid.");
    }

    private void EnsureManager()
    {
        if (_currentUser.Role != UserRole.Manager)
            throw new UnauthorizedAccessException("Only managers can access question categories.");
    }
}
