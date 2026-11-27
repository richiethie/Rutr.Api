using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Rutr.Api.Models.Dtos;
using Rutr.Api.Services.Places;

namespace Rutr.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/places")]
public class PlacesController(PlacesService places) : ControllerBase
{
    /// <summary>Address autocomplete proxy. Returns mock suggestions when Places key is unset.</summary>
    [HttpGet("autocomplete")]
    public async Task<ActionResult<PlacesAutocompleteResponse>> Autocomplete(
        [FromQuery] string? q,
        [FromQuery] string? sessionToken,
        CancellationToken ct) =>
        Ok(await places.AutocompleteAsync(q, sessionToken, ct));

    /// <summary>Place details proxy for filling property fields + lat/lng.</summary>
    [HttpGet("details")]
    public async Task<ActionResult<PlaceDetailsDto>> Details(
        [FromQuery] string placeId,
        [FromQuery] string? sessionToken,
        CancellationToken ct)
    {
        var details = await places.GetDetailsAsync(placeId, sessionToken, ct);
        return details is null ? BadRequest(new { error = "placeId is required." }) : Ok(details);
    }
}
