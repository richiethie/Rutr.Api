using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Rutr.Api.Components;
using Rutr.Api.Models.Dtos;
using Rutr.Api.Services;

namespace Rutr.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/consents")]
public class ConsentsController(ConsentService consents) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<ConsentDto>>> List(
        [FromQuery] Guid? contactId,
        [FromQuery] Guid? jobId,
        CancellationToken ct) =>
        Ok(await consents.ListAsync(User.GetOrganizationId(), contactId, jobId, ct));

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ConsentDto>> Get(Guid id, CancellationToken ct)
    {
        var item = await consents.GetAsync(User.GetOrganizationId(), id, ct);
        return item is null ? NotFound() : Ok(item);
    }

    [HttpPost]
    public async Task<ActionResult<ConsentDto>> Create([FromBody] ConsentWriteRequest req, CancellationToken ct)
    {
        try
        {
            var created = await consents.CreateAsync(User.GetOrganizationId(), req, ct);
            return CreatedAtAction(nameof(Get), new { id = created.Id }, created);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<ConsentDto>> Update(Guid id, [FromBody] ConsentWriteRequest req, CancellationToken ct)
    {
        var updated = await consents.UpdateAsync(User.GetOrganizationId(), id, req, ct);
        return updated is null ? NotFound() : Ok(updated);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct) =>
        await consents.DeleteAsync(User.GetOrganizationId(), id, ct) ? NoContent() : NotFound();
}
