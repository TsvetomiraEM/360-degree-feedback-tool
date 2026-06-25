using Feedback360.Domain.Enums;

namespace Feedback360.Application.Common;

public interface IAuditLogService
{
    Task LogAsync(AuditAction action, Guid? targetUserId, object metadata, CancellationToken cancellationToken = default);
}
