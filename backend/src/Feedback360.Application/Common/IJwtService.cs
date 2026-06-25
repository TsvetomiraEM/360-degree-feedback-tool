using Feedback360.Domain.Entities;

namespace Feedback360.Application.Common;

public interface IJwtService
{
    string GenerateAccessToken(User user);
    string GenerateRefreshToken();
    Guid? ValidateRefreshToken(string token);
}
