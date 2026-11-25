using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Rutr.Api.Components;
using Rutr.Api.Models.Dtos;
using Rutr.Api.Services;

namespace Rutr.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/organizations")]
public class OrganizationsController(OrganizationService orgs) : ControllerBase
{
    [HttpGet("current")]
    public async Task<ActionResult<OrganizationDto>> Current(CancellationToken ct)
    {
        var item = await orgs.GetAsync(User.GetOrganizationId(), ct);
        return item is null ? NotFound() : Ok(item);
    }

    [HttpPut("current")]
    public async Task<ActionResult<OrganizationDto>> UpdateCurrent([FromBody] OrganizationUpdateRequest req, CancellationToken ct)
    {
        var updated = await orgs.UpdateAsync(User.GetOrganizationId(), req, ct);
        return updated is null ? NotFound() : Ok(updated);
    }

    [HttpGet("current/members")]
    public async Task<ActionResult<IReadOnlyList<StaffMemberDto>>> Members(CancellationToken ct) =>
        Ok(await orgs.ListStaffAsync(User.GetOrganizationId(), ct));

    [HttpPatch("current/members/{staffId:guid}/role")]
    public async Task<ActionResult<StaffMemberDto>> UpdateRole(Guid staffId, [FromBody] StaffRoleUpdateRequest req, CancellationToken ct)
    {
        try
        {
            var updated = await orgs.UpdateStaffRoleAsync(User.GetOrganizationId(), staffId, req, ct);
            return updated is null ? NotFound() : Ok(updated);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }
}
