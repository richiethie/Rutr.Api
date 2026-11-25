using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Rutr.Api.Components;
using Rutr.Api.Models.Dtos;
using Rutr.Api.Services;

namespace Rutr.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/integrations")]
public class IntegrationsController(
    ConnectSyncService sync,
    IntegrationConnectionService connections) : ControllerBase
{
    [HttpGet("status")]
    public ActionResult<IntegrationStatusDto> Status() => Ok(sync.Status());

    [HttpGet("connections")]
    public async Task<ActionResult<IReadOnlyList<IntegrationConnectionDto>>> Connections(CancellationToken ct) =>
        Ok(await connections.ListAsync(User.GetOrganizationId(), ct));

    [HttpPost("connections/{provider}/connect")]
    [HttpPost("{provider}/connect")] // Web client alias
    public async Task<ActionResult<IntegrationConnectionDto>> Connect(string provider, CancellationToken ct)
    {
        var result = await connections.ConnectAsync(User.GetOrganizationId(), provider, ct);
        return Ok(result);
    }

    [HttpPost("connections/{provider}/disconnect")]
    [HttpPost("{provider}/disconnect")] // Web client alias
    public async Task<ActionResult<IntegrationConnectionDto>> Disconnect(string provider, CancellationToken ct)
    {
        var result = await connections.DisconnectAsync(User.GetOrganizationId(), provider, ct);
        return Ok(result);
    }

    [HttpPost("sync")]
    public async Task<ActionResult<SyncResultDto>> Sync(CancellationToken ct)
    {
        var result = await sync.SyncAsync(User.GetOrganizationId(), User.GetStaffUserId(), ct);
        return Ok(result);
    }
}
