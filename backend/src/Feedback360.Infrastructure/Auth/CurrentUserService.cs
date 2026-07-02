using System.Security.Claims;
using System.Text.Json;
using Feedback360.Application.Common;
using Feedback360.Domain.Entities;
using Feedback360.Domain.Enums;
using Feedback360.Infrastructure.Persistence;
using Microsoft.AspNetCore.Http;

namespace Feedback360.Infrastructure.Auth;

public class CurrentUserService : ICurrentUserService
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CurrentUserService(IHttpContextAccessor httpContextAccessor) => _httpContextAccessor = httpContextAccessor;

    public bool IsAuthenticated => _httpContextAccessor.HttpContext?.User?.Identity?.IsAuthenticated ?? false;

    public Guid UserId => Guid.Parse(_httpContextAccessor.HttpContext!.User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    public string Email => _httpContextAccessor.HttpContext!.User.FindFirstValue(ClaimTypes.Email)!;

    public UserRole Role => Enum.Parse<UserRole>(_httpContextAccessor.HttpContext!.User.FindFirstValue(ClaimTypes.Role)!);

    public Guid? ManagerId
    {
        get
        {
            var val = _httpContextAccessor.HttpContext!.User.FindFirstValue("manager_id");
            return string.IsNullOrEmpty(val) ? null : Guid.Parse(val);
        }
    }
}

public class AuditLogService : IAuditLogService
{
    private readonly AppDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public AuditLogService(AppDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task LogAsync(AuditAction action, Guid? targetUserId, object metadata, CancellationToken cancellationToken = default)
    {
        _db.AuditLogs.Add(new AuditLog
        {
            Id = Guid.NewGuid(),
            ActorUserId = _currentUser.UserId,
            Action = action,
            TargetUserId = targetUserId,
            Metadata = JsonSerializer.Serialize(metadata),
            CreatedAt = DateTime.UtcNow
        });
        await _db.SaveChangesAsync(cancellationToken);
    }
}
