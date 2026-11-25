using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Rutr.Api.Data;
using Rutr.Api.Models.Dtos;
using Rutr.Api.Services;

namespace Rutr.Api.Controllers;

[ApiController]
[Route("api/v1/auth")]
public class AuthController(AuthService auth) : ControllerBase
{
    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<ActionResult<LoginResponse>> Login([FromBody] LoginRequest request, CancellationToken ct)
    {
        var result = await auth.LoginAsync(request, ct);
        return result is null ? Unauthorized(new { error = "Invalid email or password." }) : Ok(result);
    }

    [HttpGet("me")]
    [Authorize]
    public async Task<ActionResult<StaffMeDto>> Me(CancellationToken ct)
    {
        var staffId = GetStaffId() ?? SeedData.PmUserId;
        var me = await auth.GetMeAsync(staffId, ct);
        return me is null ? NotFound() : Ok(me);
    }

    private Guid? GetStaffId()
    {
        var sub = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                  ?? User.FindFirst("sub")?.Value;
        return Guid.TryParse(sub, out var id) ? id : null;
    }
}
