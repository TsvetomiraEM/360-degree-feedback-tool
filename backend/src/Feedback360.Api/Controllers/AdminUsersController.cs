using Feedback360.Application.DTOs;
using Feedback360.Application.Services;
using Feedback360.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Feedback360.Api.Controllers;

[ApiController]
[Route("api/v1/admin/users")]
[Authorize(Roles = nameof(UserRole.Admin))]
public class AdminUsersController : ControllerBase
{
    private readonly UserService _userService;

    public AdminUsersController(UserService userService) => _userService = userService;

    [HttpGet]
    public async Task<ActionResult<List<UserDto>>> GetAll(CancellationToken ct) =>
        Ok(await _userService.GetAllAsync(ct));

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<UserDto>> GetById(Guid id, CancellationToken ct)
    {
        var user = await _userService.GetByIdAsync(id, ct);
        return user is null ? NotFound() : Ok(user);
    }

    [HttpPost]
    public async Task<ActionResult<UserDto>> Create([FromBody] CreateUserRequest request, CancellationToken ct)
    {
        try
        {
            var user = await _userService.CreateAsync(request, ct);
            return CreatedAtAction(nameof(GetById), new { id = user.Id }, user);
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { message = ex.Message });
        }
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<UserDto>> Update(Guid id, [FromBody] UpdateUserRequest request, CancellationToken ct)
    {
        try
        {
            return Ok(await _userService.UpdateAsync(id, request, ct));
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { message = ex.Message });
        }
    }

    [HttpPatch("{id:guid}/activate")]
    public async Task<IActionResult> SetActive(Guid id, [FromBody] SetActiveRequest request, CancellationToken ct)
    {
        try
        {
            await _userService.SetActiveAsync(id, request.IsActive, ct);
            return NoContent();
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        try
        {
            await _userService.DeleteAsync(id, ct);
            return NoContent();
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { message = ex.Message });
        }
    }
}
