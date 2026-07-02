using System.Text.Json;
using Feedback360.Application.Common;
using Feedback360.Application.DTOs;
using Feedback360.Domain.Entities;
using Feedback360.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Feedback360.Application.Services;

public class UserService
{
    private readonly IApplicationDbContext _db;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IAuditLogService _auditLog;
    private readonly ICurrentUserService _currentUser;

    public UserService(IApplicationDbContext db, IPasswordHasher passwordHasher, IAuditLogService auditLog, ICurrentUserService currentUser)
    {
        _db = db;
        _passwordHasher = passwordHasher;
        _auditLog = auditLog;
        _currentUser = currentUser;
    }

    public async Task<List<UserDto>> GetAllAsync(CancellationToken ct = default)
    {
        var users = await _db.Users.Include(u => u.Manager).OrderBy(u => u.Name).ToListAsync(ct);
        return users.Select(AuthService.MapUser).ToList();
    }

    public async Task<UserDto?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var user = await _db.Users.Include(u => u.Manager).FirstOrDefaultAsync(u => u.Id == id, ct);
        return user is null ? null : AuthService.MapUser(user);
    }

    public async Task<UserDto> CreateAsync(CreateUserRequest request, CancellationToken ct = default)
    {
        if (await _db.Users.AnyAsync(u => u.Email == request.Email.ToLowerInvariant(), ct))
            throw new InvalidOperationException("Email already exists.");

        if (request.Role != UserRole.Admin && request.ManagerId.HasValue)
            await ValidateManagerAsync(request.ManagerId.Value, ct);

        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = request.Email.ToLowerInvariant(),
            Name = request.Name,
            Role = request.Role,
            ManagerId = request.Role == UserRole.Admin ? null : request.ManagerId,
            IsActive = true,
            AuthProvider = "local",
            PasswordHash = string.IsNullOrWhiteSpace(request.Password) ? null : _passwordHasher.Hash(request.Password!)
        };

        _db.Users.Add(user);
        await _db.SaveChangesAsync(ct);
        await _auditLog.LogAsync(AuditAction.UserCreated, user.Id, new { user.Email, user.Role }, ct);

        return AuthService.MapUser(await _db.Users.Include(u => u.Manager).FirstAsync(u => u.Id == user.Id, ct));
    }

    public async Task<UserDto> UpdateAsync(Guid id, UpdateUserRequest request, CancellationToken ct = default)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == id, ct)
            ?? throw new KeyNotFoundException("User not found.");

        if (await _db.Users.AnyAsync(u => u.Email == request.Email.ToLowerInvariant() && u.Id != id, ct))
            throw new InvalidOperationException("Email already exists.");

        if (request.Role != UserRole.Admin && request.ManagerId.HasValue)
            await ValidateManagerAsync(request.ManagerId.Value, ct);

        var oldRole = user.Role;
        user.Email = request.Email.ToLowerInvariant();
        user.Name = request.Name;
        user.Role = request.Role;
        user.ManagerId = request.Role == UserRole.Admin ? null : request.ManagerId;
        if (!string.IsNullOrWhiteSpace(request.Password))
            user.PasswordHash = _passwordHasher.Hash(request.Password);

        await _db.SaveChangesAsync(ct);
        await _auditLog.LogAsync(AuditAction.UserUpdated, user.Id, new { user.Email, OldRole = oldRole, NewRole = user.Role }, ct);

        return AuthService.MapUser(await _db.Users.Include(u => u.Manager).FirstAsync(u => u.Id == id, ct));
    }

    public async Task SetActiveAsync(Guid id, bool isActive, CancellationToken ct = default)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == id, ct)
            ?? throw new KeyNotFoundException("User not found.");

        user.IsActive = isActive;
        await _db.SaveChangesAsync(ct);
        await _auditLog.LogAsync(isActive ? AuditAction.UserActivated : AuditAction.UserDeactivated, user.Id, new { user.Email }, ct);
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == id, ct)
            ?? throw new KeyNotFoundException("User not found.");

        if (user.Role == UserRole.Admin)
        {
            var adminCount = await _db.Users.CountAsync(u => u.Role == UserRole.Admin, ct);
            if (adminCount <= 1)
                throw new InvalidOperationException("Cannot delete the last admin account.");
        }

        var surveysRemoved = await _db.Surveys.CountAsync(s => s.SubjectEmployeeId == id || s.CreatedById == id, ct);
        var responsesRemoved = await _db.Responses.CountAsync(r => r.Assignment.ReviewerId == id, ct);

        // Null manager references on direct reports
        var directReports = await _db.Users.Where(u => u.ManagerId == id).ToListAsync(ct);
        foreach (var report in directReports)
            report.ManagerId = null;

        // Delete subject surveys (cascade assignments/responses via EF)
        var subjectSurveys = await _db.Surveys.Where(s => s.SubjectEmployeeId == id).ToListAsync(ct);
        _db.Surveys.RemoveRange(subjectSurveys);

        // Delete surveys created by user (not already removed as subject)
        var createdSurveys = await _db.Surveys.Where(s => s.CreatedById == id && s.SubjectEmployeeId != id).ToListAsync(ct);
        _db.Surveys.RemoveRange(createdSurveys);

        // Delete reviewer assignments and responses
        var assignments = await _db.SurveyAssignments
            .Include(a => a.Responses)
            .Where(a => a.ReviewerId == id)
            .ToListAsync(ct);
        _db.SurveyAssignments.RemoveRange(assignments);

        // Delete templates and shares
        var templates = await _db.SurveyTemplates.Where(t => t.CreatedById == id).ToListAsync(ct);
        _db.SurveyTemplates.RemoveRange(templates);
        var shares = await _db.TemplateShares.Where(ts => ts.SharedWithManagerId == id).ToListAsync(ct);
        _db.TemplateShares.RemoveRange(shares);

        var email = user.Email;
        _db.Users.Remove(user);
        await _db.SaveChangesAsync(ct);

        await _auditLog.LogAsync(AuditAction.UserDeleted, null, new { deletedUserId = id, deletedEmail = email, surveysRemoved, responsesRemoved }, ct);
    }

    private async Task ValidateManagerAsync(Guid managerId, CancellationToken ct)
    {
        var manager = await _db.Users.FirstOrDefaultAsync(u => u.Id == managerId, ct);
        if (manager is null || manager.Role != UserRole.Manager)
            throw new InvalidOperationException("Invalid manager.");
    }
}
