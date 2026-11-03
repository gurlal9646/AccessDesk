namespace AccessDesk.Api.Services.Assistant;

/// <summary>One section of a policy document: the text under a single "## heading".</summary>
public record PolicyChunk(string Source, string Heading, string Text);
