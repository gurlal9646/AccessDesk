using AccessDesk.Api.Services.Assistant;

namespace AccessDesk.Tests;

public class PolicyRetrieverTests
{
    // These tests use the real policy documents that ship with the API, so a policy edit that breaks retrieval shows up here.
    private static readonly PolicyRetriever Retriever = new(new PolicyStore());

    [Fact]
    public void Split_MakesOneChunkPerHeading_AndIgnoresTextBeforeTheFirstHeading()
    {
        var chunks = PolicyStore.Split("demo", "# Title\nintro text\n\n## First\nalpha\nbeta\n\n## Second\ngamma\n");

        Assert.Collection(chunks,
            c => { Assert.Equal("First", c.Heading); Assert.Equal("alpha\nbeta", c.Text); Assert.Equal("demo", c.Source); },
            c => { Assert.Equal("Second", c.Heading); Assert.Equal("gamma", c.Text); });
    }

    [Fact]
    public void Store_LoadsTheShippedPolicy()
    {
        var chunks = new PolicyStore().Chunks;

        Assert.True(chunks.Count >= 8);
        Assert.Contains(chunks, c => c.Heading == "Who can approve access requests");
    }

    [Theory]
    [InlineData("Who can approve my access request?", "Who can approve access requests")]
    [InlineData("Can I get direct production database access?", "Production database access")]
    [InlineData("How long does Azure production access last?", "Azure subscriptions")]
    [InlineData("What happens to my access when I leave the company?", "Leaving the company or changing teams")]
    [InlineData("Who is allowed to read the audit log?", "Audit log")]
    public void Retrieve_PutsTheMatchingSectionFirst(string question, string expectedHeading)
    {
        var result = Retriever.Retrieve(question);

        Assert.NotEmpty(result);
        Assert.Equal(expectedHeading, result[0].Heading);
    }

    [Fact]
    public void Retrieve_ReturnsNothingForAQuestionThePolicyDoesNotCover()
    {
        var result = Retriever.Retrieve("What is the weather in Paris today?");

        Assert.Empty(result);
    }

    [Fact]
    public void Retrieve_RespectsTopK()
    {
        var result = Retriever.Retrieve("access request approval manager admin", topK: 2);

        Assert.Equal(2, result.Count);
    }
}
