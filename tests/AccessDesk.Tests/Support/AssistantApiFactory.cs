using AccessDesk.Api.Services.Assistant;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;

namespace AccessDesk.Tests.Support;

/// <summary>The API with the Claude client replaced by a fake that the test controls.</summary>
public class AssistantApiFactory : ApiFactory
{
    public FakeClaudeClient Claude { get; } = new();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        base.ConfigureWebHost(builder);
        builder.ConfigureServices(services => services.AddSingleton<IClaudeClient>(Claude));
    }
}
