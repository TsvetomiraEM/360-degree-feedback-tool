using Feedback360.Application.Common;
using Feedback360.Application.DTOs;
using Feedback360.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Feedback360.Application.Services;

public class AuditLogQueryService
{
    private readonly IApplicationDbContext _db;

    public AuditLogQueryService(IApplicationDbContext db) => _db = db;

    public async Task<PagedResult<AuditLogDto>> GetAsync(AuditLogQuery query, CancellationToken ct = default)
    {
        var q = _db.AuditLogs.Include(a => a.Actor).Include(a => a.TargetUser).AsQueryable();

        if (query.Action.HasValue)
            q = q.Where(a => a.Action == query.Action.Value);
        if (query.From.HasValue)
            q = q.Where(a => a.CreatedAt >= query.From.Value);
        if (query.To.HasValue)
            q = q.Where(a => a.CreatedAt <= query.To.Value);

        var total = await q.CountAsync(ct);
        var items = await q.OrderByDescending(a => a.CreatedAt)
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .Select(a => new AuditLogDto(
                a.Id, a.ActorUserId, a.Actor.Name, a.Action, a.TargetUserId,
                a.TargetUser != null ? a.TargetUser.Name : null, a.Metadata, a.CreatedAt))
            .ToListAsync(ct);

        return new PagedResult<AuditLogDto>(items, total, query.Page, query.PageSize);
    }
}
