using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Rutr.Api.Components;
using Rutr.Api.Models.Dtos;
using Rutr.Api.Services;

namespace Rutr.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/evidence")]
public class EvidenceController(EvidenceService evidence) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<EvidenceDto>>> List(
        [FromQuery] Guid? jobId,
        [FromQuery] bool includeDeleted = false,
        CancellationToken ct = default) =>
        Ok(await evidence.ListAsync(User.GetOrganizationId(), jobId, includeDeleted, ct));

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<EvidenceDto>> Get(Guid id, CancellationToken ct)
    {
        var item = await evidence.GetAsync(User.GetOrganizationId(), id, ct);
        return item is null ? NotFound() : Ok(item);
    }

    [HttpPost]
    public async Task<ActionResult<EvidenceDto>> Create([FromBody] EvidenceWriteRequest req, CancellationToken ct)
    {
        var created = await evidence.CreateAsync(User.GetOrganizationId(), req, ct);
        return CreatedAtAction(nameof(Get), new { id = created.Id }, created);
    }

    /// <summary>Multipart upload — file + caption (+ optional visibility / CompanyCam id).</summary>
    [HttpPost("upload")]
    [RequestSizeLimit(12 * 1024 * 1024)]
    public async Task<ActionResult<EvidenceDto>> Upload(
        [FromForm] Guid jobId,
        [FromForm] string caption,
        [FromForm] IFormFile file,
        [FromForm] string? externalCompanyCamId,
        [FromForm] bool customerVisible = false,
        CancellationToken ct = default)
    {
        if (file is null || file.Length == 0)
        {
            return BadRequest(new { error = "Choose an image file." });
        }

        if (string.IsNullOrWhiteSpace(caption))
        {
            return BadRequest(new { error = "Caption is required." });
        }

        try
        {
            var webRoot = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
            var publicBase = $"{Request.Scheme}://{Request.Host}";
            var created = await evidence.CreateFromUploadAsync(
                User.GetOrganizationId(),
                jobId,
                caption,
                file,
                externalCompanyCamId,
                customerVisible,
                publicBase,
                webRoot,
                ct);
            return CreatedAtAction(nameof(Get), new { id = created.Id }, created);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<EvidenceDto>> Update(Guid id, [FromBody] EvidenceWriteRequest req, CancellationToken ct)
    {
        var updated = await evidence.UpdateAsync(User.GetOrganizationId(), id, req, ct);
        return updated is null ? NotFound() : Ok(updated);
    }

    [HttpPatch("{id:guid}/visibility")]
    public async Task<ActionResult<EvidenceDto>> Visibility(Guid id, [FromBody] SetPhotoVisibilityRequest req, CancellationToken ct)
    {
        var updated = await evidence.SetVisibilityAsync(User.GetOrganizationId(), id, req.CustomerVisible, User.GetStaffUserId(), ct);
        return updated is null ? NotFound() : Ok(updated);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> SoftDelete(Guid id, CancellationToken ct) =>
        await evidence.SoftDeleteAsync(User.GetOrganizationId(), id, User.GetStaffUserId(), ct) ? NoContent() : NotFound();
}
