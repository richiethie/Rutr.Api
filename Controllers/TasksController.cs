using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Rutr.Api.Components;
using Rutr.Api.Models.Dtos;
using Rutr.Api.Services;

namespace Rutr.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/tasks")]
public class TasksController(TaskService tasks) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<TaskDto>>> List([FromQuery] Guid? jobId, [FromQuery] Guid? contactId, CancellationToken ct) =>
        Ok(await tasks.ListAsync(User.GetOrganizationId(), jobId, contactId, ct));

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<TaskDto>> Get(Guid id, CancellationToken ct)
    {
        var item = await tasks.GetAsync(User.GetOrganizationId(), id, ct);
        return item is null ? NotFound() : Ok(item);
    }

    [HttpPost]
    public async Task<ActionResult<TaskDto>> Create([FromBody] TaskWriteRequest req, CancellationToken ct)
    {
        try
        {
            var created = await tasks.CreateAsync(User.GetOrganizationId(), req, ct);
            return CreatedAtAction(nameof(Get), new { id = created.Id }, created);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<TaskDto>> Update(Guid id, [FromBody] TaskWriteRequest req, CancellationToken ct)
    {
        var updated = await tasks.UpdateAsync(User.GetOrganizationId(), id, req, ct);
        return updated is null ? NotFound() : Ok(updated);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct) =>
        await tasks.DeleteAsync(User.GetOrganizationId(), id, ct) ? NoContent() : NotFound();
}
