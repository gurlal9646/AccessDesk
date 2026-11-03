using System.Net;
using System.Net.Http.Json;
using AccessDesk.Api.Contracts;
using AccessDesk.Api.Services.Assistant;
using AccessDesk.Tests.Support;

namespace AccessDesk.Tests;

public class AssistantApiTests : IClassFixture<AssistantApiFactory>
{
    private readonly AssistantApiFactory _factory;

    public AssistantApiTests(AssistantApiFactory factory)
    {
        _factory = factory;
        _factory.Claude.Failure = null;
        _factory.Claude.Calls.Clear();
    }

    [Fact]
    public async Task Anonymous_Gets401()
    {
        var response = await _factory.CreateClient().PostAsJsonAsync("/api/assistant/ask", new AskPolicyDto("Who can approve?"));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Employee_GetsAnAnswerWithSources()
    {
        _factory.Claude.Reply = "Managers and admins can approve [1].";
        var client = _factory.CreateClientFor("alice-10", "alice", "employee");

        var response = await client.PostAsJsonAsync("/api/assistant/ask", new AskPolicyDto("Who can approve an access request?"));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<AskPolicyResponseDto>();
        Assert.NotNull(body);
        Assert.Equal("Managers and admins can approve [1].", body.Answer);
        Assert.Equal("Who can approve access requests", body.Sources[0].Section);
    }

    [Fact]
    public async Task TooShortQuestion_Returns400_AndDoesNotCallTheModel()
    {
        var client = _factory.CreateClientFor("alice-11", "alice", "employee");

        var response = await client.PostAsJsonAsync("/api/assistant/ask", new AskPolicyDto("hi"));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Empty(_factory.Claude.Calls);
    }

    [Fact]
    public async Task OverlongQuestion_Returns400()
    {
        var client = _factory.CreateClientFor("alice-12", "alice", "employee");

        var response = await client.PostAsJsonAsync("/api/assistant/ask", new AskPolicyDto(new string('a', 501)));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task WhenTheModelIsUnavailable_Returns503WithAMessage()
    {
        _factory.Claude.Failure = new AssistantUnavailableException("The assistant is not configured.");
        var client = _factory.CreateClientFor("alice-13", "alice", "employee");

        var response = await client.PostAsJsonAsync("/api/assistant/ask", new AskPolicyDto("Who can approve an access request?"));

        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
        Assert.Contains("not configured", await response.Content.ReadAsStringAsync());
    }
}
