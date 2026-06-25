using Feedback360.Application.DTOs;
using Feedback360.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Feedback360.Api.Controllers;

[ApiController]
[Route("api/v1/assignments")]
[Authorize]
public class AssignmentsController : ControllerBase
{
    private readonly AssignmentService _assignmentService;

    public AssignmentsController(AssignmentService assignmentService) => _assignmentService = assignmentService;

    [HttpGet("mine")]
    public async Task<ActionResult<List<AssignmentDto>>> GetMine(CancellationToken ct)
    {
        try
        {
            return Ok(await _assignmentService.GetMineAsync(ct));
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<AssignmentDetailDto>> GetById(Guid id, CancellationToken ct)
    {
        try
        {
            var assignment = await _assignmentService.GetByIdAsync(id, ct);
            return assignment is null ? NotFound() : Ok(assignment);
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
    }

    [HttpPost("{id:guid}/submit")]
    public async Task<IActionResult> Submit(Guid id, [FromBody] SubmitResponsesRequest request, CancellationToken ct)
    {
        try
        {
            await _assignmentService.SubmitAsync(id, request, ct);
            return NoContent();
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
    }
}
