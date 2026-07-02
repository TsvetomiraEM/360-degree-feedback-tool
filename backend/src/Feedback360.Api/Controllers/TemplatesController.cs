using Feedback360.Application.DTOs;
using Feedback360.Application.Services;
using Feedback360.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Feedback360.Api.Controllers;

[ApiController]
[Route("api/v1/templates")]
[Authorize(Roles = nameof(UserRole.Manager))]
public class TemplatesController : ControllerBase
{
    private readonly TemplateService _templateService;

    public TemplatesController(TemplateService templateService) => _templateService = templateService;

    [HttpGet]
    public async Task<ActionResult<List<TemplateDto>>> GetAll(CancellationToken ct) =>
        Ok(await _templateService.GetAllAsync(ct));

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<TemplateDto>> GetById(Guid id, CancellationToken ct)
    {
        var template = await _templateService.GetByIdAsync(id, ct);
        return template is null ? NotFound() : Ok(template);
    }

    [HttpPost]
    public async Task<ActionResult<TemplateDto>> Create([FromBody] CreateTemplateRequest request, CancellationToken ct)
    {
        try
        {
            var template = await _templateService.CreateAsync(request, ct);
            return CreatedAtAction(nameof(GetById), new { id = template.Id }, template);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<TemplateDto>> Update(Guid id, [FromBody] UpdateTemplateRequest request, CancellationToken ct)
    {
        try
        {
            return Ok(await _templateService.UpdateAsync(id, request, ct));
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

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        try
        {
            await _templateService.DeleteAsync(id, ct);
            return NoContent();
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
    }

    [HttpPost("{id:guid}/share")]
    public async Task<IActionResult> Share(Guid id, [FromBody] ShareTemplateRequest request, CancellationToken ct)
    {
        try
        {
            await _templateService.ShareAsync(id, request, ct);
            return NoContent();
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
    }
}
