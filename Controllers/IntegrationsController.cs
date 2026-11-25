using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Rutr.Api.Data;
using Rutr.Api.Models.Dtos;
using Rutr.Api.Services;

namespace Rutr.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/integrations")]
public class IntegrationsController(ConnectSyncService sync) : ControllerBase
{
    [HttpGet("status")]
    public ActionResult<IntegrationStatusDto> Status() => Ok(sync.Status());

    [HttpPost("sync")]
    public async Task<ActionResult<SyncResultDto>> Sync(CancellationToken ct)
    {
        var result = await sync.SyncAsync(GetOrgId(), GetStaffId(), ct);
        return Ok(result);
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
