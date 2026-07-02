using Feedback360.Application.DTOs;
using Feedback360.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Feedback360.Api.Controllers;

[ApiController]
[Route("api/v1/results")]
[Authorize]
public class ResultsController : ControllerBase
{
    private readonly ResultsService _resultsService;

    public ResultsController(ResultsService resultsService) => _resultsService = resultsService;

    [HttpGet]
    public async Task<ActionResult<List<SurveyDto>>> GetViewableSurveys(CancellationToken ct)
    {
        try
        {
            return Ok(await _resultsService.GetViewableSurveysAsync(ct));
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
    }

    [HttpGet("{surveyId:guid}")]
    public async Task<ActionResult<ResultsDto>> GetResults(Guid surveyId, CancellationToken ct)
    {
        try
        {
            var results = await _resultsService.GetResultsAsync(surveyId, ct);
            return results is null ? NotFound() : Ok(results);
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
    }
}
