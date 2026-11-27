using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;

namespace RutrApi.Tests;

[Collection("SerialApi")]
public class PlacesTests : IClassFixture<RutrApiFactory>
{
    private readonly HttpClient _client;
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    public PlacesTests(RutrApiFactory factory)
    {
        _client = factory.CreateClient();
        _client.DefaultRequestHeaders.Add("X-Demo-Auth", "pm");
    }

    [Fact]
    public async Task Autocomplete_without_key_returns_mock_suggestions()
    {
        var response = await _client.GetAsync("/api/v1/places/autocomplete?q=Maple");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
        body.GetProperty("provider").GetString().Should().Be("mock");
        body.GetProperty("suggestions").GetArrayLength().Should().BeGreaterThan(0);
        var first = body.GetProperty("suggestions")[0];
        first.GetProperty("placeId").GetString().Should().NotBeNullOrWhiteSpace();
        first.GetProperty("primaryText").GetString().Should().NotBeNullOrWhiteSpace();
        first.GetProperty("description").GetString().Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task Autocomplete_short_query_returns_empty()
    {
        var response = await _client.GetAsync("/api/v1/places/autocomplete?q=M");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
        body.GetProperty("suggestions").GetArrayLength().Should().Be(0);
    }

    [Fact]
    public async Task Details_mock_place_returns_address_and_coords()
    {
        var response = await _client.GetAsync("/api/v1/places/details?placeId=mock-maple");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
        body.GetProperty("provider").GetString().Should().Be("mock");
        body.GetProperty("addressLine1").GetString().Should().Contain("Maple");
        body.GetProperty("city").GetString().Should().Be("Cedar Falls");
        body.GetProperty("state").GetString().Should().Be("IA");
        body.GetProperty("postalCode").GetString().Should().Be("50613");
        body.GetProperty("latitude").GetDouble().Should().BeApproximately(42.5341, 0.001);
        body.GetProperty("longitude").GetDouble().Should().BeApproximately(-92.4453, 0.001);
    }

    [Fact]
    public async Task Property_create_accepts_latitude_longitude()
    {
        var created = await _client.PostAsJsonAsync("/api/v1/properties", new
        {
            addressLine1 = "100 Main Street",
            city = "Cedar Falls",
            state = "IA",
            postalCode = "50613",
            propertyType = "Residential",
            latitude = 42.5278,
            longitude = -92.4453
        });
        created.StatusCode.Should().Be(HttpStatusCode.Created);
        var body = await created.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
        body.GetProperty("latitude").GetDouble().Should().BeApproximately(42.5278, 0.0001);
        body.GetProperty("longitude").GetDouble().Should().BeApproximately(-92.4453, 0.0001);
    }
}
