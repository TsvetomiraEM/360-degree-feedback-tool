using Feedback360.Application.DTOs;
using Feedback360.Application.Services;
using Feedback360.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Feedback360.Api.Controllers;

[ApiController]
[Route("api/v1/admin/audit-logs")]
[Authorize(Roles = nameof(UserRole.Admin))]
public class AdminAuditLogsController : ControllerBase
{
    private readonly AuditLogQueryService _auditLogQueryService;

    public AdminAuditLogsController(AuditLogQueryService auditLogQueryService) =>
        _auditLogQueryService = auditLogQueryService;

    [HttpGet]
    public async Task<ActionResult<PagedResult<AuditLogDto>>> Get(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] AuditAction? action = null,
        [FromQuery] DateTime? from = null,
        [FromQuery] DateTime? to = null,
        CancellationToken ct = default) =>
        Ok(await _auditLogQueryService.GetAsync(new AuditLogQuery(page, pageSize, action, from, to), ct));
}
