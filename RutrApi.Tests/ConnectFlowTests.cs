using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;

namespace RutrApi.Tests;

[Collection("SerialApi")]
public class ConnectFlowTests : IClassFixture<RutrApiFactory>
{
    private readonly HttpClient _client;
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    public ConnectFlowTests(RutrApiFactory factory)
    {
        _client = factory.CreateClient();
        _client.DefaultRequestHeaders.Remove("X-Demo-Auth");
        _client.DefaultRequestHeaders.Add("X-Demo-Auth", "pm");
    }

    [Fact]
    public async Task Sync_creates_jobs_photos_drafts_then_approve_send()
    {
        var status = await _client.GetFromJsonAsync<JsonElement>("/api/v1/integrations/status", JsonOptions);
        status.GetProperty("useMocks").GetBoolean().Should().BeTrue();

        var sync = await _client.PostAsync("/api/v1/integrations/sync", null);
        sync.StatusCode.Should().Be(HttpStatusCode.OK);
        var syncBody = await sync.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
        syncBody.GetProperty("jobsUpserted").GetInt32().Should().BeGreaterThanOrEqualTo(4);
        syncBody.GetProperty("photosAdded").GetInt32().Should().BeGreaterThan(0);
        syncBody.GetProperty("draftsCreated").GetInt32().Should().BeGreaterThan(0);

        var jobs = await _client.GetFromJsonAsync<JsonElement>("/api/v1/jobs", JsonOptions);
        jobs.GetArrayLength().Should().BeGreaterThanOrEqualTo(4);

        var approvals = await _client.GetFromJsonAsync<JsonElement>("/api/v1/approvals", JsonOptions);
        approvals.GetArrayLength().Should().BeGreaterThan(0);
        var draftId = approvals[0].GetProperty("draftId").GetGuid();
        var jobId = approvals[0].GetProperty("jobId").GetGuid();

        var approve = await _client.PostAsJsonAsync($"/api/v1/approvals/{draftId}/approve", new { });
        approve.StatusCode.Should().Be(HttpStatusCode.OK);
        var approved = await approve.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
        approved.GetProperty("status").GetString().Should().Be("Approved");

        // Approved drafts stay on the queue so the UI can Approve → Send.
        var queueAfterApprove = await _client.GetFromJsonAsync<JsonElement>("/api/v1/approvals", JsonOptions);
        var stillQueued = queueAfterApprove.EnumerateArray()
            .First(x => x.GetProperty("draftId").GetGuid() == draftId);
        stillQueued.GetProperty("status").GetString().Should().Be("Approved");

        var send = await _client.PostAsync($"/api/v1/approvals/{draftId}/send", null);
        send.StatusCode.Should().Be(HttpStatusCode.OK);
        var sent = await send.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
        sent.GetProperty("status").GetString().Should().Be("Sent");

        var audit = await _client.GetFromJsonAsync<JsonElement>($"/api/v1/jobs/{jobId}/audit", JsonOptions);
        audit.GetArrayLength().Should().BeGreaterThan(0);

        var detail = await _client.GetFromJsonAsync<JsonElement>($"/api/v1/jobs/{jobId}", JsonOptions);
        var token = detail.GetProperty("portalToken").GetString();
        token.Should().NotBeNullOrWhiteSpace();

        var portal = await _client.GetAsync($"/api/v1/portal/{token}");
        portal.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Reject_removes_draft_from_queue()
    {
        await _client.PostAsync("/api/v1/integrations/sync", null);
        var approvals = await _client.GetFromJsonAsync<JsonElement>("/api/v1/approvals", JsonOptions);
        approvals.GetArrayLength().Should().BeGreaterThan(0);
        var draftId = approvals[0].GetProperty("draftId").GetGuid();

        var reject = await _client.PostAsJsonAsync(
            $"/api/v1/approvals/{draftId}/reject",
            new { reason = "Evidence incomplete" });
        reject.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await reject.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
        body.GetProperty("status").GetString().Should().Be("Rejected");

        var queue = await _client.GetFromJsonAsync<JsonElement>("/api/v1/approvals", JsonOptions);
        queue.EnumerateArray().Any(x => x.GetProperty("draftId").GetGuid() == draftId).Should().BeFalse();
    }
}

[CollectionDefinition("SerialApi", DisableParallelization = true)]
public class SerialApiCollection;
