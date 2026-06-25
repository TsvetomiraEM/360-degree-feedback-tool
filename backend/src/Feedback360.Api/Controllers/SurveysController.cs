using Feedback360.Application.DTOs;
using Feedback360.Application.Services;
using Feedback360.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Feedback360.Api.Controllers;

[ApiController]
[Route("api/v1/surveys")]
[Authorize]
public class SurveysController : ControllerBase
{
    private readonly SurveyService _surveyService;

    public SurveysController(SurveyService surveyService) => _surveyService = surveyService;

    [HttpGet]
    public async Task<ActionResult<List<SurveyDto>>> GetAll(CancellationToken ct)
    {
        try
        {
            return Ok(await _surveyService.GetAllAsync(ct));
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<SurveyDetailDto>> GetById(Guid id, CancellationToken ct)
    {
        try
        {
            var survey = await _surveyService.GetByIdAsync(id, ct);
            return survey is null ? NotFound() : Ok(survey);
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
    }

    [HttpPost]
    [Authorize(Roles = nameof(UserRole.Manager))]
    public async Task<ActionResult<SurveyDto>> Create([FromBody] CreateSurveyRequest request, CancellationToken ct)
    {
        try
        {
            var survey = await _surveyService.CreateAsync(request, ct);
            return CreatedAtAction(nameof(GetById), new { id = survey.Id }, survey);
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

    [HttpPost("from-template/{templateId:guid}")]
    [Authorize(Roles = nameof(UserRole.Manager))]
    public async Task<ActionResult<SurveyDto>> CreateFromTemplate(Guid templateId, [FromBody] CreateSurveyFromTemplateRequest request, CancellationToken ct)
    {
        try
        {
            var survey = await _surveyService.CreateFromTemplateAsync(templateId, request, ct);
            return CreatedAtAction(nameof(GetById), new { id = survey.Id }, survey);
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
    }

    [HttpPost("{id:guid}/assign")]
    [Authorize(Roles = nameof(UserRole.Manager))]
    public async Task<IActionResult> Assign(Guid id, [FromBody] AssignSurveyRequest request, CancellationToken ct)
    {
        try
        {
            await _surveyService.AssignAsync(id, request, ct);
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
    }

    [HttpPost("{id:guid}/publish-results")]
    [Authorize(Roles = nameof(UserRole.Manager))]
    public async Task<IActionResult> PublishResults(Guid id, CancellationToken ct)
    {
        try
        {
            await _surveyService.PublishResultsAsync(id, ct);
            return NoContent();
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Roles = nameof(UserRole.Manager))]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        try
        {
            await _surveyService.DeleteAsync(id, ct);
            return NoContent();
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
    }

    [HttpGet("direct-reports")]
    [Authorize(Roles = nameof(UserRole.Manager))]
    public async Task<ActionResult<List<UserDto>>> GetDirectReports(CancellationToken ct) =>
        Ok(await _surveyService.GetDirectReportsAsync(ct));

    [HttpGet("peer-candidates/{employeeId:guid}")]
    [Authorize(Roles = nameof(UserRole.Manager))]
    public async Task<ActionResult<List<UserDto>>> GetPeerCandidates(Guid employeeId, CancellationToken ct) =>
        Ok(await _surveyService.GetPeerCandidatesAsync(employeeId, ct));

    [HttpGet("managers")]
    [Authorize(Roles = nameof(UserRole.Manager))]
    public async Task<ActionResult<List<UserDto>>> GetManagers(CancellationToken ct) =>
        Ok(await _surveyService.GetManagersAsync(ct));
}
