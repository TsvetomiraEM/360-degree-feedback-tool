using System.Text.Json;
using Feedback360.Application.Common;
using Feedback360.Application.DTOs;
using Feedback360.Domain.Entities;
using Feedback360.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Feedback360.Application.Services;

public class AuthService
{
    private readonly IApplicationDbContext _db;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IJwtService _jwtService;
    private readonly IGoogleTokenValidator _googleValidator;

    public AuthService(IApplicationDbContext db, IPasswordHasher passwordHasher, IJwtService jwtService, IGoogleTokenValidator googleValidator)
    {
        _db = db;
        _passwordHasher = passwordHasher;
        _jwtService = jwtService;
        _googleValidator = googleValidator;
    }

    public async Task<AuthResponse?> LoginAsync(LoginRequest request, CancellationToken ct = default)
    {
        var user = await _db.Users.Include(u => u.Manager)
            .FirstOrDefaultAsync(u => u.Email == request.Email.ToLowerInvariant(), ct);
        if (user is null || !user.IsActive || string.IsNullOrEmpty(user.PasswordHash)) return null;
        if (!_passwordHasher.Verify(request.Password, user.PasswordHash)) return null;
        return CreateAuthResponse(user);
    }

    public async Task<AuthResponse?> GoogleLoginAsync(GoogleLoginRequest request, CancellationToken ct = default)
    {
        var googleUser = await _googleValidator.ValidateAsync(request.IdToken, ct);
        if (googleUser is null) return null;

        var user = await _db.Users.Include(u => u.Manager)
            .FirstOrDefaultAsync(u => u.Email == googleUser.Email.ToLowerInvariant(), ct);
        if (user is null || !user.IsActive) return null;

        if (user.AuthProvider == "local")
        {
            user.AuthProvider = "google";
            await _db.SaveChangesAsync(ct);
        }

        return CreateAuthResponse(user);
    }

    public async Task<UserDto?> GetMeAsync(Guid userId, CancellationToken ct = default)
    {
        var user = await _db.Users.Include(u => u.Manager).FirstOrDefaultAsync(u => u.Id == userId, ct);
        return user is null ? null : MapUser(user);
    }

    private AuthResponse CreateAuthResponse(User user) =>
        new(_jwtService.GenerateAccessToken(user), _jwtService.GenerateRefreshToken(), MapUser(user));

    public static UserDto MapUser(User user) =>
        new(user.Id, user.Email, user.Name, user.Role, user.ManagerId, user.Manager?.Name, user.IsActive, user.AuthProvider);
}
