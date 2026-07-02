using Feedback360.Application.Common;
using Google.Apis.Auth;
using Microsoft.Extensions.Configuration;

namespace Feedback360.Infrastructure.Auth;

public class GoogleTokenValidator : IGoogleTokenValidator
{
    private readonly string? _clientId;

    public GoogleTokenValidator(IConfiguration config) => _clientId = config["Google:ClientId"];

    public async Task<GoogleUserInfo?> ValidateAsync(string idToken, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_clientId) || _clientId == "your-google-client-id")
            return null;

        try
        {
            var settings = new GoogleJsonWebSignature.ValidationSettings { Audience = new[] { _clientId } };
            var payload = await GoogleJsonWebSignature.ValidateAsync(idToken, settings);
            return new GoogleUserInfo(payload.Email, payload.Name);
        }
        catch
        {
            return null;
        }
    }
}
