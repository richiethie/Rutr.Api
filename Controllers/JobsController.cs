using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Rutr.Api.Components;
using Rutr.Api.Models.Dtos;
using Rutr.Api.Services;

namespace Rutr.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/jobs")]
public class JobsController(JobService jobs, PhotoService photos, AuditService audit) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<JobListItemDto>>> List(CancellationToken ct) =>
        Ok(await jobs.ListAsync(User.GetOrganizationId(), ct));

    [HttpGet("{jobId:guid}")]
    public async Task<ActionResult<JobDetailDto>> Get(Guid jobId, CancellationToken ct)
    {
        var job = await jobs.GetAsync(User.GetOrganizationId(), jobId, ct);
        return job is null ? NotFound() : Ok(job);
    }

    [HttpPost]
    public async Task<ActionResult<JobDetailDto>> Create([FromBody] JobWriteRequest req, CancellationToken ct)
    {
        var created = await jobs.CreateAsync(User.GetOrganizationId(), req, ct);
        return CreatedAtAction(nameof(Get), new { jobId = created.Id }, created);
    }

    [HttpPut("{jobId:guid}")]
    public async Task<ActionResult<JobDetailDto>> Update(Guid jobId, [FromBody] JobWriteRequest req, CancellationToken ct)
    {
        var updated = await jobs.UpdateAsync(User.GetOrganizationId(), jobId, req, ct);
        return updated is null ? NotFound() : Ok(updated);
    }

    [HttpPatch("{jobId:guid}/status")]
    public async Task<ActionResult<JobDetailDto>> UpdateStatus(Guid jobId, [FromBody] JobStatusUpdateRequest req, CancellationToken ct)
    {
        var updated = await jobs.UpdateStatusAsync(User.GetOrganizationId(), jobId, req, ct);
        return updated is null ? NotFound() : Ok(updated);
    }

    [HttpDelete("{jobId:guid}")]
    public async Task<IActionResult> Delete(Guid jobId, CancellationToken ct) =>
        await jobs.DeleteAsync(User.GetOrganizationId(), jobId, ct) ? NoContent() : NotFound();

    [HttpGet("{jobId:guid}/audit")]
    public async Task<ActionResult<IReadOnlyList<AuditEventDto>>> Audit(Guid jobId, CancellationToken ct) =>
        Ok(await audit.ListForJobAsync(User.GetOrganizationId(), jobId, ct));

    [HttpPatch("{jobId:guid}/photos/{photoId:guid}/visibility")]
    public async Task<ActionResult<PhotoDto>> SetPhotoVisibility(
        Guid jobId,
        Guid photoId,
        [FromBody] SetPhotoVisibilityRequest request,
        CancellationToken ct)
    {
        var result = await photos.SetVisibilityAsync(
            User.GetOrganizationId(),
            jobId,
            photoId,
            request.CustomerVisible,
            User.GetStaffUserId(),
            ct);
        return result is null ? NotFound() : Ok(result);
    }
}
