using System.Text.RegularExpressions;

namespace AccessDesk.Api.Services.Assistant;

/// <summary>
/// Picks the policy sections most relevant to a question. This is plain keyword scoring (TF-IDF with a heading
/// boost), not embeddings: the policy set is small, and keyword scoring is deterministic and easy to test.
/// </summary>
public partial class PolicyRetriever(IPolicyStore store)
{
    private static readonly HashSet<string> StopWords = new(StringComparer.Ordinal)
    {
        "the", "and", "for", "are", "can", "who", "how", "what", "when", "where", "which", "does", "did", "that",
        "this", "with", "from", "need", "have", "has", "not", "you", "your", "get", "any", "into", "about", "should",
        "would", "could", "will", "there", "they", "their", "than", "then", "but", "all", "may", "must"
    };

    public IReadOnlyList<PolicyChunk> Retrieve(string question, int topK = 3)
    {
        var queryTerms = Tokenize(question).Distinct().ToList();
        var chunks = store.Chunks;
        if (queryTerms.Count == 0 || chunks.Count == 0)
        {
            return [];
        }

        var documents = chunks
            .Select(c => (Chunk: c, Body: Tokenize(c.Text).ToList(), Heading: Tokenize(c.Heading).ToList()))
            .ToList();

        var scored = documents
            .Select(d => (d.Chunk, Score: Score(queryTerms, d.Body, d.Heading, documents.Count,
                term => documents.Count(x => x.Body.Contains(term) || x.Heading.Contains(term)))))
            .Where(x => x.Score > 0)
            .OrderByDescending(x => x.Score)
            .Take(topK)
            .Select(x => x.Chunk)
            .ToList();

        return scored;
    }

    private static double Score(IReadOnlyList<string> queryTerms, List<string> body, List<string> heading,
        int documentCount, Func<string, int> documentFrequency)
    {
        var score = 0.0;
        foreach (var term in queryTerms)
        {
            var tf = body.Count(t => t == term) + 3 * heading.Count(t => t == term); // a heading match counts triple
            if (tf == 0)
            {
                continue;
            }
            var idf = Math.Log((documentCount + 1.0) / (documentFrequency(term) + 1.0)) + 1.0;
            score += tf * idf;
        }
        // Long sections should not win just by repeating more words.
        return score / Math.Sqrt(Math.Max(body.Count, 1));
    }

    internal static IEnumerable<string> Tokenize(string text)
    {
        foreach (Match m in WordPattern().Matches(text.ToLowerInvariant()))
        {
            var word = Stem(m.Value);
            if (word.Length >= 3 && !StopWords.Contains(word))
            {
                yield return word;
            }
        }
    }

    // "requests" and "request", "approves" and "approve" should match; this only trims a trailing plural "s".
    private static string Stem(string word)
    {
        if (word.Length > 4 && word.EndsWith("ies", StringComparison.Ordinal)) return word[..^3] + "y";
        if (word.Length > 3 && word.EndsWith('s') && !word.EndsWith("ss", StringComparison.Ordinal)) return word[..^1];
        return word;
    }

    [GeneratedRegex("[a-z0-9]+")]
    private static partial Regex WordPattern();
}
