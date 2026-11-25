using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Rutr.Api.Data;
using Rutr.Api.Models.Dtos;
using Rutr.Api.Services;

namespace Rutr.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/jobs")]
public class JobsController(JobService jobs) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<JobListItemDto>>> List(CancellationToken ct)
    {
        return Ok(await jobs.ListAsync(GetOrgId(), ct));
    }

    [HttpGet("{jobId:guid}")]
    public async Task<ActionResult<JobDetailDto>> Get(Guid jobId, CancellationToken ct)
    {
        var job = await jobs.GetAsync(GetOrgId(), jobId, ct);
        return job is null ? NotFound() : Ok(job);
    }

    private Guid GetOrgId()
    {
        var claim = User.FindFirst("org")?.Value;
        if (Guid.TryParse(claim, out var orgId))
        {
            return orgId;
        }

        // Demo fallback when auth is bypassed in Development with AllowAnonymous demo header path.
        return SeedData.OrgId;
    }
}
