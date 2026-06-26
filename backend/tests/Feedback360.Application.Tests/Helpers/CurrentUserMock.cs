using Feedback360.Application.Common;
using Feedback360.Domain.Enums;
using NSubstitute;

namespace Feedback360.Application.Tests.Helpers;

public static class CurrentUserMock
{
    public static ICurrentUserService Create(Guid userId, UserRole role, string email = "test@feedback360.local")
    {
        var mock = Substitute.For<ICurrentUserService>();
        mock.UserId.Returns(userId);
        mock.Role.Returns(role);
        mock.Email.Returns(email);
        mock.IsAuthenticated.Returns(true);
        return mock;
    }

    public static ICurrentUserService AsAdmin() =>
        Create(TestIds.AdminId, UserRole.Admin, "admin@feedback360.local");

    public static ICurrentUserService AsManager(Guid? userId = null) =>
        Create(userId ?? TestIds.ManagerId, UserRole.Manager, "manager@feedback360.local");

    public static ICurrentUserService AsEmployee(Guid? userId = null) =>
        Create(userId ?? TestIds.Employee1Id, UserRole.Employee, "alice@feedback360.local");
}
