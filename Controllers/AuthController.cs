using Microsoft.AspNetCore.Mvc;
using Rutr.Api.Models.Dtos;
using Rutr.Api.Services;

namespace Rutr.Api.Controllers;

[ApiController]
[Route("api/v1/auth")]
public class AuthController(AuthService auth) : ControllerBase
{
    [HttpPost("login")]
    public async Task<ActionResult<LoginResponse>> Login([FromBody] LoginRequest request, CancellationToken ct)
    {
        var result = await auth.LoginAsync(request, ct);
        return result is null ? Unauthorized(new { error = "Invalid email or password." }) : Ok(result);
    }
}
