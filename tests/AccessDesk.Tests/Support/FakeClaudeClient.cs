using AccessDesk.Api.Services.Assistant;

namespace AccessDesk.Tests.Support;

/// <summary>Stands in for the Claude API so tests and CI need no API key and make no network calls.</summary>
public class FakeClaudeClient : IClaudeClient
{
    public string Reply { get; set; } = "Managers and admins can approve requests [1].";
    public Exception? Failure { get; set; }
    public List<(string System, string User)> Calls { get; } = [];

    public Task<string> CompleteAsync(string systemPrompt, string userMessage, CancellationToken ct)
    {
        Calls.Add((systemPrompt, userMessage));
        return Failure is null ? Task.FromResult(Reply) : Task.FromException<string>(Failure);
    }
}
