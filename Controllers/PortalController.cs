using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Rutr.Api.Models.Dtos;
using Rutr.Api.Services;

namespace Rutr.Api.Controllers;

[ApiController]
[AllowAnonymous]
[Route("api/v1/portal")]
public class PortalController(PortalService portal) : ControllerBase
{
    [HttpGet("{token}")]
    public async Task<ActionResult<PortalJobDto>> GetByToken(string token, CancellationToken ct)
    {
        var result = await portal.GetByTokenAsync(token, ct);
        return result is null ? NotFound(new { error = "Portal link expired or not found." }) : Ok(result);
    }
}
