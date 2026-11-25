using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;

namespace RutrApi.Tests;

[Collection("SerialApi")]
public class CrmCrudTests : IClassFixture<RutrApiFactory>
{
    private readonly HttpClient _client;
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    public CrmCrudTests(RutrApiFactory factory)
    {
        _client = factory.CreateClient();
        _client.DefaultRequestHeaders.Add("X-Demo-Auth", "pm");
    }

    [Fact]
    public async Task Contacts_properties_notes_tasks_milestones_consents_integrations_crud()
    {
        var contacts = await _client.GetFromJsonAsync<JsonElement>("/api/v1/contacts", JsonOptions);
        contacts.GetArrayLength().Should().BeGreaterThanOrEqualTo(4);

        var createdContact = await _client.PostAsJsonAsync("/api/v1/contacts", new
        {
            firstName = "Taylor",
            lastName = "Reed",
            email = "taylor.reed@example.com",
            phone = "+13195550999",
            role = "Homeowner",
            preferredChannel = "Sms",
            language = "en"
        });
        createdContact.StatusCode.Should().Be(HttpStatusCode.Created);
        var contact = await createdContact.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
        var contactId = contact.GetProperty("id").GetGuid();

        var props = await _client.GetFromJsonAsync<JsonElement>("/api/v1/properties", JsonOptions);
        props.GetArrayLength().Should().BeGreaterThanOrEqualTo(3);

        var jobs = await _client.GetFromJsonAsync<JsonElement>("/api/v1/jobs", JsonOptions);
        jobs.GetArrayLength().Should().BeGreaterThanOrEqualTo(3);
        var jobId = jobs[0].GetProperty("id").GetGuid();

        var note = await _client.PostAsJsonAsync("/api/v1/notes", new { jobId, body = "Seeded CRUD note" });
        note.StatusCode.Should().Be(HttpStatusCode.Created);

        var task = await _client.PostAsJsonAsync("/api/v1/tasks", new
        {
            jobId,
            title = "Call supplier",
            description = "Confirm drip edge",
            status = "Open"
        });
        task.StatusCode.Should().Be(HttpStatusCode.Created);

        var milestone = await _client.PostAsJsonAsync("/api/v1/milestones", new
        {
            jobId,
            name = "Punch list",
            status = "Pending",
            sortOrder = 99
        });
        milestone.StatusCode.Should().Be(HttpStatusCode.Created);

        var consent = await _client.PostAsJsonAsync("/api/v1/consents", new
        {
            contactId,
            jobId,
            smsOptIn = true,
            emailOptIn = true,
            smsStopped = false,
            source = "test"
        });
        consent.StatusCode.Should().Be(HttpStatusCode.Created);

        var evidence = await _client.GetFromJsonAsync<JsonElement>("/api/v1/evidence?jobId=" + jobId, JsonOptions);
        evidence.GetArrayLength().Should().BeGreaterThan(0);
        var photoId = evidence[0].GetProperty("id").GetGuid();
        var softDelete = await _client.DeleteAsync($"/api/v1/evidence/{photoId}");
        softDelete.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var connections = await _client.GetFromJsonAsync<JsonElement>("/api/v1/integrations/connections", JsonOptions);
        connections.GetArrayLength().Should().BeGreaterThanOrEqualTo(2);
        var disconnect = await _client.PostAsync("/api/v1/integrations/connections/Twilio/disconnect", null);
        disconnect.StatusCode.Should().Be(HttpStatusCode.OK);
        var connect = await _client.PostAsync("/api/v1/integrations/connections/Twilio/connect", null);
        connect.StatusCode.Should().Be(HttpStatusCode.OK);
        var twilio = await connect.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
        twilio.GetProperty("isConnected").GetBoolean().Should().BeTrue();

        var org = await _client.GetFromJsonAsync<JsonElement>("/api/v1/organizations/current", JsonOptions);
        org.GetProperty("name").GetString().Should().Contain("Rutters");

        var members = await _client.GetFromJsonAsync<JsonElement>("/api/v1/organizations/current/members", JsonOptions);
        members.GetArrayLength().Should().BeGreaterThanOrEqualTo(3);

        var portal = await _client.GetFromJsonAsync<JsonElement>("/api/v1/portal/keller-maple-demo", JsonOptions);
        portal.GetProperty("milestones").GetArrayLength().Should().BeGreaterThan(0);
    }
}
