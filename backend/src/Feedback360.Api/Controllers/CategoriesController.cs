using Feedback360.Application.DTOs;
using Feedback360.Application.Services;
using Feedback360.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Feedback360.Api.Controllers;

[ApiController]
[Route("api/v1/categories")]
[Authorize(Roles = nameof(UserRole.Manager))]
public class CategoriesController : ControllerBase
{
    private readonly CategoryService _categoryService;

    public CategoriesController(CategoryService categoryService) => _categoryService = categoryService;

    [HttpGet]
    public async Task<ActionResult<List<QuestionCategoryDto>>> GetAll(CancellationToken ct) =>
        Ok(await _categoryService.GetAllAsync(ct));

    [HttpPost]
    public async Task<ActionResult<QuestionCategoryDto>> Create([FromBody] CreateQuestionCategoryRequest request, CancellationToken ct)
    {
        try
        {
            var category = await _categoryService.CreateAsync(request, ct);
            return Ok(category);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}
