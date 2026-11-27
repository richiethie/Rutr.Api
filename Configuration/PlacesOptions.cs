namespace Rutr.Api.Configuration;

public class PlacesOptions
{
    public const string SectionName = "Places";

    /// <summary>Google Places API key. Prefer env RUTR_GOOGLE_PLACES_API_KEY.</summary>
    public string? GoogleApiKey { get; set; }
}
