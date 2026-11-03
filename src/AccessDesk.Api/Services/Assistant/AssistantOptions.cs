namespace AccessDesk.Api.Services.Assistant;

public class AssistantOptions
{
    /// <summary>Anthropic API key. Set it as the environment variable Assistant__ApiKey; never commit it.</summary>
    public string? ApiKey { get; set; }

    public string Model { get; set; } = "claude-sonnet-5-5";

    public int MaxTokens { get; set; } = 600;

    /// <summary>How many policy sections are sent to the model with each question.</summary>
    public int TopSections { get; set; } = 3;
}
