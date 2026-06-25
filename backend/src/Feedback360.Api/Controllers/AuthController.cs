using System.Text;
using Feedback360.Application.DTOs;
using Feedback360.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Feedback360.Api.Controllers;

[ApiController]
[Route("api/v1/auth")]
public class AuthController : ControllerBase
{
    private readonly AuthService _authService;

    public AuthController(AuthService authService) => _authService = authService;

    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<ActionResult<AuthResponse>> Login([FromBody] LoginRequest request, CancellationToken ct)
    {
        var result = await _authService.LoginAsync(request, ct);
        return result is null ? Unauthorized() : Ok(result);
    }

    [HttpPost("google")]
    [AllowAnonymous]
    public async Task<ActionResult<AuthResponse>> Google([FromBody] GoogleLoginRequest request, CancellationToken ct)
    {
        var result = await _authService.GoogleLoginAsync(request, ct);
        return result is null ? Unauthorized() : Ok(result);
    }

    [HttpGet("me")]
    [Authorize]
    public async Task<ActionResult<UserDto>> Me(CancellationToken ct)
    {
        var userId = Guid.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)!.Value);
        var result = await _authService.GetMeAsync(userId, ct);
        return result is null ? NotFound() : Ok(result);
    }
}
