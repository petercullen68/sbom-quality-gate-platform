using System.Net.Http.Headers;
using SbomQualityGate.IntegrationTests.Infrastructure;

namespace SbomQualityGate.IntegrationTests;

public abstract class IntegrationTestBase(SbomQualityGateApiFactory factory) : IAsyncLifetime
{
    protected SbomQualityGateApiFactory Factory { get; } = factory;
    protected HttpClient Client { get; private set; } = null!;

    public virtual async Task InitializeAsync()
    {
        Client = Factory.CreateClient();
        Client.DefaultRequestHeaders.Accept.Add(
            new MediaTypeWithQualityHeaderValue("application/json"));

        await Factory.ResetDatabaseAsync();
    }

    public Task DisposeAsync() => Task.CompletedTask;
}
