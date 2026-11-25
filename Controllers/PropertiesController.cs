using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Rutr.Api.Components;
using Rutr.Api.Models.Dtos;
using Rutr.Api.Services;

namespace Rutr.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/properties")]
public class PropertiesController(PropertyService properties) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<PropertyDto>>> List(CancellationToken ct) =>
        Ok(await properties.ListAsync(User.GetOrganizationId(), ct));

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<PropertyDto>> Get(Guid id, CancellationToken ct)
    {
        var item = await properties.GetAsync(User.GetOrganizationId(), id, ct);
        return item is null ? NotFound() : Ok(item);
    }

    [HttpPost]
    public async Task<ActionResult<PropertyDto>> Create([FromBody] PropertyWriteRequest req, CancellationToken ct)
    {
        var created = await properties.CreateAsync(User.GetOrganizationId(), req, ct);
        return CreatedAtAction(nameof(Get), new { id = created.Id }, created);
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<PropertyDto>> Update(Guid id, [FromBody] PropertyWriteRequest req, CancellationToken ct)
    {
        var updated = await properties.UpdateAsync(User.GetOrganizationId(), id, req, ct);
        return updated is null ? NotFound() : Ok(updated);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct) =>
        await properties.DeleteAsync(User.GetOrganizationId(), id, ct) ? NoContent() : NotFound();
}
