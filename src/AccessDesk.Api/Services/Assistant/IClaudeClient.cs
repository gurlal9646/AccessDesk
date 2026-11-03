namespace AccessDesk.Api.Services.Assistant;

public interface IClaudeClient
{
    Task<string> CompleteAsync(string systemPrompt, string userMessage, CancellationToken ct);
}

/// <summary>The model could not be reached or is not configured. The message is safe to show to users.</summary>
public class AssistantUnavailableException(string message, Exception? inner = null) : Exception(message, inner);
