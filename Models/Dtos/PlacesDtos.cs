namespace Rutr.Api.Models.Dtos;

public record PlaceSuggestionDto(
    string PlaceId,
    string PrimaryText,
    string SecondaryText,
    string Description);

public record PlacesAutocompleteResponse(
    string Provider,
    IReadOnlyList<PlaceSuggestionDto> Suggestions);

public record PlaceDetailsDto(
    string Provider,
    string PlaceId,
    string AddressLine1,
    string City,
    string State,
    string PostalCode,
    string Country,
    string FormattedAddress,
    double? Latitude,
    double? Longitude);
