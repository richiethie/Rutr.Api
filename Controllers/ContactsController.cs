using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Rutr.Api.Components;
using Rutr.Api.Models.Dtos;
using Rutr.Api.Services;

namespace Rutr.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/contacts")]
public class ContactsController(ContactService contacts) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<ContactDto>>> List(CancellationToken ct) =>
        Ok(await contacts.ListAsync(User.GetOrganizationId(), ct));

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ContactDto>> Get(Guid id, CancellationToken ct)
    {
        var item = await contacts.GetAsync(User.GetOrganizationId(), id, ct);
        return item is null ? NotFound() : Ok(item);
    }

    [HttpPost]
    public async Task<ActionResult<ContactDto>> Create([FromBody] ContactWriteRequest req, CancellationToken ct)
    {
        var created = await contacts.CreateAsync(User.GetOrganizationId(), req, ct);
        return CreatedAtAction(nameof(Get), new { id = created.Id }, created);
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<ContactDto>> Update(Guid id, [FromBody] ContactWriteRequest req, CancellationToken ct)
    {
        var updated = await contacts.UpdateAsync(User.GetOrganizationId(), id, req, ct);
        return updated is null ? NotFound() : Ok(updated);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct) =>
        await contacts.DeleteAsync(User.GetOrganizationId(), id, ct) ? NoContent() : NotFound();
}
