using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Rutr.Api.Data;
using Rutr.Api.Models.Dtos;
using Rutr.Api.Services;

namespace Rutr.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/approvals")]
public class ApprovalsController(ApprovalService approvals) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<ApprovalQueueItemDto>>> Queue(CancellationToken ct)
    {
        return Ok(await approvals.QueueAsync(GetOrgId(), ct));
    }

    [HttpPost("{draftId:guid}/approve")]
    public async Task<ActionResult<MessageDraftDto>> Approve(
        Guid draftId,
        [FromBody] ApproveDraftRequest? request,
        CancellationToken ct)
    {
        var staffId = GetStaffId() ?? request?.StaffUserId ?? SeedData.PmUserId;
        var result = await approvals.ApproveAsync(GetOrgId(), draftId, staffId, ct);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpPost("{draftId:guid}/reject")]
    public async Task<ActionResult<MessageDraftDto>> Reject(
        Guid draftId,
        [FromBody] RejectDraftRequest? request,
        CancellationToken ct)
    {
        var staffId = GetStaffId() ?? SeedData.PmUserId;
        var result = await approvals.RejectAsync(GetOrgId(), draftId, staffId, request?.Reason, ct);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpPost("{draftId:guid}/send")]
    public async Task<ActionResult<MessageDraftDto>> Send(Guid draftId, CancellationToken ct)
    {
        var staffId = GetStaffId() ?? SeedData.PmUserId;
        var result = await approvals.SendAsync(GetOrgId(), draftId, staffId, ct);
        return result is null
            ? NotFound(new { error = "Draft not found or not in Approved status." })
            : Ok(result);
    }

    private Guid GetOrgId()
    {
        var claim = User.FindFirst("org")?.Value;
        return Guid.TryParse(claim, out var orgId) ? orgId : SeedData.OrgId;
    }

    private Guid? GetStaffId()
    {
        var sub = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                  ?? User.FindFirst("sub")?.Value;
        return Guid.TryParse(sub, out var id) ? id : null;
    }
}
