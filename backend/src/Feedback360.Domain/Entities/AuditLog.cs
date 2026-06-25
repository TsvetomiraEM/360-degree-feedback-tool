using Feedback360.Domain.Enums;

namespace Feedback360.Domain.Entities;

public class AuditLog
{
    public Guid Id { get; set; }
    public Guid ActorUserId { get; set; }
    public AuditAction Action { get; set; }
    public Guid? TargetUserId { get; set; }
    public string Metadata { get; set; } = "{}";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public User Actor { get; set; } = null!;
    public User? TargetUser { get; set; }
}
