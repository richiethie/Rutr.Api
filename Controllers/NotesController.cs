using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Rutr.Api.Components;
using Rutr.Api.Models.Dtos;
using Rutr.Api.Services;

namespace Rutr.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/notes")]
public class NotesController(NoteService notes) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<NoteDto>>> List([FromQuery] Guid? jobId, [FromQuery] Guid? contactId, CancellationToken ct) =>
        Ok(await notes.ListAsync(User.GetOrganizationId(), jobId, contactId, ct));

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<NoteDto>> Get(Guid id, CancellationToken ct)
    {
        var item = await notes.GetAsync(User.GetOrganizationId(), id, ct);
        return item is null ? NotFound() : Ok(item);
    }

    [HttpPost]
    public async Task<ActionResult<NoteDto>> Create([FromBody] NoteWriteRequest req, CancellationToken ct)
    {
        var created = await notes.CreateAsync(User.GetOrganizationId(), User.GetStaffUserId(), req, ct);
        return CreatedAtAction(nameof(Get), new { id = created.Id }, created);
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<NoteDto>> Update(Guid id, [FromBody] NoteWriteRequest req, CancellationToken ct)
    {
        var updated = await notes.UpdateAsync(User.GetOrganizationId(), id, req, ct);
        return updated is null ? NotFound() : Ok(updated);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct) =>
        await notes.DeleteAsync(User.GetOrganizationId(), id, ct) ? NoContent() : NotFound();
}
