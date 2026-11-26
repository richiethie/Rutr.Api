using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Rutr.Api.Components;
using Rutr.Api.Models.Dtos;
using Rutr.Api.Services;

namespace Rutr.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/onboarding")]
public class OnboardingController(OnboardingService onboarding) : ControllerBase
{
    [HttpPost("seed")]
    public async Task<ActionResult<OnboardingSeedResult>> Seed([FromBody] OnboardingSeedRequest request, CancellationToken ct)
    {
        try
        {
            return Ok(await onboarding.SeedAsync(User.GetOrganizationId(), request, ct));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpPost("import")]
    public async Task<ActionResult<OnboardingImportResult>> Import([FromBody] OnboardingImportRequest request, CancellationToken ct)
    {
        try
        {
            return Ok(await onboarding.ImportAsync(User.GetOrganizationId(), request, ct));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }
}
