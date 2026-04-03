namespace SbomQualityGate.UnitTests.Helpers;

public class FakeHttpClientFactory(HttpClient client) : IHttpClientFactory
{
    public HttpClient CreateClient(string name) => client;
}
