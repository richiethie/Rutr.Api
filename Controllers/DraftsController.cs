using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Rutr.Api.Components;
using Rutr.Api.Models.Dtos;
using Rutr.Api.Services;

namespace Rutr.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/drafts")]
public class DraftsController(DraftService drafts) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<MessageDraftDto>>> List([FromQuery] Guid? jobId, CancellationToken ct) =>
        Ok(await drafts.ListAsync(User.GetOrganizationId(), jobId, ct));

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<MessageDraftDto>> Get(Guid id, CancellationToken ct)
    {
        var item = await drafts.GetAsync(User.GetOrganizationId(), id, ct);
        return item is null ? NotFound() : Ok(item);
    }

    [HttpPost]
    public async Task<ActionResult<MessageDraftDto>> Create([FromBody] DraftWriteRequest req, CancellationToken ct)
    {
        var created = await drafts.CreateAsync(User.GetOrganizationId(), User.GetStaffUserId(), req, ct);
        return CreatedAtAction(nameof(Get), new { id = created.Id }, created);
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<MessageDraftDto>> Update(Guid id, [FromBody] DraftWriteRequest req, CancellationToken ct)
    {
        try
        {
            var updated = await drafts.UpdateAsync(User.GetOrganizationId(), id, req, ct);
            return updated is null ? NotFound() : Ok(updated);
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { error = ex.Message });
        }
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        try
        {
            return await drafts.DeleteAsync(User.GetOrganizationId(), id, User.GetStaffUserId(), ct)
                ? NoContent()
                : NotFound();
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { error = ex.Message });
        }
    }
}
