using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Options;

namespace AccessDesk.Api.Services.Assistant;

/// <summary>Calls the Anthropic Messages API directly over HTTP. One request in, one text answer out.</summary>
public class AnthropicClient(HttpClient http, IOptions<AssistantOptions> options, ILogger<AnthropicClient> logger) : IClaudeClient
{
    private const string AnthropicVersion = "2023-06-01";

    public async Task<string> CompleteAsync(string systemPrompt, string userMessage, CancellationToken ct)
    {
        var settings = options.Value;
        if (string.IsNullOrWhiteSpace(settings.ApiKey))
        {
            throw new AssistantUnavailableException("The assistant is not configured.");
        }

        using var request = new HttpRequestMessage(HttpMethod.Post, "v1/messages")
        {
            Content = JsonContent.Create(new MessagesRequest(
                settings.Model,
                settings.MaxTokens,
                systemPrompt,
                [new Message("user", userMessage)]))
        };
        request.Headers.Add("x-api-key", settings.ApiKey);
        request.Headers.Add("anthropic-version", AnthropicVersion);

        HttpResponseMessage response;
        try
        {
            response = await http.SendAsync(request, ct);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException && !ct.IsCancellationRequested)
        {
            logger.LogWarning(ex, "Anthropic request failed to send");
            throw new AssistantUnavailableException("The assistant could not be reached. Try again in a moment.", ex);
        }

        using (response)
        {
            if (!response.IsSuccessStatusCode)
            {
                // Log the status only: the response body can echo the prompt, which should not end up in logs.
                logger.LogWarning("Anthropic returned {StatusCode}", (int)response.StatusCode);
                throw new AssistantUnavailableException("The assistant could not answer right now. Try again in a moment.");
            }

            var body = await response.Content.ReadFromJsonAsync<MessagesResponse>(ct);
            var text = string.Concat(body?.Content?.Where(c => c.Type == "text").Select(c => c.Text) ?? []);
            if (string.IsNullOrWhiteSpace(text))
            {
                throw new AssistantUnavailableException("The assistant returned an empty answer. Try rephrasing the question.");
            }
            return text.Trim();
        }
    }

    private record Message(string Role, string Content);

    private record MessagesRequest(
        string Model,
        [property: JsonPropertyName("max_tokens")] int MaxTokens,
        string System,
        IReadOnlyList<Message> Messages);

    private record ContentBlock(string Type, string? Text);

    private record MessagesResponse(IReadOnlyList<ContentBlock>? Content);
}
