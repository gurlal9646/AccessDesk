namespace AccessDesk.Api.Services.Assistant;

public interface IPolicyStore
{
    IReadOnlyList<PolicyChunk> Chunks { get; }
}

/// <summary>
/// Loads the markdown files in the Policies folder and splits them into one chunk per "## heading".
/// The documents are small and static, so they are read once.
/// </summary>
public class PolicyStore : IPolicyStore
{
    private readonly Lazy<IReadOnlyList<PolicyChunk>> _chunks;

    public PolicyStore() : this(Path.Combine(AppContext.BaseDirectory, "Policies"))
    {
    }

    public PolicyStore(string folder)
    {
        _chunks = new Lazy<IReadOnlyList<PolicyChunk>>(() => Load(folder));
    }

    public IReadOnlyList<PolicyChunk> Chunks => _chunks.Value;

    public static IReadOnlyList<PolicyChunk> Split(string source, string markdown)
    {
        var chunks = new List<PolicyChunk>();
        string? heading = null;
        var body = new List<string>();

        void Flush()
        {
            var text = string.Join('\n', body).Trim();
            if (heading is not null && text.Length > 0)
            {
                chunks.Add(new PolicyChunk(source, heading, text));
            }
            body.Clear();
        }

        foreach (var line in markdown.Split('\n'))
        {
            var trimmed = line.TrimEnd('\r');
            if (trimmed.StartsWith("## ", StringComparison.Ordinal))
            {
                Flush();
                heading = trimmed[3..].Trim();
            }
            else if (heading is not null)
            {
                body.Add(trimmed);
            }
        }
        Flush();
        return chunks;
    }

    private static IReadOnlyList<PolicyChunk> Load(string folder)
    {
        if (!Directory.Exists(folder))
        {
            return [];
        }

        return Directory.EnumerateFiles(folder, "*.md")
            .Order(StringComparer.OrdinalIgnoreCase)
            .SelectMany(path => Split(Path.GetFileNameWithoutExtension(path), File.ReadAllText(path)))
            .ToList();
    }
}
