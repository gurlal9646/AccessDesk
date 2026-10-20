using System.Net;
using System.Net.Http.Json;
using AccessDesk.Api.Contracts;
using AccessDesk.Api.Domain;
using AccessDesk.Tests.Support;

namespace AccessDesk.Tests;

public class AccessRequestsApiTests : IClassFixture<ApiFactory>
{
    private readonly ApiFactory _factory;

    public AccessRequestsApiTests(ApiFactory factory) => _factory = factory;

    private static CreateAccessRequestDto NewRequest(string system = "GitHub")
        => new(system, "I need this access to deliver my team's work.");

    [Fact]
    public async Task Anonymous_Gets401()
    {
        var response = await _factory.CreateClient().GetAsync("/api/requests");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Employee_CanCreateAndListOwnRequest_ButNotSeeOthers()
    {
        var alice = _factory.CreateClientFor("alice-1", "alice", "employee");
        var bob = _factory.CreateClientFor("bob-1", "bob", "employee");

        var created = await alice.PostAsJsonAsync("/api/requests", NewRequest("Jira"));
        await bob.PostAsJsonAsync("/api/requests", NewRequest("Confluence"));

        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        var aliceList = await alice.GetFromJsonAsync<List<AccessRequestDto>>("/api/requests");
        Assert.NotNull(aliceList);
        Assert.All(aliceList, r => Assert.Equal("alice", r.RequesterName));
        Assert.Contains(aliceList, r => r.System == "Jira");
    }

    [Fact]
    public async Task Create_WithTooShortJustification_Returns400()
    {
        var client = _factory.CreateClientFor("alice-2", "alice2", "employee");

        var response = await client.PostAsJsonAsync("/api/requests", new CreateAccessRequestDto("GitHub", "short"));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Employee_CannotApprove_Returns403()
    {
        var alice = _factory.CreateClientFor("alice-3", "alice3", "employee");
        var created = await (await alice.PostAsJsonAsync("/api/requests", NewRequest()))
            .Content.ReadFromJsonAsync<AccessRequestDto>();

        var response = await alice.PostAsJsonAsync($"/api/requests/{created!.Id}/approve", new DecisionDto(null));

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Manager_CanApprove_ThenSecondDecisionConflicts()
    {
        var alice = _factory.CreateClientFor("alice-4", "alice4", "employee");
        var manager = _factory.CreateClientFor("mia-4", "mia4", "manager");
        var created = await (await alice.PostAsJsonAsync("/api/requests", NewRequest()))
            .Content.ReadFromJsonAsync<AccessRequestDto>();

        var approve = await manager.PostAsJsonAsync($"/api/requests/{created!.Id}/approve", new DecisionDto("ok"));
        var again = await manager.PostAsJsonAsync($"/api/requests/{created.Id}/reject", new DecisionDto(null));

        Assert.Equal(HttpStatusCode.OK, approve.StatusCode);
        var approved = await approve.Content.ReadFromJsonAsync<AccessRequestDto>();
        Assert.Equal(AccessRequestStatus.Approved, approved!.Status);
        Assert.Equal("mia4", approved.DecidedBy);
        Assert.Equal(HttpStatusCode.Conflict, again.StatusCode);
    }

    [Fact]
    public async Task Manager_CannotDecideOwnRequest_Returns403()
    {
        var manager = _factory.CreateClientFor("mia-5", "mia5", "manager");
        var created = await (await manager.PostAsJsonAsync("/api/requests", NewRequest()))
            .Content.ReadFromJsonAsync<AccessRequestDto>();

        var response = await manager.PostAsJsonAsync($"/api/requests/{created!.Id}/approve", new DecisionDto(null));

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Approve_UnknownId_Returns404()
    {
        var manager = _factory.CreateClientFor("mia-6", "mia6", "manager");

        var response = await manager.PostAsJsonAsync("/api/requests/987654/approve", new DecisionDto(null));

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Audit_IsAdminOnly()
    {
        var alice = _factory.CreateClientFor("alice-7", "alice7", "employee");
        var manager = _factory.CreateClientFor("mia-7", "mia7", "manager");
        var admin = _factory.CreateClientFor("ann-7", "ann7", "admin");
        await alice.PostAsJsonAsync("/api/requests", NewRequest("Audit target"));

        var asManager = await manager.GetAsync("/api/audit");
        var asAdmin = await admin.GetAsync("/api/audit");

        Assert.Equal(HttpStatusCode.Forbidden, asManager.StatusCode);
        Assert.Equal(HttpStatusCode.OK, asAdmin.StatusCode);
        var entries = await asAdmin.Content.ReadFromJsonAsync<List<AuditEntryDto>>();
        Assert.Contains(entries!, e => e.Action == "request.created" && e.Details.Contains("Audit target"));
    }
}
