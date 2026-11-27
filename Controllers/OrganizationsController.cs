using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Rutr.Api.Components;
using Rutr.Api.Models.Dtos;
using Rutr.Api.Services;

namespace Rutr.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/organizations")]
public class OrganizationsController(OrganizationService orgs, OnboardingService onboarding) : ControllerBase
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

    [HttpPost("current/members")]
    public async Task<ActionResult<StaffMemberDto>> AddMember([FromBody] AddStaffMemberRequest req, CancellationToken ct)
    {
        try
        {
            var created = await onboarding.AddMemberAsync(User.GetOrganizationId(), User.GetStaffRole(), req, ct);
            return CreatedAtAction(nameof(Members), created);
        }
        catch (UnauthorizedAccessException ex)
        {
            return StatusCode(StatusCodes.Status403Forbidden, new { error = ex.Message });
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("Email already", StringComparison.OrdinalIgnoreCase))
        {
            return Conflict(new { error = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

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

    [HttpGet("current/invites")]
    public async Task<ActionResult<IReadOnlyList<InviteDto>>> ListInvites(CancellationToken ct)
    {
        try
        {
            return Ok(await onboarding.ListInvitesAsync(User.GetOrganizationId(), User.GetStaffRole(), ct));
        }
        catch (UnauthorizedAccessException ex)
        {
            return StatusCode(StatusCodes.Status403Forbidden, new { error = ex.Message });
        }
    }

    [HttpPost("current/invites")]
    public async Task<ActionResult<InviteDto>> Invite([FromBody] InviteStaffRequest req, CancellationToken ct)
    {
        try
        {
            var created = await onboarding.InviteAsync(User.GetOrganizationId(), User.GetStaffRole(), req, ct);
            return CreatedAtAction(nameof(ListInvites), created);
        }
        catch (UnauthorizedAccessException ex)
        {
            return StatusCode(StatusCodes.Status403Forbidden, new { error = ex.Message });
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("Email already", StringComparison.OrdinalIgnoreCase))
        {
            return Conflict(new { error = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }
}
