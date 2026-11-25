using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Rutr.Api.Data;
using Rutr.Api.Models.Dtos;
using Rutr.Api.Services;

namespace Rutr.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/jobs")]
public class JobsController(JobService jobs, PhotoService photos, AuditService audit) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<JobListItemDto>>> List(CancellationToken ct)
    {
        return Ok(await jobs.ListAsync(GetOrgId(), ct));
    }

    [HttpGet("{jobId:guid}")]
    public async Task<ActionResult<JobDetailDto>> Get(Guid jobId, CancellationToken ct)
    {
        var job = await jobs.GetAsync(GetOrgId(), jobId, ct);
        return job is null ? NotFound() : Ok(job);
    }

    [HttpGet("{jobId:guid}/audit")]
    public async Task<ActionResult<IReadOnlyList<AuditEventDto>>> Audit(Guid jobId, CancellationToken ct)
    {
        return Ok(await audit.ListForJobAsync(GetOrgId(), jobId, ct));
    }

    [HttpPatch("{jobId:guid}/photos/{photoId:guid}/visibility")]
    public async Task<ActionResult<PhotoDto>> SetPhotoVisibility(
        Guid jobId,
        Guid photoId,
        [FromBody] SetPhotoVisibilityRequest request,
        CancellationToken ct)
    {
        var result = await photos.SetVisibilityAsync(
            GetOrgId(),
            jobId,
            photoId,
            request.CustomerVisible,
            GetStaffId(),
            ct);
        return result is null ? NotFound() : Ok(result);
    }

    private Guid GetOrgId()
    {
        var claim = User.FindFirst("org")?.Value;
        if (Guid.TryParse(claim, out var orgId))
        {
            return orgId;
        }

        return SeedData.OrgId;
    }

    private Guid? GetStaffId()
    {
        var sub = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                  ?? User.FindFirst("sub")?.Value;
        return Guid.TryParse(sub, out var id) ? id : null;
    }
}
