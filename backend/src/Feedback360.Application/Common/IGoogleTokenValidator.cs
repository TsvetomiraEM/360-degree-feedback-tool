namespace Feedback360.Application.Common;

public interface IGoogleTokenValidator
{
    Task<GoogleUserInfo?> ValidateAsync(string idToken, CancellationToken cancellationToken = default);
}

public record GoogleUserInfo(string Email, string Name);
