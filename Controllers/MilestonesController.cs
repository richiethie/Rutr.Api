using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Rutr.Api.Components;
using Rutr.Api.Models.Dtos;
using Rutr.Api.Services;

namespace Rutr.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/milestones")]
public class MilestonesController(MilestoneService milestones) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<MilestoneDto>>> List([FromQuery] Guid? jobId, CancellationToken ct) =>
        Ok(await milestones.ListAsync(User.GetOrganizationId(), jobId, ct));

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<MilestoneDto>> Get(Guid id, CancellationToken ct)
    {
        var item = await milestones.GetAsync(User.GetOrganizationId(), id, ct);
        return item is null ? NotFound() : Ok(item);
    }

    [HttpPost]
    public async Task<ActionResult<MilestoneDto>> Create([FromBody] MilestoneWriteRequest req, CancellationToken ct)
    {
        var created = await milestones.CreateAsync(User.GetOrganizationId(), req, ct);
        return CreatedAtAction(nameof(Get), new { id = created.Id }, created);
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<MilestoneDto>> Update(Guid id, [FromBody] MilestoneWriteRequest req, CancellationToken ct)
    {
        var updated = await milestones.UpdateAsync(User.GetOrganizationId(), id, req, ct);
        return updated is null ? NotFound() : Ok(updated);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct) =>
        await milestones.DeleteAsync(User.GetOrganizationId(), id, ct) ? NoContent() : NotFound();
}
