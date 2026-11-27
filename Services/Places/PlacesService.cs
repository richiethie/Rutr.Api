using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Options;
using Rutr.Api.Configuration;
using Rutr.Api.Models.Dtos;

namespace Rutr.Api.Services.Places;

public class PlacesService(
    IHttpClientFactory httpClientFactory,
    IOptions<PlacesOptions> options,
    IConfiguration config,
    ILogger<PlacesService> logger)
{
    private static readonly JsonSerializerOptions JsonOpts = new() { PropertyNameCaseInsensitive = true };

    private string? ResolveApiKey()
    {
        var fromEnv = Environment.GetEnvironmentVariable("RUTR_GOOGLE_PLACES_API_KEY");
        if (!string.IsNullOrWhiteSpace(fromEnv))
        {
            return fromEnv.Trim();
        }

        var fromConfig = options.Value.GoogleApiKey
                         ?? config["Places:GoogleApiKey"];
        return string.IsNullOrWhiteSpace(fromConfig) ? null : fromConfig.Trim();
    }

    public async Task<PlacesAutocompleteResponse> AutocompleteAsync(
        string? query,
        string? sessionToken,
        CancellationToken ct = default)
    {
        var q = (query ?? "").Trim();
        if (q.Length < 2)
        {
            return new PlacesAutocompleteResponse(ResolveApiKey() is null ? "mock" : "google", []);
        }

        var key = ResolveApiKey();
        if (key is null)
        {
            return MockAutocomplete(q);
        }

        try
        {
            var client = httpClientFactory.CreateClient("GooglePlaces");
            var url =
                $"place/autocomplete/json?input={Uri.EscapeDataString(q)}&types=address&components=country:us&key={Uri.EscapeDataString(key)}";
            if (!string.IsNullOrWhiteSpace(sessionToken))
            {
                url += $"&sessiontoken={Uri.EscapeDataString(sessionToken)}";
            }

            using var response = await client.GetAsync(url, ct);
            response.EnsureSuccessStatusCode();
            var payload = await response.Content.ReadFromJsonAsync<GoogleAutocompletePayload>(JsonOpts, ct);
            if (payload is null || !string.Equals(payload.Status, "OK", StringComparison.OrdinalIgnoreCase)
                && !string.Equals(payload.Status, "ZERO_RESULTS", StringComparison.OrdinalIgnoreCase))
            {
                logger.LogWarning("Google Places autocomplete status={Status}", payload?.Status);
                return MockAutocomplete(q);
            }

            var suggestions = (payload.Predictions ?? [])
                .Select(p => new PlaceSuggestionDto(
                    p.PlaceId ?? "",
                    p.StructuredFormatting?.MainText ?? p.Description ?? "",
                    p.StructuredFormatting?.SecondaryText ?? "",
                    p.Description ?? ""))
                .Where(s => !string.IsNullOrWhiteSpace(s.PlaceId))
                .ToList();

            return new PlacesAutocompleteResponse("google", suggestions);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Google Places autocomplete failed — using mock");
            return MockAutocomplete(q);
        }
    }

    public async Task<PlaceDetailsDto?> GetDetailsAsync(
        string placeId,
        string? sessionToken,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(placeId))
        {
            return null;
        }

        var key = ResolveApiKey();
        if (key is null || placeId.StartsWith("mock-", StringComparison.OrdinalIgnoreCase))
        {
            return MockDetails(placeId);
        }

        try
        {
            var client = httpClientFactory.CreateClient("GooglePlaces");
            var url =
                $"place/details/json?place_id={Uri.EscapeDataString(placeId)}&fields=place_id,formatted_address,address_component,geometry&key={Uri.EscapeDataString(key)}";
            if (!string.IsNullOrWhiteSpace(sessionToken))
            {
                url += $"&sessiontoken={Uri.EscapeDataString(sessionToken)}";
            }

            using var response = await client.GetAsync(url, ct);
            response.EnsureSuccessStatusCode();
            var payload = await response.Content.ReadFromJsonAsync<GoogleDetailsPayload>(JsonOpts, ct);
            if (payload?.Result is null
                || (!string.Equals(payload.Status, "OK", StringComparison.OrdinalIgnoreCase)))
            {
                logger.LogWarning("Google Places details status={Status}", payload?.Status);
                return MockDetails(placeId);
            }

            var result = payload.Result;
            var components = result.AddressComponents ?? [];
            string Comp(params string[] types) =>
                components.FirstOrDefault(c => c.Types is not null && types.Any(t => c.Types.Contains(t)))
                    ?.ShortName
                ?? components.FirstOrDefault(c => c.Types is not null && types.Any(t => c.Types.Contains(t)))
                    ?.LongName
                ?? "";

            string CompLong(params string[] types) =>
                components.FirstOrDefault(c => c.Types is not null && types.Any(t => c.Types.Contains(t)))
                    ?.LongName
                ?? Comp(types);

            var streetNumber = CompLong("street_number");
            var route = CompLong("route");
            var addressLine1 = string.Join(" ", new[] { streetNumber, route }.Where(s => !string.IsNullOrWhiteSpace(s))).Trim();
            if (string.IsNullOrWhiteSpace(addressLine1))
            {
                addressLine1 = result.FormattedAddress?.Split(',')[0].Trim() ?? "";
            }

            return new PlaceDetailsDto(
                "google",
                result.PlaceId ?? placeId,
                addressLine1,
                CompLong("locality", "sublocality", "administrative_area_level_3", "postal_town"),
                Comp("administrative_area_level_1"),
                Comp("postal_code"),
                Comp("country"),
                result.FormattedAddress ?? "",
                result.Geometry?.Location?.Lat,
                result.Geometry?.Location?.Lng);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Google Places details failed — using mock");
            return MockDetails(placeId);
        }
    }

    private static PlacesAutocompleteResponse MockAutocomplete(string query)
    {
        var catalog = MockCatalog()
            .Where(m => m.Description.Contains(query, StringComparison.OrdinalIgnoreCase)
                        || m.PrimaryText.Contains(query, StringComparison.OrdinalIgnoreCase))
            .Take(5)
            .Select(m => new PlaceSuggestionDto(m.PlaceId, m.PrimaryText, m.SecondaryText, m.Description))
            .ToList();

        if (catalog.Count == 0)
        {
            catalog = MockCatalog()
                .Take(3)
                .Select(m => new PlaceSuggestionDto(m.PlaceId, m.PrimaryText, m.SecondaryText, m.Description))
                .ToList();
        }

        return new PlacesAutocompleteResponse("mock", catalog);
    }

    private static PlaceDetailsDto MockDetails(string placeId)
    {
        var match = MockCatalog().FirstOrDefault(m => m.PlaceId.Equals(placeId, StringComparison.OrdinalIgnoreCase))
                    ?? MockCatalog()[0];
        return new PlaceDetailsDto(
            "mock",
            match.PlaceId,
            match.AddressLine1,
            match.City,
            match.State,
            match.PostalCode,
            "US",
            match.Description,
            match.Latitude,
            match.Longitude);
    }

    private static IReadOnlyList<MockPlace> MockCatalog() =>
    [
        new("mock-maple", "1428 Maple Ridge Dr", "Cedar Falls, IA 50613, USA",
            "1428 Maple Ridge Dr, Cedar Falls, IA 50613, USA",
            "1428 Maple Ridge Dr", "Cedar Falls", "IA", "50613", 42.5341, -92.4453),
        new("mock-prairie", "88 Prairie View Ct", "Waterloo, IA 50701, USA",
            "88 Prairie View Ct, Waterloo, IA 50701, USA",
            "88 Prairie View Ct", "Waterloo", "IA", "50701", 42.4928, -92.3426),
        new("mock-oak", "409 Oak Street", "Waverly, IA 50677, USA",
            "409 Oak Street, Waverly, IA 50677, USA",
            "409 Oak Street", "Waverly", "IA", "50677", 42.7258, -92.4752),
        new("mock-main", "100 Main Street", "Cedar Falls, IA 50613, USA",
            "100 Main Street, Cedar Falls, IA 50613, USA",
            "100 Main Street", "Cedar Falls", "IA", "50613", 42.5278, -92.4453)
    ];

    private sealed record MockPlace(
        string PlaceId,
        string PrimaryText,
        string SecondaryText,
        string Description,
        string AddressLine1,
        string City,
        string State,
        string PostalCode,
        double Latitude,
        double Longitude);

    private sealed class GoogleAutocompletePayload
    {
        public string? Status { get; set; }
        public List<GooglePrediction>? Predictions { get; set; }
    }

    private sealed class GooglePrediction
    {
        public string? Description { get; set; }
        [JsonPropertyName("place_id")]
        public string? PlaceId { get; set; }
        [JsonPropertyName("structured_formatting")]
        public GoogleStructuredFormatting? StructuredFormatting { get; set; }
    }

    private sealed class GoogleStructuredFormatting
    {
        [JsonPropertyName("main_text")]
        public string? MainText { get; set; }
        [JsonPropertyName("secondary_text")]
        public string? SecondaryText { get; set; }
    }

    private sealed class GoogleDetailsPayload
    {
        public string? Status { get; set; }
        public GooglePlaceResult? Result { get; set; }
    }

    private sealed class GooglePlaceResult
    {
        [JsonPropertyName("place_id")]
        public string? PlaceId { get; set; }
        [JsonPropertyName("formatted_address")]
        public string? FormattedAddress { get; set; }
        [JsonPropertyName("address_components")]
        public List<GoogleAddressComponent>? AddressComponents { get; set; }
        public GoogleGeometry? Geometry { get; set; }
    }

    private sealed class GoogleAddressComponent
    {
        [JsonPropertyName("long_name")]
        public string? LongName { get; set; }
        [JsonPropertyName("short_name")]
        public string? ShortName { get; set; }
        public List<string>? Types { get; set; }
    }

    private sealed class GoogleGeometry
    {
        public GoogleLatLng? Location { get; set; }
    }

    private sealed class GoogleLatLng
    {
        public double Lat { get; set; }
        public double Lng { get; set; }
    }
}
