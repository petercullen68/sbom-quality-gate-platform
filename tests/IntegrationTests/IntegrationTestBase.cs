using System.Net.Http.Headers;
using SbomQualityGate.IntegrationTests.Infrastructure;

namespace SbomQualityGate.IntegrationTests;
 
public abstract class IntegrationTestBase : IAsyncLifetime
{
    protected SbomQualityGateApiFactory Factory { get; } = new();
    protected HttpClient Client { get; private set; } = null!;
 
    public virtual async Task InitializeAsync()
    {
        Client = Factory.CreateClient();
        Client.DefaultRequestHeaders.Accept.Add(
            new MediaTypeWithQualityHeaderValue("application/json"));
 
        await Factory.InitialiseDatabaseAsync();
        await Factory.ResetDatabaseAsync();
    }
 
    public async Task DisposeAsync()
    {
        await Factory.DisposeAsync();
    }
}
