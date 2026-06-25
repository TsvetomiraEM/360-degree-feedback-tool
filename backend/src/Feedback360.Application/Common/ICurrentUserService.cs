using Feedback360.Domain.Enums;

namespace Feedback360.Application.Common;

public interface ICurrentUserService
{
    Guid UserId { get; }
    string Email { get; }
    UserRole Role { get; }
    Guid? ManagerId { get; }
    bool IsAuthenticated { get; }
}
